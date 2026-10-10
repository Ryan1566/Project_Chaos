using System;	

[Serializable]	
public class ChunkSocketConfig
{
	public uint Id;//说明
	public uint chunkId;//外键 → MapChunk.Id
	public uint dir;//方向：1 左 2 右 3 上 4 下
	public uint heightProfile;//横向高度档（0/1/2）；竖直开口恒为 0，按同列匹配
	public uint socketType;//1 普通通道 2 跨层通道 3 boss 门
	public float posX;//块内局部坐标 X（单位：米）
	public float posY;//块内局部坐标 Y（单位：米）
}

