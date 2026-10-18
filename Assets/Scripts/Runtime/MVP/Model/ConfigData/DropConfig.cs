using System;	

[Serializable]	
public class DropConfig
{
	public uint Index;//序号
	public uint DropGroup;//掉落组
	public uint ItemId;//物品ID
	public uint MinCount;//最小数量
	public uint MaxCount;//最大数量
	public uint Weight;//权重
	public bool Enable;//是否启用
}

