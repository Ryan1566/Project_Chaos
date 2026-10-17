using System;	

[Serializable]	
public class BuildingConfig
{
	public uint Index;//序号
	public uint BuildingId;//建筑ID
	public string NameKey;//名称key
	public uint Level;//等级
	public uint BuildSeconds;//建造秒数
	public uint OutputResourceType;//产出资源类型
	public uint OutputAmount;//每次产出数量
	public uint HarvestSeconds;//收获周期秒
	public uint CostResourceType;//建造消耗资源类型
	public uint CostAmount;//建造消耗数量
	public bool Enable;//是否启用
}

