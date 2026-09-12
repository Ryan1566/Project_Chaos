using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GitTool.Editor
{
    /// <summary>
    /// Git 拉取助手窗口。
    /// 把「拉取最新版本」这件事变成：检查更新 → 看清影响 → 确认拉取，
    /// 避免在 Unity 里盲目更新导致长时间重编译却不知道原因。
    ///
    /// 另有一个破坏性入口「放弃本地更改并更新」：本地改动挡住了 fast-forward 时，
    /// 丢弃「已跟踪文件」的改动再拉取。它不会删除未跟踪的新文件（全程不调用 git clean），
    /// 也不会丢弃未推送的提交 —— 遇到本地领先远端时会直接中止并列出那些提交。
    /// </summary>
    public class GitToolWindow : EditorWindow
    {
        private const string PREF_REMOTE = "GitTool_Remote";
        private const int MAX_LISTED_FILES = 300;

        private string remote = "origin";

        // 仓库信息
        private string repoRoot = "";
        private string branch = "";
        private string upstream = "";
        private string suggestedUpstream = "";
        private string inProgress = "";
        private bool unityRunning;
        private List<string> dirtyFiles = new List<string>();

        // 预览结果
        private GitHelper.UpdatePreview preview;
        private bool previewReady;
        private string statusMessage = "";
        private bool statusIsError;

        private Vector2 scroll;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle wrapStyle;

        [MenuItem("Tool/Git拉取助手")]
        public static void ShowWindow()
        {
            var window = GetWindow<GitToolWindow>("Git拉取助手");
            window.minSize = new Vector2(620, 520);
            window.Show();
        }

        private void OnEnable()
        {
            remote = EditorPrefs.GetString(PREF_REMOTE, "origin");
            RefreshRepoInfo();
        }

        // ------------------------------------------------------------------ 样式

        private void InitStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            wrapStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        /// <summary>按影响等级取显示颜色，深浅皮肤下都保持可读</summary>
        private static Color LevelColor(GitHelper.ImpactLevel level)
        {
            switch (level)
            {
                case GitHelper.ImpactLevel.FullReimport: return new Color(1f, 0.42f, 0.42f);
                case GitHelper.ImpactLevel.Recompile: return new Color(1f, 0.75f, 0.25f);
                case GitHelper.ImpactLevel.None: return new Color(0.6f, 0.6f, 0.6f);
                default: return EditorGUIUtility.isProSkin
                        ? new Color(0.85f, 0.85f, 0.85f)
                        : new Color(0.15f, 0.15f, 0.15f);
            }
        }

        // ------------------------------------------------------------------ 数据刷新

        /// <summary>只读地收集仓库状态，不联网</summary>
        private void RefreshRepoInfo()
        {
            repoRoot = GitHelper.RepoRoot;

            if (!GitHelper.IsRepository())
            {
                branch = upstream = suggestedUpstream = inProgress = "";
                dirtyFiles = new List<string>();
                unityRunning = false;
                SetStatus($"当前目录不是 Git 仓库：{repoRoot}", true);
                return;
            }

            branch = GitHelper.GetCurrentBranch();
            upstream = GitHelper.GetUpstream(branch);
            suggestedUpstream = string.IsNullOrEmpty(upstream) ? GitHelper.SuggestUpstream(branch, remote) : "";
            inProgress = GitHelper.GetInProgressOperation();
            unityRunning = GitHelper.IsUnityRunning();
            dirtyFiles = GitHelper.GetDirtyFiles();

            if (string.IsNullOrEmpty(branch))
                SetStatus("当前处于 detached HEAD 状态，请先切到一个分支", true);
        }

        /// <summary>联网获取远端更新并生成预览</summary>
        private void CheckForUpdates()
        {
            previewReady = false;
            RefreshRepoInfo();

            if (!string.IsNullOrEmpty(inProgress))
            {
                SetStatus($"检测到未完成的 {inProgress} 操作，请先解决冲突再更新", true);
                return;
            }

            GitHelper.GitResult fetch;
            try
            {
                EditorUtility.DisplayProgressBar("Git 拉取助手", $"正在获取 {remote} 的更新…", 0.5f);
                fetch = GitHelper.Fetch(remote);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!fetch.Success)
            {
                SetStatus($"fetch 失败：{fetch.Message}", true);
                return;
            }

            preview = GitHelper.BuildPreview();
            previewReady = true;

            if (string.IsNullOrEmpty(preview.Upstream))
            {
                SetStatus(string.IsNullOrEmpty(suggestedUpstream)
                    ? $"分支 '{branch}' 没有可用的跟踪分支，无法比较差异"
                    : $"分支 '{branch}' 没有设置跟踪分支，点上方按钮设置后即可检查更新", true);
                return;
            }

            if (preview.Behind == 0)
            {
                SetStatus(preview.Ahead > 0
                    ? $"已经是最新版本（本地领先远端 {preview.Ahead} 个提交，记得 push）"
                    : "已经是最新版本", false);
                return;
            }

            int high = GitHelper.CountHighImpact(preview.Items);
            SetStatus(high > 0
                ? $"落后 {preview.Behind} 个提交，其中 {high} 个高影响文件，更新后可能触发全量 Reimport"
                : $"落后 {preview.Behind} 个提交，预计是增量更新，耗时较短", false);
        }

        /// <summary>确认后执行 fast-forward 拉取</summary>
        private void PullUpdates()
        {
            if (!previewReady || preview == null || !preview.HasUpdate)
            {
                EditorUtility.DisplayDialog("Git 拉取助手", "请先点击「检查更新」，确认有更新可拉取。", "确定");
                return;
            }

            var confirm = new StringBuilder();
            confirm.AppendLine($"分支 {preview.Branch} 将更新到 {preview.Upstream}");
            confirm.AppendLine($"落后 {preview.Behind} 个提交，共 {preview.Items.Count} 个文件变更。");
            confirm.AppendLine();

            int high = GitHelper.CountHighImpact(preview.Items);
            if (high > 0)
            {
                confirm.AppendLine($"其中 {high} 个高影响文件（ProjectSettings / Packages / 版本号），");
                confirm.AppendLine("更新后首次打开 Unity 会比较慢。");
                confirm.AppendLine();
            }

            confirm.AppendLine("本次只做 fast-forward，不会产生 merge commit，也不会删除 Library。");

            if (unityRunning)
            {
                confirm.AppendLine();
                confirm.AppendLine("警告：Unity 正在运行。建议先关闭 Unity 再拉取，否则可能出现文件占用或导入错误。");
            }

            if (!EditorUtility.DisplayDialog("确认拉取", confirm.ToString(), "拉取", "取消"))
                return;

            GitHelper.GitResult result;
            try
            {
                EditorUtility.DisplayProgressBar("Git 拉取助手", "正在拉取…", 0.5f);
                result = GitHelper.Pull();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (result.Success)
            {
                AssetDatabase.Refresh();
                previewReady = false;
                preview = null;
                RefreshRepoInfo();
                SetStatus("拉取成功。请切回 Unity 等待它完成资源导入（中途不要关闭编辑器）。", false);

                if (high > 0)
                    EditorUtility.DisplayDialog("拉取成功",
                        "本次更新包含高影响文件，Unity 接下来会做较大范围的 Reimport。\n\n" +
                        "请耐心等待导入完成，不要中途关闭编辑器，也不要去删 Library 目录。", "知道了");
            }
            else
            {
                SetStatus($"拉取失败：{result.Message}", true);
                EditorUtility.DisplayDialog("拉取失败",
                    result.Message + "\n\n常见原因：本地有提交或未提交改动与远端冲突。\n\n" +
                    "可在 Git Bash 中手动处理：\n" +
                    "  git pull --rebase\n" +
                    "  git stash && git pull\n\n" +
                    "本工具不会自动 rebase 或 stash，以免覆盖你的工作。", "知道了");
            }
        }

        /// <summary>
        /// 放弃本地已跟踪文件的改动，然后 fast-forward 拉取。
        ///
        /// 破坏性操作，所以拆成三步拦截：
        ///   1. 有更新要拉、且本地有未推送提交 → 直接中止（reset --hard HEAD 丢不掉提交）
        ///   2. 列出将被丢弃的文件，要求二次确认
        ///   3. 确认后才 reset；有更新才继续 pull
        /// 未跟踪的文件全程不参与，本方法不会调用 git clean。
        ///
        /// 「落后 0 个提交」不会提前返回 —— 那时丢弃改动本身仍然是用户要的结果。
        /// </summary>
        private void DiscardAndPull()
        {
            RefreshRepoInfo();

            if (!GitHelper.IsRepository())
            {
                EditorUtility.DisplayDialog("Git 拉取助手", "当前目录不是 Git 仓库。", "确定");
                return;
            }

            if (!string.IsNullOrEmpty(inProgress))
            {
                EditorUtility.DisplayDialog("Git 拉取助手",
                    $"检测到未完成的 {inProgress} 操作，请先在 Git Bash 中解决冲突（或 git {inProgress} --abort）。",
                    "确定");
                return;
            }

            // 先 fetch，才能知道落后多少、以及有没有未推送的提交
            GitHelper.GitResult fetch;
            try
            {
                EditorUtility.DisplayProgressBar("Git 拉取助手", $"正在获取 {remote} 的更新…", 0.5f);
                fetch = GitHelper.Fetch(remote);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!fetch.Success)
            {
                SetStatus($"fetch 失败：{fetch.Message}", true);
                return;
            }

            preview = GitHelper.BuildPreview();
            previewReady = true;

            if (string.IsNullOrEmpty(preview.Upstream))
            {
                EditorUtility.DisplayDialog("Git 拉取助手",
                    $"分支 '{branch}' 没有可用的跟踪分支，无法比较差异。请先设置 upstream。", "确定");
                return;
            }

            // ---- 拦截 1：本地有未推送的提交 ----
            // 只在「确实有更新要拉」时才拦截：落后 0 个提交时，丢弃改动本身没有问题，
            // 领先远端与否都不影响 reset --hard HEAD 把工作区恢复干净。
            if (preview.Behind > 0 && preview.Ahead > 0)
            {
                var ahead = new StringBuilder();
                ahead.AppendLine($"本地领先 {preview.Upstream} {preview.Ahead} 个提交。");
                ahead.AppendLine();
                ahead.AppendLine("丢弃本地改动解决不了这种情况：");
                ahead.AppendLine("git reset --hard HEAD 只能丢掉未提交的改动，丢不掉已提交的内容。");
                ahead.AppendLine("重置后 HEAD 仍在远端前面，fast-forward 依旧会失败。");
                ahead.AppendLine();
                ahead.AppendLine("将要放弃的提交：");

                List<string> commits = GitHelper.GetUnpushedCommits(preview.Upstream);
                int shown = Mathf.Min(commits.Count, 10);
                for (int i = 0; i < shown; i++)
                    ahead.AppendLine("  " + commits[i]);
                if (commits.Count > shown)
                    ahead.AppendLine($"  …以及另外 {commits.Count - shown} 个");

                ahead.AppendLine();
                ahead.AppendLine("本工具不会替你丢弃提交历史。确实要放弃的话，在 Git Bash 里执行：");
                ahead.AppendLine($"  git reset --hard {preview.Upstream}");

                SetStatus($"本地领先远端 {preview.Ahead} 个提交，无法用「放弃本地更改」解决", true);
                EditorUtility.DisplayDialog("无法放弃：本地有未推送的提交", ahead.ToString(), "知道了");
                return;
            }

            // ---- 拦截 2：列出会被丢弃的文件，要求确认 ----
            // 注意：这里不再因为「落后 0 个提交」就提前 return。
            // 「没有更新可拉」和「不用丢弃本地改动」是两件事，把二者划等号会导致
            // 用户点了放弃却什么都没发生 —— 那正是这个按钮最不该有的行为。
            List<string> dirty = GitHelper.GetDirtyFiles();

            var confirm = new StringBuilder();
            if (dirty.Count > 0)
            {
                confirm.AppendLine($"以下 {dirty.Count} 个已跟踪文件的改动将被丢弃，且无法恢复：");
                confirm.AppendLine();
                int shown = Mathf.Min(dirty.Count, 10);
                for (int i = 0; i < shown; i++)
                {
                    string line = dirty[i];
                    confirm.AppendLine("  " + (line.Length > 3 ? line.Substring(3) : line));
                }
                if (dirty.Count > shown)
                    confirm.AppendLine($"  …以及另外 {dirty.Count - shown} 个");
                confirm.AppendLine();
            }
            else
            {
                confirm.AppendLine("本地没有已跟踪的改动需要丢弃。");
                confirm.AppendLine();
            }

            confirm.AppendLine("不会删除未跟踪的新文件（还没提交的脚本、资源都会保留）。");
            confirm.AppendLine();

            if (unityRunning)
            {
                confirm.AppendLine("警告：Unity 正在运行。建议先关闭 Unity，否则可能出现文件占用或导入错误。");
                confirm.AppendLine();
            }

            confirm.AppendLine(preview.Behind > 0
                ? $"确认丢弃后，将拉取 {preview.Behind} 个提交到 {branch}。"
                : "远端没有新提交，本次只会丢弃改动，不会拉取任何东西。");

            if (!EditorUtility.DisplayDialog("确认放弃本地更改", confirm.ToString(), "丢弃并更新", "取消"))
                return;

            // ---- 拦截 3：先丢弃 ----
            if (dirty.Count > 0)
            {
                GitHelper.GitResult reset;
                try
                {
                    EditorUtility.DisplayProgressBar("Git 拉取助手", "正在丢弃本地改动…", 0.3f);
                    reset = GitHelper.DiscardTrackedChanges();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                if (!reset.Success)
                {
                    SetStatus($"丢弃本地改动失败：{reset.Message}", true);
                    EditorUtility.DisplayDialog("丢弃失败",
                        reset.Message + "\n\n已中止，未执行后续操作。", "知道了");
                    return;
                }
            }

            // ---- 没有更新可拉：丢弃本身就已经是全部目的，到此为止 ----
            if (preview.Behind == 0)
            {
                AssetDatabase.Refresh();
                previewReady = false;
                preview = null;
                RefreshRepoInfo();
                SetStatus($"已丢弃 {dirty.Count} 个已跟踪文件的本地改动（远端无新提交）。", false);
                EditorUtility.DisplayDialog("完成",
                    $"已丢弃 {dirty.Count} 个已跟踪文件的本地改动（不可恢复）。\n\n" +
                    "远端本来就没有新提交，所以没有执行拉取。\n" +
                    "未跟踪的新文件未受影响。", "知道了");
                return;
            }

            int high = GitHelper.CountHighImpact(preview.Items);
            GitHelper.GitResult result;
            try
            {
                EditorUtility.DisplayProgressBar("Git 拉取助手", "正在拉取…", 0.6f);
                result = GitHelper.Pull();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (result.Success)
            {
                AssetDatabase.Refresh();
                previewReady = false;
                preview = null;
                RefreshRepoInfo();
                SetStatus($"已丢弃 {dirty.Count} 个文件的本地改动并更新完成。", false);

                EditorUtility.DisplayDialog("更新完成",
                    (dirty.Count > 0
                        ? $"已丢弃 {dirty.Count} 个已跟踪文件的本地改动（不可恢复）。\n\n"
                        : "") +
                    (high > 0
                        ? "本次更新包含高影响文件，Unity 接下来会做较大范围的 Reimport。\n" +
                          "请耐心等待导入完成，不要中途关闭编辑器，也不要去删 Library 目录。"
                        : "请切回 Unity 等待它完成资源导入。"),
                    "知道了");
            }
            else
            {
                SetStatus($"拉取失败：{result.Message}", true);
                EditorUtility.DisplayDialog("拉取失败",
                    result.Message + "\n\n本地未提交的改动已经在上一步丢弃，" +
                    "仍然失败通常是别的原因（例如未推送的提交）。\n\n" +
                    "可在 Git Bash 中查看：\n" +
                    $"  git log --oneline {preview?.Upstream}..HEAD", "知道了");
            }
        }

        private void SetStatus(string message, bool isError)
        {
            statusMessage = message;
            statusIsError = isError;
        }

        // ------------------------------------------------------------------ 界面

        private void OnGUI()
        {
            InitStyles();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawRepoInfo();
            EditorGUILayout.Space(6);
            DrawWarnings();
            EditorGUILayout.Space(6);
            DrawActions();
            EditorGUILayout.Space(6);
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        private void DrawRepoInfo()
        {
            EditorGUILayout.LabelField("仓库信息", headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("工程根目录", GUILayout.Width(80));
            EditorGUILayout.SelectableLabel(repoRoot, EditorStyles.textField, GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前分支", GUILayout.Width(80));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(branch) ? "—" : branch);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("跟踪分支", GUILayout.Width(80));
            if (string.IsNullOrEmpty(upstream))
                EditorGUILayout.LabelField("未设置（git pull 会直接失败）");
            else
                EditorGUILayout.LabelField(upstream);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("远端名称", GUILayout.Width(80));
            string newRemote = EditorGUILayout.TextField(remote, GUILayout.Width(140));
            if (newRemote != remote)
            {
                remote = newRemote;
                EditorPrefs.SetString(PREF_REMOTE, remote);
            }
            if (GUILayout.Button("刷新状态", GUILayout.Width(80)))
            {
                RefreshRepoInfo();
                SetStatus("已刷新仓库状态", false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawWarnings()
        {
            bool any = false;

            if (!string.IsNullOrEmpty(inProgress))
            {
                EditorGUILayout.HelpBox(
                    $"检测到未完成的 {inProgress} 操作。请先在 Git Bash 中解决冲突（或 git {inProgress} --abort）再使用本工具。",
                    MessageType.Error);
                any = true;
            }

            if (unityRunning)
            {
                EditorGUILayout.HelpBox(
                    "Unity 正在运行。拉取期间 Unity 可能同时改写 Assets，导致文件占用或导入错误。建议先关闭 Unity 再更新。",
                    MessageType.Warning);
                any = true;
            }

            if (dirtyFiles.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"有 {dirtyFiles.Count} 个已跟踪文件未提交。如果远端更新到这些文件，fast-forward 拉取会被拒绝。\n" +
                    "要么先自己提交/备份，要么用下面的「放弃本地更改并更新」（不可恢复）。",
                    MessageType.Warning);
                any = true;
            }

            if (!string.IsNullOrEmpty(suggestedUpstream))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    $"分支 '{branch}' 没有跟踪分支，建议设为 {suggestedUpstream}", wrapStyle);
                if (GUILayout.Button("设置", GUILayout.Width(60)))
                {
                    GitHelper.GitResult r = GitHelper.SetUpstream(branch, suggestedUpstream);
                    RefreshRepoInfo();
                    SetStatus(r.Success ? $"已设置跟踪分支为 {suggestedUpstream}" : $"设置失败：{r.Message}", !r.Success);
                }
                EditorGUILayout.EndHorizontal();
                any = true;
            }

            if (!any && GitHelper.IsRepository())
            {
                EditorGUILayout.HelpBox("状态正常，可以检查更新。", MessageType.Info);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("操作", headerStyle);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("1. 检查更新", GUILayout.Height(28)))
                CheckForUpdates();

            using (new EditorGUI.DisabledScope(!previewReady || preview == null || !preview.HasUpdate))
            {
                if (GUILayout.Button("2. 拉取更新", GUILayout.Height(28)))
                    PullUpdates();
            }

            if (GUILayout.Button("打开命令行", GUILayout.Height(28), GUILayout.Width(100)))
                OpenTerminal();

            EditorGUILayout.EndHorizontal();

            // ---- 破坏性操作，单独一行并标红 ----
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.92f, 0.45f, 0.45f);
            if (GUILayout.Button("※ 放弃本地更改并更新（丢弃后不可恢复）", GUILayout.Height(26)))
                DiscardAndPull();
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(statusMessage, statusIsError ? MessageType.Error : MessageType.None);
            }
        }

        private void DrawPreview()
        {
            if (!previewReady || preview == null || !preview.HasUpdate)
                return;

            EditorGUILayout.LabelField($"更新预览（{preview.Items.Count} 个文件）", headerStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ---- 分类汇总 ----
            List<KeyValuePair<GitHelper.Category, int>> summary = GitHelper.Summarize(preview.Items);
            foreach (KeyValuePair<GitHelper.Category, int> entry in summary)
            {
                EditorGUILayout.BeginHorizontal();
                Rect swatch = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
                swatch.y += 3;
                EditorGUI.DrawRect(swatch, LevelColor(entry.Key.Level));
                GUILayout.Space(4);
                EditorGUILayout.LabelField($"{entry.Key.Name}  ×{entry.Value}", GUILayout.Width(140));
                EditorGUILayout.LabelField(entry.Key.Impact, wrapStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // ---- 文件明细 ----
            EditorGUILayout.LabelField("文件明细", subHeaderStyle);
            int shown = Mathf.Min(preview.Items.Count, MAX_LISTED_FILES);
            for (int i = 0; i < shown; i++)
            {
                GitHelper.ChangeItem item = preview.Items[i];
                EditorGUILayout.BeginHorizontal();
                Color prev = GUI.contentColor;
                GUI.contentColor = LevelColor(item.Category.Level);
                EditorGUILayout.LabelField(item.StatusText, GUILayout.Width(46));
                GUI.contentColor = prev;
                EditorGUILayout.LabelField(item.Category.Name, GUILayout.Width(80));
                EditorGUILayout.LabelField(item.Path, wrapStyle);
                EditorGUILayout.EndHorizontal();
            }

            if (preview.Items.Count > shown)
                EditorGUILayout.LabelField($"… 以及另外 {preview.Items.Count - shown} 个文件");

            EditorGUILayout.EndVertical();
        }

        private void OpenTerminal()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/K cd /d \"" + repoRoot + "\"",
                    UseShellExecute = true,
                    WorkingDirectory = repoRoot,
                });
            }
            catch (System.Exception e)
            {
                SetStatus($"无法打开命令行：{e.Message}", true);
            }
        }
    }
}
