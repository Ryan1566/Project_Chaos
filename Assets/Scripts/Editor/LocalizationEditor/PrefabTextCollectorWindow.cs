using System;
using System.Collections.Generic;
using System.Text;
using ChaosDebug;
using TMPro;
using UnityEditor;
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

        /// <summary>一条「预制体里的文字」。</summary>
        private class TextItem
        {
            public string PrefabPath;      // Assets/... 完整路径
            public string PrefabName;      // 文件名（不含扩展名）
            public string ObjectPath;      // 相对预制体根的层级路径
            public string Text;            // Trim 之后的文字
            public bool HadWhitespace;     // 原文有首尾空白（导入时会提示）
            public string Key;             // 当前 Key（可在窗口里改）
            public bool Selected;
            public bool AlreadyInConfig;   // 目标配置里已有同 Key 或同中文的条目
            public bool KeyDuplicated;     // 本次结果里 Key 撞了
        }

        /// <summary>按预制体分组，便于折叠查看。</summary>
        private class PrefabGroup
        {
            public string Path;
            public string Name;
            public bool Foldout = true;
            public readonly List<TextItem> Items = new List<TextItem>();
        }

        private readonly List<string> _searchPaths = new List<string>();
        private readonly List<PrefabGroup> _groups = new List<PrefabGroup>();
        private LocalizationData _targetConfig;
        private Vector2 _scroll;
        private bool _scanned;

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
            EditorGUILayout.LabelField("搜索路径（扫描这些目录下的所有预制体）", EditorStyles.boldLabel);

            int removeAt = -1;
            for (int i = 0; i < _searchPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _searchPaths[i] = EditorGUILayout.TextField(_searchPaths[i]);
                if (GUILayout.Button("移除", GUILayout.Width(52f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeAt >= 0) _searchPaths.RemoveAt(removeAt);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 添加路径", GUILayout.Width(100f))) _searchPaths.Add("Assets/");
            if (GUILayout.Button("恢复 GlobalPath 默认", GUILayout.Width(160f))) ResetSearchPathsToDefault();
            EditorGUILayout.EndHorizontal();

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
                EditorGUILayout.HelpBox("请指定一个 LocalizationData 资产（例如 Data/Localization/UILocalizationConfig.asset）。",
                    MessageType.Warning);
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
            MessageType type;
            if (item.KeyDuplicated) { badge = "Key 重复"; type = MessageType.Error; }
            else if (item.AlreadyInConfig) { badge = "已存在"; type = MessageType.None; }
            else { badge = "新增"; type = MessageType.None; }

            Color old = GUI.color;
            if (item.KeyDuplicated) GUI.color = new Color(1f, 0.5f, 0.5f);
            else if (!item.AlreadyInConfig) GUI.color = new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.LabelField(badge, EditorStyles.miniLabel, GUILayout.Width(58f));
            GUI.color = old;

            if (item.HadWhitespace)
            {
                EditorGUILayout.LabelField("原文含首尾空白", EditorStyles.miniLabel, GUILayout.Width(100f));
            }

            EditorGUILayout.EndHorizontal();
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

            if (GUILayout.Button("扫描", GUILayout.Height(30f))) Scan();

            using (new EditorGUI.DisabledScope(_targetConfig == null || !_scanned))
            {
                if (GUILayout.Button("导入到配置", GUILayout.Height(30f))) ImportToConfig();
                if (GUILayout.Button("写入 LocalizedText 组件", GUILayout.Height(30f))) WriteComponents();
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
                if (!AssetDatabase.IsValidFolder(path))
                {
                    ChaosLog.Warn(LogChannel.Localization, "扫描路径不存在，已跳过：" + path);
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                for (int i = 0; i < guids.Length; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    PrefabGroup group = ScanPrefab(prefabPath);
                    if (group != null && group.Items.Count > 0) _groups.Add(group);
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

                TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts == null || texts.Length == 0) return null;

                var group = new PrefabGroup
                {
                    Path = prefabPath,
                    Name = System.IO.Path.GetFileNameWithoutExtension(prefabPath),
                };

                for (int i = 0; i < texts.Length; i++)
                {
                    TextMeshProUGUI t = texts[i];
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
                        Key = BuildKey(group.Name, BuildObjectPath(root.transform, t.transform)),
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

            //重新算一遍「已存在」，让界面状态和配置对齐
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
                    if (it.AlreadyInConfig) it.Selected = false;
                }
            }
        }

        // ══════════════════ 写入 LocalizedText 组件 ══════════════════

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

            //按预制体归组：一个预制体只开关一次，否则隔离场景会被反复创建
            var byPrefab = new Dictionary<string, List<TextItem>>();
            for (int i = 0; i < picked.Count; i++)
            {
                List<TextItem> list;
                if (!byPrefab.TryGetValue(picked[i].PrefabPath, out list))
                {
                    list = new List<TextItem>();
                    byPrefab[picked[i].PrefabPath] = list;
                }
                list.Add(picked[i]);
            }

            int addedCount = 0, updatedCount = 0, missingCount = 0;

            AssetDatabase.StartAssetEditing();//批量改预制体期间暂停导入，快很多
            try
            {
                foreach (KeyValuePair<string, List<TextItem>> kv in byPrefab)
                {
                    GameObject root = null;
                    try
                    {
                        root = PrefabUtility.LoadPrefabContents(kv.Key);
                        if (root == null) { missingCount += kv.Value.Count; continue; }

                        bool dirty = false;
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            TextItem it = kv.Value[i];
                            Transform target = root.transform.Find(it.ObjectPath);
                            if (target == null) { missingCount++; continue; }

                            LocalizedText lt = target.GetComponent<LocalizedText>();
                            if (lt == null)
                            {
                                lt = target.gameObject.AddComponent<LocalizedText>();
                                addedCount++;
                            }
                            else
                            {
                                updatedCount++;
                            }
                            lt.localizationKey = it.Key;
                            lt.autoUpdateOnStart = true;
                            lt.listenToLanguageChange = true;
                            dirty = true;
                        }

                        if (dirty) PrefabUtility.SaveAsPrefabAsset(root, kv.Key);
                    }
                    finally
                    {
                        if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string msg = "新增组件 " + addedCount + " 个，更新 Key " + updatedCount + " 个。";
            if (missingCount > 0) msg += "\n\n有 " + missingCount + " 条没找到对应物体（层级路径可能已变），已跳过。";
            Notify("写入 LocalizedText 组件", msg);

            ChaosLog.Info(LogChannel.Localization, "面板文字组件写入完成：" + msg.Replace("\n", " "));
        }
    }
}
