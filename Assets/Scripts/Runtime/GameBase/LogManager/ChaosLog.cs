using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ChaosDebug
{
    /// <summary>
    /// 日志级别。数值越大越严重，用于 MinLevel 过滤。
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Success = 2,
        Warn = 3,
        Error = 4,
    }

    /// <summary>
    /// Project_Chaos 统一日志入口。
    ///
    /// 设计目标：
    ///   1. 每条日志自带调用点（文件:行号:方法），告警还能内联打印完整调用链 —— 也就是「溯源」。
    ///   2. 级别 + 频道双维度着色，在 Console 里一眼分辨「多严重 / 哪个模块 / 从哪来」。
    ///
    /// 为什么不叫 Debug：项目里几乎每个文件都有 using UnityEngine;，
    /// 定义 Debug 会与 UnityEngine.Debug 二义，全项目编译报错。
    ///
    /// ══════════ 典型用法 ══════════
    ///   ChaosLog.Info("连接成功");                                  // 通用频道
    ///   ChaosLog.Info(LogChannel.Network, "连接成功");               // 指定频道
    ///   ChaosLog.Warn(this, "面板未注册 PanelId=3");                 // 带 context，双击 Console 条目可高亮该物体
    ///   ChaosLog.WarnTrace("寻路失败");                              // 告警 + 内联调用链
    ///
    /// 调用点信息由编译器经 [CallerFilePath] 等特性自动填入，调用方无需手写任何参数。
    /// </summary>
    public static class ChaosLog
    {
        // ══════════════════ 配置开关 ══════════════════

        /// <summary>总开关。关闭后所有级别（含 Warn / Error）都不输出。</summary>
        public static bool Enabled = true;

        /// <summary>Debug 级别单独开关，默认关闭（该级别最啰嗦，通常是逐帧/逐步输出）。</summary>
        public static bool EnableDebug = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 最低输出级别。编辑器与开发版默认 Debug（全都放出来）；
        /// 正式包中 Info 及以下已被编译期裁剪，只剩 Warn / Error，故默认 Warn。
        /// </summary>
        public static LogLevel MinLevel = LogLevel.Debug;
#else
        /// <summary>
        /// 最低输出级别。正式包中 Info 及以下已被编译期裁剪，只剩 Warn / Error，故默认 Warn。
        /// 需要临时静音线上告警时改这个值即可。
        /// </summary>
        public static LogLevel MinLevel = LogLevel.Warn;
#endif

        /// <summary>是否在消息里内联调用点（文件:行号:方法）。默认开启，这是溯源的主力。</summary>
        public static bool ShowCallSite = true;

        /// <summary>
        /// 是否在消息前加时间戳，默认【关闭】。
        ///
        /// 注意：Unity Console 的 Collapse 是按消息文本去重的，一旦每条日志都带时间戳，
        /// 就永远折叠不成一条，循环刷屏的日志会瞬间淹没 Console。
        /// 而调用点的「文件:行号」不会破坏折叠（同一行反复调用时文本相同，照样能折叠）。
        /// 因此默认不带时间戳，排查特定问题时再临时打开。
        /// </summary>
        public static bool ShowTimestamp = false;

        /// <summary>让所有 Warn 都自动附带内联调用链（不必改用 WarnTrace）。调用链抓取开销大，默认关闭。</summary>
        public static bool TraceOnWarn = false;

        /// <summary>内联调用链最多渲染多少帧项目代码。</summary>
        public static int MaxTraceFrames = 12;

        // ══════════════════ 配色 ══════════════════

        private const string DimColor = "#78909C";     // 调用点 / 调用链用的暗灰
        private const string DebugColor = "#9E9E9E";   // 灰
        private const string InfoColor = "#4FC3F7";    // 青蓝
        private const string SuccessColor = "#81C784"; // 绿
        private const string WarnColor = "#FFB74D";    // 橙
        private const string ErrorColor = "#EF5350";   // 红

        // ══════════════════ 内部实现 ══════════════════

        // 每条日志都新建 StringBuilder 会产生大量垃圾，这里按线程复用。
        [ThreadStatic] private static StringBuilder _builder;

        private static StringBuilder Builder
        {
            get
            {
                if (_builder == null) _builder = new StringBuilder(256);
                return _builder;
            }
        }

        private static readonly char[] PathSeparators = { '/', '\\' };

        private static bool ShouldEmit(LogLevel level)
        {
            if (!Enabled) return false;
            if (level == LogLevel.Debug && !EnableDebug) return false;
            return level >= MinLevel;
        }

        /// <summary>级别前缀，统一 5 字符宽以便大致对齐。</summary>
        private static string GetLevelPrefix(LogLevel level)
        {
            string word;
            string color;
            switch (level)
            {
                case LogLevel.Debug: word = "DEBUG"; color = DebugColor; break;
                case LogLevel.Info: word = "INFO "; color = InfoColor; break;
                case LogLevel.Success: word = "OK   "; color = SuccessColor; break;
                case LogLevel.Warn: word = "WARN "; color = WarnColor; break;
                default: word = "ERROR"; color = ErrorColor; break;
            }
            // 这里刻意只用 <color>，不加 <b>。
            // <color> 有实证支持（Unity 官方 TextMeshPro 包在 TMP_SpriteAssetMenu.cs:68 就用了同样的写法），
            // 而 Console 对 <b> 的支持未经验证 —— 一旦不支持就会把标签当字面量打出来，每条日志都脏。
            // 想试加粗的话，自行把下面的字符串改成 "<color=" + color + "><b>" + word + "</b></color>" 再看一眼 Console。
            return "<color=" + color + ">" + word + "</color>";
        }

        /// <summary>
        /// [CallerFilePath] 给的是编译期绝对路径，直接用会泄露开发机路径且让消息过长，这里只取文件名。
        /// </summary>
        private static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "?";
            int slash = path.LastIndexOfAny(PathSeparators);
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        /// <summary>
        /// 内联渲染托管调用链，只保留项目代码帧。
        /// UnityEngine / UnityEditor / System 以及本日志类自身的帧都会跳过，否则噪音会盖过有效信息。
        /// </summary>
        private static void AppendInlineTrace(StringBuilder sb)
        {
            StackTrace trace;
            try
            {
                trace = new StackTrace(true); // true = 尝试带上文件与行号（编辑器内可用）
            }
            catch
            {
                return;
            }

            int shown = 0;
            for (int i = 0; i < trace.FrameCount && shown < MaxTraceFrames; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                if (frame == null) continue;

                System.Reflection.MethodBase method = frame.GetMethod();
                if (method == null) continue;

                System.Type type = method.DeclaringType;
                if (type == null) continue;

                // 过滤掉框架帧，只留项目代码
                string ns = type.Namespace ?? string.Empty;
                if (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor") ||
                    ns.StartsWith("System") || ns.StartsWith("ChaosDebug"))
                {
                    continue;
                }

                sb.Append('\n');
                sb.Append("    <color=").Append(DimColor).Append(">└─ ");
                sb.Append(shown == 0 ? "→ " : "  ");
                sb.Append(type.Name).Append('.').Append(method.Name).Append("()");

                string frameFile = frame.GetFileName();
                if (!string.IsNullOrEmpty(frameFile))
                {
                    sb.Append(" in ").Append(GetFileName(frameFile))
                      .Append(':').Append(frame.GetFileLineNumber());
                }
                sb.Append("</color>");

                shown++;
            }
        }

        /// <summary>
        /// 所有级别的统一出口。组装富文本消息后转发给 UnityEngine.Debug。
        /// </summary>
        /// <param name="bodyColor">
        /// 正文颜色（#RRGGBB）。传 null 表示【不加颜色标签】，正文保持 Unity 原生白 —— 这是默认情况。
        /// 注意频道前缀与级别前缀的颜色不受此参数影响，那是自动配的。
        /// </param>
        internal static void Emit(LogLevel level, LogChannel channel, string msg,
                                  UnityEngine.Object context,
                                  string member, string file, int line,
                                  bool withTrace, string bodyColor)
        {
            if (!ShouldEmit(level)) return;

            StringBuilder sb = Builder;
            sb.Length = 0;

            if (ShowTimestamp)
            {
                sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
            }

            // [频道] <级别> 正文 ── at 文件:行号 in 方法()
            sb.Append(LogChannelInfo.GetTaggedName(channel)).Append(' ');
            sb.Append(GetLevelPrefix(level)).Append(' ');

            // 正文默认【不加任何颜色标签】，保持 Unity 原生白。
            // 只有显式指定了颜色（走 ChaosLog.Write(...).Red() 这类链式调用）才包一层 <color>。
            if (bodyColor != null)
            {
                sb.Append("<color=").Append(bodyColor).Append('>')
                  .Append(msg)
                  .Append("</color>");
            }
            else
            {
                sb.Append(msg);
            }

            if (ShowCallSite && !string.IsNullOrEmpty(member))
            {
                sb.Append(" <color=").Append(DimColor).Append(">── at ")
                  .Append(GetFileName(file)).Append(':').Append(line)
                  .Append(" in ").Append(member).Append("()</color>");
            }

            if (withTrace || (TraceOnWarn && level == LogLevel.Warn))
            {
                AppendInlineTrace(sb);
            }

            string final = sb.ToString();

            // 注意：本文件 using 了 System.Diagnostics（为了 Conditional / StackTrace），
            // 裸写 Debug 会与 System.Diagnostics.Debug 二义，因此这里必须全限定 UnityEngine.Debug。
            switch (level)
            {
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(final, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(final, context);
                    break;
                default:
                    UnityEngine.Debug.Log(final, context);
                    break;
            }
        }

        // ══════════════════ 链式入口：ChaosLog.Write(...) ══════════════════
        //
        // 返回 LogBuilder，中间可以任意串联 .Red() / .Channel(...) / .Trace() 等配置方法，
        // 最后用一个终端方法（.Emit() / .Warn() / .Error() ...）收尾输出。
        // 末尾是哪个终端，就决定这条日志属于哪个级别。
        //
        // 【为什么这几个入口没有 [Conditional]】
        // 返回非 void 的方法不允许加 [Conditional]，所以入口必须始终存在。
        // 但它们只做几次 struct 字段赋值 —— 不格式化字符串、不产生堆分配。
        // 真正的开销（拼串、抓栈、输出）全在终端方法里，而终端方法在正式包中会被裁掉。

        /// <summary>链式入口。示例：ChaosLog.Write("连接失败").Red().Emit();</summary>
        public static LogBuilder Write(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new LogBuilder(LogLevel.Info, LogChannel.General, msg, null, member, file, line);
        }

        /// <summary>链式入口，指定频道。示例：ChaosLog.Write(LogChannel.UI, "未注册").Red().Emit();</summary>
        public static LogBuilder Write(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new LogBuilder(LogLevel.Info, channel, msg, null, member, file, line);
        }

        /// <summary>链式入口，带 context（双击 Console 条目可定位到物体）。</summary>
        public static LogBuilder Write(UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new LogBuilder(LogLevel.Info, LogChannel.General, msg, context, member, file, line);
        }

        /// <summary>链式入口，带频道与 context。</summary>
        public static LogBuilder Write(LogChannel channel, UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new LogBuilder(LogLevel.Info, channel, msg, context, member, file, line);
        }

        // ══════════════════ 普通日志：仅在编辑器 / 开发版中保留 ══════════════════
        //
        // [Conditional] 是【或】语义：只要定义了 UNITY_EDITOR 或 DEVELOPMENT_BUILD 任一符号，
        // 方法就会被编译进程序集。而这两个宏由 Unity 自动定义，因此
        //
        //     正式发布包中这些方法的【调用点会被编译器整体移除】——零调用开销、零字符串分配、零体积。
        //
        // 好处是不需要在 Project Settings 的 Scripting Define Symbols 里手工维护自定义宏，
        // 也不会因为有人忘了加宏而导致日志被误裁剪。

        /// <summary>调试细节（默认不输出，需打开 EnableDebug）。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Debug, LogChannel.General, msg, null, member, file, line, false, null);
        }

        /// <summary>调试细节，指定频道。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Debug, channel, msg, null, member, file, line, false, null);
        }

        /// <summary>普通流程信息。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Info, LogChannel.General, msg, null, member, file, line, false, null);
        }

        /// <summary>普通流程信息，指定频道。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Info, channel, msg, null, member, file, line, false, null);
        }

        /// <summary>
        /// 普通流程信息，带 context。传入 this（MonoBehaviour）后，
        /// 在 Console 里双击该条目会直接高亮/定位到场景中的这个物体，比翻堆栈快得多。
        /// 仅编辑器 / 开发版保留。
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Info, LogChannel.General, msg, context, member, file, line, false, null);
        }

        /// <summary>普通流程信息，带频道与 context。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(LogChannel channel, UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Info, channel, msg, context, member, file, line, false, null);
        }

        /// <summary>成功节点（加载完成、连接成功、初始化完毕等）。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Success(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Success, LogChannel.General, msg, null, member, file, line, false, null);
        }

        /// <summary>成功节点，指定频道。仅编辑器 / 开发版保留。</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Success(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Success, channel, msg, null, member, file, line, false, null);
        }

        // ══════════════════ 告警与错误：始终编译进包 ══════════════════
        //
        // 不加 [Conditional]，正式发布版也保留 —— 线上同样需要可观测性。
        // 需要静音时用 ChaosLog.Enabled / ChaosLog.MinLevel 在运行时控制。

        /// <summary>告警。正式包中依然保留，可用 Enabled / MinLevel 运行时静音。</summary>
        public static void Warn(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, LogChannel.General, msg, null, member, file, line, false, null);
        }

        /// <summary>告警，指定频道。</summary>
        public static void Warn(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, channel, msg, null, member, file, line, false, null);
        }

        /// <summary>告警，带 context（双击 Console 条目可定位到物体）。</summary>
        public static void Warn(UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, LogChannel.General, msg, context, member, file, line, false, null);
        }

        /// <summary>告警，带频道与 context。</summary>
        public static void Warn(LogChannel channel, UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, channel, msg, context, member, file, line, false, null);
        }

        /// <summary>
        /// 告警 + 内联调用链。用于「必须知道是谁调过来的」的场合。
        /// 抓取调用链开销明显大于普通日志，所以只在需要时显式调用；
        /// 若想让所有 Warn 都带上，改 ChaosLog.TraceOnWarn = true。
        /// </summary>
        public static void WarnTrace(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, LogChannel.General, msg, null, member, file, line, true, null);
        }

        /// <summary>告警 + 内联调用链，指定频道。</summary>
        public static void WarnTrace(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, channel, msg, null, member, file, line, true, null);
        }

        /// <summary>告警 + 内联调用链，带 context。</summary>
        public static void WarnTrace(UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Warn, LogChannel.General, msg, context, member, file, line, true, null);
        }

        /// <summary>错误。正式包中依然保留，可用 Enabled / MinLevel 运行时静音。</summary>
        public static void Error(string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Error, LogChannel.General, msg, null, member, file, line, false, null);
        }

        /// <summary>错误，指定频道。</summary>
        public static void Error(LogChannel channel, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Error, channel, msg, null, member, file, line, false, null);
        }

        /// <summary>错误，带 context（双击 Console 条目可定位到物体）。</summary>
        public static void Error(UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Error, LogChannel.General, msg, context, member, file, line, false, null);
        }

        /// <summary>错误，带频道与 context。</summary>
        public static void Error(LogChannel channel, UnityEngine.Object context, string msg,
            [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Emit(LogLevel.Error, channel, msg, context, member, file, line, false, null);
        }
    }
}
