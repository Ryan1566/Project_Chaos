using System;	

[Serializable]	
public class EventOptionConfig
{
	public uint Index;//序号
	public uint EventId;//事件ID
	public uint OptionIndex;//选项序号
	public string OptionText;//选项文本
	public uint GiveResourceType;//给资源类型
	public uint GiveResourceAmount;//给资源数量
	public uint GiveItemId;//给物品ID
	public uint GiveItemCount;//给物品数量
	public uint GiveRetainerId;//给门客ID
	public int ConfidenceDelta;//信心变化
	public uint EffectId;//效果ID
	public bool Enable;//是否启用
}

