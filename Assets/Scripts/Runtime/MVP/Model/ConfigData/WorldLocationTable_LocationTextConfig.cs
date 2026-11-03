using System;

[Serializable]
public class WorldLocationTable_LocationTextConfig
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
    /// 名称
    /// </summary>
    public string Name;
    /// <summary>
    /// 介绍
    /// </summary>
    public string Description;
    /// <summary>
    /// 条件说明
    /// </summary>
    public string ConditionText;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
