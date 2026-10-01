using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LocalizationSystem.Editor
{
    /// <summary>
    /// 本地化子配置合并器：把 Sub_LD 下拆开的各模块配置合并成一张整表。
    ///
    /// ══════════════════════ 为什么要有它 ══════════════════════
    /// 拆开是为了【好管理】：一个 UI 面板一个模块，谁改了哪个面板一目了然，
    /// 也不用几十个人在同一个 asset 上抢冲突。
    /// 但运行时只该读【一张表】—— 否则每加一个模块就要动一次场景里的 LocalizationManager，
    /// 而且"同一个 Key 到底在哪张表里"会变成需要全局搜索的问题。
    ///
    /// 所以分工是：
    ///   · 编辑期：Sub_LD/ 下按模块拆分，各自独立；
    ///   · 运行期：读 Data/Localization/ChaosLocalizationConfig.asset，由本工具生成。
    ///
    /// ⚠ 生成物是【派生产物】，不要手改。改了下次一跑合并就没了。
    ///   要改文案，改 Sub_LD 下那个模块的配置，然后重新合并。
    ///
    /// ══════════════════════ 合并是幂等的 ══════════════════════
    /// 每次都【整份重建】：删掉旧输出资产的条目，按当前 Sub_LD 的内容全部重写。
    /// 所以删掉一个模块、或删掉某个模块里的几条，重新合并后输出里也会跟着消失 ——
    /// 不会留下"早就该没了"的孤儿条目。
    /// </summary>
    public class LocalizationMerger : EditorWindow
    {
        /// <summary>子配置目录（相对工程根）。</summary>
        public const string SubConfigDir = "Assets/Data/Localization/Sub_LD";

        /// <summary>合并产物的路径。运行时读的就是这一张表。</summary>
        public const string OutputPath = "Assets/Data/Localization/ChaosLocalizationConfig.asset";

        private Vector2 _scroll;
        private MergeResult _last;
        private readonly List<string> _foldoutFiles = new List<string>();

        /// <summary>
        /// 静默模式：不弹模态对话框，结果只写日志。
        ///
        /// 与 PrefabTextCollectorWindow.SilentMode 同一个理由：DisplayDialog 会阻塞主线程，
        /// 脚本/自动化驱动时没人去点，编辑器桥接会一起失去响应。
        /// </summary>
        public static bool SilentMode = false;

        private static void Notify(string title, string message)
        {
            if (SilentMode)
            {
                ChaosDebug.ChaosLog.Info(ChaosDebug.LogChannel.Localization,
                    "[" + title + "] " + message.Replace("\n", " "));
                return;
            }
            EditorUtility.DisplayDialog(title, message, "好");
        }

        [MenuItem("Tools/本地化/合并子配置", false, 40)]
        public static void ShowWindow()
        {
            var w = GetWindow<LocalizationMerger>("本地化配置合并");
            w.minSize = new Vector2(620f, 460f);
            w.Show();
        }

        // ══════════════════ 界面 ══════════════════

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("子配置目录（可含子目录）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(SubConfigDir, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.LabelField("合并产物（运行时读这一张）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(OutputPath, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.LabelField(
                "产物是派生产物，不要手改；要改文案请改 Sub_LD 下对应模块后重新合并。",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("扫描子配置（只看不改）", GUILayout.Height(28f)))
            {
                _last = Merge(dryRun: true);
                Report(_last);
            }

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("合并并写入 " + System.IO.Path.GetFileName(OutputPath), GUILayout.Height(34f)))
            {
                _last = Merge(dryRun: false);
                Report(_last);
            }
            GUI.backgroundColor = old;

            if (_last == null)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox("点上面的按钮开始。建议先「扫描」看清楚会合并哪些模块。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6f);
            DrawResult(_last);
        }

        private void DrawResult(MergeResult r)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!r.Success)
            {
                EditorGUILayout.HelpBox("合并【已中止】，产物未被修改。", MessageType.Error);
            }
            else if (r.DryRun)
            {
                EditorGUILayout.HelpBox("扫描完成（未写入）。共 " + r.SubConfigPaths.Count + " 个模块，将得到 "
                    + r.EntryCount + " 条。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("合并完成：" + r.SubConfigPaths.Count + " 个模块 → "
                    + r.EntryCount + " 条，已写入 " + OutputPath, MessageType.Info);
            }

            if (r.Errors.Count > 0)
            {
                EditorGUILayout.LabelField("阻止合并的问题", EditorStyles.boldLabel);
                for (int i = 0; i < r.Errors.Count; i++)
                {
                    EditorGUILayout.HelpBox(r.Errors[i], MessageType.Error);
                }
            }

            if (r.Warnings.Count > 0)
            {
                EditorGUILayout.LabelField("不阻止合并，但请留意", EditorStyles.boldLabel);
                for (int i = 0; i < r.Warnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(r.Warnings[i], MessageType.Warning);
                }
            }

            if (r.Success && r.SubConfigPaths.Count > 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("合并进去的模块（按此顺序）", EditorStyles.boldLabel);

                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120f));
                for (int i = 0; i < r.SubConfigPaths.Count; i++)
                {
                    string path = r.SubConfigPaths[i];
                    int count = i < r.PerFileCounts.Count ? r.PerFileCounts[i] : 0;
                    EditorGUILayout.LabelField((i + 1) + ".  [" + count + " 条]  " + path, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void Report(MergeResult r)
        {
            if (!r.Success)
            {
                var sb = new StringBuilder("合并已中止：\n");
                for (int i = 0; i < r.Errors.Count; i++) sb.Append("· ").Append(r.Errors[i]).Append('\n');
                Notify("合并子配置", sb.ToString());
                return;
            }

            string msg = (r.DryRun ? "扫描完成（未写入）" : "合并完成")
                + "\n\n模块 " + r.SubConfigPaths.Count + " 个"
                + "\n条目 " + r.EntryCount + " 条"
                + (r.DryRun ? "" : "\n已写入 " + OutputPath);

            if (r.Warnings.Count > 0) msg += "\n\n留意 " + r.Warnings.Count + " 项，详见窗口。";
            Notify("合并子配置", msg);

            ChaosDebug.ChaosLog.Info(ChaosDebug.LogChannel.Localization, msg.Replace("\n", " "));
        }

        // ══════════════════ 合并逻辑 ══════════════════

        /// <summary>一次合并的产出与过程报告。</summary>
        public class MergeResult
        {
            public bool Success;
            public bool DryRun;
            public int EntryCount;
            public readonly List<string> SubConfigPaths = new List<string>();
            public readonly List<int> PerFileCounts = new List<int>();
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();
        }

        /// <summary>
        /// 把 SubConfigDir 下的全部 LocalizationData 依次合并进 OutputPath。
        ///
        /// ══════════════ 处理顺序是确定的 ══════════════
        /// 文件按路径排序后再合并。FindAssets 的顺序不保证稳定，
        /// 而"同一个 Key 出现在两个模块里"这种冲突的报错内容取决于顺序 ——
        /// 不排序的话，同一个工程跑两次可能给出不同的诊断，没法复现。
        ///
        /// ══════════════ 冲突一律中止，不静默取舍 ══════════════
        /// 同一个 Key 出现在两个模块里、且文案不一致时【直接中止】，不做"后者覆盖前者"。
        /// 理由：那样线上到底显示哪一条取决于文件的字典序，是个谁也说不清的 bug；
        /// 而且键冲突几乎总是"复制了一个模块忘了改 Key"，该由人去改，不该由工具猜。
        ///
        /// 内容完全相同的重复（同 Key 同中文）只警告并合并成一条 ——
        /// 运行时按 Key 查表，重复条目不会造成行为差异，但会让人以为改了 A 却显示 B。
        ///
        /// dryRun = true 时只扫描与校验，不碰产物文件。
        /// </summary>
        public static MergeResult Merge(bool dryRun)
        {
            return Merge(dryRun, SubConfigDir);
        }

        /// <summary>
        /// 指定子配置目录的合并。默认目录见 <see cref="SubConfigDir"/>。
        ///
        /// 把目录开成参数是为了验证：脚本要拿一个临时目录造出"Key 冲突""同文件内重复"
        /// 这类异常输入，而不能往真实模块目录里塞脏数据。工具界面只走默认目录那一条路。
        /// </summary>
        public static MergeResult Merge(bool dryRun, string subConfigDir)
        {
            var r = new MergeResult { DryRun = dryRun };

            // ── ① 收集子配置 ──────────────────────────────────────────
            if (!AssetDatabase.IsValidFolder(subConfigDir))
            {
                r.Errors.Add("子配置目录不存在：" + subConfigDir + "\n（若模块配置还没搬进去，请先建目录并放入 LocalizationData 资产）");
                return r;
            }

            string[] guids = AssetDatabase.FindAssets("t:LocalizationData", new[] { subConfigDir });

            // ══════════════ 产物自身必须排除，否则会自我吞并 ══════════════
            // 三种判据都上，因为前两种各有盲区：
            //   · 比路径（p == OutputPath）：产物被挪走后就失效；
            //   · 比 GUID（AssetDatabase.AssetPathToGUID(OutputPath)）：产物一旦不在默认位置，
            //     这个调用【返回空串】—— 于是那条判断根本没参与比较（不是比不中，是没比）；
            //   · 比文件名：挪路径不改文件名，而"产物被塞进 Sub_LD"这种事故恰好只动路径。
            // 后加上文件名这一条，才是真正兜住那种情况的那一条。
            string outputName = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
            string outputGuid = AssetDatabase.AssetPathToGUID(OutputPath);

            var paths = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(p)) continue;
                if (string.Equals(p, OutputPath, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrEmpty(outputGuid)
                    && string.Equals(guids[i], outputGuid, StringComparison.Ordinal))
                {
                    continue;
                }
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(p), outputName, StringComparison.Ordinal))
                {
                    continue;
                }
                paths.Add(p);
            }
            paths.Sort(StringComparer.Ordinal);

            if (paths.Count == 0)
            {
                r.Errors.Add("子配置目录下一个 LocalizationData 都没有：" + subConfigDir);
                return r;
            }
            r.SubConfigPaths.AddRange(paths);

            // ── ② 校验并汇总 ──────────────────────────────────────────
            var merged = new List<LocalizationEntry>();
            var owner = new Dictionary<string, string>();        // key → 首次出现的模块路径
            var ownerCn = new Dictionary<string, string>();      // key → 首次出现时的中文
            var ownerEn = new Dictionary<string, string>();
            var seenPaths = new HashSet<string>();

            for (int f = 0; f < paths.Count; f++)
            {
                string path = paths[f];
                var data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                if (data == null)
                {
                    r.Errors.Add("载入失败（不是 LocalizationData？）：" + path);
                    continue;
                }
                if (data.entries == null)
                {
                    r.Errors.Add("entries 列表为 null：" + path);
                    continue;
                }

                int addedFromFile = 0;
                var inFile = new HashSet<string>();

                for (int i = 0; i < data.entries.Count; i++)
                {
                    LocalizationEntry e = data.entries[i];
                    if (e == null)
                    {
                        r.Errors.Add(path + " 的第 " + (i + 1) + " 条是空引用。");
                        continue;
                    }
                    if (string.IsNullOrEmpty(e.key))
                    {
                        r.Errors.Add(path + " 的第 " + (i + 1) + " 条 Key 为空（中文=\"" + (e.chineseSimplified ?? "") + "\"）。");
                        continue;
                    }

                    // 同一个模块内部重复的 Key：一定是编辑事故
                    if (!inFile.Add(e.key))
                    {
                        r.Errors.Add("模块【" + System.IO.Path.GetFileNameWithoutExtension(path)
                            + "】内部 Key 重复：" + e.key + "\n同一张表里出现两次，请删掉一条。");
                        continue;
                    }

                    string prevFile, prevCn, prevEn;
                    if (owner.TryGetValue(e.key, out prevFile))
                    {
                        ownerCn.TryGetValue(e.key, out prevCn);
                        ownerEn.TryGetValue(e.key, out prevEn);

                        bool sameCn = string.Equals(prevCn ?? "", e.chineseSimplified ?? "", StringComparison.Ordinal);
                        bool sameCT = string.Equals(prevEn ?? "", e.chineseTraditional ?? "", StringComparison.Ordinal);
                        bool sameEn = string.Equals(prevEn ?? "", e.english ?? "", StringComparison.Ordinal);
                        bool sameJp = string.Equals(prevEn ?? "", e.japanese ?? "", StringComparison.Ordinal);
                        bool sameKr = string.Equals(prevEn ?? "", e.korean ?? "", StringComparison.Ordinal);

                        if (sameCn && sameEn)
                        {
                            // 内容完全一样：合并成一条，只提醒
                            r.Warnings.Add("Key 在两个模块里内容相同，已合并为一条：" + e.key
                                + "\n  · " + prevFile
                                + "\n  · " + path);
                        }
                        else
                        {
                            r.Errors.Add("Key 冲突：" + e.key
                                + "\n  · " + prevFile + "  中文=\"" + (prevCn ?? "") + "\"  英文=\"" + (prevEn ?? "") + "\""
                                + "\n  · " + path + "  中文=\"" + (e.chineseSimplified ?? "") + "\"  英文=\"" + (e.english ?? "") + "\""
                                + "\n同一个 Key 不能指向两套文案，请改掉其中一个（大多是复制模块时忘了改 Key）。");
                        }
                        continue;
                    }

                    owner[e.key] = path;
                    ownerCn[e.key] = e.chineseSimplified ?? "";
                    ownerEn[e.key] = e.chineseTraditional ?? "";
                    ownerEn[e.key] = e.english ?? "";
                    ownerEn[e.key] = e.japanese ?? "";
                    ownerEn[e.key] = e.korean ?? "";

                    // 保留原文，并在 description 末尾标上来源模块：排查"这条从哪来的"时不用全局搜索
                    string source = System.IO.Path.GetFileNameWithoutExtension(path);
                    string desc = e.description ?? "";
                    if (desc.IndexOf(source, StringComparison.Ordinal) < 0)
                    {
                        desc = string.IsNullOrEmpty(desc) ? ("来源：" + source) : (desc + "　|　来源：" + source);
                    }

                    merged.Add(new LocalizationEntry
                    {
                        key = e.key,
                        description = desc,
                        chineseSimplified = e.chineseSimplified,
                        chineseTraditional = e.chineseTraditional,
                        english = e.english,
                        japanese = e.japanese,
                        korean = e.korean
                    });
                    addedFromFile++;
                }

                seenPaths.Add(path);
                r.PerFileCounts.Add(addedFromFile);
            }

            r.EntryCount = merged.Count;

            if (r.Errors.Count > 0)
            {
                // 有冲突时【不写产物】：否则会把一半新一半旧的表灌进运行时，
                // 而运行时的表现是"某些文本莫名其妙还是旧翻译"，比直接失败难查得多
                return r;
            }

            // ── ③ 缺翻译的提醒（不阻止合并） ──────────────────────────
            int emptyCn = 0, emptyEn = 0, emptyCt = 0, emptyJp = 0, emptyKr = 0;
            var emptyCnKeys = new List<string>();
            var emptyCtKeys = new List<string>();
            var emptyEnKeys = new List<string>();
            var emptyJpKeys = new List<string>();
            var emptyKrKeys = new List<string>();
            for (int i = 0; i < merged.Count; i++)
            {
                if (string.IsNullOrEmpty(merged[i].chineseSimplified))
                {
                    emptyCn++;
                    if (emptyCnKeys.Count < 10) emptyCnKeys.Add(merged[i].key);
                }
                if (string.IsNullOrEmpty(merged[i].chineseTraditional))
                {
                    emptyCt++;
                    if (emptyCtKeys.Count < 10) emptyCtKeys.Add(merged[i].key);
                }
                if (string.IsNullOrEmpty(merged[i].english))
                {
                    emptyEn++;
                    if (emptyEnKeys.Count < 10) emptyEnKeys.Add(merged[i].key);
                }
                if (string.IsNullOrEmpty(merged[i].english))
                {
                    emptyJp++;
                    if (emptyJpKeys.Count < 10) emptyJpKeys.Add(merged[i].key);
                }
                if (string.IsNullOrEmpty(merged[i].english))
                {
                    emptyKr++;
                    if (emptyKrKeys.Count < 10) emptyKrKeys.Add(merged[i].key);
                }
            }

            if (emptyCn > 0)
            {
                r.Warnings.Add("有 " + emptyCn + " 条【没有中文】：" + Join(emptyCnKeys)
                    + "\n中文是回退链的最后一环，缺了它某种语言下会显示 Key 本身。");
            }
            if (emptyCt > 0)
            {
                r.Warnings.Add("有 " + emptyEn + " 条【没有英文】：" + Join(emptyEnKeys)
                    + "\n繁中缺失时切到英文会回退显示中文（不会报错，所以容易一直没人发现）。");
            }
            if (emptyEn > 0)
            {
                r.Warnings.Add("有 " + emptyEn + " 条【没有英文】：" + Join(emptyEnKeys)
                    + "\n英文缺失时切到英文会回退显示中文（不会报错，所以容易一直没人发现）。");
            }
            if (emptyJp > 0)
            {
                r.Warnings.Add("有 " + emptyEn + " 条【没有英文】：" + Join(emptyEnKeys)
                    + "\n日语缺失时切到英文会回退显示中文（不会报错，所以容易一直没人发现）。");
            }
            if (emptyKr > 0)
            {
                r.Warnings.Add("有 " + emptyEn + " 条【没有英文】：" + Join(emptyEnKeys)
                    + "\n韩语缺失时切到英文会回退显示中文（不会报错，所以容易一直没人发现）。");
            }

            // ── ④ 写产物 ──────────────────────────────────────────────
            if (dryRun)
            {
                r.Success = true;
                return r;
            }

            // ══════════════ 产物不在预期路径就直接停 ══════════════
            // 如果产物资产被挪到了别的地方（GUID 还在，但路径变了），这里若照常继续，
            // AssetDatabase.CreateAsset(OutputPath) 会【再建一份】——
            // 结果是工程里出现两个同名配置，一个 90 条、一个空，而场景引的是被挪走那份。
            // 与其留下这种要排查半天的双份资产，不如明确停下、把话说清楚。
            var output = AssetDatabase.LoadAssetAtPath<LocalizationData>(OutputPath);
            if (output == null)
            {
                // ⚠ 这里【不能】用 AssetDatabase.AssetPathToGUID(OutputPath) 去找：
                // 产物不在默认路径时它返回空串，整条判断就废了（实测踩过）。
                // 改成按【文件名】在工程里找同名 LocalizationData —— 挪路径不改名，一定找得到。
                string movedTo = FindMovedOutput();
                r.Errors.Add("产物资产已经被挪到别处了：\n  当前在 " + movedTo + "\n  期望在 " + OutputPath
                    + "\n\n请先把它移回 " + OutputPath + "（或删掉它让本工具重建），再执行合并。"
                    + "\n不自动重建是为了避免工程里出现两个同名配置。");
                r.Success = false;
                return r;
            }

            if (output == null)
            {
                output = ScriptableObject.CreateInstance<LocalizationData>();
                output.configName = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
                output.description = "由 Sub_LD 下的模块配置合并生成 —— 请勿手改，改子配置后重新合并";
                AssetDatabase.CreateAsset(output, OutputPath);
            }

            // 整份重建：清空后按当前子配置重填，删掉的模块与条目才会真的消失
            output.entries.Clear();
            output.entries.AddRange(merged);
            EditorUtility.SetDirty(output);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            r.Success = true;
            return r;
        }

        /// <summary>
        /// 在工程里按【文件名】找产物资产，返回它在别处的路径；在预期位置或找不到时返回 null。
        ///
        /// ══════════════ 为什么不能靠 GUID 找 ══════════════
        /// AssetDatabase.AssetPathToGUID(路径) 在【那个路径上没有资产】时返回空串 ——
        /// 而产物被挪走恰恰就是这种情况，于是它返回空、判断被视为没有 GUID 而跳过，
        /// 永远进不了发现产物挪走了的分支。文件名不受移动影响，用它反查才可靠。
        /// </summary>
        private static string FindMovedOutput()
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
            string[] found = AssetDatabase.FindAssets(name + " t:LocalizationData", new[] { "Assets" });
            for (int i = 0; i < found.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(found[i]);
                if (string.Equals(p, OutputPath, StringComparison.Ordinal)) continue;
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(p), name, StringComparison.Ordinal))
                {
                    return p;
                }
            }
            return null;
        }

        private static string Join(List<string> keys)
        {
            if (keys.Count == 0) return "";
            string s = "\n  · " + string.Join("\n  · ", keys.ToArray());
            if (keys.Count >= 10) s += "\n  · …（只列前 10 条）";
            return s;
        }

        // ══════════════════ 供自动化 / MCP 驱动的入口 ══════════════════
        //
        // 与面板文字收集器同一套约定：窗口的正常用法是人点按钮，
        // 但"合并 → 校验"这条链路必须能被脚本驱动，否则没法做回归验证。

        /// <summary>无对话框版本的合并。返回结果对象供调用方断言。</summary>
        public static MergeResult MergeForTest(bool dryRun)
        {
            return Merge(dryRun);
        }

        /// <summary>把一次合并结果压成一段可读文本（验证脚本打印用）。</summary>
        public static string DescribeForTest(MergeResult r)
        {
            if (r == null) return "<null>";
            var sb = new StringBuilder();
            sb.Append("成功=").Append(r.Success).Append(" 试跑=").Append(r.DryRun)
              .Append(" 模块=").Append(r.SubConfigPaths.Count).Append(" 条目=").Append(r.EntryCount).Append('\n');
            for (int i = 0; i < r.SubConfigPaths.Count; i++)
            {
                sb.Append("  [").Append(i < r.PerFileCounts.Count ? r.PerFileCounts[i] : 0).Append(" 条] ")
                  .Append(r.SubConfigPaths[i]).Append('\n');
            }
            for (int i = 0; i < r.Errors.Count; i++) sb.Append("  ✗ ").Append(r.Errors[i]).Append('\n');
            for (int i = 0; i < r.Warnings.Count; i++) sb.Append("  ⚠ ").Append(r.Warnings[i]).Append('\n');
            return sb.ToString();
        }
    }
}
