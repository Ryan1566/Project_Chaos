using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace LocalizationSystem.Editor
{
    /// <summary>
    /// 语言字体映射配置工具。菜单：Tools/本地化/语言字体映射
    ///
    /// ══════════════════════ 它解决什么 ══════════════════════
    /// 运行时的换字体逻辑（LocalizedTextFont + LanguageFontMap）只认"哪种语言用哪个字体"，
    /// 而这份对照表要人来定：项目里有哪几种字体、各自覆盖哪些字形，只有看图/看资产才知道。
    /// 这个窗口把三件事放到一起：
    ///   · 逐语言挑字体；
    ///   · 【覆盖自检】：这个字体到底有没有这种语言的必要字形（没有就是方框）；
    ///   · 【一键补全】：把映射表和 LocalizedTextFont 组件铺到所有预制体上。
    ///
    /// ══════════════════════ 为什么"覆盖自检"是重点 ══════════════════════
    /// 换字体的目的是"让这个语言显示得出来"。而字体缺字不会报错、只会画成方框，
    /// 所以配错了往往要等到有人切到那个语言才发现。这里用几个代表性字符去问字体有没有，
    /// 能提前把这种错误挡下来。
    /// </summary>
    public class LanguageFontMapWindow : EditorWindow
    {
        private const string DefaultMapPath = "Assets/Data/Localization/LanguageFontMap.asset";

        private LanguageFontMap _map;
        private Vector2 _scroll;
        private List<TMP_FontAsset> _fonts = new List<TMP_FontAsset>();
        private string _lastResult;

        [MenuItem("Tools/本地化/语言字体映射", false, 60)]
        public static void ShowWindow()
        {
            var w = GetWindow<LanguageFontMapWindow>("语言字体映射");
            w.minSize = new Vector2(620f, 520f);
            w.Show();
        }

        private void OnEnable()
        {
            _map = AssetDatabase.LoadAssetAtPath<LanguageFontMap>(DefaultMapPath);
            RefreshFonts();
        }

        private void RefreshFonts()
        {
            _fonts.Clear();
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (f != null) _fonts.Add(f);
            }
            _fonts.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("映射表资产", EditorStyles.boldLabel);
            _map = (LanguageFontMap)EditorGUILayout.ObjectField(_map, typeof(LanguageFontMap), false);

            if (_map == null)
            {
                EditorGUILayout.HelpBox(
                    "还没有映射表。点下面的按钮在 " + DefaultMapPath + " 建一张，然后逐语言挑字体。",
                    MessageType.Info);
                if (GUILayout.Button("创建映射表资产", GUILayout.Height(28f)))
                {
                    _map = CreateMap();
                }
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("路径：" + AssetDatabase.GetAssetPath(_map), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("刷新字体列表（重新扫描 Assets 下的 TMP 字体）", GUILayout.Height(24f)))
            {
                RefreshFonts();
                _lastResult = null;
            }

            EditorGUILayout.Space(4f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ══════════════ 逐语言一行 ══════════════
            if (_map.EnsureAllLanguages())
            {
                EditorUtility.SetDirty(_map);
            }

            Array languages = Enum.GetValues(typeof(LanguageType));
            for (int i = 0; i < languages.Length; i++)
            {
                LanguageType lang = (LanguageType)languages.GetValue(i);
                LanguageFontMap.Entry entry = FindEntry(lang);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(lang.ToString(), EditorStyles.boldLabel, GUILayout.Width(160f));

                TMP_FontAsset picked = (TMP_FontAsset)EditorGUILayout.ObjectField(
                    entry == null ? null : entry.font, typeof(TMP_FontAsset), false);
                if (entry != null && picked != entry.font)
                {
                    entry.font = picked;
                    EditorUtility.SetDirty(_map);
                }

                // 覆盖自检：这个字体有没有这种语言的代表性字形
                string verdict = DescribeCoverage(lang, entry == null ? null : entry.font);
                EditorGUILayout.LabelField(verdict, GUILayout.Width(190f));

                EditorGUILayout.EndHorizontal();

                if (entry != null)
                {
                    EditorGUI.BeginChangeCheck();
                    string note = EditorGUILayout.TextField("备注", entry.note);
                    if (EditorGUI.EndChangeCheck()) { entry.note = note; EditorUtility.SetDirty(_map); }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);

            // ══════════════ 一键补全 ══════════════
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("铺到预制体", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "给 Assets 下【所有带 LocalizedText 的预制体】补上 LocalizedTextFont 组件，并指向这张映射表。\n" +
                "已经有的只更新引用，不会重复添加。",
                EditorStyles.miniLabel);

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.75f, 0.9f, 1f);
            if (GUILayout.Button("补全所有预制体（LocalizedText → +LocalizedTextFont）", GUILayout.Height(30f)))
            {
                _lastResult = PatchAllPrefabs(_map);
            }
            GUI.backgroundColor = old;
            EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }
        }

        private LanguageFontMap.Entry FindEntry(LanguageType lang)
        {
            for (int i = 0; i < _map.entries.Count; i++)
            {
                if (_map.entries[i] != null && _map.entries[i].language == lang) return _map.entries[i];
            }
            return null;
        }

        // ══════════════════ 覆盖自检 ══════════════════

        /// <summary>
        /// 每种语言用来探测的字符：挑"这个语言里绝不会缺席"的那些字。
        /// 只要有一个没有，就说明这个字体不足以撑起这种语言（界面上会出方框）。
        /// </summary>
        private static char[] ProbeChars(LanguageType lang)
        {
            switch (lang)
            {
                case LanguageType.ChineseSimplified: return new[] { '设', '置', '开', '始' };
                case LanguageType.ChineseTraditional: return new[] { '設', '置', '開', '始' };
                case LanguageType.English: return new[] { 'A', 'a', '0' };
                case LanguageType.Japanese: return new[] { 'あ', 'ア', '漢', '字' };
                case LanguageType.Korean: return new[] { '한', '글', '설', '정' };
                default: return new[] { 'A' };
            }
        }

        private static string DescribeCoverage(LanguageType lang, TMP_FontAsset font)
        {
            if (font == null) return "未配置（保留原字体）";

            char[] probes = ProbeChars(lang);
            int missing = 0;
            var missList = new StringBuilder();
            for (int i = 0; i < probes.Length; i++)
            {
                if (!font.HasCharacter(probes[i]))
                {
                    missing++;
                    if (missList.Length < 6) missList.Append(probes[i]);
                }
            }

            if (missing == 0) return "✔ 覆盖 OK";
            return "✘ 缺 " + missing + "/" + probes.Length + " 个探针字：" + missList;
        }

        // ══════════════════ 建资产 ══════════════════

        private static LanguageFontMap CreateMap()
        {
            string dir = System.IO.Path.GetDirectoryName(DefaultMapPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogError("[语言字体] 目录不存在：" + dir + "，请在 Project 里先建好再试。");
                return null;
            }

            var map = CreateInstance<LanguageFontMap>();
            map.EnsureAllLanguages();
            AssetDatabase.CreateAsset(map, DefaultMapPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[语言字体] 已创建映射表：" + DefaultMapPath + "，请在窗口里为每种语言指定字体。");
            return map;
        }

        // ══════════════════ 铺到预制体 ══════════════════

        /// <summary>
        /// 扫描 Assets 下所有预制体，给每个带 LocalizedText 的对象补一个 LocalizedTextFont，
        /// 并把 map 指向给定映射表。
        ///
        /// ══════════════ 为什么需要"铺"这一步 ══════════════
        /// LocalizedTextFont 是独立组件（这样纯符号文字也能单独用），代价是已有的 prefab
        /// 上都没有它。而本工程的 UI prefab 是手工维护的（生成器已删），
        /// 一个一个加组件不现实 —— 所以给一个批量入口。
        ///
        /// ══════════════ 只动预制体，不碰场景 ══════════════
        /// 场景里的对象是摆放结果，批量往里塞组件容易把场景标脏、甚至误改实例覆盖。
        /// 场景里确实需要的，手工挂即可（通常是临时预览节点）。
        /// </summary>
        private static string PatchAllPrefabs(LanguageFontMap map)
        {
            if (map == null) return "没有映射表，未执行。";

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int scanned = 0, added = 0, updated = 0, skipped = 0;
            var failed = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    // 第三方插件的示例 prefab 不碰 —— 改了也只会在升级插件时被覆盖
                    if (path.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase)) continue;

                    GameObject root = null;
                    try
                    {
                        root = PrefabUtility.LoadPrefabContents(path);
                        if (root == null) { skipped++; continue; }

                        var texts = root.GetComponentsInChildren<TMP_Text>(true);
                        bool dirty = false;
                        bool touched = false;

                        for (int t = 0; t < texts.Length; t++)
                        {
                            // 判定依据是"这个对象需不需要翻译"，而本地化正是由 LocalizedText 表示的
                            if (texts[t].GetComponent<LocalizedText>() == null) continue;

                            touched = true;
                            var lf = texts[t].GetComponent<LocalizedTextFont>();
                            if (lf == null)
                            {
                                lf = texts[t].gameObject.AddComponent<LocalizedTextFont>();
                                lf.map = map;
                                added++;
                                dirty = true;
                            }
                            else if (lf.map != map)
                            {
                                lf.map = map;
                                updated++;
                                dirty = true;
                            }
                        }

                        if (touched) scanned++;
                        if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    catch (Exception e)
                    {
                        failed.Add(path + "：" + e.Message);
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

            var sb = new StringBuilder();
            sb.Append("扫描到带 LocalizedText 的预制体 ").Append(scanned).Append(" 个；")
              .Append("新增组件 ").Append(added).Append(" 个，更新引用 ").Append(updated).Append(" 个。");
            if (skipped > 0) sb.Append("\n跳过（载入失败）").Append(skipped).Append(" 个。");
            if (failed.Count > 0)
            {
                sb.Append("\n处理失败 ").Append(failed.Count).Append(" 个：");
                for (int i = 0; i < failed.Count && i < 5; i++) sb.Append("\n  · ").Append(failed[i]);
            }
            Debug.Log("[语言字体] " + sb.ToString().Replace("\n", " "));
            return sb.ToString();
        }
    }
}
