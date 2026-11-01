using System;

[Serializable]
public class WorldMapTable_WorldFactionConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 时期
    /// </summary>
    public uint Stage;
    /// <summary>
    /// 势力ID
    /// </summary>
    public uint FactionId;
    /// <summary>
    /// 区域ID
    /// </summary>
    public uint RegionId;
    /// <summary>
    /// 顶点序号
    /// </summary>
    public uint PointOrder;
    /// <summary>
    /// 横坐标
    /// </summary>
    public float X;
    /// <summary>
    /// 纵坐标
    /// </summary>
    public float Y;
    /// <summary>
    /// 填充颜色
    /// </summary>
    public string Color;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
