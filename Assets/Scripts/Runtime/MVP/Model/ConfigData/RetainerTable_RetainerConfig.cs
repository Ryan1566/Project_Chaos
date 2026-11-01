using System;

[Serializable]
public class RetainerTable_RetainerConfig
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
    /// 品质
    /// </summary>
    public uint Quality;
    /// <summary>
    /// 生命
    /// </summary>
    public uint Hp;
    /// <summary>
    /// 攻击
    /// </summary>
    public uint Attack;
    /// <summary>
    /// 防御
    /// </summary>
    public uint Defense;
    /// <summary>
    /// 速度
    /// </summary>
    public uint Speed;
    /// <summary>
    /// 初始信心
    /// </summary>
    public uint InitialConfidence;
    /// <summary>
    /// 招募权重
    /// </summary>
    public uint RecruitWeight;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
