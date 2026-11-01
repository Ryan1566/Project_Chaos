using System;

[Serializable]
public class ProgressTable_DepthConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 时代
    /// </summary>
    public uint Era;
    /// <summary>
    /// 深度
    /// </summary>
    public uint Depth;
    /// <summary>
    /// 难度增量
    /// </summary>
    public float DifficultyDelta;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
