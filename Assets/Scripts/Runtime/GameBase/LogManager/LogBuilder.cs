using System.Diagnostics;
using UnityEngine;

// ══════════ 下面这行 【#line hidden】 是为了 Console 双击跳转，不要删 ══════════
// Unity Console 双击一条日志时，跳转目标是【堆栈里第一个带文件路径的帧】。
// 直接写 Debug.Log 时，那个帧就是业务代码本身，所以跳得对；
// 而本类是包装层，真正调用 UnityEngine.Debug.Log 的帧在包装文件里 ——
// 不加处理的话双击永远跳进本文件，跳不到业务代码。
//   · 实测未加本指令：LogEntry.file = ChaosLog.cs（跳错）
//   · 实测加了本指令：LogEntry.file = FileUtil.cs, line = 20 / 36（正确落到调用方）
// #line hidden 让本文件不生成序列点，这些包装帧就报不出文件名，被 Unity 自动跳过。
// 注意：消息正文里的「── at 文件:行号」来自 [CallerFilePath] / [CallerLineNumber]，
// 取的是【调用方】的位置，完全不受本指令影响，依旧准确。
// 代价：调试时无法单步进入本文件 —— 对日志工具类无所谓。
#line hidden

namespace ChaosDebug
{
    /// <summary>
    /// 链式日志构造器。由 <see cref="ChaosLog.Write(string)"/> 系列方法创建。
    ///
    /// 用法（配置链 + 终端）：
    /// <code>
    /// ChaosLog.Write("连接成功").Emit();                                    // 默认白，等价于 ChaosLog.Info
    /// ChaosLog.Write("连接失败").Red().Emit();                              // 正文染红
    /// ChaosLog.Write("重连中").Channel(LogChannel.Network).Orange().Warn(); // 频道 + 颜色 + 级别
    /// ChaosLog.Write("异常").Hex("#FF88CC").Trace().Error();                // 自定义色 + 调用链 + 错误
    /// </code>
    ///
    /// ══════════ 两条规则 ══════════
    ///
    /// 1. 【配置方法】Channel / With / Trace / Color / Hex / Red / Orange ... 一律返回自身，
    ///    可以任意顺序串联，但【不会输出任何东西】。顺序无所谓，怎么排都行。
    ///
    /// 2. 【终端方法】Emit / Debug / Info / Success / Warn / Error —— 调用即输出，且必须放在最后。
    ///    只有一个终端能出现在链尾，因为终端不返回自身。
    ///
    /// 换句话说：链式表达式最后那个方法决定了它属于哪个级别。
    /// 想让正文变成红色但还要是 Warn，就写 .Red().Warn()，而不是 .Red() 单独收尾。
    ///
    /// ══════════ 为什么不用「颜色方法直接输出」 ══════════
    ///
    /// 那样写起来是短一点（.Red() 即输出），但会立刻失去「颜色 + 级别」的组合能力：
    /// .Orange().Warn() 这种就没法表达了，因为 .Orange() 已经把日志发出去了。
    /// 级别作为终端能让每种组合都写得出来，是更有扩展性的取舍。
    ///
    /// ══════════ 编译期裁剪 ══════════
    ///
    /// Emit / Debug / Info / Success 是 [Conditional] 的，正式发布包里会被编译器移除，
    /// 连同链上其他调用一起消失。Warn / Error 不裁剪，始终保留。
    /// 由于本类型是 struct，即使编译器保留了链上的配置方法，也只是几次栈上的字段赋值，
    /// 不产生堆分配、不格式化成字符串 —— 真正的开销都在被裁掉的终端方法里。
    /// </summary>
    public struct LogBuilder
    {
        private LogLevel _level;
        private LogChannel _channel;
        private string _msg;
        private UnityEngine.Object _context;
        private string _color;   // null = 不加颜色标签（默认）
        private bool _trace;

        // 调用点信息，由 ChaosLog.Write 的 [CallerXxx] 特性在入口处捕获
        private string _member;
        private string _file;
        private int _line;

        internal LogBuilder(LogLevel level, LogChannel channel, string msg, UnityEngine.Object context,
                            string member, string file, int line)
        {
            _level = level;
            _channel = channel;
            _msg = msg;
            _context = context;
            _color = null;
            _trace = false;
            _member = member;
            _file = file;
            _line = line;
        }

        // ══════════════════ 配置方法（返回自身，不输出） ══════════════════

        /// <summary>切换频道（模块分类），影响 [前缀] 的颜色。不指定则为「通用」。</summary>
        public LogBuilder Channel(LogChannel channel)
        {
            _channel = channel;
            return this;
        }

        /// <summary>
        /// 附加 context。传 this（MonoBehaviour）后，双击 Console 条目可直接高亮场景中的该物体。
        /// </summary>
        public LogBuilder With(UnityEngine.Object context)
        {
            _context = context;
            return this;
        }

        /// <summary>让这条日志额外内联打印调用链（只保留项目代码帧）。</summary>
        public LogBuilder Trace()
        {
            _trace = true;
            return this;
        }

        /// <summary>用预设颜色给正文染色。传 LogColor.None 等价于不染色。</summary>
        public LogBuilder Color(LogColor color)
        {
            _color = LogColorInfo.GetHex(color);
            return this;
        }

        /// <summary>
        /// 用自定义十六进制色值给正文染色，形如 "#FF88CC" 或 "FF88CC"。
        /// 预设色不够用时走这个。
        /// </summary>
        public LogBuilder Hex(string hex)
        {
            _color = NormalizeHex(hex);
            return this;
        }

        // 以下是常用颜色的快捷写法，等价于 .Color(LogColor.Xxx)
        public LogBuilder White() { _color = "#FFFFFF"; return this; }
        public LogBuilder Gray() { _color = "#9E9E9E"; return this; }
        public LogBuilder Red() { _color = "#EF5350"; return this; }
        public LogBuilder Orange() { _color = "#FFB74D"; return this; }
        public LogBuilder Yellow() { _color = "#FFEE58"; return this; }
        public LogBuilder Green() { _color = "#81C784"; return this; }
        public LogBuilder Cyan() { _color = "#4DD0E1"; return this; }
        public LogBuilder Blue() { _color = "#64B5F6"; return this; }
        public LogBuilder Purple() { _color = "#BA68C8"; return this; }
        public LogBuilder Pink() { _color = "#F06292"; return this; }

        // ══════════════════ 终端方法（调用即输出，必须收尾） ══════════════════

        /// <summary>以 Info 级别输出。链尾不写级别时用它。仅编辑器 / 开发版保留。</summary>
        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Emit()
        {
            _level = LogLevel.Info;
            EmitCore();
        }

        /// <summary>以 Debug 级别输出（还需 ChaosLog.EnableDebug = true）。仅编辑器 / 开发版保留。</summary>
        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Debug()
        {
            _level = LogLevel.Debug;
            EmitCore();
        }

        /// <summary>以 Info 级别输出。仅编辑器 / 开发版保留。</summary>
        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Info()
        {
            _level = LogLevel.Info;
            EmitCore();
        }

        /// <summary>以 Success 级别输出。仅编辑器 / 开发版保留。</summary>
        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Success()
        {
            _level = LogLevel.Success;
            EmitCore();
        }

        /// <summary>以 Warn 级别输出。正式包中依然保留。</summary>
        [HideInCallstack]
        public void Warn()
        {
            _level = LogLevel.Warn;
            EmitCore();
        }

        /// <summary>以 Error 级别输出。正式包中依然保留。</summary>
        [HideInCallstack]
        public void Error()
        {
            _level = LogLevel.Error;
            EmitCore();
        }

        // ══════════════════ 内部 ══════════════════

        [HideInCallstack]
        private void EmitCore()
        {
            ChaosLog.Emit(_level, _channel, _msg, _context, _member, _file, _line, _trace, _color);
        }

        /// <summary>接受 "#RRGGBB" 或 "RRGGBB"；空值返回 null（表示不染色）。</summary>
        private static string NormalizeHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
            return hex[0] == '#' ? hex : "#" + hex;
        }
    }
}

#line default