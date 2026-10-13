using UnityEngine;

/// <summary>
/// 局外地图上的**一个点位**（运行时视图）。
///
/// 由 <see cref="WorldMapService"/> 从 `WorldMapConfig` 转出来：
/// 只保留 `Enable = true` 的行，并把坐标换成归一化 `Vector2`。
/// 它是**纯数据**，不含任何 Unity 对象引用 —— 所以表被删掉也不会影响已生成的对象。
/// </summary>
public class WorldMapPoint
{
    /// <summary>表内序号（`WorldMapConfig.Index`），排错用。</summary>
    public uint index;

    /// <summary>属于哪张底图（`MapId`）。</summary>
    public uint mapId;

    /// <summary>条目类别：1 = 固定地点、2 = 兴趣点、3+ = 预留。</summary>
    public uint kind;

    /// <summary>分类值（含义由地点表/兴趣点表定义，本层不解释）。</summary>
    public uint type;

    /// <summary>对应表的主键。`(kind, type, refId)` 三元组唯一定位一个条目。</summary>
    public uint refId;

    /// <summary>归一化坐标：**X 自左向右、Y 自下向上**（与配表口径一致，不要再翻一次）。</summary>
    public Vector2 position;

    /// <summary>图标缩放倍数（&lt;= 0 时按 1 处理）。</summary>
    public float scale = 1f;

    /// <summary>显示层级：数值越大越靠上。</summary>
    public uint layer;
}
