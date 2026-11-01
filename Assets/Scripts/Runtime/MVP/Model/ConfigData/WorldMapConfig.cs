using System;

[Serializable]
public class WorldMapConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 地图ID
    /// </summary>
    public uint MapId;
    /// <summary>
    /// 条目类别
    /// </summary>
    public uint Kind;
    /// <summary>
    /// 地点类型
    /// </summary>
    public uint Type;
    /// <summary>
    /// 地点ID
    /// </summary>
    public uint RefId;
    /// <summary>
    /// 横坐标
    /// </summary>
    public float X;
    /// <summary>
    /// 纵坐标
    /// </summary>
    public float Y;
    /// <summary>
    /// 图标缩放
    /// </summary>
    public float Scale;
    /// <summary>
    /// 显示层级
    /// </summary>
    public uint Layer;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
