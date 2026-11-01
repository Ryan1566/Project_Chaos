using System;

[Serializable]
public class ItemConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 名称key
    /// </summary>
    public string NameKey;
    /// <summary>
    /// 类别
    /// </summary>
    public uint Category;
    /// <summary>
    /// 是否扎营类
    /// </summary>
    public bool IsCampItem;
    /// <summary>
    /// 叠加上限
    /// </summary>
    public uint StackLimit;
    /// <summary>
    /// 效果ID
    /// </summary>
    public uint EffectId;
    /// <summary>
    /// 图标key
    /// </summary>
    public string IconKey;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
    /// <summary>
    /// 买价
    /// </summary>
    public uint BuyPrice;
    /// <summary>
    /// 卖价
    /// </summary>
    public uint SellPrice;
    /// <summary>
    /// 局内用途
    /// </summary>
    public uint RunCategory;
    /// <summary>
    /// 唯一道具
    /// </summary>
    public bool IsUnique;
}
