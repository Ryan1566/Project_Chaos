using System;	

[Serializable]	
public class EventConfig
{
	public uint Index;//序号
	public uint TriggerType;//触发类型
	public uint TriggerValue;//触发值
	public bool Repeatable;//是否可重复
	public uint Priority;//优先级
	public string TitleText;//标题
	public string BodyText;//正文
	public bool Enable;//是否启用
}

