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

            // 普通日志：不抓栈。
            // 抓栈开销明显，而 Info 的量最大，全抓既拖慢运行又会把 Console 淹掉。
            // 需要普通日志的调用链时，走 ChaosLog 的内联调用点或 WarnTrace。
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
    }
}
