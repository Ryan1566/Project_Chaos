using System;	

[Serializable]	
public class MapPresetConfig
{
	public uint Id;//说明
	public uint biomeId;//外键 → MapBiome.Id
	public string nameCN;//备注名
	public uint mainPathMin;//主路房间数下限
	public uint mainPathMax;//主路房间数上限
	public uint branchMin;//支路房间总数下限
	public uint branchMax;//支路房间总数上限
	public uint totalMin;//房间总数下限
	public uint totalMax;//房间总数上限
	public uint specialTreasure;//死路奖励房数量下限
	public uint specialCamp;//扎营点数量（预留）
	public uint retryMax;//单节点选块/回溯上限
	public bool enable;//是否启用
}

