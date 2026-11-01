using System;

[Serializable]
public class ItemTable_DropConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 掉落组
    /// </summary>
    public uint DropGroup;
    /// <summary>
    /// 物品ID
    /// </summary>
    public uint ItemId;
    /// <summary>
    /// 最小数量
    /// </summary>
    public uint MinCount;
    /// <summary>
    /// 最大数量
    /// </summary>
    public uint MaxCount;
    /// <summary>
    /// 权重
    /// </summary>
    public uint Weight;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
