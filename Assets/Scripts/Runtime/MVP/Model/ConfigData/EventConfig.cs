using System;

[Serializable]
public class EventConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 触发类型
    /// </summary>
    public uint TriggerType;
    /// <summary>
    /// 触发值
    /// </summary>
    public uint TriggerValue;
    /// <summary>
    /// 是否可重复
    /// </summary>
    public bool Repeatable;
    /// <summary>
    /// 优先级
    /// </summary>
    public uint Priority;
    /// <summary>
    /// 标题
    /// </summary>
    public string TitleText;
    /// <summary>
    /// 正文
    /// </summary>
    public string BodyText;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
