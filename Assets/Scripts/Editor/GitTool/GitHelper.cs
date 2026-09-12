using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace GitTool.Editor
{
    /// <summary>
    /// git 命令执行与变更分类，供 GitToolWindow 使用。
    /// 只做只读查询和 fast-forward 拉取，绝不执行 git clean / reset --hard。
    /// </summary>
    public static class GitHelper
    {
        /// <summary>单条 git 命令的执行结果</summary>
        public class GitResult
        {
            public int ExitCode;
            public string StdOut = "";
            public string StdErr = "";
            public bool TimedOut;

            /// <summary>整体是否成功</summary>
            public bool Success => ExitCode == 0 && !TimedOut;

            /// <summary>成功时给出 stdout，失败时给出 stderr，方便直接显示</summary>
            public string Message => Success
                ? StdOut.Trim()
                : (TimedOut ? "命令超时" : (string.IsNullOrEmpty(StdErr) ? StdOut.Trim() : StdErr.Trim()));
        }

        /// <summary>变更对 Unity 的影响等级，数字越大越费时间</summary>
        public enum ImpactLevel
        {
            /// <summary>不影响 Unity 导入流程</summary>
            None = 0,
            /// <summary>局部资源 Reimport（快）</summary>
            Low = 1,
            /// <summary>触发脚本重编译（增量，较慢）</summary>
            Recompile = 2,
            /// <summary>可能触发全量 Reimport（最慢）</summary>
            FullReimport = 3,
        }

        /// <summary>一个变更分类的描述</summary>
        public struct Category
        {
            public string Name;
            public ImpactLevel Level;
            public string Impact;
        }

        /// <summary>单个文件的一条变更记录</summary>
        public class ChangeItem
        {
            public string Status;   // 单字母状态：A/D/M/R
            public string Path;
            public Category Category;

            /// <summary>用于显示的中文状态名</summary>
            public string StatusText
            {
                get
                {
                    switch (Status)
                    {
                        case "A": return "新增";
                        case "D": return "删除";
                        case "M": return "修改";
                        case "R": return "重命名";
                        case "C": return "复制";
                        default: return Status;
                    }
                }
            }
        }

        /// <summary>一次更新预览的完整结果</summary>
        public class UpdatePreview
        {
            public string Branch = "";
            public string Upstream = "";
            public int Behind;
            public int Ahead;
            public List<ChangeItem> Items = new List<ChangeItem>();

            /// <summary>有变更时的提交数</summary>
            public bool HasUpdate => Behind > 0;
        }

        // ------------------------------------------------------------------ 仓库定位

        /// <summary>Unity 工程根目录（Assets 的上一级）</summary>
        public static string RepoRoot
        {
            get
            {
                string dir = Path.GetDirectoryName(Application.dataPath);
                return string.IsNullOrEmpty(dir) ? Application.dataPath : dir;
            }
        }

        private static string _gitExe;

        /// <summary>定位 git.exe。PATH 里没有时回退到常见安装位置</summary>
        public static string GitExe
        {
            get
            {
                if (!string.IsNullOrEmpty(_gitExe)) return _gitExe;

                string[] candidates =
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Git\bin\git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Git\bin\git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Git\bin\git.exe"),
                };

                foreach (string c in candidates)
                {
                    if (File.Exists(c)) { _gitExe = c; return _gitExe; }
                }

                // 交给 PATH 解析，执行失败时再报错
                _gitExe = "git";
                return _gitExe;
            }
        }

        // ------------------------------------------------------------------ 执行 git

        /// <summary>在工程根目录执行一条 git 命令</summary>
        public static GitResult Run(int timeoutMs, params string[] args)
        {
            var result = new GitResult();

            var startInfo = new ProcessStartInfo
            {
                FileName = GitExe,
                WorkingDirectory = RepoRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // 仓库里有中文文件名，必须按 UTF-8 读取输出
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };

            // core.quotepath=false：否则非 ASCII 路径会被转义成 "\346\226\207"，
            // 下面的分类逻辑会完全失效
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("core.quotepath=false");
            foreach (string a in args) startInfo.ArgumentList.Add(a);

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        result.TimedOut = true;
                        try { process.Kill(); } catch { /* 进程可能已经退出 */ }
                        return result;
                    }

                    // WaitForExit(int) 不保证异步输出已读完，再等一次
                    process.WaitForExit();

                    result.ExitCode = process.ExitCode;
                    result.StdOut = stdout.ToString();
                    result.StdErr = stderr.ToString();
                    return result;
                }
            }
            catch (Exception e)
            {
                result.ExitCode = -1;
                result.StdErr = $"无法启动 git（{GitExe}）：{e.Message}";
                return result;
            }
        }

        /// <summary>确认这是不是一个可用的 Git 仓库</summary>
        public static bool IsRepository()
        {
            return Run(10000, "rev-parse", "--git-dir").Success;
        }

        /// <summary>当前分支名；detached HEAD 时返回空串</summary>
        public static string GetCurrentBranch()
        {
            GitResult r = Run(10000, "symbolic-ref", "--short", "-q", "HEAD");
            return r.Success ? r.StdOut.Trim() : "";
        }

        /// <summary>当前分支跟踪的远端分支（如 origin/main）；没有则返回空串</summary>
        public static string GetUpstream(string branch)
        {
            if (string.IsNullOrEmpty(branch)) return "";

            GitResult r = Run(10000, "for-each-ref", "--format=%(upstream:short)", "refs/heads/" + branch);
            string upstream = r.Success ? r.StdOut.Trim() : "";
            if (string.IsNullOrEmpty(upstream)) return "";

            // fetch --prune 之后上游可能已被删除，此时仍需当作「没有上游」处理
            if (!Run(10000, "rev-parse", "--verify", "--quiet", "refs/remotes/" + upstream).Success)
                return "";

            return upstream;
        }

        /// <summary>把当前分支的 upstream 设置为指定远端分支</summary>
        public static GitResult SetUpstream(string branch, string upstream)
        {
            return Run(15000, "branch", "--set-upstream-to=" + upstream, branch);
        }

        /// <summary>
        /// 推断某个分支应该跟踪哪个远端分支，用于「设置跟踪分支」按钮。
        /// 优先同名分支，其次 main / master，本地都没有记录时才联网问远端默认分支。
        /// 推断不出来时返回空串。
        /// </summary>
        public static string SuggestUpstream(string branch, string remote = "origin")
        {
            if (string.IsNullOrEmpty(branch)) return "";

            string[] candidates = { branch, "main", "master" };
            foreach (string candidate in candidates)
            {
                if (Run(10000, "rev-parse", "--verify", "--quiet",
                        $"refs/remotes/{remote}/{candidate}").Success)
                    return $"{remote}/{candidate}";
            }

            // 本地没有任何远端分支记录，才去问远端（需要网络）
            GitResult symref = Run(30000, "ls-remote", "--symref", remote, "HEAD");
            if (!symref.Success) return "";

            foreach (string raw in symref.StdOut.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (!line.StartsWith("ref:")) continue;

                // 形如 "ref: refs/heads/main\tHEAD"
                string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string target = parts[1];
                if (target.StartsWith("refs/heads/"))
                    return $"{remote}/{target.Substring("refs/heads/".Length)}";
            }

            return "";
        }

        /// <summary>列出未提交的改动（不含未跟踪文件），每项形如 " M path"</summary>
        public static List<string> GetDirtyFiles()
        {
            var list = new List<string>();
            GitResult r = Run(15000, "status", "--porcelain", "--untracked-files=no");
            if (!r.Success) return list;

            foreach (string line in r.StdOut.Split('\n'))
            {
                string s = line.TrimEnd('\r');
                if (s.Length > 3) list.Add(s);
            }
            return list;
        }

        /// <summary>检测未完成的 merge / rebase / cherry-pick</summary>
        public static string GetInProgressOperation()
        {
            GitResult gitDir = Run(10000, "rev-parse", "--git-dir");
            if (!gitDir.Success) return "";

            string dir = gitDir.StdOut.Trim();
            if (!Path.IsPathRooted(dir)) dir = Path.Combine(RepoRoot, dir);

            if (Directory.Exists(Path.Combine(dir, "rebase-merge"))) return "rebase";
            if (Directory.Exists(Path.Combine(dir, "rebase-apply"))) return "rebase";
            if (File.Exists(Path.Combine(dir, "MERGE_HEAD"))) return "merge";
            if (File.Exists(Path.Combine(dir, "CHERRY_PICK_HEAD"))) return "cherry-pick";
            return "";
        }

        /// <summary>
        /// Unity 是否正在运行。运行中拉取会与 Unity 的文件写入互相打架，
        /// 所以每次都提醒用户先关掉编辑器。
        /// </summary>
        public static bool IsUnityRunning()
        {
            try
            {
                return File.Exists(Path.Combine(RepoRoot, "Temp", "UnityLockfile"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>fetch 远端（只更新远端引用，不动工作区）</summary>
        public static GitResult Fetch(string remote = "origin")
        {
            return Run(180000, "fetch", "--prune", remote);
        }

        // ------------------------------------------------------------------ 变更分类

        /// <summary>把一个仓库相对路径归类到对应的 Unity 影响类别</summary>
        public static Category Classify(string path)
        {
            path = path.Replace('\\', '/');

            if (path == "ProjectSettings/ProjectVersion.txt")
                return Make("版本升级", ImpactLevel.FullReimport, "需要安装对应 Unity 版本，否则无法打开工程");

            if (path.StartsWith("ProjectSettings/"))
                return Make("工程配置", ImpactLevel.FullReimport, "大概率触发全量 Reimport（最耗时，建议先关闭 Unity）");

            if (path.StartsWith("Packages/"))
                return Make("包依赖", ImpactLevel.FullReimport, "需要重新解析包，可能触发全量 Reimport");

            if (path.StartsWith(".vs/") || path.StartsWith("obj/") || path.StartsWith("UserSettings/")
                || path.StartsWith("Logs/") || path.StartsWith("Library/") || path.StartsWith("Temp/"))
                return Make("本地生成物", ImpactLevel.None, "本地缓存，不该进版本库");

            if (path.EndsWith(".csproj") || path.EndsWith(".sln"))
                return Make("IDE工程", ImpactLevel.None, "仅 IDE 工程文件，Unity 会自行重生成");

            if (path.StartsWith("Assets/") && path.EndsWith(".dll"))
                return Make("插件程序集", ImpactLevel.Recompile, "重编译所有引用该程序集的脚本");

            if (path.EndsWith(".cs"))
                return Make("C#脚本", ImpactLevel.Recompile, "增量重编译（通常几秒~几十秒）");

            if (path.EndsWith(".meta"))
                return Make("资源元数据", ImpactLevel.Low, "可能引起资源引用重连");

            if (path.StartsWith("Assets/"))
                return Make("资源", ImpactLevel.Low, "局部 Reimport");

            return Make("其他", ImpactLevel.Low, "—");
        }

        private static Category Make(string name, ImpactLevel level, string impact)
        {
            var c = new Category();
            c.Name = name;
            c.Level = level;
            c.Impact = impact;
            return c;
        }

        /// <summary>
        /// 生成更新预览：比较 HEAD 与 upstream，逐文件分类。
        /// 调用前需先 Fetch。
        /// </summary>
        public static UpdatePreview BuildPreview()
        {
            var preview = new UpdatePreview();
            preview.Branch = GetCurrentBranch();
            if (string.IsNullOrEmpty(preview.Branch)) return preview;

            preview.Upstream = GetUpstream(preview.Branch);
            if (string.IsNullOrEmpty(preview.Upstream)) return preview;

            GitResult behind = Run(20000, "rev-list", "--count", "HEAD.." + preview.Upstream);
            GitResult ahead = Run(20000, "rev-list", "--count", preview.Upstream + "..HEAD");
            if (!behind.Success || !ahead.Success) return preview;

            int.TryParse(behind.StdOut.Trim(), out preview.Behind);
            int.TryParse(ahead.StdOut.Trim(), out preview.Ahead);

            if (preview.Behind == 0) return preview;

            GitResult diff = Run(60000, "diff", "--name-status", "HEAD.." + preview.Upstream);
            if (!diff.Success) return preview;

            foreach (string raw in diff.StdOut.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0) continue;

                // 每行形如 "M<TAB>Assets/Foo.cs"，重命名是 "R100<TAB>旧<TAB>新"
                string[] parts = line.Split('\t');
                if (parts.Length < 2) continue;

                string status = parts[0];
                string path = parts[parts.Length - 1];   // 重命名/复制时取最后一列的目标路径

                preview.Items.Add(new ChangeItem
                {
                    Status = status.Substring(0, 1),
                    Path = path,
                    Category = Classify(path),
                });
            }

            return preview;
        }

        /// <summary>把变更列表按类别聚合，用于汇总表格</summary>
        public static List<KeyValuePair<Category, int>> Summarize(List<ChangeItem> items)
        {
            var counts = new Dictionary<string, KeyValuePair<Category, int>>();

            foreach (ChangeItem item in items)
            {
                string key = item.Category.Name;
                if (counts.TryGetValue(key, out KeyValuePair<Category, int> existing))
                    counts[key] = new KeyValuePair<Category, int>(existing.Key, existing.Value + 1);
                else
                    counts[key] = new KeyValuePair<Category, int>(item.Category, 1);
            }

            var list = new List<KeyValuePair<Category, int>>(counts.Values);
            list.Sort((a, b) =>
            {
                int byLevel = b.Key.Level.CompareTo(a.Key.Level);
                return byLevel != 0 ? byLevel : b.Value.CompareTo(a.Value);
            });
            return list;
        }

        /// <summary>统计高影响文件数量（等级 3）</summary>
        public static int CountHighImpact(List<ChangeItem> items)
        {
            int n = 0;
            foreach (ChangeItem item in items)
                if (item.Category.Level == ImpactLevel.FullReimport) n++;
            return n;
        }

        /// <summary>fast-forward 拉取。失败时返回 git 的原始错误，由调用方展示处理建议</summary>
        public static GitResult Pull()
        {
            string branch = GetCurrentBranch();
            GitResult r = Run(180000, "pull", "--ff-only");

            // upstream 没配好时上一条会失败，退回到显式指定远端分支
            if (!r.Success && !string.IsNullOrEmpty(branch))
            {
                GitResult retry = Run(180000, "pull", "--ff-only", "origin", branch);
                if (retry.Success) return retry;
            }

            return r;
        }
    }
}
