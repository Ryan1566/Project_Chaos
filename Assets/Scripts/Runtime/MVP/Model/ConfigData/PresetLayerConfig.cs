using System;	

[Serializable]	
public class PresetLayerConfig
{
	public uint Id;//说明
	public uint presetId;//外键 → MapPreset.Id
	public uint layerOrder;//层顺序，从 1 连续递增
	public uint layer;//空间层：1 地下 2 地表 3 地表建筑
	public bool isMainPath;//主路是否必须经过本层
	public uint linkFromLayerOrder;//与第几层相连（0=首层）
	public bool allowDirect;//是否允许直连该层
	public uint linkMin;//跨层连接件数量下限
	public uint linkMax;//跨层连接件数量上限
}

