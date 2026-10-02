using System;
using System.Collections.Generic;
using System.Text;
using ChaosDebug;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LocalizationSystem.Editor
{
    /// <summary>
    /// 面板文字收集器：把 UI 预制体里的 TMP 文字捞出来，批量导入本地化配置，
    /// 并能顺手把这些文字接上 LocalizedText 组件。
    ///
    /// ══════════════════ 它解决什么 ══════════════════
    /// 手工接本地化有两个容易漏的地方：
    ///   ① 得先知道每个面板上有哪些文字 —— 要一个个打开预制体去看；
    ///   ② 加完 Key 还得再回到预制体上把 Key 填进 LocalizedText，两处容易对不上。
    /// 这个工具把这两步并成一步：扫描 → 勾选 → 导入配置 → 回写组件。
    ///
    /// ══════════════════ 可重复执行 ══════════════════
    /// 扫描和导入都是幂等的：
    ///   · 已在配置里的条目会被标成「已存在」且默认不勾选；
    ///   · 已挂 LocalizedText 的物体只更新 Key，不会重复加组件。
    /// 所以文案改完可以随时重扫一遍。
    ///
    /// ══════════════════ 扫描路径 ══════════════════
    /// 默认值来自 GlobalPath.ui_PanelPrefabSearchPaths（加新面板目录改那里即可），
    /// 窗口里也能临时增删 —— 但那只是本次会话的改动，不落盘。
    /// </summary>
    public class PrefabTextCollectorWindow : EditorWindow
    {
        // ══════════════════ 数据 ══════════════════

        /// <summary>这条文字来自哪里。决定「写入 LocalizedText」时怎么定位对象。</summary>
        public enum TextSource
        {
            Prefab,
            Scene,
        }

        /// <summary>一条「预制体里的文字」。</summary>
        public class TextItem
        {
            public string PrefabPath;      // 预制体或场景的 Assets/... 完整路径
            public string PrefabName;      // 显示名（含来源标记）
            public string ObjectPath;      // 相对根节点的层级路径
            public string Text;            // Trim 之后的文字
            public bool HadWhitespace;     // 原文有首尾空白（导入时会提示）
            public string Key;             // 当前 Key（可在窗口里改）
            public bool Selected;
            public bool AlreadyInConfig;   // 目标配置里已有同 Key 或同中文的条目
            public bool KeyDuplicated;     // 本次结果里 Key 撞了
            public TextSource SourceKind;  // 预制体 / 场景
        }

        /// <summary>按来源分组（预制体或场景），便于折叠查看。</summary>
        private class PrefabGroup
        {
            public string Path;
            public string Name;
            public bool Foldout = true;
            public TextSource SourceKind = TextSource.Prefab;
            public readonly List<TextItem> Items = new List<TextItem>();
        }

        private readonly List<string> _searchPaths = new List<string>();
        private readonly List<PrefabGroup> _groups = new List<PrefabGroup>();
        private LocalizationData _targetConfig;
        private Vector2 _scroll;
        private bool _scanned;

        /// <summary>
        /// 是否连场景一起扫。默认开：场景里的硬编码文案最容易漏，
        /// 而漏掉时没有任何提示 —— 打开默认值让它自然被发现。
        /// </summary>
        private bool _scanScenes = true;

        private const string TargetConfigKey = "PrefabTextCollector_TargetConfig";

        /// <summary>
        /// 静默模式：不弹模态对话框，结果只走 ChaosLog。
        ///
        /// ══════════════ 为什么需要这个开关 ══════════════
        /// EditorUtility.DisplayDialog 是【模态】的，它会阻塞 Unity 主线程直到有人点掉。
        /// 用脚本/自动化（例如通过 MCP 驱动）调这个工具时没人去点，
        /// 主线程就被永久卡住 —— 连编辑器桥接都会一起失去响应。
        /// 所以凡是可能被程序化调用的入口，都要能关掉对话框。
        ///
        /// 默认 false：正常手工使用仍然要看确认框。
        /// </summary>
        public static bool SilentMode = false;

        /// <summary>统一的提示出口：静默模式下只写日志。</summary>
        private static void Notify(string title, string message)
        {
            if (SilentMode)
            {
                ChaosLog.Info(LogChannel.Localization, "[" + title + "] " + message.Replace("\n", " "));
                return;
            }
            EditorUtility.DisplayDialog(title, message, "好");
        }

        // ══════════════════ 窗口 ══════════════════

        [MenuItem("Tools/本地化/面板文字收集器", false, 20)]
        public static void ShowWindow()
        {
            var w = GetWindow<PrefabTextCollectorWindow>("面板文字收集器");
            w.minSize = new Vector2(760f, 620f);
            w.Show();
        }

        private void OnEnable()
        {
            ResetSearchPathsToDefault();

            //上次选的配置要记住：把面板里的 Key 填进组件的活儿常常要来回切窗口
            string last = EditorPrefs.GetString(TargetConfigKey, string.Empty);
            if (!string.IsNullOrEmpty(last))
            {
                _targetConfig = AssetDatabase.LoadAssetAtPath<LocalizationData>(last);
            }

            // ══════════════ 记的那条失效了就退回默认目标 ══════════════
            // 配置资产被改名/拆分之后，EditorPrefs 里存的是一条已经不存在的路径 ——
            // 这时窗口会以"空目标"打开，而用户看到的只是"按钮都是灰的"，完全想不到是路径过期。
            //
            // ⚠ 默认目标单独用一个常量，不能用下面数组的第一项：
            // 那张清单回答的是"有哪几张子配置"，第一项是【主菜单】那张；
            // 而收集器扫的面板绝大多数属于设置面板 —— 默认指向主菜单配置会把设置页的 Key
            // 写进别人的表里，且写的时候不报错。
            if (_targetConfig == null)
            {
                _targetConfig = AssetDatabase.LoadAssetAtPath<LocalizationData>(
                    GlobalPath.ui_DefaultLocalizationConfigPath);
                if (_targetConfig != null)
                {
                    EditorPrefs.SetString(TargetConfigKey, GlobalPath.ui_DefaultLocalizationConfigPath);
                }
            }

            // 默认目标自己也不在（目录被搬过 / 名字改了）：再退回清单里第一张真能加载的，
            // 至少不让窗口开着却是空目标
            if (_targetConfig == null)
            {
                string[] defaults = GlobalPath.ui_LocalizationConfigPaths;
                if (defaults != null)
                {
                    for (int i = 0; i < defaults.Length; i++)
                    {
                        _targetConfig = AssetDatabase.LoadAssetAtPath<LocalizationData>(defaults[i]);
                        if (_targetConfig != null)
                        {
                            EditorPrefs.SetString(TargetConfigKey, defaults[i]);
                            break;
                        }
                    }
                }
            }
        }

        private void ResetSearchPathsToDefault()
        {
            _searchPaths.Clear();
            string[] defaults = GlobalPath.ui_PanelPrefabSearchPaths;
            if (defaults != null)
            {
                for (int i = 0; i < defaults.Length; i++)
                {
                    if (!string.IsNullOrEmpty(defaults[i])) _searchPaths.Add(defaults[i]);
                }
            }
        }

        private void OnGUI()
        {
            DrawPathsSection();
            EditorGUILayout.Space(6f);
            DrawTargetSection();
            EditorGUILayout.Space(6f);
            DrawResultSection();
            EditorGUILayout.Space(6f);
            DrawActionBar();
        }

        // ══════════════════ 上半部分：路径与目标 ══════════════════

        private void DrawPathsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("搜索路径（目录，或直接指定某个 .prefab / .unity）", EditorStyles.boldLabel);

            int removeAt = -1;
            for (int i = 0; i < _searchPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                //路径既可以是目录也可以是具体资产，用 ObjectField 而不是文本框：
                //拖进来就不会有"手打路径打错 → 扫描静默跳过"这种问题。
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUILayout.TextField(_searchPaths[i]);
                //ObjectField 里只放"当前路径指向的那个资产"（目录会显示为 DefaultAsset），
                //非 Assets 下的路径它显示不出来，但文本框仍然生效 —— 两条输入方式并存
                UnityEngine.Object current = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_searchPaths[i]);
                var picked = (UnityEngine.Object)EditorGUILayout.ObjectField(current, typeof(UnityEngine.Object), false, GUILayout.Width(56f));
                if (EditorGUI.EndChangeCheck())
                {
                    if (picked != current)
                    {
                        string assetPath = picked != null ? AssetDatabase.GetAssetPath(picked) : string.Empty;
                        if (!string.IsNullOrEmpty(assetPath)) _searchPaths[i] = assetPath;
                    }
                    else
                    {
                        _searchPaths[i] = typed;
                    }
                }

                if (GUILayout.Button("移除", GUILayout.Width(52f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeAt >= 0) _searchPaths.RemoveAt(removeAt);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 添加目录", GUILayout.Width(90f))) _searchPaths.Add("Assets/");
            //把当前选中的资产加进来：在 Project 里点几下就能凑出要扫的目标，不用手打路径
            using (new EditorGUI.DisabledScope(Selection.activeObject == null
                || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject))))
            {
                if (GUILayout.Button("+ 添加选中的资产", GUILayout.Width(130f)))
                {
                    string sel = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (!string.IsNullOrEmpty(sel) && !_searchPaths.Contains(sel)) _searchPaths.Add(sel);
                }
            }
            if (GUILayout.Button("恢复 GlobalPath 默认", GUILayout.Width(160f))) ResetSearchPathsToDefault();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2f);
            _scanScenes = EditorGUILayout.ToggleLeft(
                "同时扫描场景（全工程 t:Scene；场景实例化的预制体文字会被跳过，避免重复）", _scanScenes);
            if (_scanScenes)
            {
                EditorGUILayout.LabelField(
                    "场景会用 Additive 方式打开后再关闭，不动你当前打开的场景；" +
                    "当前场景有未保存改动时会跳过场景扫描。",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField(
                "默认值来自 GlobalPath.ui_PanelPrefabSearchPaths；窗口里的增删只在本次会话有效。",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("目标本地化配置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _targetConfig = (LocalizationData)EditorGUILayout.ObjectField(
                "LocalizationData", _targetConfig, typeof(LocalizationData), false);
            if (EditorGUI.EndChangeCheck())
            {
                string p = _targetConfig != null ? AssetDatabase.GetAssetPath(_targetConfig) : string.Empty;
                EditorPrefs.SetString(TargetConfigKey, p);
            }

            if (_targetConfig == null)
            {
                //提示里给出"该填什么"，而不是只抱怨"没填"。默认目标来自 GlobalPath，
                //所以这里列出的是真正会被自动选中的那几个
                string hint = "请指定一个 LocalizationData 资产。\n默认目标（GlobalPath.ui_DefaultLocalizationConfigPath）：\n· "
                    + GlobalPath.ui_DefaultLocalizationConfigPath
                    + "\n兜底清单（GlobalPath.ui_LocalizationConfigPaths）：";
                string[] defaults = GlobalPath.ui_LocalizationConfigPaths;
                if (defaults != null && defaults.Length > 0) hint += "\n· " + string.Join("\n· ", defaults);
                else hint += "\n（GlobalPath 里没有配置默认路径，请手动指定）";

                EditorGUILayout.HelpBox(hint, MessageType.Warning);
            }
            else
            {
                //多张配置表并存时，明确写出"这次导入会写进哪一张" —— 写错表很难发现
                EditorGUILayout.LabelField("本次操作写入：" + AssetDatabase.GetAssetPath(_targetConfig),
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        // ══════════════════ 结果区 ══════════════════

        private void DrawResultSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            int total = 0, selected = 0;
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    total++;
                    if (_groups[g].Items[i].Selected) selected++;
                }
            }
            EditorGUILayout.LabelField("扫描结果：" + total + " 条，已勾选 " + selected + " 条", EditorStyles.boldLabel);
            if (_scanned && GUILayout.Button("全选", GUILayout.Width(60f))) SetSelectionAll(true);
            if (_scanned && GUILayout.Button("全选新增项", GUILayout.Width(100f))) SetSelectionForNewItems(true);
            if (_scanned && GUILayout.Button("清空勾选", GUILayout.Width(90f))) SetSelectionAll(false);
            EditorGUILayout.EndHorizontal();

            if (!_scanned)
            {
                EditorGUILayout.HelpBox("点下面的「扫描」开始。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            if (_groups.Count == 0)
            {
                EditorGUILayout.HelpBox("这些路径下没有找到带 TMP 文字的预制体。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(240f));
            for (int g = 0; g < _groups.Count; g++)
            {
                PrefabGroup group = _groups[g];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                group.Foldout = EditorGUILayout.Foldout(group.Foldout, group.Name + "  (" + group.Items.Count + ")", true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("定位", GUILayout.Width(52f)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(group.Path);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.EndHorizontal();

                if (group.Foldout)
                {
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        DrawItemRow(group.Items[i]);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawItemRow(TextItem item)
        {
            EditorGUILayout.BeginHorizontal();

            item.Selected = EditorGUILayout.Toggle(item.Selected, GUILayout.Width(18f));

            //文字（只读展示，改文案要去预制体里改 —— 这里不越权改原文）
            string shown = item.Text.Length > 26 ? item.Text.Substring(0, 26) + "…" : item.Text;
            EditorGUILayout.LabelField(shown, GUILayout.Width(200f));

            EditorGUILayout.LabelField(item.ObjectPath, EditorStyles.miniLabel, GUILayout.Width(190f));

            item.Key = EditorGUILayout.TextField(item.Key, GUILayout.Width(200f));

            //状态标记
            string badge;
            if (item.KeyDuplicated) badge = "Key 重复";
            else if (item.AlreadyInConfig) badge = "已存在";
            else badge = "新增";

            Color old = GUI.color;
            if (item.KeyDuplicated) GUI.color = new Color(1f, 0.5f, 0.5f);
            else if (!item.AlreadyInConfig) GUI.color = new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.LabelField(badge, EditorStyles.miniLabel, GUILayout.Width(58f));
            GUI.color = old;

            if (item.HadWhitespace)
            {
                EditorGUILayout.LabelField("原文含首尾空白", EditorStyles.miniLabel, GUILayout.Width(100f));
            }

            //单条清除：勾选一大堆时，要精准干掉某一条不必去改勾选状态。
            //做成"只勾这一条→走同一套清除逻辑→还原勾选"，是为了让单条与批量走完全相同的代码路径，
            //否则两条路径的删除规则（配置去重、场景保存、重复组件清理）迟早会走岔
            GUI.color = new Color(1f, 0.72f, 0.72f);
            if (GUILayout.Button("清除", EditorStyles.miniButton, GUILayout.Width(44f)))
            {
                ClearSingleItem(item);
            }
            GUI.color = old;

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 只清除某一行的条目：从配置删条目 + 摘掉该物体上的 LocalizedText。
        /// 实现是"临时把勾选改成只有它"，复用 ClearSelectedItems 的整套规则。
        /// </summary>
        private void ClearSingleItem(TextItem item)
        {
            if (_targetConfig == null) return;

            var keep = new List<string>();
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) keep.Add(MakeItemId(_groups[g].Items[i]));
                }
            }
            SetSelectionAll(false);
            item.Selected = true;

            //清除走的是"勾选项"那套逻辑（会弹一次确认框、并重扫保留勾选）
            ClearSelectedItems();

            //还原原先的勾选（被清掉的那条在重扫后已不在配置里，不必也不该被选中）
            RestoreSelection(keep);
        }

        /// <summary>
        /// 勾选/取消【全部】条目，不区分是不是已存在。
        ///
        /// 与「全选新增项」的区别：那个只勾配置里还没有的，
        /// 这个不管有没有都勾上 —— 想在已有条目上重跑一遍回写组件、
        /// 或者想批量清理勾选状态时用它。
        /// </summary>
        private void SetSelectionAll(bool select)
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    _groups[g].Items[i].Selected = select;
                }
            }
        }

        private void SetSelectionForNewItems(bool select)
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    TextItem it = _groups[g].Items[i];
                    //「全选新增项」只勾没有的；「清空勾选」一律取消
                    it.Selected = select && !it.AlreadyInConfig && !it.KeyDuplicated;
                }
            }
        }

        // ══════════════════ 底部按钮 ══════════════════

        private void DrawActionBar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("扫描", GUILayout.Height(30f))) RescanPreservingSelection();

            using (new EditorGUI.DisabledScope(_targetConfig == null || !_scanned))
            {
                if (GUILayout.Button("导入到配置", GUILayout.Height(30f))) ImportToConfig();
                if (GUILayout.Button("写入 LocalizedText 组件", GUILayout.Height(30f))) WriteComponents();
            }

            // 清除：把勾选条目从配置里删掉，并摘掉物体上的 LocalizedText。
            // 与"写入"刚好相反，放在同一行末尾、并用红底标示它是破坏性操作
            using (new EditorGUI.DisabledScope(_targetConfig == null || !_scanned || CountSelected() == 0))
            {
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.72f, 0.72f);
                if (GUILayout.Button("清除勾选项（配置 + 组件）", GUILayout.Height(30f), GUILayout.Width(210f)))
                {
                    ClearSelectedItems();
                }
                GUI.backgroundColor = old;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ══════════════════ 扫描 ══════════════════

        private void Scan()
        {
            _groups.Clear();
            _scanned = true;

            var seenKeys = new Dictionary<string, int>();

            for (int p = 0; p < _searchPaths.Count; p++)
            {
                string path = _searchPaths[p];
                if (string.IsNullOrEmpty(path)) continue;
                if (path.EndsWith("/")) path = path.TrimEnd('/');

                // ══════════════ 三种路径 ══════════════
                // 目录       → 扫描目录下所有预制体
                // .prefab    → 只扫这一个（从一个具体预制体入手时最省事，不用为了它单开一个目录）
                // .unity     → 当场景扫（走 ScanScene，会 Additive 打开再关掉）
                if (AssetDatabase.IsValidFolder(path))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                        PrefabGroup group = ScanPrefab(prefabPath);
                        if (group != null && group.Items.Count > 0) _groups.Add(group);
                    }
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    {
                        ChaosLog.Warn(LogChannel.Localization, "预制体路径无效，已跳过：" + path);
                        continue;
                    }
                    PrefabGroup group = ScanPrefab(path);
                    if (group != null && group.Items.Count > 0) _groups.Add(group);
                }
                else if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    if (Application.isPlaying)
                    {
                        ChaosLog.Warn(LogChannel.Localization, "正在播放模式，无法扫描场景，已跳过：" + path);
                    }
                    else
                    {
                        PrefabGroup group = ScanScene(path);
                        if (group != null && group.Items.Count > 0) _groups.Add(group);
                    }
                }
                else
                {
                    ChaosLog.Warn(LogChannel.Localization,
                        "扫描路径既不是目录，也不是 .prefab/.unity，已跳过：" + path);
                }
            }

            // ══════════════ 场景 ══════════════
            // 场景不按搜索路径找（那两条是 UI 目录），而是全工程找 —— 文案散落在哪个场景都可能。
            //
            // 这里再判一次 Application.isPlaying（ScanScene 里也判了）：
            // OpenScene 在播放模式下必然抛异常，在最外层拦掉能保证【整个扫描】不会因此中断，
            // 而不是靠每个场景各自 catch 一遍。
            if (_scanScenes && Application.isPlaying)
            {
                ChaosLog.Warn(LogChannel.Localization,
                    "正在播放模式，已跳过场景扫描（Unity 的 OpenScene 在播放时不可用）。预制体部分照常扫描。");
            }
            else if (_scanScenes)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                {
                    ChaosLog.Warn(LogChannel.Localization,
                        "当前场景有未保存改动，已跳过场景扫描以免打断你。请先保存场景再扫。");
                }
                else
                {
                    string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                    for (int i = 0; i < sceneGuids.Length; i++)
                    {
                        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

                        // 跳过第三方插件自带的示例场景（Spine 示例就有 30 个）——
                        // 它们不是本项目的文案，扫进来只会淹没结果。
                        if (scenePath.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        PrefabGroup group = ScanScene(scenePath);
                        if (group != null && group.Items.Count > 0) _groups.Add(group);
                    }
                }
            }

            _groups.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            //统计 Key 撞车（跨预制体也算，因为导入的是同一份配置）
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    TextItem it = _groups[g].Items[i];
                    if (string.IsNullOrEmpty(it.Key)) continue;
                    int n;
                    seenKeys.TryGetValue(it.Key, out n);
                    seenKeys[it.Key] = n + 1;
                }
            }
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    TextItem it = _groups[g].Items[i];
                    int n;
                    if (!string.IsNullOrEmpty(it.Key) && seenKeys.TryGetValue(it.Key, out n) && n > 1)
                    {
                        it.KeyDuplicated = true;
                    }
                }
            }

            ChaosLog.Info(LogChannel.Localization, "面板文字扫描完成，共 " + _groups.Count + " 个预制体");
        }

        private PrefabGroup ScanPrefab(string prefabPath)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) return null;

                // 只收 UGUI 文本（TextMeshProUGUI），不收世界空间文字。
                //
                // LocalizedText 上有 [RequireComponent(typeof(TextMeshProUGUI))]：
                // 世界空间的 TextMeshPro 挂不上它 —— 硬挂会被 Unity 偷偷补一个
                // 不渲染的 TextMeshProUGUI，本地化就此静默失效。
                // 既然收进来也写不了，就不收：列表里的每一条都是能真正本地化的。
                TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts == null || texts.Length == 0) return null;

                var group = new PrefabGroup
                {
                    Path = prefabPath,
                    Name = System.IO.Path.GetFileNameWithoutExtension(prefabPath),
                    SourceKind = TextSource.Prefab,
                };

                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text t = texts[i];
                    if (t == null) continue;

                    string raw = t.text;
                    if (string.IsNullOrEmpty(raw)) continue;

                    string trimmed = raw.Trim();
                    if (trimmed.Length == 0) continue;//纯空白的不收集

                    var item = new TextItem
                    {
                        PrefabPath = prefabPath,
                        PrefabName = group.Name,
                        ObjectPath = BuildObjectPath(root.transform, t.transform),
                        Text = trimmed,
                        HadWhitespace = raw.Length != trimmed.Length,
                        Key = SuggestKey(group.Name, t),
                    };
                    item.AlreadyInConfig = IsAlreadyInConfig(item);
                    group.Items.Add(item);
                }
                return group;
            }
            catch (Exception e)
            {
                ChaosLog.Error(LogChannel.Localization, "扫描预制体失败 " + prefabPath + "：" + e.Message);
                return null;
            }
            finally
            {
                //必须成对，否则隔离场景会泄漏（参考 UIPanelAnimatorTool）
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 扫描一个场景里的全部 TMP 文字。
        ///
        /// ══════════════ 为什么必须管场景 ══════════════
        /// 面板预制体只是文案的一半。场景里经常有直接摆在层级里的文字
        /// （标题、测试面板、开场提示），它们不在任何预制体里 ——
        /// 只扫预制体会漏掉，而且漏得毫无提示。Project_Chaos 的 TestScene 就是例子。
        ///
        /// ══════════════ 用 Additive 打开，不动当前场景 ══════════════
        /// 这样不会把你正在编辑的场景挤掉。代价是打开时会跑一次场景加载，
        /// 而且如果【当前场景有未保存改动】，Unity 可能拦一道 —— 由调用方事先检查并提示。
        ///
        /// 遍历含未激活对象（includeInactive: true）：未激活的页签/弹窗里的文案一样要本地化。
        /// </summary>
        private PrefabGroup ScanScene(string scenePath)
        {
            // EditorSceneManager.OpenScene 在播放模式下会直接抛异常，而异常信息很晦涩。
            // 这里提前挡住并给出可执行的原因 —— 否则表现是"场景扫描静默没结果"，极难排查。
            if (Application.isPlaying)
            {
                ChaosLog.Warn(LogChannel.Localization,
                    "正在播放模式，无法扫描场景（Unity 的 OpenScene 在播放时不可用）。请先停止播放。已跳过：" + scenePath);
                return null;
            }

            // 已经打开的场景不要再 Additive 开一份：
            // 一是没必要，二是关掉它可能触发 "Unloading the last loaded scene is not supported" 警告。
            // 直接用已打开的那份遍历即可。
            UnityEngine.SceneManagement.Scene already =
                UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedByUs = !already.IsValid() || !already.isLoaded;

            UnityEngine.SceneManagement.Scene scene = openedByUs
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
                : already;
            try
            {
                var texts = new List<TextMeshProUGUI>();
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] == null) continue;
                    texts.AddRange(roots[i].GetComponentsInChildren<TextMeshProUGUI>(true));
                }
                if (texts.Count == 0) return null;

                // 场景实例化的预制体，其文字属于那个预制体 —— 跳过，避免与预制体扫描重复
                var prefabInstances = new List<Transform>();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] == null) continue;
                    Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                    for (int j = 0; j < all.Length; j++)
                    {
                        if (all[j] != null && PrefabUtility.GetPrefabInstanceStatus(all[j].gameObject)
                            == PrefabInstanceStatus.Connected)
                        {
                            prefabInstances.Add(all[j]);
                        }
                    }
                }

                var group = new PrefabGroup
                {
                    Path = scenePath,
                    Name = System.IO.Path.GetFileNameWithoutExtension(scenePath) + " (场景)",
                    SourceKind = TextSource.Scene,
                };

                for (int i = 0; i < texts.Count; i++)
                {
                    TMP_Text t = texts[i];
                    if (t == null) continue;

                    string raw = t.text;
                    if (string.IsNullOrEmpty(raw)) continue;
                    string trimmed = raw.Trim();
                    if (trimmed.Length == 0) continue;

                    // 落在预制体实例里的跳过（那部分归预制体管）
                    bool inPrefab = false;
                    for (int k = 0; k < prefabInstances.Count; k++)
                    {
                        if (prefabInstances[k] != null && t.transform.IsChildOf(prefabInstances[k]))
                        {
                            inPrefab = true;
                            break;
                        }
                    }
                    if (inPrefab) continue;

                    string objectPath = BuildObjectPath(null, t.transform);
                    var item = new TextItem
                    {
                        PrefabPath = scenePath,
                        PrefabName = group.Name,
                        ObjectPath = objectPath,
                        Text = trimmed,
                        HadWhitespace = raw.Length != trimmed.Length,
                        Key = SuggestKey(System.IO.Path.GetFileNameWithoutExtension(scenePath), t),
                        SourceKind = TextSource.Scene,
                    };
                    item.AlreadyInConfig = IsAlreadyInConfig(item);
                    group.Items.Add(item);
                }
                return group;
            }
            catch (Exception e)
            {
                ChaosLog.Error(LogChannel.Localization, "扫描场景失败 " + scenePath + "：" + e.Message);
                return null;
            }
            finally
            {
                //只关【我们自己开的】那份；本来就打开的场景不能替用户关掉。
                if (openedByUs && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        /// <summary>拼出相对预制体根的层级路径；根节点本身不出现在路径里。</summary>
        private static string BuildObjectPath(Transform root, Transform target)
        {
            var parts = new List<string>();
            Transform cur = target;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        /// <summary>
        /// 给一个 TMP 文本建议一个 Key。优先级（从高到低）：
        ///
        /// ① 对象上【已经有的】Key（LocalizedText.localizationKey 非空）
        ///    —— 尊重既有分配。这条保证了重新扫描不会把人工改过的 Key 冲掉。
        /// ② 同一行有 settingId 时，由它派生（ui_setting_ + 去分类前缀）
        ///    —— 设置行标签沿用这套约定，配置里的 Key 因此不用重建。
        /// ③ 否则用完整层级路径净化（ui_ + 预制体短名 + 路径）
        ///
        /// ══════════════ 为什么 settingId 只作为第 ② 档 ══════════════
        /// 它是"设置系统内部的行标识"，本来不该当本地化合同。但已有配置是按它建的，
        /// 所以这里【只读不改】地沿用，而不是把它当唯一来源 —— 没有 settingId 的对象
        /// （主选列表按钮、页签、底部按钮、场景里的文字）走 ① 或 ③，一样能覆盖。
        ///
        /// 注意 ① 让本方法对"生成器只挂空组件"的新产物同样有效：
        /// 空 Key → 落到 ② → 派生出与旧约定一致的 Key。
        /// </summary>
        private static string SuggestKey(string assetShortName, TMP_Text text)
        {
            // ① 已有 Key
            LocalizedText existing = text.GetComponent<LocalizedText>();
            if (existing != null && !string.IsNullOrEmpty(existing.localizationKey))
            {
                return existing.localizationKey;
            }

            // ② 所在行的 settingId —— 但【只认行的直接子节点 Label】
            //
            // 为什么必须限定 Label：一行里有多个文本。以选择器行为例，
            // 除 Label 外还有 Prev/Next 的 "<" ">" 和 ValueText（运行时显示数值）。
            // 不加限定的话它们都会命中同一个 settingId、算出同一个 Key ——
            // 结果是 ValueText 的 "0" 也去抢 ui_setting_*，既错又污染配置。
            // 只有 Label 才是"这一行的标题"，才该用行的语义 Key。
            //
            // ⚠ 光判名字不够：Prev/Label、Next/Label、Button/Label、ResetButton/Label
            // 也叫 "Label"，它们是【行以下第二层】的子控件，不是行标题。
            // 只按名字判会让它们全部命中 GetComponentInParent 找到的同一个 row，
            // 于是 "<" ">" "重置" 全都拿到 ui_setting_resolution 这种行 Key ——
            // 和 ValueText 是同一类错误，只是藏在更深一层。
            // 所以这里要求【直接父节点就是那一行】（SettingRowBase 的 Find 也只用直接子节点，
            // 行模板的子节点命名约定见 SettingRowBase 的注释）。
            if (string.Equals(text.transform.name, "Label", StringComparison.Ordinal))
            {
                Transform parent = text.transform.parent;
                SettingRowBase row = parent != null ? parent.GetComponent<SettingRowBase>() : null;
                if (row != null && !string.IsNullOrEmpty(row.settingId))
                {
                    return MakeSettingLabelKey(row.settingId);
                }
            }

            // ③ 完整层级路径（用整个场景做根，路径才完整）
            Transform root = text.transform.root;
            return BuildKey(assetShortName, BuildObjectPath(root, text.transform));
        }

        /// <summary>settingId → 标签 Key。与生成器此前的派生规则保持一致，配置无需重建。</summary>
        private static string MakeSettingLabelKey(string settingId)
        {
            int dot = settingId.IndexOf('.');
            string tail = (dot >= 0 && dot + 1 < settingId.Length) ? settingId.Substring(dot + 1) : settingId;
            return "ui_setting_" + tail.ToLowerInvariant();
        }

        /// <summary>
        /// 生成建议 Key：ui_ + 预制体短名 + _ + 【完整层级路径】的净化形式。
        /// 例：SettingPanel 的 ContentArea/Page_Graphics/.../Row_Resolution/Label
        ///     → ui_setting_contentarea_page_graphics_viewport_content_row_resolution_label
        ///
        /// ══════════════ 为什么必须用完整路径而不是末级物体名 ══════════════
        /// 面板里同名节点极多：SettingPanel 有二十多个叫 Label 的物体。
        /// 只取末级名会让它们全部算出同一个 Key —— 导入时被去重逻辑成批跳过，
        /// 而且真导进去也分不清哪个 Key 对应哪个控件。
        /// 完整路径虽然长，但唯一、可回溯（照着路径重开预制体就能找到那个物体）。
        ///
        /// 这仍然只是建议值：业务语义要靠人在窗口里逐条改。
        /// </summary>
        private static string BuildKey(string prefabName, string objectPath)
        {
            string shortName = prefabName;
            if (shortName.EndsWith("Panel", StringComparison.Ordinal))
            {
                shortName = shortName.Substring(0, shortName.Length - "Panel".Length);
            }

            //「Text (TMP)」这类节点名只表示"这里挂了个 TMP"，对 Key 没有信息量，去掉
            string path = objectPath
                .Replace("Text (TMP)", string.Empty)
                .Replace("(TMP)", string.Empty);

            return Sanitize("ui_" + shortName + "_" + path);
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool lastUnderscore = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char lower = char.ToLowerInvariant(c);
                if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
                {
                    sb.Append(lower);
                    lastUnderscore = false;
                }
                else if (!lastUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                    lastUnderscore = true;
                }
            }
            //去掉尾部多余的 _
            string r = sb.ToString();
            return r.TrimEnd('_');
        }

        private bool IsAlreadyInConfig(TextItem item)
        {
            if (_targetConfig == null) return false;
            if (!string.IsNullOrEmpty(item.Key) && _targetConfig.ContainsKey(item.Key)) return true;

            //也按中文文字认一遍：换 Key 但文字没变时不该再建一条重复的
            List<LocalizationEntry> entries = _targetConfig.entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].chineseSimplified == item.Text) return true;
            }
            return false;
        }

        // ══════════════════ 导入配置 ══════════════════

        private void ImportToConfig()
        {
            ImportToConfigInternal();
        }

        // ══════════════════ 供自动化 / MCP 驱动的入口 ══════════════════
        //
        // 窗口的正常用法是人点按钮。但"扫描 → 导入 → 回写"这套流程必须能被脚本驱动，
        // 否则没法做回归验证 —— 一百多条靠手点也验证不了。
        // 下面几个方法就是那套流程的无对话框版本，配合 PrefabTextCollectorWindow.SilentMode = true 使用。
        // 故意不设成 private：验证脚本要用。

        /// <summary>
        /// 跑一次完整扫描，然后把某个来源路径下的条目按 predicate 勾选。
        /// 返回该来源的条目总数（找不到这个来源返回 -1）。
        /// </summary>
        public int ScanAndSelectForTest(string sourcePath, Func<TextItem, bool> predicate)
        {
            RescanPreservingSelection();
            for (int g = 0; g < _groups.Count; g++)
            {
                if (!string.Equals(_groups[g].Path, sourcePath, StringComparison.Ordinal)) continue;
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    TextItem it = _groups[g].Items[i];
                    it.Selected = predicate == null || predicate(it);
                }
                return _groups[g].Items.Count;
            }
            return -1;
        }

        /// <summary>无对话框版本的导入。</summary>
        public void ImportToConfigForTest()
        {
            ImportToConfigInternal();
        }

        /// <summary>无对话框版本的回写。</summary>
        public void WriteComponentsForTest()
        {
            WriteComponents();
        }

        /// <summary>无对话框版本的清除（静默模式下 ClearSelectedItems 不弹确认框）。</summary>
        public void ClearSelectedForTest()
        {
            ClearSelectedItems();
        }

        /// <summary>当前勾选条数（验证脚本断言"操作后勾选还在不在"要用）。</summary>
        public int CountSelectedForTest()
        {
            return CountSelected();
        }

        /// <summary>
        /// 给这个实例指定本地化配置。
        ///
        /// 窗口正常使用时目标配置存在 EditorPrefs 里、由 OnEnable 读回来；脚本驱动的实例
        /// 没有这一段生命周期，所以必须能显式指定 —— 否则 _targetConfig 是 null，
        /// 导入会静默什么也不做。
        /// </summary>
        public void SetTargetConfigForTest(string configPath)
        {
            _targetConfig = AssetDatabase.LoadAssetAtPath<LocalizationData>(configPath);
        }

        /// <summary>取当前扫描结果里某个来源的条目（供验证脚本断言）。</summary>
        public List<TextItem> GetItemsForTest(string sourcePath)
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                if (string.Equals(_groups[g].Path, sourcePath, StringComparison.Ordinal)) return _groups[g].Items;
            }
            return null;
        }

        /// <summary>
        /// 诊断用：探测某个场景路径在【扫描结果】与【直接按路径查找】两条路下各能定位到几个物体。
        /// 两者数字不一致就说明定位逻辑有分歧（例如场景其实由多个根节点组成）。只读，不改任何东西。
        /// </summary>
        public static string ProbeSceneLookupForTest(string scenePath)
        {
            var sb = new StringBuilder();

            var w = CreateInstance<PrefabTextCollectorWindow>();
            try
            {
                int total = w.ScanAndSelectForTest(scenePath, null);
                if (total < 0) return "扫描结果里没有这个来源：" + scenePath;
                List<TextItem> items = w.GetItemsForTest(scenePath);

                UnityEngine.SceneManagement.Scene scene =
                    UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                bool loaded = scene.IsValid() && scene.isLoaded;
                GameObject[] roots = loaded ? scene.GetRootGameObjects() : null;
                sb.Append("场景已加载=").Append(loaded)
                  .Append("，根节点数=").Append(roots == null ? -1 : roots.Length).Append('\n');
                if (roots != null)
                {
                    for (int i = 0; i < roots.Length; i++)
                    {
                        if (roots[i] != null) sb.Append("  根：").Append(roots[i].name).Append('\n');
                    }
                }

                for (int i = 0; i < items.Count; i++)
                {
                    TextItem it = items[i];
                    Transform t = roots == null ? null : FindInSceneRoots(roots, it.ObjectPath);
                    sb.Append(t == null ? "查不到  " : "查得到  ").Append(it.ObjectPath).Append('\n');
                }
            }
            finally
            {
                DestroyImmediate(w);
            }
            return sb.ToString();
        }

        /// <summary>只勾选这些层级路径的条目 —— 给验证用，避免把上百条一次性写成资产。</summary>
        public static Func<TextItem, bool> OnlyForTest(params string[] objectPaths)
        {
            var set = new HashSet<string>(objectPaths);
            return it => it != null && set.Contains(it.ObjectPath);
        }

        /// <summary>
        /// 一次性跑完「扫描 → 导入 → 回写」，全程无对话框。给 MCP / 批处理调用。
        ///
        /// ══════════════ 为什么要"再选一次" ══════════════
        /// ImportToConfigInternal 结尾会调 RefreshConfigStatus，把这时已经进配置的条目全部取消勾选
        /// —— 界面上的正常行为（导入完就不该再勾着）。但自动化流程紧接着还要回写组件，
        /// 所以这里导入后必须按同一个 predicate 重新勾选，否则回写会以"没勾选任何条目"提前返回。
        ///
        /// 返回值是给人看的诊断文本，不是给程序解析的。
        /// </summary>
        public static string RunPipelineForTest(string scenePath, Func<TextItem, bool> predicate,
            bool writeComponents, string configPath)
        {
            var sb = new StringBuilder();
            var w = CreateInstance<PrefabTextCollectorWindow>();
            w._targetConfig = AssetDatabase.LoadAssetAtPath<LocalizationData>(configPath);
            if (w._targetConfig == null) return "配置没找到：" + configPath;

            bool oldSilent = SilentMode;
            SilentMode = true;
            try
            {
                int total = w.ScanAndSelectForTest(scenePath, predicate);
                if (total < 0) return "扫描结果里没有这个来源：" + scenePath;

                List<TextItem> items = w.GetItemsForTest(scenePath);
                for (int i = 0; i < items.Count; i++)
                {
                    TextItem it = items[i];
                    sb.Append(it.Selected ? "[选中] " : "[    ] ").Append(it.ObjectPath)
                      .Append("  →  ").Append(it.Key)
                      .Append("  (dup=").Append(it.KeyDuplicated)
                      .Append(", inConfig=").Append(it.AlreadyInConfig).Append(")\n");
                }

                w.ImportToConfigForTest();
                sb.Append("导入完成\n");

                if (writeComponents)
                {
                    //导入后勾选状态被清掉了，重新按同一条件选一次
                    w.ScanAndSelectForTest(scenePath, predicate);
                    w.WriteComponentsForTest();
                    sb.Append("回写完成\n");
                }
            }
            finally
            {
                SilentMode = oldSilent;
                DestroyImmediate(w);
            }
            return sb.ToString();
        }

        private void ImportToConfigInternal()
        {
            if (_targetConfig == null) return;

            var picked = new List<TextItem>();
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) picked.Add(_groups[g].Items[i]);
                }
            }
            if (picked.Count == 0)
            {
                Notify("导入到配置", "没有勾选任何条目。");
                return;
            }

            var byKey = new Dictionary<string, TextItem>();
            var order = new List<string>();
            int skipped = 0;
            for (int i = 0; i < picked.Count; i++)
            {
                TextItem it = picked[i];
                if (string.IsNullOrEmpty(it.Key)) { skipped++; continue; }
                if (!byKey.ContainsKey(it.Key))
                {
                    byKey[it.Key] = it;
                    order.Add(it.Key);
                }
                else if (byKey[it.Key].Text != it.Text)
                {
                    //同一个 Key 对应不同的文字：不能静默合并，让用户去改 Key
                    skipped++;
                }
            }

            int added = 0, filled = 0;
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                TextItem it = byKey[key];

                LocalizationEntry entry = _targetConfig.GetEntry(key);
                if (entry != null)
                {
                    //只补空的语言列，绝不覆盖已有译文
                    if (string.IsNullOrEmpty(entry.chineseSimplified))
                    {
                        entry.chineseSimplified = it.Text;
                        filled++;
                    }
                    if (string.IsNullOrEmpty(entry.description))
                    {
                        entry.description = it.PrefabName + " › " + it.ObjectPath;
                    }
                }
                else
                {
                    _targetConfig.entries.Add(new LocalizationEntry
                    {
                        key = key,
                        description = it.PrefabName + " › " + it.ObjectPath,
                        chineseSimplified = it.Text,
                    });
                    added++;
                }
            }

            EditorUtility.SetDirty(_targetConfig);
            AssetDatabase.SaveAssets();

            string msg = "新增 " + added + " 条，补充中文 " + filled + " 条。";
            if (skipped > 0) msg += "\n\n跳过 " + skipped + " 条（Key 为空，或同一 Key 对应了不同文字）。";
            msg += "\n\n注意：英文列仍需手工翻译 —— 留空会导致语言切换时回退显示中文。";
            Notify("导入到配置", msg);

            ChaosLog.Info(LogChannel.Localization, "面板文字导入完成：" + msg.Replace("\n", " "));

            // ══════════════ 勾选保持不动 ══════════════
            // 这里【不清空勾选】：用户常常要连着做"导入 → 回写组件"两步，
            // 中间被清掉就得重新勾一遍。只重算「已存在」标记，让状态列跟着配置更新。
            RefreshConfigStatus();
        }

        private void RefreshConfigStatus()
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    TextItem it = _groups[g].Items[i];
                    it.AlreadyInConfig = IsAlreadyInConfig(it);
                }
            }
        }

        // ══════════════════ 勾选状态的保留 ══════════════════
        //
        // ══════════════ 为什么需要"先记下来再恢复" ══════════════
        // 好几个操作（导入、回写、清除）结束后都要重扫或重算状态，而重扫是【重建整个列表】：
        // 旧的对象连同它们的 Selected 一起被丢掉，界面上表现为"点完按钮，勾选全没了"。
        // 用户往往要连着做几步（导入 → 回写 → 再挑几条处理），每次都重新勾一遍非常烦。
        //
        // 所以这些操作统一走这个模式：操作前用 GetSelectedPaths 记下勾选，
        // 操作后调 RestoreSelection 按路径还原。用路径而不是索引，是因为重扫后条目顺序可能变。
        private List<string> GetSelectedPaths()
        {
            var list = new List<string>();
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) list.Add(MakeItemId(_groups[g].Items[i]));
                }
            }
            return list;
        }

        private void RestoreSelection(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return;
            var set = new HashSet<string>(ids);
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (set.Contains(MakeItemId(_groups[g].Items[i]))) _groups[g].Items[i].Selected = true;
                }
            }
        }

        /// <summary>条目的唯一标识：来源路径 + 层级路径。同一个物体在两个来源里出现的可能不存在，但这样最稳。</summary>
        private static string MakeItemId(TextItem it)
        {
            return it.PrefabPath + "|" + it.ObjectPath;
        }

        /// <summary>
        /// 重扫一遍并尽力保留勾选。
        ///
        /// 注意 Scan 会重建列表，所以【调用方传进来的 TextItem 引用在调用后就失效了】，
        /// 需要继续用某条目的属性时必须重新按路径取。这也是这里只接受 id 列表的原因。
        /// </summary>
        private void RescanPreservingSelection()
        {
            List<string> selected = GetSelectedPaths();
            Scan();
            RestoreSelection(selected);
        }

        /// <summary>当前勾选条数（界面按钮的禁用判断要用）。</summary>
        private int CountSelected()
        {
            int n = 0;
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) n++;
                }
            }
            return n;
        }

        // ══════════════════ 写入 LocalizedText 组件 ══════════════════

        /// <summary>
        /// 把勾选条目的 Key 写进对应物体上的 LocalizedText 组件（没有就加一个）。
        ///
        /// ══════════════ 两种来源，两套写法 ══════════════
        /// 预制体和场景的改法不同，这是本方法唯一复杂的地方：
        ///   · 【预制体】走 LoadPrefabContents 隔离场景 → 改 → SaveAsPrefabAsset 存回。
        ///   · 【场景】直接改【已加载的那个场景实例】→ MarkSceneDirty → SaveScene。
        ///     不能对场景做 LoadPrefabContents（那不是预制体），也不能用 write 类工具直接改
        ///     .unity 文本 —— Unity 内存里的版本会在下次保存时覆盖磁盘，改动静默丢失。
        ///
        /// ══════════════ 场景改完必须自己存 ══════════════
        /// MarkSceneDirty 只是打脏标记，不落盘。用户下次直接关编辑器时如果选了"不保存"，
        /// 这次的写入就白做了。所以这里改完立刻 SaveScene。
        ///
        /// 只有"我们自己 Additive 打开的场景"或"用户本来就打开的场景"会被处理：
        /// 扫描阶段（ScanScene）打开的场景在扫描结束时就关掉了，所以这里通常是【重新打开】
        /// 或直接命中用户本来就开着的那个场景。两种情况下改完都由本方法负责落盘。
        /// </summary>
        private void WriteComponents()
        {
            var picked = new List<TextItem>();
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) picked.Add(_groups[g].Items[i]);
                }
            }
            if (picked.Count == 0)
            {
                Notify("写入组件", "没有勾选任何条目。");
                return;
            }

            //按来源路径归组：一个预制体只开关一次，否则隔离场景会被反复创建
            var byAsset = new Dictionary<string, List<TextItem>>();
            for (int i = 0; i < picked.Count; i++)
            {
                List<TextItem> list;
                if (!byAsset.TryGetValue(picked[i].PrefabPath, out list))
                {
                    list = new List<TextItem>();
                    byAsset[picked[i].PrefabPath] = list;
                }
                list.Add(picked[i]);
            }

            int addedCount = 0, updatedCount = 0, missingCount = 0;
            var dirtyScenes = new List<UnityEngine.SceneManagement.Scene>();

            AssetDatabase.StartAssetEditing();//批量改预制体期间暂停导入，快很多
            try
            {
                foreach (KeyValuePair<string, List<TextItem>> kv in byAsset)
                {
                    if (IsScenePath(kv.Key))
                    {
                        WriteSceneItems(kv.Key, kv.Value, dirtyScenes, ref addedCount, ref updatedCount, ref missingCount);
                    }
                    else
                    {
                        WritePrefabItems(kv.Key, kv.Value, ref addedCount, ref updatedCount, ref missingCount);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // 场景落盘放在 StartAssetEditing 之外：SaveScene 会自己触发一轮导入，
            // 夹在批量编辑里容易被忽略掉。
            for (int i = 0; i < dirtyScenes.Count; i++)
            {
                try
                {
                    EditorSceneManager.SaveScene(dirtyScenes[i]);
                    ChaosLog.Info(LogChannel.Localization, "已保存场景：" + dirtyScenes[i].path);
                }
                catch (Exception e)
                {
                    ChaosLog.Error(LogChannel.Localization, "保存场景失败：" + e.Message);
                }
            }

            string msg = "新增组件 " + addedCount + " 个，更新 Key " + updatedCount + " 个。";
            if (dirtyScenes.Count > 0) msg += "\n\n已保存 " + dirtyScenes.Count + " 个场景。";
            if (missingCount > 0) msg += "\n\n有 " + missingCount + " 条没找到对应物体（层级路径可能已变），已跳过。";
            Notify("写入 LocalizedText 组件", msg);

            ChaosLog.Info(LogChannel.Localization, "面板文字组件写入完成：" + msg.Replace("\n", " "));

            //勾选保持不动，只把状态列重算一遍（组件刚被写过，Key 可能与配置不再一致）
            RefreshConfigStatus();
        }

        // ══════════════════ 清除勾选项 ══════════════════

        /// <summary>
        /// 把勾选的条目【双向撤掉】：从本地化配置里删掉对应条目，并摘掉物体上的 LocalizedText 组件。
        ///
        /// ══════════════ 为什么两件事必须一起做 ══════════════
        /// 只删配置：物体上的 LocalizedText 还在，切语言时它会去查一个不存在的 Key，
        /// 而 LocalizationManager 找不到条目时是【把 Key 本身当文本返回】——
        /// 界面上就会出现 `ui_setting_bottombar_returnbtn_label` 这种字符串，比不本地化更糟。
        /// 只摘组件：配置里留一堆没有控件引用的孤儿条目，越攒越多。
        ///
        /// ══════════════ 摘组件为什么不用删节点 ══════════════
        /// 只移除组件，不动层级与文字。物体上的 TMP 文字保持原样，
        /// 所以撤掉之后界面退回显示预制体里烘焙的中文原文 —— 这是"未接本地化"的正常状态。
        ///
        /// ══════════════ 破坏性操作，会先确认 ══════════════
        /// 删的是配置条目（可能已经翻译好几种语言），所以默认要确认。
        /// SilentMode 下（脚本驱动）跳过确认，由调用方自己把关。
        /// </summary>
        private void ClearSelectedItems()
        {
            if (_targetConfig == null) return;

            var picked = new List<TextItem>();
            for (int g = 0; g < _groups.Count; g++)
            {
                for (int i = 0; i < _groups[g].Items.Count; i++)
                {
                    if (_groups[g].Items[i].Selected) picked.Add(_groups[g].Items[i]);
                }
            }
            if (picked.Count == 0)
            {
                Notify("清除勾选项", "没有勾选任何条目。");
                return;
            }

            if (!SilentMode)
            {
                bool ok = EditorUtility.DisplayDialog("确认清除",
                    "将清除勾选的 " + picked.Count + " 条：\n" +
                    "· 从配置 " + _targetConfig.name + " 里删除对应条目\n" +
                    "· 并摘掉物体上的 LocalizedText 组件\n\n" +
                    "此操作不可撤销（删除的译文无法恢复）。",
                    "清除", "取消");
                if (!ok) return;
            }

            // ══════════════ 先按来源归组，一个资产只开关一次 ══════════════
            var byAsset = new Dictionary<string, List<TextItem>>();
            for (int i = 0; i < picked.Count; i++)
            {
                List<TextItem> list;
                if (!byAsset.TryGetValue(picked[i].PrefabPath, out list))
                {
                    list = new List<TextItem>();
                    byAsset[picked[i].PrefabPath] = list;
                }
                list.Add(picked[i]);
            }

            // ══════════════ 配置条目：先把该删的 Key 收齐，再统一删 ══════════════
            // 不能边遍历条目边删：删完索引就错位了。而且"某些条目被多个控件共用"是正常的
            // （比如多行共用一个"重置"），所以按 Key 去重后一次删干净。
            var keysToRemove = new HashSet<string>();
            for (int i = 0; i < picked.Count; i++)
            {
                if (!string.IsNullOrEmpty(picked[i].Key)) keysToRemove.Add(picked[i].Key);
            }

            int removedEntries = 0;
            for (int i = _targetConfig.entries.Count - 1; i >= 0; i--)
            {
                LocalizationEntry e = _targetConfig.entries[i];
                if (e != null && keysToRemove.Contains(e.key))
                {
                    _targetConfig.entries.RemoveAt(i);
                    removedEntries++;
                }
            }
            EditorUtility.SetDirty(_targetConfig);

            int removedComponents = 0, missing = 0;
            var dirtyScenes = new List<UnityEngine.SceneManagement.Scene>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (KeyValuePair<string, List<TextItem>> kv in byAsset)
                {
                    if (IsScenePath(kv.Key))
                    {
                        RemoveLocalizedTextInScene(kv.Key, kv.Value, dirtyScenes, ref removedComponents, ref missing);
                    }
                    else
                    {
                        RemoveLocalizedTextInPrefab(kv.Key, kv.Value, ref removedComponents, ref missing);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            for (int i = 0; i < dirtyScenes.Count; i++)
            {
                try { EditorSceneManager.SaveScene(dirtyScenes[i]); }
                catch (Exception e) { ChaosLog.Error(LogChannel.Localization, "保存场景失败：" + e.Message); }
            }

            string msg = "删除配置条目 " + removedEntries + " 条，移除 LocalizedText 组件 " + removedComponents + " 个。";
            if (missing > 0) msg += "\n\n有 " + missing + " 条没找到对应物体或组件，已跳过。";
            msg += "\n\n这些控件已回到\"未接本地化\"状态，界面会显示预制体里烘焙的中文原文。";
            Notify("清除勾选项", msg);

            ChaosLog.Info(LogChannel.Localization, "清除完成：" + msg.Replace("\n", " "));

            // ══════════════ 重扫并保留勾选 ══════════════
            // 必须重扫：条目没了 → AlreadyInConfig 变了、Key 建议值也会变（组件已被摘掉）。
            // 保留勾选是为了让用户能接着操作同一批条目（比如误删了想重新导入）。
            RescanPreservingSelection();
            RefreshConfigStatus();
        }

        /// <summary>在预制体里摘掉 LocalizedText 组件。</summary>
        private void RemoveLocalizedTextInPrefab(string prefabPath, List<TextItem> items,
            ref int removedComponents, ref int missing)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) { missing += items.Count; return; }

                bool dirty = false;
                for (int i = 0; i < items.Count; i++)
                {
                    Transform target = root.transform.Find(items[i].ObjectPath);
                    if (target == null) { missing++; continue; }

                    int n = RemoveLocalizedText(target);
                    if (n == 0) { missing++; continue; }
                    removedComponents += n;
                    dirty = true;
                }

                if (dirty) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>在场景里摘掉 LocalizedText 组件。与 WriteSceneItems 同一套"改完自己存"的约定。</summary>
        private void RemoveLocalizedTextInScene(string scenePath, List<TextItem> items,
            List<UnityEngine.SceneManagement.Scene> dirtyScenes, ref int removedComponents, ref int missing)
        {
            if (Application.isPlaying)
            {
                ChaosLog.Warn(LogChannel.Localization, "正在播放模式，跳过场景清除：" + scenePath);
                missing += items.Count;
                return;
            }

            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
                catch (Exception e)
                {
                    ChaosLog.Error(LogChannel.Localization, "打开场景失败，跳过清除 " + scenePath + "：" + e.Message);
                    missing += items.Count;
                    return;
                }
            }

            GameObject[] roots = scene.GetRootGameObjects();
            bool dirty = false;
            for (int i = 0; i < items.Count; i++)
            {
                Transform target = FindInSceneRoots(roots, items[i].ObjectPath);
                if (target == null) { missing++; continue; }

                int n = RemoveLocalizedText(target);
                if (n == 0) { missing++; continue; }
                removedComponents += n;
                dirty = true;
            }

            if (dirty && !dirtyScenes.Contains(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                dirtyScenes.Add(scene);
            }
        }

        /// <summary>
        /// 摘掉一个物体上的全部 LocalizedText，返回摘掉的个数。
        ///
        /// 用 DestroyImmediate 而不是 Destroy：这里是编辑器、且不在播放中，
        /// Destroy 会把销毁推迟到帧末，紧接着的 SaveAsPrefabAsset 会把还没销毁的组件一起存下去。
        /// 允许"摘多个"是为了顺手清掉历史上误挂出来的重复组件。
        /// </summary>
        private static int RemoveLocalizedText(Transform target)
        {
            LocalizedText[] all = target.GetComponents<LocalizedText>();
            for (int i = 0; i < all.Length; i++) UnityEngine.Object.DestroyImmediate(all[i], true);
            return all.Length;
        }

        /// <summary>场景资产的扩展名判定 —— 与预制体分派用。</summary>
        private static bool IsScenePath(string path)
        {
            return !string.IsNullOrEmpty(path)
                   && path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>写一个预制体。隔离场景保证不会误改到场景里的实例。</summary>
        private void WritePrefabItems(string prefabPath, List<TextItem> items,
            ref int addedCount, ref int updatedCount, ref int missingCount)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) { missingCount += items.Count; return; }

                bool dirty = false;
                for (int i = 0; i < items.Count; i++)
                {
                    TextItem it = items[i];
                    if (string.IsNullOrEmpty(it.Key)) { missingCount++; continue; }

                    Transform target = root.transform.Find(it.ObjectPath);
                    if (target == null) { missingCount++; continue; }

                    int r = ApplyLocalizedText(target, it.Key);
                    if (r == ResultMissing) missingCount++;
                    else if (r == ResultAdded) { addedCount++; dirty = true; }
                    else { updatedCount++; dirty = true; }
                }

                if (dirty) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 写一个场景。定位用的是【已加载的场景实例】，所以：
        ///   · 用户本来就打开着这个场景 → 直接改它（最理想，改完看得见）；
        ///   · 没打开 → 这里 Additive 打开一次。
        /// 扫描阶段虽然也 Additive 打开过，但 ScanScene 结束时就关掉了，所以通常走第二种。
        ///
        /// ⚠ 改完【不关】这个场景：关掉会顺带丢掉用户的场景视图状态（相机、折叠），
        /// 而且用户可能正想看看写进去的结果。代价是它会一直留在 Hierarchy 里，
        /// 由用户自己决定何时关。这一点写在这里，免得以后有人以为是漏了 CloseScene。
        ///
        /// 不碰预制体实例：扫描阶段已经把它们排除在外了，这里再挡一道，
        /// 免得手动改了组的数据导致误改预制体（那会变成 Prefab Override，问题很难查）。
        /// </summary>
        private void WriteSceneItems(string scenePath, List<TextItem> items, List<UnityEngine.SceneManagement.Scene> dirtyScenes,
            ref int addedCount, ref int updatedCount, ref int missingCount)
        {
            if (Application.isPlaying)
            {
                ChaosLog.Warn(LogChannel.Localization,
                    "正在播放模式，跳过场景写入（改的是运行时副本，不会保存）：" + scenePath);
                missingCount += items.Count;
                return;
            }

            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
                catch (Exception e)
                {
                    ChaosLog.Error(LogChannel.Localization, "打开场景失败，跳过写入 " + scenePath + "：" + e.Message);
                    missingCount += items.Count;
                    return;
                }
            }

            GameObject[] roots = scene.GetRootGameObjects();
            bool dirty = false;

            for (int i = 0; i < items.Count; i++)
            {
                TextItem it = items[i];
                if (string.IsNullOrEmpty(it.Key)) { missingCount++; continue; }

                Transform target = FindInSceneRoots(roots, it.ObjectPath);
                if (target == null) { missingCount++; continue; }

                if (PrefabUtility.GetPrefabInstanceStatus(target.gameObject) != PrefabInstanceStatus.NotAPrefab)
                {
                    ChaosLog.Warn(LogChannel.Localization,
                        "跳过预制体实例内的物体（应由预制体自己本地化）：" + it.ObjectPath);
                    missingCount++;
                    continue;
                }

                int r = ApplyLocalizedText(target, it.Key);
                if (r == ResultMissing) missingCount++;
                else if (r == ResultAdded) { addedCount++; dirty = true; }
                else { updatedCount++; dirty = true; }
            }

            if (dirty && !dirtyScenes.Contains(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                dirtyScenes.Add(scene);
            }
        }

        /// <summary>在场景的根节点列表里按层级路径找物体；路径为空表示根节点自身。</summary>
        private static Transform FindInSceneRoots(GameObject[] roots, string objectPath)
        {
            if (roots == null || roots.Length == 0) return null;

            if (string.IsNullOrEmpty(objectPath)) return roots[0] != null ? roots[0].transform : null;

            int slash = objectPath.IndexOf('/');
            string head = slash < 0 ? objectPath : objectPath.Substring(0, slash);
            string tail = slash < 0 ? string.Empty : objectPath.Substring(slash + 1);

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null || !string.Equals(roots[i].name, head, StringComparison.Ordinal)) continue;
                if (tail.Length == 0) return roots[i].transform;

                Transform found = roots[i].transform.Find(tail);
                if (found != null) return found;
            }
            return null;
        }

        private const int ResultMissing = 0;
        private const int ResultAdded = 1;
        private const int ResultUpdated = 2;

        /// <summary>
        /// 给一个物体挂上/更新 LocalizedText。返回 Result* 之一。
        ///
        /// ══════════════ 为什么必须挡掉非 UGUI 文本 ══════════════
        /// LocalizedText 上有 [RequireComponent(typeof(TextMeshProUGUI))]。
        /// 对着【世界空间文字】（TextMeshPro 而不是 TextMeshProUGUI）调 AddComponent 时，
        /// Unity 会自动再补一个 TextMeshProUGUI 上去，而这个组件是不会被渲染的：
        /// 结果是物体上多了一个隐形的文字组件，LocalizedText 又只认它、不认原来那个 ——
        /// 看起来"本地化完全没生效"，而且报错都没有。所以这里直接拒绝，并让调用方计入 skipped。
        /// </summary>
        private static int ApplyLocalizedText(Transform target, string key)
        {
            if (target.GetComponent<TMP_Text>() == null) return ResultMissing;
            if (target.GetComponent<TextMeshProUGUI>() == null) return ResultMissing;

            // ══════════════ 先清掉重复的 LocalizedText ══════════════
            // 同一个物体上挂两个 LocalizedText 是有害且【极难发现】的：
            //   · GetComponent 只返回第一个，于是"写进去的那个"与"运行时实际用的那个"可能不是同一个；
            //   · UnityEvent 的序列化绑定也可能落到另一个上。
            // 现象是"收集器报告写入成功，界面却始终不翻译"，而 Inspector 上两个组件长得一模一样。
            // 这个坑真实发生过一次（生成器的 NewText 与 NewLabel 各挂了一个），
            // 所以这里主动收敛成一个，宁可多花一次遍历，也不要再出一次这种哑巴故障。
            LocalizedText[] all = target.GetComponents<LocalizedText>();
            if (all.Length > 1)
            {
                ChaosLog.Warn(LogChannel.Localization,
                    target.name + " 上挂了 " + all.Length + " 个 LocalizedText，已清理多余的（保留第一个）。" +
                    "重复挂载通常来自生成器脚本 —— 请检查它是否在两个地方都 AddComponent。");
                for (int i = 1; i < all.Length; i++) UnityEngine.Object.DestroyImmediate(all[i], true);
            }
            LocalizedText lt = all.Length > 0 ? all[0] : null;

            int result = lt == null ? ResultAdded : ResultUpdated;
            if (lt == null) lt = target.gameObject.AddComponent<LocalizedText>();

            lt.localizationKey = key;
            lt.autoUpdateOnStart = true;
            lt.listenToLanguageChange = true;
            return result;
        }
    }
}
