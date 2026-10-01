using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LocalizationSystem.Editor
{
    /// <summary>
    /// 本地化编辑器窗口
    /// </summary>
    public class LocalizationEditorWindow : EditorWindow
    {
        private LocalizationData currentData;//当前编辑的本地化数据
        private Vector2 leftPanelScroll;//左侧面板滚动位置
        private Vector2 rightPanelScroll;//右侧面板滚动位置
        private int selectedEntryIndex = -1;//当前选中的条目索引
        private string searchFilter = "";//搜索过滤
        private LanguageType previewLanguage = LanguageType.ChineseSimplified;//预览语言

        //显示设置
        private bool[] showLanguageFields;//控制各语言字段的展开状态
        private bool showAllLanguages = false;//是否显示所有语言

        //常用语言（默认显示）
        private readonly LanguageType[] commonLanguages = new LanguageType[]
        {
            LanguageType.ChineseSimplified,
            LanguageType.ChineseTraditional,
            LanguageType.English,
            LanguageType.Japanese,
            LanguageType.Korean
        };

        private const string EDITOR_PREFS_KEY = "LocalizationEditor_LastFile";
        private const string LEFT_WIDTH_KEY = "LocalizationEditor_LeftWidth";

        /// <summary>
        /// 左栏宽度。以前是写死的 350，配置项一长就被截断，而且没法调。
        /// 现在可以拖中间的分隔条改，并记进 EditorPrefs 下次打开还是这个宽度。
        /// </summary>
        private float leftWidth = 420f;
        private const float MinLeftWidth = 260f;
        private const float MaxLeftWidth = 900f;
        private bool draggingSplitter = false;

        /// <summary>
        /// 静默模式：跳过"确认清空"这类模态对话框。
        ///
        /// ══════════════ 为什么需要它 ══════════════
        /// EditorUtility.DisplayDialog 会阻塞 Unity 主线程直到有人点掉。
        /// 用脚本/自动化驱动这个窗口时没人去点，主线程就被永久卡住，连编辑器桥接都会失去响应。
        /// 与 PrefabTextCollectorWindow.SilentMode 是同一个理由、同一个约定。
        ///
        /// 默认 false：正常手工使用仍然要看确认框（清空是不可撤销的，必须拦一道）。
        /// </summary>
        public static bool SilentMode = false;

        /// <summary>统一的确认出口：静默模式下直接当作"用户点了确认"。</summary>
        private static bool Confirm(string title, string message, string ok)
        {
            if (SilentMode) return true;
            return EditorUtility.DisplayDialog(title, message, ok, "取消");
        }
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;

        [MenuItem("Tools/本地化编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationEditorWindow>("本地化编辑器");
            window.minSize = new Vector2(1200, 700);
            window.Show();
        }

        private void OnEnable()
        {
            string lastFile = EditorPrefs.GetString(EDITOR_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
            {
                currentData = AssetDatabase.LoadAssetAtPath<LocalizationData>(lastFile);
            }

            showLanguageFields = new bool[System.Enum.GetValues(typeof(LanguageType)).Length];
            for (int i = 0; i < showLanguageFields.Length; i++)
            {
                showLanguageFields[i] = true;
            }

            //上次拖到的宽度：没存过就按窗口宽度的三分之一给一个比原来 350 宽松的默认值
            leftWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(LEFT_WIDTH_KEY, 420f), MinLeftWidth, MaxLeftWidth);
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 16;
                headerStyle.margin = new RectOffset(10, 10, 10, 10);
            }
            if (subHeaderStyle == null)
            {
                subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                subHeaderStyle.fontSize = 13;
                subHeaderStyle.margin = new RectOffset(5, 5, 5, 5);
            }
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(10, 10, 10, 10);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // ══════════════ 窗口变窄时按比例收一收左栏 ══════════════
            // 不夹的话：用户把左栏拖到 900、再把窗口缩窄，分隔条就被推出可视区，
            // 于是再也拖不回来（只能去删 EditorPrefs）。这里保证左栏最多占八成宽，
            // 右栏与分隔条永远留得住。只影响这一次显示，不写 EditorPrefs ——
            // 窗口拉回去之后用户原来设的宽度会自己回来。
            float displayWidth = Mathf.Min(leftWidth, Mathf.Max(MinLeftWidth, position.width * 0.8f));

            EditorGUILayout.BeginHorizontal();

            //左侧面板（宽度可拖）
            EditorGUILayout.BeginVertical(GUILayout.Width(displayWidth));
            DrawLeftPanel(displayWidth);
            EditorGUILayout.EndVertical();

            DrawSplitter();

            //右侧面板
            EditorGUILayout.BeginVertical();
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 左栏与右栏之间那条可拖动的分隔条。
        ///
        /// ══════════════ 为什么要手写 ══════════════
        /// EditorGUILayout 没有内置分隔条。这里用 GUILayoutUtility.GetRect 占位 +
        /// EditorGUIUtility.AddCursorRect 的办法：只占一条 6px 的竖条，
        /// 拖动时按 delta.x 改左栏宽度。
        ///
        /// ══════════════ 两个容易踩的点 ══════════════
        /// 1) 鼠标形状必须每帧重新注册（AddCursorRect 只在当前事件里有效）；
        /// 2) 宽度必须夹在上下限之间 —— 拖到 0 之后分隔条本身也一起没了，
        ///    用户就再也拖不回来，只能删 EditorPrefs。
        /// </summary>
        /// <summary>
        /// 按一次拖动的位移量改左栏宽度并落盘。
        /// 从 DrawSplitter 里抽出来是为了它能被单独调用（GUI 事件在编辑器脚本里造不出来，
        /// 不抽出来这段夹取逻辑就只能靠肉眼验证）。
        /// </summary>
        private void ApplySplitterDrag(float deltaX)
        {
            leftWidth = Mathf.Clamp(leftWidth + deltaX, MinLeftWidth, MaxLeftWidth);
            EditorPrefs.SetFloat(LEFT_WIDTH_KEY, leftWidth);
            Repaint();
        }

        private void DrawSplitter()
        {
            Rect r = GUILayoutUtility.GetRect(6f, 6f, GUILayout.Width(6f), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                draggingSplitter = true;
                Event.current.Use();
            }
            if (draggingSplitter)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    ApplySplitterDrag(Event.current.delta.x);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    draggingSplitter = false;
                    EditorPrefs.SetFloat(LEFT_WIDTH_KEY, leftWidth);
                    Event.current.Use();
                }
            }

            //拖动时高亮：不然"这里能拖"只能靠猜
            Color old = GUI.color;
            GUI.color = draggingSplitter ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            GUI.Box(r, "");
            GUI.color = old;
        }

        /// <summary>
        /// 绘制左侧面板
        /// </summary>
        private void DrawLeftPanel(float columnWidth)
        {
            //文件操作区域
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("本地化文件", headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(currentData, typeof(LocalizationData), false);
            if (GUILayout.Button("新建", GUILayout.Width(50)))
            {
                CreateNewData();
            }
            if (GUILayout.Button("打开", GUILayout.Width(50)))
            {
                OpenData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(currentData == null))
            {
                if (GUILayout.Button("保存", GUILayout.Height(25)))
                {
                    SaveData();
                }
                //清空是不可撤销的破坏性操作（可能删掉已经翻好的所有语言），所以用红底 + 二次确认
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.72f, 0.72f);
                if (GUILayout.Button("清空全部条目", GUILayout.Height(25), GUILayout.Width(120)))
                {
                    ClearAllEntries();
                }
                GUI.backgroundColor = oldBg;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (currentData == null) return;

            leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);

            //统计信息
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("统计信息", subHeaderStyle);
            EditorGUILayout.LabelField($"总条目数: {currentData.entries.Count}");
            EditorGUILayout.LabelField($"完整翻译: {GetCompleteTranslationCount()}");
            EditorGUILayout.LabelField($"缺失翻译: {GetMissingTranslationCount()}");
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            //工具栏
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 添加条目", GUILayout.Height(30)))
            {
                AddNewEntry();
            }
            if (GUILayout.Button("导出CSV", GUILayout.Height(30), GUILayout.Width(80)))
            {
                ExportToCSV();
            }
            if (GUILayout.Button("导入CSV", GUILayout.Height(30), GUILayout.Width(80)))
            {
                ImportFromCSV();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            //搜索过滤
            searchFilter = EditorGUILayout.TextField("搜索:", searchFilter);

            //预览语言选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预览语言:", GUILayout.Width(70));
            previewLanguage = (LanguageType)EditorGUILayout.EnumPopup(previewLanguage);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            //显示条目列表
            for (int i = 0; i < currentData.entries.Count; i++)
            {
                var entry = currentData.entries[i];
                string previewText = entry.GetText(previewLanguage);
                if (string.IsNullOrEmpty(previewText))
                {
                    previewText = entry.english;//后备到英语
                }
                if (string.IsNullOrEmpty(previewText))
                {
                    previewText = entry.chineseSimplified;//后备到中文
                }
                if (string.IsNullOrEmpty(previewText))
                {
                    previewText = "[空]";
                }

                // ══════════════ 两行都要截断 ══════════════
                // 以前只截译文，Key 不截 —— 而本项目的 Key 动辄五六十个字符
                // （ui_setting_contentarea_page_graphics_viewport_content_row_windowmode_options_option_0），
                // 于是按钮被撑得很宽、或者译文那一行被挤到看不见。
                // 截断长度按当前左栏宽度算：栏宽了就能多看到一些，不用改代码。
                int maxChars = Mathf.Max(12, (int)(columnWidth / 7.2f));
                string shownKey = entry.key;
                if (shownKey.Length > maxChars) shownKey = shownKey.Substring(0, maxChars) + "...";
                if (previewText.Length > maxChars) previewText = previewText.Substring(0, maxChars) + "...";

                //搜索过滤
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!entry.key.Contains(searchFilter) && !previewText.Contains(searchFilter))
                        continue;
                }

                EditorGUILayout.BeginHorizontal();

                //选中高亮
                GUI.backgroundColor = (i == selectedEntryIndex) ? new Color(0.5f, 0.8f, 1f) : Color.white;

                //显示条目按钮
                string buttonText = $"{shownKey}\n{previewText}";
                if (GUILayout.Button(buttonText, GUILayout.Height(45)))
                {
                    selectedEntryIndex = i;
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = Color.white;

                //删除按钮
                if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(45)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除条目 '{entry.key}' 吗？", "删除", "取消"))
                    {
                        currentData.entries.RemoveAt(i);
                        if (selectedEntryIndex == i)
                            selectedEntryIndex = -1;
                        else if (selectedEntryIndex > i)
                            selectedEntryIndex--;
                        EditorUtility.SetDirty(currentData);
                    }
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制右侧面板
        /// </summary>
        private void DrawRightPanel()
        {
            if (currentData == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请创建或打开一个本地化配置文件", headerStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

            //基本信息
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("配置信息", headerStyle);
            currentData.configName = EditorGUILayout.TextField("配置名称:", currentData.configName);
            EditorGUILayout.LabelField("描述:");
            currentData.description = EditorGUILayout.TextArea(currentData.description, GUILayout.Height(50));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            //条目编辑
            if (selectedEntryIndex >= 0 && selectedEntryIndex < currentData.entries.Count)
            {
                DrawEntryEditor(currentData.entries[selectedEntryIndex]);
            }
            else
            {
                GUILayout.Label("请在左侧选择一个条目进行编辑", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制条目编辑器
        /// </summary>
        private void DrawEntryEditor(LocalizationEntry entry)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            GUILayout.Label("条目编辑", headerStyle);

            //Key
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key:", GUILayout.Width(80));
            string newKey = EditorGUILayout.TextField(entry.key);
            if (newKey != entry.key)
            {
                //检查Key是否重复
                if (currentData.ContainsKey(newKey))
                {
                    EditorUtility.DisplayDialog("错误", $"Key '{newKey}' 已存在！", "确定");
                }
                else
                {
                    entry.key = newKey;
                }
            }
            EditorGUILayout.EndHorizontal();

            //描述
            EditorGUILayout.LabelField("描述/备注:");
            entry.description = EditorGUILayout.TextArea(entry.description, GUILayout.Height(60));

            GUILayout.Space(10);

            //语言显示设置
            EditorGUILayout.BeginHorizontal();
            showAllLanguages = EditorGUILayout.Toggle("显示所有语言:", showAllLanguages);

            GUILayout.FlexibleSpace();
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.85f, 0.7f);
            if (GUILayout.Button("清空该条译文", GUILayout.Width(110)))
            {
                ClearEntryText(entry);
            }
            GUI.backgroundColor = oldBg;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            //语言字段
            var languages = System.Enum.GetValues(typeof(LanguageType));
            for (int i = 0; i < languages.Length; i++)
            {
                LanguageType lang = (LanguageType)languages.GetValue(i);

                //判断是否显示该语言
                bool isCommon = commonLanguages.Contains(lang);
                if (!showAllLanguages && !isCommon)
                    continue;

                DrawLanguageField(entry, lang);
            }

            EditorGUILayout.EndVertical();

            //标记为已修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(currentData);
            }
        }

        /// <summary>
        /// 绘制单个语言字段
        /// </summary>
        private void DrawLanguageField(LocalizationEntry entry, LanguageType language)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginHorizontal();
            string langName = LocalizationManager.GetLanguageDisplayName(language);
            EditorGUILayout.LabelField(langName, subHeaderStyle, GUILayout.Width(120));

            //检查是否有文本
            string text = entry.GetText(language);
            bool hasText = !string.IsNullOrEmpty(text);

            //显示状态指示
            GUI.color = hasText ? Color.green : Color.red;
            GUILayout.Label(hasText ? "?" : "?", GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            //文本输入
            string newText = EditorGUILayout.TextArea(text, GUILayout.MinHeight(40));
            if (newText != text)
            {
                entry.SetText(language, newText);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        /// <summary>
        /// 清空某一个条目的全部译文（Key 与描述保留）。
        ///
        /// ══════════════ 为什么保留 Key ══════════════
        /// Key 是界面与配置之间的合同：清掉它，已经被 LocalizedText 引用的控件就会查不到条目，
        /// 而查不到时 LocalizationManager 会把 Key 本身当文本显示出来，界面会变成一串标识符。
        /// 所以这里只清译文，给"重新翻一遍这条"用。
        /// </summary>
        private void ClearEntryText(LocalizationEntry entry)
        {
            if (entry == null) return;
            if (!Confirm("确认清空",
                $"清空条目 '{entry.key}' 的全部译文？\n（Key 与描述会保留）", "清空")) return;

            entry.chineseSimplified = "";
            entry.chineseTraditional = "";
            entry.english = "";
            entry.japanese = "";
            entry.korean = "";
            EditorUtility.SetDirty(currentData);
        }

        /// <summary>
        /// 清空配置里的【全部条目】。
        ///
        /// ══════════════ 为什么必须二次确认 ══════════════
        /// 这一步会删掉所有已翻好的译文，而且 Unity 的撤销栈对这种"批量改 ScriptableObject 列表"
        /// 不生效（Ctrl+Z 找不回来）。返回按钮拿不到撤销，就只能靠事先确认。
        /// </summary>
        private void ClearAllEntries()
        {
            if (currentData == null) return;
            int n = currentData.entries.Count;
            if (n == 0)
            {
                if (!SilentMode) EditorUtility.DisplayDialog("清空全部", "当前没有任何条目。", "确定");
                return;
            }

            if (!Confirm("确认清空全部",
                $"将删除【全部 {n} 条】条目，包括所有语言的译文。\n\n此操作不可撤销，确定继续？",
                "清空全部")) return;

            currentData.entries.Clear();
            selectedEntryIndex = -1;
            EditorUtility.SetDirty(currentData);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 获取完整翻译的条目数
        /// </summary>
        private int GetCompleteTranslationCount()
        {
            int count = 0;
            foreach (var entry in currentData.entries)
            {
                bool isComplete = !string.IsNullOrEmpty(entry.chineseSimplified) &&
                                 !string.IsNullOrEmpty(entry.chineseTraditional) &&
                                 !string.IsNullOrEmpty(entry.english) &&
                                 !string.IsNullOrEmpty(entry.japanese) &&
                                 !string.IsNullOrEmpty(entry.korean);
                if (isComplete) count++;
            }
            return count;
        }

        /// <summary>
        /// 获取缺失翻译的条目数
        /// </summary>
        private int GetMissingTranslationCount()
        {
            return currentData.entries.Count - GetCompleteTranslationCount();
        }

        /// <summary>
        /// 创建新数据
        /// </summary>
        private void CreateNewData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建本地化配置",
                "LocalizationConfig",
                "asset",
                "选择保存位置",
                "Assets/Data/Localization");

            if (!string.IsNullOrEmpty(path))
            {
                //确保目录存在
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = CreateInstance<LocalizationData>();
                data.configName = Path.GetFileNameWithoutExtension(path);

                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentData = data;
                EditorPrefs.SetString(EDITOR_PREFS_KEY, path);
                selectedEntryIndex = -1;
            }
        }

        /// <summary>
        /// 打开数据
        /// </summary>
        private void OpenData()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "打开本地化配置",
                "Assets/Data/Localization",
                new string[] { "本地化配置", "asset" });

            if (!string.IsNullOrEmpty(path))
            {
                //转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                currentData = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                if (currentData != null)
                {
                    EditorPrefs.SetString(EDITOR_PREFS_KEY, path);
                    selectedEntryIndex = -1;
                }
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        private void SaveData()
        {
            if (currentData != null)
            {
                EditorUtility.SetDirty(currentData);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("保存成功", "本地化配置已保存！", "确定");
            }
        }

        /// <summary>
        /// 添加新条目
        /// </summary>
        private void AddNewEntry()
        {
            if (currentData.entries == null)
                currentData.entries = new List<LocalizationEntry>();

            //生成唯一Key
            string newKey = $"NEW_KEY_{currentData.entries.Count + 1}";
            int counter = 1;
            while (currentData.ContainsKey(newKey))
            {
                newKey = $"NEW_KEY_{currentData.entries.Count + counter}";
                counter++;
            }

            var newEntry = new LocalizationEntry()
            {
                key = newKey,
                description = ""
            };

            currentData.entries.Add(newEntry);
            selectedEntryIndex = currentData.entries.Count - 1;
            EditorUtility.SetDirty(currentData);
        }

        /// <summary>
        /// 导出到CSV
        /// </summary>
        private void ExportToCSV()
        {
            string path = EditorUtility.SaveFilePanel(
                "导出CSV",
                @"D:\Unity Projects\Project_Chaos\Assets\Excel\Chaos_localization_excel\",
                $"{currentData.name}_Export.csv",
                "csv");

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    //写入表头
                    writer.WriteLine("Key,Description,ChineseSimplified,ChineseTraditional,English,Japanese,Korean");
                        //+
                        //"French,German,Spanish,Russian,Portuguese,Italian,Arabic,Thai,Vietnamese,Turkish,Polish,Dutch,Indonesian");

                    //写入数据
                    foreach (var entry in currentData.entries)
                    {
                        writer.WriteLine($"\"{EscapeCSV(entry.key)}\"," +
                            $"\"{EscapeCSV(entry.description)}\"," +
                            $"\"{EscapeCSV(entry.chineseSimplified)}\"," +
                            $"\"{EscapeCSV(entry.chineseTraditional)}\"," +
                            $"\"{EscapeCSV(entry.english)}\"," +
                            $"\"{EscapeCSV(entry.japanese)}\"," +
                            $"\"{EscapeCSV(entry.korean)}\"," 
                            //$"\"{EscapeCSV(entry.french)}\"," +
                            //$"\"{EscapeCSV(entry.german)}\"," +
                            //$"\"{EscapeCSV(entry.spanish)}\"," +
                            //$"\"{EscapeCSV(entry.russian)}\"," +
                            //$"\"{EscapeCSV(entry.portuguese)}\"," +
                            //$"\"{EscapeCSV(entry.italian)}\"," +
                            //$"\"{EscapeCSV(entry.arabic)}\"," +
                            //$"\"{EscapeCSV(entry.thai)}\"," +
                            //$"\"{EscapeCSV(entry.vietnamese)}\"," +
                            //$"\"{EscapeCSV(entry.turkish)}\"," +
                            //$"\"{EscapeCSV(entry.polish)}\"," +
                            //$"\"{EscapeCSV(entry.dutch)}\"," +
                            //$"\"{EscapeCSV(entry.indonesian)}\""
                            );
                    }
                }

                EditorUtility.DisplayDialog("导出成功", $"已导出到:\n{path}", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导出失败", e.Message, "确定");
            }
        }

        /// <summary>
        /// 从CSV导入
        /// </summary>
        private void ImportFromCSV()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "导入CSV",
                @"D:\Unity Projects\Project_Chaos\Assets\Excel\Chaos_localization_excel\",
                new string[] { "CSV文件", "csv" });

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (StreamReader reader = new StreamReader(path, System.Text.Encoding.UTF8))
                {
                    //读取表头
                    string header = reader.ReadLine();
                    if (string.IsNullOrEmpty(header))
                    {
                        EditorUtility.DisplayDialog("导入失败", "CSV文件为空", "确定");
                        return;
                    }

                    //清空现有数据或追加
                    bool clearExisting = EditorUtility.DisplayDialog(
                        "导入方式",
                        "是否清空现有数据？\n选择'是'将清空现有数据，选择'否'将追加新数据",
                        "清空并导入", "追加导入");

                    if (clearExisting)
                    {
                        currentData.entries.Clear();
                    }

                    //读取数据行
                    string line;
                    int importedCount = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = ParseCSVLine(line);
                        if (values.Length < 3) continue;

                        string key = values[0];
                        if (string.IsNullOrEmpty(key)) continue;

                        //检查Key是否已存在
                        if (currentData.ContainsKey(key))
                        {
                            //更新现有条目
                            var existingEntry = currentData.GetEntry(key);
                            existingEntry.description = values[1];
                            existingEntry.chineseSimplified = values[2];
                            if (values.Length > 3) existingEntry.chineseTraditional = values[3];
                            if (values.Length > 4) existingEntry.english = values[4];
                            if (values.Length > 5) existingEntry.japanese = values[5];
                            if (values.Length > 6) existingEntry.korean = values[6];
                            //if (values.Length > 7) existingEntry.french = values[7];
                            //if (values.Length > 8) existingEntry.german = values[8];
                            //if (values.Length > 9) existingEntry.spanish = values[9];
                            //if (values.Length > 10) existingEntry.russian = values[10];
                            //if (values.Length > 11) existingEntry.portuguese = values[11];
                            //if (values.Length > 12) existingEntry.italian = values[12];
                            //if (values.Length > 13) existingEntry.arabic = values[13];
                            //if (values.Length > 14) existingEntry.thai = values[14];
                            //if (values.Length > 15) existingEntry.vietnamese = values[15];
                            //if (values.Length > 16) existingEntry.turkish = values[16];
                            //if (values.Length > 17) existingEntry.polish = values[17];
                            //if (values.Length > 18) existingEntry.dutch = values[18];
                            //if (values.Length > 19) existingEntry.indonesian = values[19];
                        }
                        else
                        {
                            //创建新条目
                            var newEntry = new LocalizationEntry()
                            {
                                key = key,
                                description = values[1],
                                chineseSimplified = values[2]
                            };
                            if (values.Length > 3) newEntry.chineseTraditional = values[3];
                            if (values.Length > 4) newEntry.english = values[4];
                            if (values.Length > 5) newEntry.japanese = values[5];
                            if (values.Length > 6) newEntry.korean = values[6];
                            //if (values.Length > 7) newEntry.french = values[7];
                            //if (values.Length > 8) newEntry.german = values[8];
                            //if (values.Length > 9) newEntry.spanish = values[9];
                            //if (values.Length > 10) newEntry.russian = values[10];
                            //if (values.Length > 11) newEntry.portuguese = values[11];
                            //if (values.Length > 12) newEntry.italian = values[12];
                            //if (values.Length > 13) newEntry.arabic = values[13];
                            //if (values.Length > 14) newEntry.thai = values[14];
                            //if (values.Length > 15) newEntry.vietnamese = values[15];
                            //if (values.Length > 16) newEntry.turkish = values[16];
                            //if (values.Length > 17) newEntry.polish = values[17];
                            //if (values.Length > 18) newEntry.dutch = values[18];
                            //if (values.Length > 19) newEntry.indonesian = values[19];

                            currentData.entries.Add(newEntry);
                        }
                        importedCount++;
                    }

                    EditorUtility.SetDirty(currentData);
                    EditorUtility.DisplayDialog("导入成功", $"成功导入 {importedCount} 条数据", "确定");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导入失败", e.Message, "确定");
            }
        }

        /// <summary>
        /// 转义CSV特殊字符
        /// </summary>
        private string EscapeCSV(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
        }

        /// <summary>
        /// 解析CSV行
        /// </summary>
        private string[] ParseCSVLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            System.Text.StringBuilder currentValue = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        //转义的引号
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Replace("\\n", "\n"));
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString().Replace("\\n", "\n"));
            return values.ToArray();
        }
    }
}
