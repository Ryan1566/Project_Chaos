namespace ChaosDebug
{
    /// <summary>
    /// 日志频道（系统模块分类）。用于在 Unity Console 中按颜色区分「这条日志是哪个模块打的」。
    ///
    /// 【扩展方式】新增一个频道只需两步：
    ///   1. 在本枚举【末尾】追加成员（务必加在最后，避免改变既有成员的值）
    ///   2. 在 LogChannelInfo 的 Names / Colors 两张表中各补一项，顺序与枚举一一对应
    ///
    /// 若忘记补表，运行时会自动回退到 General 的显示效果，不会抛异常。
    /// </summary>
    public enum LogChannel
    {
        General = 0,
        Network,
        UI,
        Audio,
        Config,
        Dialogue,
        Localization,
        Input,
        Pool,
        Scene,
        Save,
        Battle,
        Editor,
    }

    /// <summary>
    /// LogChannel 的显示名与配色表。索引 = 枚举值，两张表必须与 LogChannel 一一对应、顺序一致。
    /// </summary>
    public static class LogChannelInfo
    {
        // 与 LogChannel 一一对应（索引 = 枚举值）
        private static readonly string[] Names =
        {
            "通用",     // General
            "网络",     // Network
            "UI",       // UI
            "音频",     // Audio
            "配置",     // Config
            "对话",     // Dialogue
            "本地化",   // Localization
            "输入",     // Input
            "对象池",   // Pool
            "场景",     // Scene
            "存档",     // Save
            "战斗",     // Battle
            "编辑器",   // Editor
        };

        // 与 LogChannel 一一对应（索引 = 枚举值），十六进制 #RRGGBB，供 <color=...> 使用
        private static readonly string[] Colors =
        {
            "#B0BEC5",  // General      蓝灰
            "#4DD0E1",  // Network      青
            "#F06292",  // UI           粉
            "#BA68C8",  // Audio        紫
            "#AED581",  // Config       黄绿
            "#FFD54F",  // Dialogue     琥珀
            "#4DB6AC",  // Localization 蓝绿
            "#FF8A65",  // Input        珊瑚
            "#90A4AE",  // Pool         灰蓝
            "#7986CB",  // Scene        靛蓝
            "#A1887F",  // Save         棕
            "#E57373",  // Battle       红
            "#64B5F6",  // Editor       蓝
        };

        // 预拼好的 "[网络]" 富文本片段，避免每条日志都重新拼字符串。
        // 用静态构造函数构建，避免静态字段初始化顺序带来的坑。
        private static readonly string[] TaggedNames;

        static LogChannelInfo()
        {
            TaggedNames = new string[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                // 两张表长度不一致时，越界项留空，取用时回退到 General
                if (i >= Colors.Length) break;
                TaggedNames[i] = "<color=" + Colors[i] + ">[" + Names[i] + "]</color>";
            }
        }

        /// <summary>已定义的频道数量。</summary>
        public static int Count { get { return Names.Length; } }

        /// <summary>取频道中文短名。越界时回退到 General。</summary>
        public static string GetName(LogChannel channel)
        {
            int i = (int)channel;
            if (i < 0 || i >= Names.Length) return Names[0];
            return Names[i];
        }

        /// <summary>取频道颜色（#RRGGBB）。越界时回退到 General。</summary>
        public static string GetColor(LogChannel channel)
        {
            int i = (int)channel;
            if (i < 0 || i >= Colors.Length) return Colors[0];
            return Colors[i];
        }

        /// <summary>
        /// 取可直接拼进日志的 "[频道]" 富文本片段（已带颜色标签）。
        /// 越界或漏配表项时回退到 General 的片段。
        /// </summary>
        public static string GetTaggedName(LogChannel channel)
        {
            int i = (int)channel;
            if (i >= 0 && i < TaggedNames.Length && TaggedNames[i] != null) return TaggedNames[i];
            return TaggedNames[0];
        }
    }
}
