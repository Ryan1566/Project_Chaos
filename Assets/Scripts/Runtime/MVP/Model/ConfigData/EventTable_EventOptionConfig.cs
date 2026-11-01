using System;

[Serializable]
public class EventTable_EventOptionConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 事件ID
    /// </summary>
    public uint EventId;
    /// <summary>
    /// 选项序号
    /// </summary>
    public uint OptionIndex;
    /// <summary>
    /// 选项文本
    /// </summary>
    public string OptionText;
    /// <summary>
    /// 给资源类型
    /// </summary>
    public uint GiveResourceType;
    /// <summary>
    /// 给资源数量
    /// </summary>
    public uint GiveResourceAmount;
    /// <summary>
    /// 给物品ID
    /// </summary>
    public uint GiveItemId;
    /// <summary>
    /// 给物品数量
    /// </summary>
    public uint GiveItemCount;
    /// <summary>
    /// 给门客ID
    /// </summary>
    public uint GiveRetainerId;
    /// <summary>
    /// 信心变化
    /// </summary>
    public int ConfidenceDelta;
    /// <summary>
    /// 效果ID
    /// </summary>
    public uint EffectId;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
