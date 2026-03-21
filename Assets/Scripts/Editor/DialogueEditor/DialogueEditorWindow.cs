using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// 剧情对话编辑器窗口
    /// </summary>
    public class DialogueEditorWindow : EditorWindow
    {
        private DialogData currentDialogue;//当前编辑的对话
        private Vector2 leftPanelScroll;//左侧面板滚动位置
        private Vector2 rightPanelScroll;//右侧面板滚动位置
        private int selectedEntryIndex = -1;//当前选中的对话条目索引
        //private int selectedCharacterIndex = -1;//当前选中的人物索引
        private bool showCharacters = true;//是否展开人物面板
        private bool showSettings = true;//是否展开设置面板
        private string searchFilter = "";//搜索过滤

        private const string EDITOR_PREFS_KEY = "DialogueEditor_LastFile";
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;

        [MenuItem("Tool/剧情对话编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueEditorWindow>("对话编辑器");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        private void OnEnable()
        {
            string lastFile = EditorPrefs.GetString(EDITOR_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
            {
                currentDialogue = AssetDatabase.LoadAssetAtPath<DialogData>(lastFile);
            }
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

            EditorGUILayout.BeginHorizontal();

            //左侧面板
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();

            //分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();

            //右侧面板
            EditorGUILayout.BeginVertical();
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制左侧面板
        /// </summary>
        private void DrawLeftPanel()
        {
            //文件操作区域
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("对话文件", headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(currentDialogue, typeof(DialogData), false);
            if (GUILayout.Button("新建", GUILayout.Width(50)))
            {
                CreateNewDialogue();
            }
            if (GUILayout.Button("打开", GUILayout.Width(50)))
            {
                OpenDialogue();
            }
            EditorGUILayout.EndHorizontal();

            if (currentDialogue != null && GUILayout.Button("保存", GUILayout.Height(25)))
            {
                SaveDialogue();
            }
            EditorGUILayout.EndVertical();

            if (currentDialogue == null) return;

            leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);

            //人物列表
            showCharacters = EditorGUILayout.Foldout(showCharacters, "人物配置", true, subHeaderStyle);
            if (showCharacters)
            {
                DrawCharacterList();
            }

            GUILayout.Space(10);

            //对话条目列表
            GUILayout.Label("对话条目", subHeaderStyle);

            //搜索过滤
            searchFilter = EditorGUILayout.TextField("搜索:", searchFilter);

            GUILayout.Space(5);

            if (GUILayout.Button("+ 添加对话", GUILayout.Height(30)))
            {
                AddNewDialogueEntry();
            }

            GUILayout.Space(5);

            //显示对话条目列表
            for (int i = 0; i < currentDialogue.dialogueEntries.Count; i++)
            {
                var entry = currentDialogue.dialogueEntries[i];
                string characterName = GetCharacterName(entry.characterId);
                string previewText = entry.dialogueText.Length > 20 ? 
                    entry.dialogueText.Substring(0, 20) + "..." : entry.dialogueText;

                //搜索过滤
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!characterName.Contains(searchFilter) && !entry.dialogueText.Contains(searchFilter))
                        continue;
                }

                EditorGUILayout.BeginHorizontal();

                //选中高亮
                GUI.backgroundColor = (i == selectedEntryIndex) ? Color.cyan : Color.white;

                if (GUILayout.Button($"{i + 1}. [{characterName}] {previewText}", 
                    GUILayout.Height(30)))
                {
                    selectedEntryIndex = i;
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = Color.white;

                //删除按钮
                if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", "确定要删除这条对话吗？", "删除", "取消"))
                    {
                        currentDialogue.dialogueEntries.RemoveAt(i);
                        if (selectedEntryIndex == i)
                            selectedEntryIndex = -1;
                        else if (selectedEntryIndex > i)
                            selectedEntryIndex--;
                        EditorUtility.SetDirty(currentDialogue);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制人物列表
        /// </summary>
        private void DrawCharacterList()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (GUILayout.Button("+ 添加人物", GUILayout.Height(25)))
            {
                currentDialogue.characters.Add(new DialogCharacter());
                EditorUtility.SetDirty(currentDialogue);
            }

            for (int i = 0; i < currentDialogue.characters.Count; i++)
            {
                var character = currentDialogue.characters[i];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                character.characterId = EditorGUILayout.TextField("ID:", character.characterId);
                character.characterName = EditorGUILayout.TextField("名称:", character.characterName);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(40)))
                {
                    currentDialogue.characters.RemoveAt(i);
                    EditorUtility.SetDirty(currentDialogue);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制右侧面板
        /// </summary>
        private void DrawRightPanel()
        {
            if (currentDialogue == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请创建或打开一个对话文件", headerStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

            //基本信息
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("基本信息", headerStyle);
            currentDialogue.dialogueId = EditorGUILayout.TextField("对话ID:", currentDialogue.dialogueId);
            currentDialogue.dialogueName = EditorGUILayout.TextField("对话名称:", currentDialogue.dialogueName);
            EditorGUILayout.LabelField("描述:");
            currentDialogue.description = EditorGUILayout.TextArea(currentDialogue.description, GUILayout.Height(50));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            //全局设置
            showSettings = EditorGUILayout.Foldout(showSettings, "全局设置", true, subHeaderStyle);
            if (showSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                currentDialogue.canSkip = EditorGUILayout.Toggle("可跳过:", currentDialogue.canSkip);
                currentDialogue.autoPlay = EditorGUILayout.Toggle("自动播放:", currentDialogue.autoPlay);
                currentDialogue.showHistory = EditorGUILayout.Toggle("显示历史:", currentDialogue.showHistory);
                currentDialogue.pauseGame = EditorGUILayout.Toggle("暂停游戏:", currentDialogue.pauseGame);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            //对话条目编辑
            if (selectedEntryIndex >= 0 && selectedEntryIndex < currentDialogue.dialogueEntries.Count)
            {
                DrawDialogueEntryEditor(currentDialogue.dialogueEntries[selectedEntryIndex], selectedEntryIndex);
            }
            else
            {
                GUILayout.Label("请在左侧选择一个对话条目进行编辑", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制对话条目编辑器
        /// </summary>
        private void DrawDialogueEntryEditor(DialogEntry entry, int index)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            GUILayout.Label($"对话条目 #{index + 1}", headerStyle);

            //人物选择
            EditorGUILayout.LabelField("说话人物:", EditorStyles.boldLabel);
            string[] characterNames = GetCharacterNames();
            string[] characterIds = GetCharacterIds();

            int selectedIndex = 0;
            for (int i = 0; i < characterIds.Length; i++)
            {
                if (characterIds[i] == entry.characterId)
                {
                    selectedIndex = i;
                    break;
                }
            }

            selectedIndex = EditorGUILayout.Popup(selectedIndex, characterNames);
            if (selectedIndex >= 0 && selectedIndex < characterIds.Length)
            {
                entry.characterId = characterIds[selectedIndex];
            }

            GUILayout.Space(10);

            //对话文本
            EditorGUILayout.LabelField("对话文本:", EditorStyles.boldLabel);
            entry.dialogueText = EditorGUILayout.TextArea(entry.dialogueText, GUILayout.MinHeight(80));

            GUILayout.Space(10);

            //打字速度
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("打字速度:", GUILayout.Width(80));
            entry.typingSpeed = EditorGUILayout.Slider(entry.typingSpeed, 0.01f, 0.5f);
            EditorGUILayout.LabelField($"{entry.typingSpeed:F2}秒/字", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            //自动跳过延迟
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("自动跳过:", GUILayout.Width(80));
            entry.autoSkipDelay = EditorGUILayout.FloatField(entry.autoSkipDelay);
            EditorGUILayout.LabelField("秒 (-1=不自动跳过)", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            //语音
            entry.voiceClip = (AudioClip)EditorGUILayout.ObjectField("语音:", entry.voiceClip, typeof(AudioClip), false);

            GUILayout.Space(10);

            //下一条对话
            EditorGUILayout.LabelField("跳转设置:", EditorStyles.boldLabel);
            entry.nextDialogueId = EditorGUILayout.TextField("下一条对话ID:", entry.nextDialogueId);

            GUILayout.Space(10);

            //对话选项
            EditorGUILayout.LabelField("对话选项:", EditorStyles.boldLabel);
            DrawDialogueOptions(entry);

            GUILayout.Space(10);

            //事件
            EditorGUILayout.LabelField("触发事件:", EditorStyles.boldLabel);
            DrawDialogueEvents(entry);

            EditorGUILayout.EndVertical();

            //标记为已修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(currentDialogue);
            }
        }

        /// <summary>
        /// 绘制对话选项
        /// </summary>
        private void DrawDialogueOptions(DialogEntry entry)
        {
            if (entry.options == null)
                entry.options = new List<DialogOption>();

            if (GUILayout.Button("+ 添加选项", GUILayout.Height(25)))
            {
                entry.options.Add(new DialogOption());
                EditorUtility.SetDirty(currentDialogue);
            }

            for (int i = 0; i < entry.options.Count; i++)
            {
                var option = entry.options[i];

                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"选项 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    entry.options.RemoveAt(i);
                    EditorUtility.SetDirty(currentDialogue);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                option.optionText = EditorGUILayout.TextField("选项文本:", option.optionText);
                option.nextDialogueId = EditorGUILayout.TextField("跳转ID:", option.nextDialogueId);

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        /// <summary>
        /// 绘制对话事件
        /// </summary>
        private void DrawDialogueEvents(DialogEntry entry)
        {
            if (entry.events == null)
                entry.events = new List<DialogEvent>();

            if (GUILayout.Button("+ 添加事件", GUILayout.Height(25)))
            {
                entry.events.Add(new DialogEvent());
                EditorUtility.SetDirty(currentDialogue);
            }

            for (int i = 0; i < entry.events.Count; i++)
            {
                var evt = entry.events[i];

                EditorGUILayout.BeginHorizontal();
                evt.eventName = EditorGUILayout.TextField("事件名:", evt.eventName);
                evt.eventParam = EditorGUILayout.TextField("参数:", evt.eventParam);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    entry.events.RemoveAt(i);
                    EditorUtility.SetDirty(currentDialogue);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 获取人物名称数组
        /// </summary>
        private string[] GetCharacterNames()
        {
            if (currentDialogue.characters == null || currentDialogue.characters.Count == 0)
                return new string[] { "未配置" };

            return currentDialogue.characters.Select(c => $"{c.characterName} ({c.characterId})").ToArray();
        }

        /// <summary>
        /// 获取人物ID数组
        /// </summary>
        private string[] GetCharacterIds()
        {
            if (currentDialogue.characters == null || currentDialogue.characters.Count == 0)
                return new string[] { "" };

            return currentDialogue.characters.Select(c => c.characterId).ToArray();
        }

        /// <summary>
        /// 根据ID获取人物名称
        /// </summary>
        private string GetCharacterName(string characterId)
        {
            if (currentDialogue.characters == null) return "未知";
            var character = currentDialogue.characters.Find(c => c.characterId == characterId);
            return character != null ? character.characterName : "未知";
        }

        /// <summary>
        /// 创建新对话
        /// </summary>
        private void CreateNewDialogue()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建对话配置",
                "NewDialogue",
                "asset",
                "选择保存位置",
                "Assets/Data/Dialogues");

            if (!string.IsNullOrEmpty(path))
            {
                //确保目录存在
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var dialogue = CreateInstance<DialogData>();
                dialogue.dialogueId = Path.GetFileNameWithoutExtension(path);
                dialogue.dialogueName = dialogue.dialogueId;

                AssetDatabase.CreateAsset(dialogue, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentDialogue = dialogue;
                EditorPrefs.SetString(EDITOR_PREFS_KEY, path);
                selectedEntryIndex = -1;
            }
        }

        /// <summary>
        /// 打开对话
        /// </summary>
        private void OpenDialogue()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "打开对话配置",
                "Assets/Data/Dialogues",
                new string[] { "对话配置", "asset" });

            if (!string.IsNullOrEmpty(path))
            {
                //转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                currentDialogue = AssetDatabase.LoadAssetAtPath<DialogData>(path);
                if (currentDialogue != null)
                {
                    EditorPrefs.SetString(EDITOR_PREFS_KEY, path);
                    selectedEntryIndex = -1;
                }
            }
        }

        /// <summary>
        /// 保存对话
        /// </summary>
        private void SaveDialogue()
        {
            if (currentDialogue != null)
            {
                EditorUtility.SetDirty(currentDialogue);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("保存成功", "对话配置已保存！", "确定");
            }
        }

        /// <summary>
        /// 添加新对话条目
        /// </summary>
        private void AddNewDialogueEntry()
        {
            if (currentDialogue.dialogueEntries == null)
                currentDialogue.dialogueEntries = new List<DialogEntry>();

            var newEntry = new DialogEntry()
            {
                characterId = currentDialogue.characters.Count > 0 ? currentDialogue.characters[0].characterId : "",
                typingSpeed = 0.05f,
                autoSkipDelay = -1
            };

            currentDialogue.dialogueEntries.Add(newEntry);
            selectedEntryIndex = currentDialogue.dialogueEntries.Count - 1;
            EditorUtility.SetDirty(currentDialogue);
        }
    }
}
