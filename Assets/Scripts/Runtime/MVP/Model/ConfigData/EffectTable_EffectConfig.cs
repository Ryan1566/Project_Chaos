using System;

[Serializable]
public class EffectTable_EffectConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 效果类型
    /// </summary>
    public uint EffectType;
    /// <summary>
    /// 目标
    /// </summary>
    public uint Target;
    /// <summary>
    /// 数值
    /// </summary>
    public float Value;
    /// <summary>
    /// 说明
    /// </summary>
    public string Note;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
