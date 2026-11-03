using System;

[Serializable]
public class WorldResourceTable_ResourceOutputConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 产物ID
    /// </summary>
    public uint OutputItemId;
    /// <summary>
    /// 产物名
    /// </summary>
    public string Name;
    /// <summary>
    /// 地点图标
    /// </summary>
    public string IconKey;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
