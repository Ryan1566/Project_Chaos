using System;

[Serializable]
public class WorldLocationTable_LocationTemporaryConfig
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
    /// 奖励物品
    /// </summary>
    public uint RewardItemId;
    /// <summary>
    /// 奖励数量
    /// </summary>
    public uint RewardCount;
    /// <summary>
    /// 触发时代
    /// </summary>
    public uint TriggerEra;
    /// <summary>
    /// 最少进入次数
    /// </summary>
    public uint MinRuns;
    /// <summary>
    /// 最少通关数
    /// </summary>
    public uint MinClears;
    /// <summary>
    /// 前置地点
    /// </summary>
    public uint PrerequisiteLocationId;
    /// <summary>
    /// 可重复
    /// </summary>
    public bool Repeatable;
    /// <summary>
    /// 冷却秒
    /// </summary>
    public uint CooldownSeconds;
    /// <summary>
    /// 需办结的任务ID，0为无
    /// </summary>
    public uint RequiredTaskId;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
