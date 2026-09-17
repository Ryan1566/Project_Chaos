using UnityEngine;

namespace ChaosDebug
{
    /// <summary>
    /// 日志系统启动配置。自动执行，无需手动挂载任何物体。
    ///
    /// 这里只做一件事：调整 Unity 各日志类型的堆栈抓取级别，让「告警能溯源」。
    /// 项目原设置为全套 ScriptOnly，且堆栈要点开 Console 条目才看得到，刷屏时基本不可用；
    /// 打包后更是完全没有堆栈。改为对 Warning 及以上抓 Full（托管 + 原生）后，
    /// 选中任意一条告警，Console 底部就是完整来路。
    ///
    /// 与 ChaosLog.WarnTrace() 的关系：这里是 Unity 原生的堆栈面板（被动、完整），
    /// WarnTrace 是把调用链内联进消息正文（主动、只留项目代码、能进日志文件）。
    /// 两者互补，一起用。
    /// </summary>
    public static class LogBootstrap
    {
        /// <summary>
        /// 进入 Play 模式 / 运行时自动执行（SubsystemRegistration 早于场景加载）。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitAtRuntime()
        {
            Apply();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器启动时也应用一次，这样【非 Play 模式】下的告警（导入、编辑器工具报的）同样有堆栈。
        /// Application.SetStackTraceLogType 只作用于当前会话，不会写进 ProjectSettings，
        /// 所以每次编辑器启动都要重新设置一遍。
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitInEditor()
        {
            Apply();
        }
#endif

        private static void Apply()
        {
            // 告警及以上：抓完整托管 + 原生调用链
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);

            // 普通日志：只抓托管（脚本）栈，不抓原生栈。
            // 这里千万不能设成 None —— Console 里双击一条日志跳转到源码，靠的就是这段堆栈；
            // 设成 None 会让 Info / Debug / Success 全部失去双击跳转能力（Warn / Error 不受影响，
            // 因为它们走上面的 Full）。项目原本是 ScriptOnly，别退回去。
            // 选 ScriptOnly 而非 Full：双击跳转只需要托管栈，Full 还要解析原生符号，明显更慢。
            // 性能代价可控：Info 带了 [Conditional]，正式包里已被编译期裁掉，
            // 这条设置实际只作用于编辑器与开发版 —— 开发便利比这点开销重要。
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        }
    }
}
