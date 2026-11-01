using System;

[Serializable]
public class MapPresetConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 外键 → MapBiome.Id
    /// </summary>
    public uint biomeId;
    /// <summary>
    /// 备注名
    /// </summary>
    public string nameCN;
    /// <summary>
    /// 主路房间数下限
    /// </summary>
    public uint mainPathMin;
    /// <summary>
    /// 主路房间数上限
    /// </summary>
    public uint mainPathMax;
    /// <summary>
    /// 支路房间总数下限
    /// </summary>
    public uint branchMin;
    /// <summary>
    /// 支路房间总数上限
    /// </summary>
    public uint branchMax;
    /// <summary>
    /// 房间总数下限
    /// </summary>
    public uint totalMin;
    /// <summary>
    /// 房间总数上限
    /// </summary>
    public uint totalMax;
    /// <summary>
    /// 死路奖励房数量下限
    /// </summary>
    public uint specialTreasure;
    /// <summary>
    /// 扎营点数量（预留）
    /// </summary>
    public uint specialCamp;
    /// <summary>
    /// 单节点选块/回溯上限
    /// </summary>
    public uint retryMax;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool enable;
}
