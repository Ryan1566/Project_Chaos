using System;

[Serializable]
public class OfficeTaskConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 分类1郡县2行政3预设4突发
    /// </summary>
    public uint Category;
    /// <summary>
    /// 任务标题
    /// </summary>
    public string TitleText;
    /// <summary>
    /// 任务说明
    /// </summary>
    public string BodyText;
    /// <summary>
    /// 事务子类
    /// </summary>
    public string Topic;
    /// <summary>
    /// 最早时代
    /// </summary>
    public uint MinEra;
    /// <summary>
    /// 最晚时代
    /// </summary>
    public uint MaxEra;
    /// <summary>
    /// 最少派遣次数
    /// </summary>
    public uint MinRuns;
    /// <summary>
    /// 最少通关深度
    /// </summary>
    public uint MinClearedDepth;
    /// <summary>
    /// 前置任务
    /// </summary>
    public uint PrerequisiteTaskId;
    /// <summary>
    /// 是否重复
    /// </summary>
    public bool Repeatable;
    /// <summary>
    /// 重复冷却秒
    /// </summary>
    public uint CooldownSeconds;
    /// <summary>
    /// 时限秒（0不限时）
    /// </summary>
    public uint DurationSeconds;
    /// <summary>
    /// 目标1选项2交付3通关4遭袭
    /// </summary>
    public uint GoalType;
    /// <summary>
    /// 交付资源类型
    /// </summary>
    public uint ResourceType;
    /// <summary>
    /// 交付数量
    /// </summary>
    public uint RequiredAmount;
    /// <summary>
    /// 所需通关次数
    /// </summary>
    public uint RequiredClears;
    /// <summary>
    /// 失败扣除资源
    /// </summary>
    public uint FailureResourceType;
    /// <summary>
    /// 失败扣除数量
    /// </summary>
    public uint FailureResourceAmount;
    /// <summary>
    /// 失败声望变化
    /// </summary>
    public int FailureReputation;
    /// <summary>
    /// 失败影响力变化
    /// </summary>
    public int FailureInfluence;
    /// <summary>
    /// 失败发展度变化
    /// </summary>
    public int FailureDevelopment;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
