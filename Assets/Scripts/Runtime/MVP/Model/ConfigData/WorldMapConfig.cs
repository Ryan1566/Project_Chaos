using System;	

[Serializable]	
public class WorldMapConfig
{
	public uint Index;//序号
	public uint MapId;//地图ID
	public uint Kind;//条目类别
	public uint Type;//地点类型
	public uint RefId;//地点ID
	public float X;//横坐标
	public float Y;//纵坐标
	public float Scale;//图标缩放
	public uint Layer;//显示层级
	public bool Enable;//是否启用
}

