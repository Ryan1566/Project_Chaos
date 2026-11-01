using System;

[Serializable]
public class MetaSkillTreeTable_FamilyTreeConfig
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
    /// <summary>
    /// 说明key
    /// </summary>
    public string DescriptionKey;
    /// <summary>
    /// 图标key
    /// </summary>
    public string IconKey;
    /// <summary>
    /// 布局横坐标
    /// </summary>
    public float LayoutX;
    /// <summary>
    /// 布局纵坐标
    /// </summary>
    public float LayoutY;
    /// <summary>
    /// 节点样式
    /// </summary>
    public uint NodeKind;
    /// <summary>
    /// 分支key
    /// </summary>
    public string BranchKey;
}
