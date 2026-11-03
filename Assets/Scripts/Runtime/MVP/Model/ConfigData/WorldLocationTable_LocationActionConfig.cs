using System;

[Serializable]
public class WorldLocationTable_LocationActionConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 地点类型
    /// </summary>
    public uint Kind;
    /// <summary>
    /// 可贸易
    /// </summary>
    public bool CanTrade;
    /// <summary>
    /// 可建造
    /// </summary>
    public bool CanBuild;
    /// <summary>
    /// 可领取
    /// </summary>
    public bool CanHarvest;
    /// <summary>
    /// 可处置临时点
    /// </summary>
    public bool CanResolve;
    /// <summary>
    /// 可进官员事务
    /// </summary>
    public bool CanOpenOffice;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
