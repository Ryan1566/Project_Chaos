using System;

[Serializable]
public class WorldLocationTable_LocationConfig
{
    /// <summary>
    /// 主键（等于稳定地点ID）
    /// </summary>
    public uint Index;
    /// <summary>
    /// 稳定地点ID
    /// </summary>
    public uint LocationId;
    /// <summary>
    /// 类型1主城2贸易3部落4资源5临时
    /// </summary>
    public uint Kind;
    /// <summary>
    /// 底图ID
    /// </summary>
    public uint MapId;
    /// <summary>
    /// 归一化X
    /// </summary>
    public float X;
    /// <summary>
    /// 归一化Y
    /// </summary>
    public float Y;
    /// <summary>
    /// 市场ID
    /// </summary>
    public uint MarketId;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
