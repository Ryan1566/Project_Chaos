namespace ChaosDebug
{
    /// <summary>
    /// 日志正文的文字颜色。
    ///
    /// 注意 None 是【默认值】，含义是「不加任何颜色标签」，正文保持 Unity 原生白。
    /// 也就是说：不指定颜色 = 零额外字符，不会往消息里塞标签。
    ///
    /// 频道前缀 [网络] 与级别前缀 INFO/WARN 的颜色不受这里影响，那是自动配的，
    /// 见 LogChannel.cs 与 ChaosLog.GetLevelPrefix。
    /// </summary>
    public enum LogColor
    {
        None = 0,   // 不加颜色标签（默认）
        White,
        Gray,
        Red,
        Orange,
        Yellow,
        Green,
        Cyan,
        Blue,
        Purple,
        Pink,
    }

    /// <summary>
    /// LogColor 到十六进制色值的映射表。索引 = 枚举值。
    /// </summary>
    public static class LogColorInfo
    {
        // 与 LogColor 一一对应。None 刻意留 null —— 表示「不加颜色标签」而不是「加一个白色标签」。
        private static readonly string[] Hexes =
        {
            null,        // None   —— 不加标签
            "#FFFFFF",   // White
            "#9E9E9E",   // Gray
            "#EF5350",   // Red
            "#FFB74D",   // Orange
            "#FFEE58",   // Yellow
            "#81C784",   // Green
            "#4DD0E1",   // Cyan
            "#64B5F6",   // Blue
            "#BA68C8",   // Purple
            "#F06292",   // Pink
        };

        /// <summary>取十六进制色值。None 或越界时返回 null，表示不加颜色标签。</summary>
        public static string GetHex(LogColor color)
        {
            int i = (int)color;
            if (i < 0 || i >= Hexes.Length) return null;
            return Hexes[i];
        }
    }
}
