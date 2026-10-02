using System;
using System.Collections.Generic;
using System.Text;
using ArtPipeline.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace InputIcon.Editor
{
    /// <summary>
    /// 按键图标映射工具。菜单：Tools/图标/按键图标映射
    ///
    /// ══════════════════════ 它解决什么 ══════════════════════
    /// 按键界面上的键名原本一律是文字，长度随语言剧烈变化（"空格" ↔ "Mouse Left Button" ↔ 韩文长句），
    /// 而键格与页签的宽度是摆 prefab 时定死的 —— 切语言就溢出、压到隔壁格子上。
    /// 换成图标能根治，但"哪个控制配哪张图"是一份必须有人维护的对照表。
    /// 本窗口把这件事的四步收在一处：
    ///   ① 扫图标库 → 认识哪些图、哪些图读不懂（如实报告，不猜）；
    ///   ② 自动匹配 → 按文件名规范把"控制 → 图"填进映射表（要挑哪张由风格/画法两个开关决定）；
    ///   ③ 铺到预制体 → 给按键行的键格补上图标显示位（KeyIconText + Icon 子节点）；
    ///   ④ 重建图集 → 只把映射表引用到的图打进图集，并把页数/尺寸/估算显存报出来。
    ///
    /// ══════════════════════ 为什么"未识别"要如实列出来 ══════════════════════
    /// 库里有一批图在 Input System 里根本没有对应控制（&lt; &gt; * : ! ? + ^、Fn、guide 键）。
    /// 把它们硬塞给一个"看起来差不多"的控制，症状是某个键显示了一张毫不相干的图 ——
    /// 比显示一串文字更难查。所以这些一律标未识别，留在报告里等人处理。
    ///
    /// ══════════════════════ 图的颜色：库是给深色界面画的，本工程是浅色界面 ══════════════════════
    /// 库里 410/436 张是【白色】线条（其余是 PS 面键的彩色变体）。
    /// 而设置面板是浅底深字（行底 0.93、文字 0.1）—— 白色图直接放上去等于看不见。
    /// 所以映射表上有一个统一 tint（默认 0.1 灰，与文字同色）：白 × 色 = 色，
    /// Image.color 正好能把它染成界面该有的颜色。彩色变体则标记 rawColor 不染色。
    /// </summary>
    public class KeyIconMapWindow : EditorWindow
    {
        /// <summary>映射表资产的默认位置。必须落在 Resources 下 —— 运行时要 Resources.Load 取它。</summary>
        public const string DefaultMapPath = "Assets/Resources/Data/Input/KeyIconMap.asset";

        /// <summary>走脚本路径时置 true，跳过所有对话框（与本地化那几个工具同一约定）。</summary>
        public static bool SilentMode;

        private KeyIconMap _map;
        private Vector2 _scroll;
        private List<IconNaming.IconInfo> _library;
        private string _lastResult;
        private string _search = "";
        private int _filter;                 // 0 全部 / 1 已配置 / 2 未配置 / 3 输入资产里用到的
        private bool _showUnrecognized;

        private static readonly string[] FilterNames = { "全部", "已配图标", "没配图标", "输入资产用到的" };

        [MenuItem("Tools/图标/按键图标映射", false, 60)]
        public static void ShowWindow()
        {
            var w = GetWindow<KeyIconMapWindow>("按键图标映射");
            w.minSize = new Vector2(720f, 560f);
            w.Show();
        }

        private void OnEnable()
        {
            _map = AssetDatabase.LoadAssetAtPath<KeyIconMap>(DefaultMapPath);
        }

        private void OnGUI()
        {
            DrawHeader();
            if (_map == null) return;

            DrawOptions();
            DrawActions();
            DrawCoverage();
            DrawTable();

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }
        }

        // ══════════════════════ 顶部 ══════════════════════

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("映射表资产", EditorStyles.boldLabel);
            _map = (KeyIconMap)EditorGUILayout.ObjectField(_map, typeof(KeyIconMap), false);

            if (_map == null)
            {
                EditorGUILayout.HelpBox(
                    "还没有映射表。按钮会在 " + DefaultMapPath + " 建一张（必须放在 Resources 下，运行时要取它）。",
                    MessageType.Info);
                if (GUILayout.Button("创建映射表资产", GUILayout.Height(28f))) CreateMap();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("路径：" + AssetDatabase.GetAssetPath(_map), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("选图口径（自动匹配按它挑）", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("画风", GUILayout.Width(60f));
            _map.style = (KeyIconMap.IconStyle)EditorGUILayout.EnumPopup(_map.style, GUILayout.Width(120f));
            _map.preferGlyphVariant = EditorGUILayout.ToggleLeft(
                "宽键优先用图形版（键帽上画一道横杠，而不是印 SPACE/SHIFT）", _map.preferGlyphVariant);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图标染色", GUILayout.Width(60f));
            _map.tint = EditorGUILayout.ColorField(_map.tint, GUILayout.Width(120f));
            EditorGUILayout.LabelField(
                "库里的图是白色线条（为深色界面画的），本面板是浅底深字 —— 不染色就看不见。默认取与文字同色的 0.1 灰",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _map.hideKeyFrameWhenIconShown = EditorGUILayout.ToggleLeft(
                "显示图标时隐藏键格的白色底（图标自带键帽外形，叠起来是双层边框）",
                _map.hideKeyFrameWhenIconShown, GUILayout.Width(420f));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_map);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("① 扫描图标库", GUILayout.Height(26f))) RescanLibrary();
            GUI.enabled = _library != null;
            if (GUILayout.Button("② 自动匹配（只填空项）", GUILayout.Height(26f))) AutoMatch(false);
            if (GUILayout.Button("自动匹配（全部重填）", GUILayout.Height(26f)))
            {
                if (Confirm("全部重填", "会把每一行的图标都按当前口径重挑一遍，\n手工改过的图标也会被覆盖。继续？"))
                {
                    AutoMatch(true);
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("③ 铺到预制体（补 KeyIconText + Icon 子节点）", GUILayout.Height(26f)))
            {
                _lastResult = PatchAllPrefabs();
            }
            if (GUILayout.Button("补全按键页页签图标", GUILayout.Height(26f)))
            {
                _lastResult = ApplyTabIcons();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("④ 重建图标图集", GUILayout.Height(26f)))
            {
                // 图集构建会碰资产（创建/写设置/打包），出错时不能把整个窗口带崩 ——
                // OnGUI 里抛异常会让窗口停在半绘制状态，连"哪里出错了"都看不到
                try
                {
                    IconAtlasBuilder.BuildReport r = IconAtlasBuilder.Rebuild(_map);
                    _lastResult = r.message;
                    Debug.Log("[图标图集] " + IconAtlasBuilder.DescribeForTest(r));
                }
                catch (Exception e)
                {
                    _lastResult = "重建图集失败：" + e.Message;
                    Debug.LogError("[图标图集] " + e);
                }
            }
            if (GUILayout.Button("只检查图集占用（不改任何东西）", GUILayout.Height(26f)))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(IconAtlasBuilder.AtlasPath);
                IconAtlasBuilder.BuildReport r = IconAtlasBuilder.Inspect(atlas);
                _lastResult = atlas == null
                    ? "还没有图集资产（" + IconAtlasBuilder.AtlasPath + "）"
                    : IconAtlasBuilder.DescribeForTest(r);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "顺序：① → ② → ③ → ④。④ 的白名单就是这张表引用到的图，所以每次改完②都要重跑一次④。",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCoverage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前选项 / 搜索", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _filter = EditorGUILayout.Popup(_filter, FilterNames, GUILayout.Width(160f));
            EditorGUILayout.LabelField("搜索控制名", GUILayout.Width(70f));
            _search = EditorGUILayout.TextField(_search);
            _showUnrecognized = EditorGUILayout.ToggleLeft("显示未识别的图", _showUnrecognized, GUILayout.Width(140f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════ 表格 ══════════════════════

        private void DrawTable()
        {
            if (_map.entries == null) _map.entries = new List<KeyIconMap.Entry>();

            var used = UsedControlKeys();   // 输入资产里真正出现过的 controlKey 集合

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            InputDeviceType lastFamily = (InputDeviceType)(-1);
            int drawn = 0;

            for (int i = 0; i < _map.entries.Count; i++)
            {
                KeyIconMap.Entry e = _map.entries[i];
                if (e == null) continue;
                if (!PassesFilter(e, used)) continue;
                if (!MatchesSearch(e)) continue;

                if (e.family != lastFamily)
                {
                    lastFamily = e.family;
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField(FamilyTitle(lastFamily), EditorStyles.boldLabel);
                }

                DrawEntryRow(e, used.Contains(Key(e.family, e.controlKey)));
                drawn++;
            }

            if (drawn == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前筛选下没有任何条目。先点「① 扫描图标库」再点「② 自动匹配」。", MessageType.Info);
            }

            if (_showUnrecognized) DrawUnrecognizedList();

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntryRow(KeyIconMap.Entry e, bool usedInAsset)
        {
            EditorGUILayout.BeginHorizontal();

            // 图标预览
            Rect preview = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));
            if (e.icon != null)
            {
                // 预览也用染色后的颜色：白色图在白底预览里等于空白，看不出配了什么
                Color old = GUI.color;
                GUI.color = e.rawColor ? Color.white : new Color(_map.tint.r, _map.tint.g, _map.tint.b, 1f);
                GUI.DrawTexture(preview, e.icon.texture, ScaleMode.ScaleToFit, true);
                GUI.color = old;
            }

            EditorGUILayout.LabelField(e.controlKey + (usedInAsset ? "  ←在用" : ""),
                GUILayout.Width(180f));

            EditorGUI.BeginChangeCheck();
            Sprite picked = (Sprite)EditorGUILayout.ObjectField(e.icon, typeof(Sprite), false, GUILayout.Width(160f));
            if (EditorGUI.EndChangeCheck())
            {
                e.icon = picked;
                e.sourceAsset = picked != null ? AssetDatabase.GetAssetPath(picked) : "";
                EditorUtility.SetDirty(_map);
            }

            EditorGUI.BeginChangeCheck();
            e.rawColor = EditorGUILayout.ToggleLeft("不染色", e.rawColor, GUILayout.Width(70f));
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_map);

            EditorGUI.BeginChangeCheck();
            e.note = EditorGUILayout.TextField(e.note);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_map);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawUnrecognizedList()
        {
            if (_library == null) return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("未识别的图（不会进映射表，也不会进图集）", EditorStyles.boldLabel);

            int shown = 0;
            for (int i = 0; i < _library.Count; i++)
            {
                IconNaming.IconInfo info = _library[i];
                if (info.Resolved) continue;
                EditorGUILayout.LabelField("  · " + info.relativePath + " —— " + info.note, EditorStyles.miniLabel);
                shown++;
            }
            if (shown == 0) EditorGUILayout.LabelField("  （没有）", EditorStyles.miniLabel);
        }

        private bool PassesFilter(KeyIconMap.Entry e, HashSet<string> used)
        {
            switch (_filter)
            {
                case 1: return e.icon != null;
                case 2: return e.icon == null;
                case 3: return used.Contains(Key(e.family, e.controlKey));
                default: return true;
            }
        }

        private bool MatchesSearch(KeyIconMap.Entry e)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            return e.controlKey != null &&
                   e.controlKey.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FamilyTitle(InputDeviceType family)
        {
            switch (family)
            {
                case InputDeviceType.PlayStation: return "── PlayStation 手柄 ──";
                case InputDeviceType.Xbox: return "── Xbox 手柄 ──";
                default: return "── 键盘 / 鼠标 ──";
            }
        }

        // ══════════════════════ ① 扫描 ══════════════════════

        private void RescanLibrary()
        {
            _library = IconNaming.Scan();
            _lastResult = IconNaming.Describe(_library);
            Debug.Log("[图标映射] " + _lastResult.Replace("\n", " "));
            Repaint();
        }

        // ══════════════════════ ② 自动匹配 ══════════════════════

        private void AutoMatch(bool overwrite)
        {
            if (_library == null) RescanLibrary();
            int changed = AutoMatchInternal(_map, _library, overwrite);
            _lastResult = (overwrite ? "全部重填" : "只填空项") + "完成：改动 " + changed + " 条。\n" +
                          "接着点「③ 铺到预制体」，再点「④ 重建图标图集」。";
            Debug.Log("[图标映射] " + _lastResult.Replace("\n", " "));
        }

        /// <summary>
        /// 按图标库填映射表。返回改动的条目数。
        ///
        /// 只为【扫描到的 (主题, 控制)】建条目：键盘控制不会凭空多出 PS/Xbox 两行空记录。
        /// overwrite = false 时只填 icon 为空的条目，手工挑过的图不会被冲掉。
        /// </summary>
        public static int AutoMatchInternal(KeyIconMap map, List<IconNaming.IconInfo> library, bool overwrite)
        {
            if (map == null || library == null) return 0;

            int changed = 0;

            // ── 控制行 ──
            for (int i = 0; i < library.Count; i++)
            {
                IconNaming.IconInfo info = library[i];
                if (!info.Resolved || info.isGlyphOnly) continue;

                string[] controls = info.Controls();
                for (int c = 0; c < controls.Length; c++)
                {
                    string control = controls[c];
                    if (string.IsNullOrEmpty(control)) continue;

                    IconNaming.IconInfo best = IconNaming.PickBest(
                        library, control, info.family, map.style, map.preferGlyphVariant);
                    if (best == null) continue;

                    KeyIconMap.Entry entry = map.GetOrAddEntry(control, info.family);
                    if (!overwrite && entry.icon != null) continue;

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(best.assetPath);
                    if (sprite == null) continue;

                    if (entry.icon != sprite || entry.sourceAsset != best.assetPath)
                    {
                        entry.icon = sprite;
                        entry.sourceAsset = best.assetPath;
                        // 彩色变体必须不染色：染成近黑会把 PS 面键的蓝/粉/绿全乘没
                        entry.rawColor = best.style == KeyIconMap.IconStyle.Color;
                        changed++;
                    }
                }
            }

            // ── 页签图例 ──
            changed += AssignTabIcon(map, library, InputDeviceType.Keyboard,
                ref map.keyboardTabIcon, "_keyboardTabIcon");
            changed += AssignTabIcon(map, library, InputDeviceType.PlayStation,
                ref map.gamepadTabIconPs, "_gamepadTabIconPs");
            changed += AssignTabIcon(map, library, InputDeviceType.Xbox,
                ref map.gamepadTabIconXbox, "_gamepadTabIconXbox");

            map.SortEntries();
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            KeyIconMap.ResetCache();
            return changed;
        }

        /// <summary>页签图例不是"某个控制"，只能按文件名精确取。返回是否有改动。</summary>
        private static int AssignTabIcon(KeyIconMap map, List<IconNaming.IconInfo> library,
            InputDeviceType family, ref Sprite slot, string label)
        {
            string wanted = IconNaming.TabFileName(family);
            for (int i = 0; i < library.Count; i++)
            {
                if (!string.Equals(library[i].fileName, wanted, StringComparison.Ordinal)) continue;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(library[i].assetPath);
                if (sprite == null) return 0;
                if (slot != sprite)
                {
                    slot = sprite;
                    EditorUtility.SetDirty(map);
                    return 1;
                }
                return 0;
            }

            Debug.LogWarning("[图标映射] 页签图例 " + label + " 想要的文件 '" + wanted +
                             "' 在图标库里没找到，页签会退回显示译文。");
            return 0;
        }

        // ══════════════════════ 覆盖率 ══════════════════════

        /// <summary>
        /// 输入资产里真正出现过的 (主题, 控制)。用来标出"在用的键有没有配图"。
        ///
        /// ⚠ 手柄路径要按【两个型号各记一次】：同一个 buttonSouth 在 PS 与 Xbox 下是两张不同的图，
        /// 所以"这个键配了没"必须分开算。这里刻意不走 InputManager.IconFamily ——
        /// 那个方法回答的是"运行时按当前型号该用哪个主题"，在本方法里没有"当前型号"可言，
        /// 硬塞一个 Keyboard 进去会把 leftStick/x 记成键鼠控制（问出来的覆盖率是假的）。
        /// </summary>
        public static HashSet<string> UsedControlKeys()
        {
            var used = new HashSet<string>();
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Resources/Input/ChaosInputActions.inputactions");
            if (asset == null) return used;

            foreach (var action in asset.actionMaps[0].actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.isComposite) continue;   // 复合头没有控制路径
                    string path = b.path;
                    if (string.IsNullOrEmpty(path)) continue;

                    string control = InputManager.ControlKey(path);
                    if (string.IsNullOrEmpty(control)) continue;

                    if (path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase))
                    {
                        used.Add(Key(InputDeviceType.PlayStation, control));
                        used.Add(Key(InputDeviceType.Xbox, control));
                    }
                    else
                    {
                        used.Add(Key(InputDeviceType.Keyboard, control));
                    }
                }
            }
            return used;
        }

        private static string Key(InputDeviceType family, string control)
        {
            return ((int)family) + "|" + control;
        }

        // ══════════════════════ ③ 铺到预制体 ══════════════════════

        /// <summary>
        /// 给所有带 SettingRow_Keybind 的预制体补上"图标显示位"：
        /// KeyText / MouseText 下加一个名为 Icon 的 Image 子节点 + KeyIconText 组件。
        ///
        /// ══════════════ 为什么由工具做，而不是手工在 Inspector 里加 ══════════════
        /// 本工程的 UI prefab 是手工维护的（生成器已删），而"每加一个键格就少加一个图标位"
        /// 这种漏，表现是那一格永远只显示文字 —— 不会报错，只会在切语言时才被人看见。
        /// 给一个批量入口，比靠人记得可靠。
        ///
        /// 已经有的只更新字段，不重复添加（幂等）。
        /// </summary>
        public static string PatchAllPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int prefabs = 0, slots = 0, textSized = 0;
            var failed = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                // 第三方插件的 prefab 不碰（改了也只会被插件升级覆盖）
                if (path.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase)) continue;

                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;

                    var rows = root.GetComponentsInChildren<SettingRow_Keybind>(true);
                    if (rows.Length == 0) continue;

                    bool dirty = false;
                    for (int r = 0; r < rows.Length; r++)
                    {
                        slots += PatchRow(rows[r].transform, ref dirty);
                        textSized += ApplyAutoSize(rows[r].transform);
                    }

                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabs++;
                    }
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sb = new StringBuilder();
            sb.Append("处理 ").Append(prefabs).Append(" 个预制体、").Append(slots).Append(" 个键格");
            if (textSized > 0) sb.Append("；另有 ").Append(textSized).Append(" 处文字开启了自动缩小字号");
            if (failed.Count > 0)
            {
                sb.Append("\n失败 ").Append(failed.Count).Append(" 个：");
                for (int i = 0; i < failed.Count && i < 5; i++) sb.Append("\n  · ").Append(failed[i]);
            }
            return sb.ToString();
        }

        /// <summary>给一行的两个键格补图标位。返回补了几格。</summary>
        private static int PatchRow(Transform row, ref bool dirty)
        {
            int count = 0;
            string[] names = { "KeyText", "MouseText" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform textNode = row.Find(names[i]);
                if (textNode == null) continue;   // 手柄行没有鼠标格，正常

                if (EnsureIconSlot(textNode)) dirty = true;
                count++;
            }
            return count;
        }

        /// <summary>
        /// 保证一个文字节点下有可用的图标位：Icon 子节点（Image）+ 同节点上的 KeyIconText。
        /// 已存在时只把字段补齐。返回是否改动了它。
        /// </summary>
        public static bool EnsureIconSlot(Transform textNode)
        {
            if (textNode == null) return false;
            bool dirty = false;

            Transform iconNode = textNode.Find("Icon");
            if (iconNode == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(textNode, false);
                iconNode = go.transform;
                dirty = true;
            }

            var rt = iconNode as RectTransform;
            if (rt != null && (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one || rt.sizeDelta != Vector2.zero))
            {
                // 铺满文字节点：键格尺寸由 prefab 决定，图标跟着那一格走，不做第二套尺寸
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                dirty = true;
            }

            var image = iconNode.GetComponent<Image>();
            if (image == null)
            {
                image = iconNode.gameObject.AddComponent<Image>();
                dirty = true;
            }
            if (!image.preserveAspect) { image.preserveAspect = true; dirty = true; }
            if (image.raycastTarget) { image.raycastTarget = false; dirty = true; }
            // 默认关掉：那一格会由 KeyIconText.SetDisplay 按"有没有图标"决定显隐。
            // 留成开着的话，没配图标的键会先闪一个空白的白方块
            if (image.enabled) { image.enabled = false; dirty = true; }
            if (image.sprite != null) { image.sprite = null; dirty = true; }

            var kit = textNode.GetComponent<KeyIconText>();
            if (kit == null)
            {
                kit = textNode.gameObject.AddComponent<KeyIconText>();
                dirty = true;
            }
            if (kit.text == null)
            {
                kit.text = textNode.GetComponent<TMP_Text>();
                dirty = true;
            }
            if (kit.icon != image)
            {
                kit.icon = image;
                dirty = true;
            }

            return dirty;
        }

        /// <summary>
        /// 键格的文字开自动字号。
        ///
        /// 这是"图标化"的兜底：总有键配不到图标（库里没画那个键），
        /// 那些格子仍要显示文字，而文字长度随语言变 —— 不开自动字号就会溢出到隔壁格。
        /// 页签的 Label 不动：它的图标由工具保底写进映射表，且它连着 LocalizedText，
        /// 改字号会同时改掉"图标缺失时回退显示译文"的样子。
        /// </summary>
        private static int ApplyAutoSize(Transform row)
        {
            int changed = 0;
            string[] names = { "KeyText", "MouseText" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform t = row.Find(names[i]);
                if (t == null) continue;

                var tmp = t.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                if (tmp.enableAutoSizing && Mathf.Approximately(tmp.fontSizeMin, 18f)) continue;

                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = tmp.fontSize > 0f ? tmp.fontSize : 38f;
                tmp.fontSizeMin = 18f;
                changed++;
            }
            return changed;
        }

        // ══════════════════════ 页签图标 ══════════════════════

        /// <summary>
        /// 给按键页的两个页签补上图标位，并把图例写进映射表。
        ///
        /// ══════════════ 页签为什么必须换图标 ══════════════
        /// 它是溢出最严重的一处：Tab_Keyboard/Label 被摆成 76x50、TMP 38pt 不换行，
        /// 而英文译文是 "Keyboard &amp; Mouse"（16 个字符，约 300px）—— 直接把旁边页签压住，
        /// 日文/韩文更宽。图标宽度恒定，语言再换也不会动。
        ///
        /// ══════════════ 文字没有丢 ══════════════
        /// Label 上的 LocalizedText 原样保留（译文照样往里写），只是 TMP 被 KeyIconText 关掉。
        /// 于是图标缺失时会自动回退成译文 —— 不需要在页面里再抄一份文案。
        /// </summary>
        public static string ApplyTabIcons()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            var touched = new List<string>();
            var missing = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase)) continue;

                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;

                    Transform header = null;
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Page_Keybind_Bindings")
                        {
                            header = t.Find("Header");
                            break;
                        }
                    }
                    if (header == null) continue;

                    bool dirty = false;
                    string[] tabs = { "Tab_Keyboard", "Tab_Gamepad" };
                    for (int t = 0; t < tabs.Length; t++)
                    {
                        Transform label = header.Find(tabs[t] + "/Label");
                        if (label == null) { missing.Add(path + " 缺 " + tabs[t] + "/Label"); continue; }
                        if (EnsureIconSlot(label)) dirty = true;
                    }

                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        touched.Add(path);
                    }
                }
                catch (Exception e)
                {
                    missing.Add(path + "：" + e.Message);
                }
                finally
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sb = new StringBuilder();
            sb.Append("补了 ").Append(touched.Count).Append(" 个面板的页签图标位");
            if (touched.Count > 0) sb.Append("：").Append(string.Join("、", touched.ToArray()));
            sb.Append("\n页签图例来自映射表的 keyboardTabIcon / gamepadTabIconPs / gamepadTabIconXbox —— 由②自动匹配写入。");
            if (missing.Count > 0)
            {
                sb.Append("\n没找到的：");
                for (int i = 0; i < missing.Count && i < 5; i++) sb.Append("\n  · ").Append(missing[i]);
            }
            return sb.ToString();
        }

        // ══════════════════════ 杂项 ══════════════════════

        private void CreateMap()
        {
            string dir = System.IO.Path.GetDirectoryName(DefaultMapPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogError("[图标映射] 目录不存在：" + dir + "，请先在 Project 里建好再试。");
                return;
            }

            var map = CreateInstance<KeyIconMap>();
            AssetDatabase.CreateAsset(map, DefaultMapPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _map = map;
            Debug.Log("[图标映射] 已创建映射表：" + DefaultMapPath);
        }

        private static bool Confirm(string title, string message)
        {
            if (SilentMode) return true;
            return EditorUtility.DisplayDialog(title, message, "继续", "取消");
        }

        // ══════════════════════ 给测试用的入口 ══════════════════════

        public static List<IconNaming.IconInfo> ScanLibraryForTest() { return IconNaming.Scan(); }

        public static int AutoMatchForTest(KeyIconMap map, bool overwrite)
        {
            return AutoMatchInternal(map, IconNaming.Scan(), overwrite);
        }

        public static string CoverageForTest()
        {
            HashSet<string> used = UsedControlKeys();
            KeyIconMap map = KeyIconMap.Load();
            var noIcon = new List<string>();
            int withIcon = 0;

            foreach (string k in used)
            {
                string[] parts = k.Split('|');
                InputDeviceType family = (InputDeviceType)int.Parse(parts[0]);
                string control = parts[1];
                Sprite s = map != null ? map.GetIcon(control, family) : null;
                if (s != null) withIcon++;
                else noIcon.Add(family + "/" + control);
            }

            var sb = new StringBuilder();
            sb.Append("输入资产用到 ").Append(used.Count).Append(" 个 (主题,控制)，有图标 ").Append(withIcon)
              .Append(" 个，没图标 ").Append(noIcon.Count).Append(" 个");
            for (int i = 0; i < noIcon.Count; i++) sb.Append("\n  · ").Append(noIcon[i]);
            return sb.ToString();
        }

        public static string PatchPrefabsForTest() { return PatchAllPrefabs(); }
        public static string ApplyTabIconsForTest() { return ApplyTabIcons(); }
        public static int AutoMatchAllForTest(KeyIconMap map) { return AutoMatchForTest(map, true); }
    }
}
