using System;	

[Serializable]	
public class WorldFactionConfig
{
	public uint Index;//序号
	public uint Stage;//时期
	public uint FactionId;//势力ID
	public uint RegionId;//区域ID
	public uint PointOrder;//顶点序号
	public float X;//横坐标
	public float Y;//纵坐标
	public string Color;//填充颜色
	public bool Enable;//是否启用
}

