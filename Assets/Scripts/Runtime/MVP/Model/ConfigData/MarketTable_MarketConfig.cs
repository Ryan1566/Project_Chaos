using System;

[Serializable]
public class MarketTable_MarketConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 市场ID
    /// </summary>
    public uint MarketId;
    /// <summary>
    /// 稳定地点ID
    /// </summary>
    public uint LocationId;
    /// <summary>
    /// 市场名
    /// </summary>
    public string Name;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
