using System;

[Serializable]
public class MarketTable_MarketOfferConfig
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
    /// 物品ID
    /// </summary>
    public uint ItemId;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
