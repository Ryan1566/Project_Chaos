using System;

[Serializable]
public class ProgressTable_EraConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 时代
    /// </summary>
    public uint Era;
    /// <summary>
    /// 名称key
    /// </summary>
    public string NameKey;
    /// <summary>
    /// 基础难度档
    /// </summary>
    public uint BaseDifficulty;
    /// <summary>
    /// 里程碑类型
    /// </summary>
    public uint MilestoneType;
    /// <summary>
    /// 里程碑阈值
    /// </summary>
    public uint MilestoneValue;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
