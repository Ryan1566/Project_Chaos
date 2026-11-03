using System;

[Serializable]
public class WorldResourceTable_ResourceFacilityConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 稳定地点ID
    /// </summary>
    public uint LocationId;
    /// <summary>
    /// 设施名
    /// </summary>
    public string FacilityName;
    /// <summary>
    /// 产物ID
    /// </summary>
    public uint OutputItemId;
    /// <summary>
    /// 每件秒数
    /// </summary>
    public uint CycleSeconds;
    /// <summary>
    /// 每周期产量
    /// </summary>
    public uint OutputPerCycle;
    /// <summary>
    /// 库存上限
    /// </summary>
    public uint StockLimit;
    /// <summary>
    /// 建造钱财
    /// </summary>
    public uint BuildMoney;
    /// <summary>
    /// 建造材料
    /// </summary>
    public uint BuildMaterial;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
