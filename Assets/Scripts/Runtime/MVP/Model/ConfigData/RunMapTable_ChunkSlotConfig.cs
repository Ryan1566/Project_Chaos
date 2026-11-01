using System;

[Serializable]
public class RunMapTable_ChunkSlotConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 外键 → MapChunk.Id
    /// </summary>
    public uint chunkId;
    /// <summary>
    /// 1 敌人 2 精英 3 宝箱 5 场景事件 6 扎营点
    /// </summary>
    public uint slotType;
    /// <summary>
    /// 块内局部坐标 X（单位：米）
    /// </summary>
    public float posX;
    /// <summary>
    /// 块内局部坐标 Y（单位：米）
    /// </summary>
    public float posY;
}
