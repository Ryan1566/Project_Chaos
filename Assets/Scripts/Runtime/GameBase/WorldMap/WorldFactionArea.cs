using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **一块势力领地**（运行时视图）：同一 `(Stage, FactionId, RegionId)` 下的所有顶点，
/// 按 `PointOrder` 升序连成的**闭合**多边形。
///
/// ⚠ 两条口径（与 `06_WorldFaction_字段规格.md` 一致）：
///   ① **不重复首点** —— 最后一个点与第一个点之间的闭合边由绘制方自己补；
///   ② 顶点坐标是**归一化、Y 自下向上**。
/// </summary>
public class WorldFactionArea
{
    /// <summary>时期 1~5。</summary>
    public uint stage;

    /// <summary>势力标识（配色含义见系统文档；本层只当数据）。</summary>
    public uint factionId;

    /// <summary>同一势力的一块独立区域一个 ID（不相连的领地要用不同 RegionId）。</summary>
    public uint regionId;

    /// <summary>填充色（来自该区域**第一行**的 `Color`，逐行携带但只取首行）。</summary>
    public Color color = Color.white;

    /// <summary>顶点（归一化、Y 向上），已按 `PointOrder` 升序；**不含**重复首点。</summary>
    public readonly List<Vector2> vertices = new List<Vector2>();

    /// <summary>顶点数 ≥3 才算一块多边形；不足的在服务层就被过滤掉了。</summary>
    public bool IsValid
    {
        get { return vertices.Count >= 3; }
    }

    /// <summary>人类可读标签（日志用）。</summary>
    public string Label
    {
        get
        {
            return "时期" + stage + "/势力" + factionId + "/区域" + regionId + "（" + vertices.Count + " 顶点）";
        }
    }
}
