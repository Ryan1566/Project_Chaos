using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一档分辨率预设。不可变结构体，作为值传递。
/// </summary>
public struct ResolutionOption
{
    public readonly int Width;
    public readonly int Height;
    /// <summary>宽高比文案，如 "16:9"。写死而不实时算：1680×1050 算出来是 1.6，四舍五入到 "16:10" 需要一堆特判，不如直接标。</summary>
    public readonly string Aspect;
    /// <summary>档位别名，如 "720p" / "1K" / "2K" / "4K"；无通用叫法的档位为空串。</summary>
    public readonly string Tier;

    public ResolutionOption(int width, int height, string aspect, string tier)
    {
        Width = width;
        Height = height;
        Aspect = aspect;
        Tier = tier;
    }

    /// <summary>选择器上显示的文案，如 "1920 × 1080 (16:9)  ·  1K"。</summary>
    public string DisplayText
    {
        get
        {
            string text = Width + " × " + Height + " (" + Aspect + ")";
            if (!string.IsNullOrEmpty(Tier)) text += "  ·  " + Tier;
            return text;
        }
    }

    public override string ToString()
    {
        return Width + "x" + Height;
    }
}

/// <summary>
/// 分辨率档位的定义与"按玩家显示器能力过滤"的逻辑。
///
/// ══════════ 为什么要有"过滤"这一步 ══════════
/// 需求要求"初始化时检测玩家电脑，考虑最高 1K / 2K / 4K"。
/// 如果无脑把 8 档全列出来，1080p 显示器的玩家也能选中 3840×2160 ——
/// 那在独占全屏下会切到一个显示器根本不支持的信号，结果是黑屏或掉回桌面。
/// 所以这里按 Screen.resolutions 报出的最大宽高做一次上限裁剪。
///
/// ⚠ Screen.resolutions 并不可靠：编辑器/某些平台/无头环境会返回空数组。
/// 检测不到时【不过滤】（宁可多给几档），而不是把选项全砍光让玩家没法选。
/// </summary>
public static class ResolutionHelper
{
    /// <summary>
    /// 8 档预设。顺序即下拉/翻页顺序，从低到高。
    /// 16:9 是主流，16:10 那几档是给老显示器和笔记本留的兼容位。
    /// </summary>
    private static readonly ResolutionOption[] Presets =
    {
        new ResolutionOption(1280,  720, "16:9",  "720p"),
        new ResolutionOption(1600,  900, "16:9",  "900p"),
        new ResolutionOption(1680, 1050, "16:10", ""),
        new ResolutionOption(1920, 1080, "16:9",  "1K"),
        new ResolutionOption(1920, 1200, "16:10", ""),
        new ResolutionOption(2560, 1440, "16:9",  "2K"),
        new ResolutionOption(2560, 1600, "16:10", ""),
        new ResolutionOption(3840, 2160, "16:9",  "4K"),
    };

    public static int PresetCount { get { return Presets.Length; } }

    public static ResolutionOption GetPresetAt(int index)
    {
        if (index < 0) index = 0;
        if (index >= Presets.Length) index = Presets.Length - 1;
        return Presets[index];
    }

    /// <summary>
    /// 取显示器能报出的最大宽高。两个来源都拿不到时返回 0，调用方据此跳过过滤。
    /// </summary>
    public static void GetDisplayMax(out int maxWidth, out int maxHeight)
    {
        maxWidth = 0;
        maxHeight = 0;

        Resolution[] all = Screen.resolutions;
        if (all != null)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].width > maxWidth) maxWidth = all[i].width;
                if (all[i].height > maxHeight) maxHeight = all[i].height;
            }
        }

        if (maxWidth <= 0 || maxHeight <= 0)
        {
            Resolution current = Screen.currentResolution;
            maxWidth = current.width;
            maxHeight = current.height;
        }
    }

    /// <summary>
    /// 返回当前显示器可用的档位列表（宽高都不超过显示器上限）。
    /// 过滤后一个都不剩时会退回全量 —— 极端情况下也要保证页面不为空。
    /// </summary>
    public static List<ResolutionOption> GetAvailableOptions()
    {
        List<ResolutionOption> list = new List<ResolutionOption>();

        int maxWidth, maxHeight;
        GetDisplayMax(out maxWidth, out maxHeight);

        if (maxWidth <= 0 || maxHeight <= 0)
        {
            list.AddRange(Presets);
            return list;
        }

        for (int i = 0; i < Presets.Length; i++)
        {
            if (Presets[i].Width <= maxWidth && Presets[i].Height <= maxHeight)
            {
                list.Add(Presets[i]);
            }
        }

        if (list.Count == 0) list.AddRange(Presets);
        return list;
    }

    /// <summary>
    /// 在可用列表里找完全匹配的档位，找不到返回 -1。
    /// </summary>
    public static int IndexOf(List<ResolutionOption> list, int width, int height)
    {
        if (list == null) return -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Width == width && list[i].Height == height) return i;
        }
        return -1;
    }

    /// <summary>
    /// 找最接近的档位（按宽高差的平方和比）。用于两种情况：
    ///   ① 存档里的分辨率在当前显示器上不可用（换了显示器）；
    ///   ② 首次运行、存档值就是默认的 1920×1080 而显示器是别的规格。
    /// 列表为空时返回 -1，调用方自行兜底。
    /// </summary>
    public static int IndexOfClosest(List<ResolutionOption> list, int width, int height)
    {
        if (list == null || list.Count == 0) return -1;

        int best = 0;
        long bestCost = long.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            long dw = list[i].Width - width;
            long dh = list[i].Height - height;
            long cost = dw * dw + dh * dh;
            if (cost < bestCost)
            {
                bestCost = cost;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// 首次运行时用：挑当前显示器能支持的最大档位。
    /// 用"最大"而不是"原生" —— 4K 显示器上默认给 4K，玩家嫌卡自己会往下调；
    /// 反过来（默认给 720p）玩家会以为游戏画质差。
    /// 列表为空时返回 1920×1080 兜底。
    /// </summary>
    public static ResolutionOption GetBestAvailable()
    {
        List<ResolutionOption> list = GetAvailableOptions();
        if (list == null || list.Count == 0)
        {
            return new ResolutionOption(1920, 1080, "16:9", "1K");
        }
        return list[list.Count - 1];
    }
}
