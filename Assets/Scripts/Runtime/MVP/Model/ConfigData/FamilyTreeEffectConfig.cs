using System;

[Serializable]
public class FamilyTreeEffectConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 节点ID
    /// </summary>
    public uint NodeId;
    /// <summary>
    /// 效果ID
    /// </summary>
    public uint EffectId;
    /// <summary>
    /// 权重
    /// </summary>
    public float Weight;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
