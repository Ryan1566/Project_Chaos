using System;

[Serializable]
public class FamilyTreeConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 板块
    /// </summary>
    public uint Board;
    /// <summary>
    /// 板块内顺序
    /// </summary>
    public uint OrderInBoard;
    /// <summary>
    /// 名称key
    /// </summary>
    public string NameKey;
    /// <summary>
    /// 前置节点
    /// </summary>
    public uint PrereqNodeId;
    /// <summary>
    /// 消耗类型
    /// </summary>
    public uint CostType;
    /// <summary>
    /// 消耗数量
    /// </summary>
    public uint CostValue;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
