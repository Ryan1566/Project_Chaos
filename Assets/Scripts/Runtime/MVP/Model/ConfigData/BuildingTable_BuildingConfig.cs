using System;

[Serializable]
public class BuildingTable_BuildingConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 建筑ID
    /// </summary>
    public uint BuildingId;
    /// <summary>
    /// 名称key
    /// </summary>
    public string NameKey;
    /// <summary>
    /// 等级
    /// </summary>
    public uint Level;
    /// <summary>
    /// 建造秒数
    /// </summary>
    public uint BuildSeconds;
    /// <summary>
    /// 产出资源类型
    /// </summary>
    public uint OutputResourceType;
    /// <summary>
    /// 每次产出数量
    /// </summary>
    public uint OutputAmount;
    /// <summary>
    /// 收获周期秒
    /// </summary>
    public uint HarvestSeconds;
    /// <summary>
    /// 建造消耗资源类型
    /// </summary>
    public uint CostResourceType;
    /// <summary>
    /// 建造消耗数量
    /// </summary>
    public uint CostAmount;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
    /// <summary>
    /// 仓库每种资源容量
    /// </summary>
    public uint ResourceCapacity;
}
