using System;	

[Serializable]	
public class ItemConfig
{
	public uint Index;//序号
	public string NameKey;//名称key
	public uint Category;//类别
	public bool IsCampItem;//是否扎营类
	public uint StackLimit;//叠加上限
	public uint EffectId;//效果ID
	public string IconKey;//图标key
	public bool Enable;//是否启用
	public uint BuyPrice;//买价
	public uint SellPrice;//卖价
}

