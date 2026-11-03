using System;

[Serializable]
public class WorldLocationTable_LocationRelationConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 关系ID
    /// </summary>
    public uint RelationId;
    /// <summary>
    /// 中文关系
    /// </summary>
    public string RelationName;
    /// <summary>
    /// 买价百分比
    /// </summary>
    public int BuyPercent;
    /// <summary>
    /// 可贸易
    /// </summary>
    public bool CanTrade;
    /// <summary>
    /// 可进官员事务
    /// </summary>
    public bool CanEnterOffice;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
