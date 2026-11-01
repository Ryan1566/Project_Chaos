using System;

[Serializable]
public class OfficeTaskTable_OfficeTaskOptionConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 任务ID
    /// </summary>
    public uint TaskId;
    /// <summary>
    /// 选项文案
    /// </summary>
    public string OptionText;
    /// <summary>
    /// 额外消耗类型
    /// </summary>
    public uint CostResourceType;
    /// <summary>
    /// 额外消耗数量
    /// </summary>
    public uint CostAmount;
    /// <summary>
    /// 奖励资源类型
    /// </summary>
    public uint RewardResourceType;
    /// <summary>
    /// 奖励数量
    /// </summary>
    public uint RewardAmount;
    /// <summary>
    /// 声望变化
    /// </summary>
    public int ReputationDelta;
    /// <summary>
    /// 影响力变化
    /// </summary>
    public int InfluenceDelta;
    /// <summary>
    /// 村庄发展度变化
    /// </summary>
    public int DevelopmentDelta;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
