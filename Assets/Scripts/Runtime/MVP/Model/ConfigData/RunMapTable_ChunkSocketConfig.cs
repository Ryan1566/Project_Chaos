using System;

[Serializable]
public class RunMapTable_ChunkSocketConfig
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
    /// 方向：1 左 2 右 3 上 4 下
    /// </summary>
    public uint dir;
    /// <summary>
    /// 横向高度档（0/1/2）；竖直开口恒为 0，按同列匹配
    /// </summary>
    public uint heightProfile;
    /// <summary>
    /// 1 普通通道 2 跨层通道 3 boss 门
    /// </summary>
    public uint socketType;
    /// <summary>
    /// 块内局部坐标 X（单位：米）
    /// </summary>
    public float posX;
    /// <summary>
    /// 块内局部坐标 Y（单位：米）
    /// </summary>
    public float posY;
}
