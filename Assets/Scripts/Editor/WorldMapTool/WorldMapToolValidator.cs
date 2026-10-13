using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>一条校验结果。`IsError = true` 表示**必须阻断导出**。</summary>
public class WorldMapToolIssue
{
    public bool IsError;
    public string Message;

    public WorldMapToolIssue(bool isError, string message)
    {
        IsError = isError;
        Message = message;
    }

    public static WorldMapToolIssue Error(string message) { return new WorldMapToolIssue(true, message); }
    public static WorldMapToolIssue Warning(string message) { return new WorldMapToolIssue(false, message); }

    public override string ToString()
    {
        return (IsError ? "[错误] " : "[警告] ") + Message;
    }
}

/// <summary>
/// 世界观地图绘制工具的**导出前校验**。
///
/// ══════════════════ 为什么校验必须存在（两个会毁掉整批导出的坑） ══════════════════
/// ① **顶层表数据行 0 条**：导出器 `GetValues`（`ConfigManager.cs:308-315`）按
///    `workSheet.Dimension.End.Row - valueIndex + 1` 建数组，只有表头（末行 = 6）时**长度 0**；
///    而页签**完全空白**更会直接抛 `Dimension == null` 的 NRE —— 两种情况都会**中断整批导出**，
///    其后所有表都不导出。所以「顶层表必须 ≥1 行数据」是硬拒绝。
/// ② **顶层同英文前缀的第二张表**：导出器按「文件名第一个 `_` 之前」推类名且**不查重**，
///    `WorldMap_别的.xlsx` 会导出同一个 `WorldMapConfig` 并**静默覆盖**。所以发现同前缀就硬拒绝。
///
/// 错误分两级：**Error = 阻断导出**；**Warning = 只提示**（例如自交 ——
/// `06_WorldFaction_字段规格.md` M7 明确说自交只提示不阻断）。
/// </summary>
public static class WorldMapToolValidator
{
    /// <summary>颜色必须是 `#RRGGBB`（7 字符）。</summary>
    public static bool IsValidColor(string color)
    {
        if (string.IsNullOrEmpty(color) || color.Length != 7 || color[0] != '#') return false;
        for (int i = 1; i < color.Length; i++)
        {
            char c = color[i];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }

    /// <summary>
    /// 字符串单元格是否合法：**禁英文逗号 / 英文双引号 / 换行**。
    /// 英文逗号会破坏整张表的 Json（导出器用 `Split(',')` 重组），是静默失败里最狠的一条。
    /// </summary>
    public static bool IsSafeString(string value, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(value)) return true;

        if (value.IndexOf(',') >= 0) { reason = "含英文逗号（会破坏整表 Json）"; return false; }
        if (value.IndexOf('"') >= 0) { reason = "含英文双引号（会破坏 Json）"; return false; }
        if (value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0) { reason = "含换行（会破坏 Json）"; return false; }
        return true;
    }

    private static string F(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    // ══════════════════ 地图表（WorldMap） ══════════════════

    /// <summary>
    /// 校验地图表。`forTopLevelExport = true` 时启用「≥1 行数据」硬约束
    /// （顶层表才有这个约束；子目录的表不参与导出）。
    /// </summary>
    public static List<WorldMapToolIssue> ValidateMapTable(WorldMapDraft draft, bool forTopLevelExport)
    {
        var issues = new List<WorldMapToolIssue>();
        if (draft == null)
        {
            issues.Add(WorldMapToolIssue.Error("草稿为 null，无法导出"));
            return issues;
        }

        WorldMapPointEntry[] points = draft.points != null ? draft.points.ToArray() : new WorldMapPointEntry[0];

        // 硬约束①：顶层表 0 行数据 → 拒绝（会让 GetValues 得到长度 0，中断整批导出）
        if (forTopLevelExport && points.Length == 0)
        {
            issues.Add(WorldMapToolIssue.Error(
                "地图表 0 行数据：顶层表只有表头会让导出器 GetValues 拿到长度 0（并可能中断整批导出）。" +
                "请先在地图上至少放 1 个点位。"));
            //0 行时后面的逐行检查没有意义，直接返回
            return issues;
        }

        if (draft.mapId != 1)
        {
            issues.Add(WorldMapToolIssue.Warning(
                "MapId = " + draft.mapId + "：当前只有 1 = 天下中原；其它底图尚未登记。"));
        }

        if (draft.imageWidth <= 0 || draft.imageHeight <= 0)
        {
            issues.Add(WorldMapToolIssue.Error(
                "源图真实像素尺寸非法（" + draft.imageWidth + "×" + draft.imageHeight +
                "）—— 坐标换算是按它做的，无法导出。请检查源图路径是否为有效 PNG。"));
        }

        var seenTriples = new Dictionary<string, int>();
        for (int i = 0; i < points.Length; i++)
        {
            WorldMapPointEntry p = points[i];
            int row = i + 7;   //表内 Excel 行号（第 7 行起是数据）

            if (p == null)
            {
                issues.Add(WorldMapToolIssue.Error("第 " + row + " 行：点位为 null"));
                continue;
            }

            if (!WorldMapToolGeometry.IsInsideUnitSquare(new Vector2(p.x, p.y)))
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：X/Y 越界 —— X=" + F(p.x) + "、Y=" + F(p.y) +
                    "，必须落在 0~1（越界会被裁掉）。"));
            }

            if (p.scale <= 0f)
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：Scale = " + F(p.scale) +
                    "，必须 > 0（0 或负数会让图标不可见/反向）。"));
            }
            else if (p.scale < 0.5f || p.scale > 2f)
            {
                issues.Add(WorldMapToolIssue.Warning(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：Scale = " + F(p.scale) + " 建议在 0.5~2 之间。"));
            }

            if (p.layer > 99)
            {
                issues.Add(WorldMapToolIssue.Warning(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：Layer = " + p.layer + " 建议 0~99。"));
            }

            if (p.kind != 1 && p.kind != 2)
            {
                issues.Add(WorldMapToolIssue.Warning(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：Kind = " + p.kind +
                    " 属预留类别（1=固定地点、2=兴趣点）；运行时遇到未知类别会跳过该行并记 warning。"));
            }

            string triple = p.kind + "/" + p.type + "/" + p.refId;
            if (seenTriples.ContainsKey(triple))
            {
                issues.Add(WorldMapToolIssue.Warning(
                    "第 " + row + " 行（点位 #" + (i + 1) + "）：(Kind,Type,RefId) = (" + triple +
                    ") 与第 " + seenTriples[triple] + " 行重复 —— 同一个地点会被画两个图标。"));
            }
            else
            {
                seenTriples.Add(triple, row);
            }
        }

        return issues;
    }

    // ══════════════════ 势力表（WorldFaction） ══════════════════

    /// <summary>
    /// 校验势力表。`forTopLevelExport = true` 时启用「≥1 行数据」硬约束
    /// （导出**合并表**到顶层时用 true；导出子目录里的**时期分表**时用 false）。
    /// </summary>
    public static List<WorldMapToolIssue> ValidateFactionTables(WorldMapDraft draft, bool forTopLevelExport)
    {
        var issues = new List<WorldMapToolIssue>();
        if (draft == null)
        {
            issues.Add(WorldMapToolIssue.Error("草稿为 null，无法导出"));
            return issues;
        }

        WorldMapFactionVertex[] all = draft.factionVertices != null
            ? draft.factionVertices.ToArray()
            : new WorldMapFactionVertex[0];

        // 硬约束①：顶层合并表 0 行数据 → 拒绝
        if (forTopLevelExport && all.Length == 0)
        {
            issues.Add(WorldMapToolIssue.Error(
                "势力合并表 0 行数据：顶层表只有表头会让导出器 GetValues 拿到长度 0（并可能中断整批导出）。" +
                "请先在至少一个时期画出至少 1 块区域。"));
            return issues;
        }

        // 逐顶点：时期合法性 / 坐标 / 颜色 / 字符串安全
        for (int i = 0; i < all.Length; i++)
        {
            WorldMapFactionVertex v = all[i];
            int row = i + 7;

            if (v == null)
            {
                issues.Add(WorldMapToolIssue.Error("第 " + row + " 行：顶点为 null"));
                continue;
            }

            if (v.stage < WorldMapToolDataIO.MinStage || v.stage > WorldMapToolDataIO.MaxStage)
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（顶点 #" + (i + 1) + "）：Stage = " + v.stage +
                    " 越界，合法范围 " + WorldMapToolDataIO.MinStage + "~" + WorldMapToolDataIO.MaxStage +
                    "（未知时期的数据永远不会被显示）。"));
            }

            if (!WorldMapToolGeometry.IsInsideUnitSquare(new Vector2(v.x, v.y)))
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（顶点 #" + (i + 1) + "）：X/Y 越界 —— X=" + F(v.x) + "、Y=" + F(v.y) +
                    "，必须落在 0~1（越界会被裁切/变形）。"));
            }

            if (!IsValidColor(v.color))
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（顶点 #" + (i + 1) + "）：Color = 「" + v.color +
                    "」不是合法的 #RRGGBB（7 字符，例如 #2F3A4F）。"));
            }

            string reason;
            if (!IsSafeString(v.color, out reason))
            {
                issues.Add(WorldMapToolIssue.Error(
                    "第 " + row + " 行（顶点 #" + (i + 1) + "）：Color " + reason));
            }
        }

        // 逐区域：顶点数 / PointOrder 连续性 / 颜色一致 / 自交
        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(draft);
        for (int r = 0; r < regions.Count; r++)
        {
            WorldMapFactionRegion region = regions[r];

            // 顶点数 <3 —— 任务裁决：报错阻断
            if (region.vertices.Count < 3)
            {
                issues.Add(WorldMapToolIssue.Error(
                    "区域「" + region.Label + "」只有 " + region.vertices.Count +
                    " 个顶点，少于 3 个不构成多边形（对应 " + region.vertices.Count + " 行数据）。"));
            }

            // PointOrder 必须从 1 起连续。本工具的 PointOrder 由组内顺序生成，天然连续；
            // 这里做防御性校验（将来若引入手改/导入路径，这一条会真正兜住）。
            int expected = 1;
            for (int k = 0; k < region.sourceIndices.Count; k++)
            {
                if (k + 1 != expected)
                {
                    issues.Add(WorldMapToolIssue.Error(
                        "区域「" + region.Label + "」的 PointOrder 不连续：期望 " + expected + "，实际 " + (k + 1)));
                    break;
                }
                expected++;
            }

            // 颜色不一致 —— M6：以第一行为准，只提示
            for (int k = 0; k < region.sourceIndices.Count; k++)
            {
                WorldMapFactionVertex vertex = draft.factionVertices[region.sourceIndices[k]];
                if (vertex != null && !string.Equals(vertex.color, region.color, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(WorldMapToolIssue.Warning(
                        "区域「" + region.Label + "」内 Color 不一致（" + region.color + " vs " + vertex.color +
                        "）—— 按 M6 以该区域第一行为准。"));
                    break;
                }
            }

            // 自交 —— M7 / 任务裁决：只告警不阻断
            if (region.vertices.Count >= 4 && WorldMapToolGeometry.IsSelfIntersecting(region.vertices))
            {
                issues.Add(WorldMapToolIssue.Warning(
                    "区域「" + region.Label + "」的边存在自交 —— 会造成填充异常（按约定只提示、不阻断导出）。"));
            }

            if (region.vertices.Count >= 3 && !WorldMapToolGeometry.IsCounterClockwise(region.vertices))
            {
                // 不算问题：导出时会自动规范化，这里只是让作者知道会被翻转
                issues.Add(WorldMapToolIssue.Warning(
                    "区域「" + region.Label + "」顶点为顺时针，导出时会自动规范化为逆时针。"));
            }
        }

        return issues;
    }

    // ══════════════════ 顶层目录：同前缀冲突（硬约束②） ══════════════════

    /// <summary>
    /// 检查顶层目录里是否已存在**同英文前缀**的其它 `.xlsx`（会导出同一个 Config 并静默互相覆盖）。
    /// 前缀 = 文件名第一个 `_` 之前的英文段（与导出器的类名推导一致）。
    /// </summary>
    /// <param name="topLevelFolderAbsolute">顶层目录的绝对路径（`Assets/Excel/Chaos_excel`）</param>
    /// <param name="expectedFileName">本次要写的文件名（自身不算冲突）</param>
    /// <param name="prefix">英文前缀，如 `WorldMap`</param>
    public static void CheckTopLevelPrefixCollision(
        string topLevelFolderAbsolute, string expectedFileName, string prefix,
        List<WorldMapToolIssue> issues)
    {
        if (issues == null) return;

        if (string.IsNullOrEmpty(topLevelFolderAbsolute) || !Directory.Exists(topLevelFolderAbsolute))
        {
            issues.Add(WorldMapToolIssue.Error("顶层目录不存在：" + topLevelFolderAbsolute));
            return;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(topLevelFolderAbsolute, "*.xlsx", SearchOption.TopDirectoryOnly);
        }
        catch (Exception e)
        {
            issues.Add(WorldMapToolIssue.Error("扫描顶层目录失败：" + e.Message));
            return;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileName(files[i]);

            //忽略自身、以及 Excel 的临时锁文件（~$ 开头）
            if (string.Equals(name, expectedFileName, StringComparison.OrdinalIgnoreCase)) continue;
            if (name.StartsWith("~$", StringComparison.Ordinal)) continue;

            if (string.Equals(ExtractPrefix(name), prefix, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(WorldMapToolIssue.Error(
                    "顶层目录已存在同英文前缀「" + prefix + "」的另一张表：" + name +
                    " —— 它与 " + expectedFileName +
                    " 会导出同一个 " + prefix + "Config 并**静默互相覆盖**。请先把它移走或改名。"));
            }
        }
    }

    /// <summary>取文件名的英文前缀（第一个 `_` 之前；没有 `_` 就用整个去扩展名的文件名）。</summary>
    public static string ExtractPrefix(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;

        string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(withoutExtension)) return string.Empty;

        int underscore = withoutExtension.IndexOf('_');
        return underscore > 0 ? withoutExtension.Substring(0, underscore) : withoutExtension;
    }

    /// <summary>列表里是否有 Error（有则必须阻断导出）。</summary>
    public static bool HasError(List<WorldMapToolIssue> issues)
    {
        if (issues == null) return false;
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i] != null && issues[i].IsError) return true;
        }
        return false;
    }

    /// <summary>把 issue 列表渲染成给 Console / 窗口用的多行文本。</summary>
    public static string Describe(List<WorldMapToolIssue> issues)
    {
        if (issues == null || issues.Count == 0) return "（无问题）";

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < issues.Count; i++) builder.AppendLine(issues[i].ToString());
        return builder.ToString();
    }
}
