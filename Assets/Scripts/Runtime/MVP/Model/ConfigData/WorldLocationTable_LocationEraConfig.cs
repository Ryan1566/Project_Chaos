using System;

[Serializable]
public class WorldLocationTable_LocationEraConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Index;
    /// <summary>
    /// 稳定地点ID
    /// </summary>
    public uint LocationId;
    /// <summary>
    /// 时代1-5
    /// </summary>
    public uint Era;
    /// <summary>
    /// 当期名称
    /// </summary>
    public string Name;
    /// <summary>
    /// 当期势力
    /// </summary>
    public uint FactionId;
    /// <summary>
    /// 势力名
    /// </summary>
    public string FactionName;
    /// <summary>
    /// 关系ID
    /// </summary>
    public uint RelationId;
    /// <summary>
    /// 主城标记
    /// </summary>
    public bool IsCapital;
    /// <summary>
    /// 可见
    /// </summary>
    public bool Visible;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable;
}
