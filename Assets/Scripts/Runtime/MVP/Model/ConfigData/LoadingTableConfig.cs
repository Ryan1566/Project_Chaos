using System;	

[Serializable]	
public class LoadingTableConfig
{
	public uint Id;//序号
	public string srcImg;//轮播图资源路径
	public uint sort;//播放顺序
	public float showSeconds;//单张停留秒数
	public float fadeSeconds;//切换淡入淡出秒数
	public bool enable;//是否启用
}

