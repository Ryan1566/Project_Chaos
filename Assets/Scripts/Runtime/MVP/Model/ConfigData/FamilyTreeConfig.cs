using System;	

[Serializable]	
public class FamilyTreeConfig
{
	public uint Index;//序号
	public uint Board;//板块
	public uint OrderInBoard;//板块内顺序
	public string NameKey;//名称key
	public uint PrereqNodeId;//前置节点
	public uint CostType;//消耗类型
	public uint CostValue;//消耗数量
	public bool Enable;//是否启用
}

