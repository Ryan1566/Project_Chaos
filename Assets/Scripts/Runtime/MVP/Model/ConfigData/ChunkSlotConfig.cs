using System;	

[Serializable]	
public class ChunkSlotConfig
{
	public uint Id;//说明
	public uint chunkId;//外键 → MapChunk.Id
	public uint slotType;//1 敌人 2 精英 3 宝箱 5 场景事件 6 扎营点
	public float posX;//块内局部坐标 X（单位：米）
	public float posY;//块内局部坐标 Y（单位：米）
}

