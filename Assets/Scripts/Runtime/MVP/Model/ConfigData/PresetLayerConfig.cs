using System;

[Serializable]
public class PresetLayerConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 外键 → MapPreset.Id
    /// </summary>
    public uint presetId;
    /// <summary>
    /// 层顺序，从 1 连续递增
    /// </summary>
    public uint layerOrder;
    /// <summary>
    /// 空间层：1 地下 2 地表 3 地表建筑
    /// </summary>
    public uint layer;
    /// <summary>
    /// 主路是否必须经过本层
    /// </summary>
    public bool isMainPath;
    /// <summary>
    /// 与第几层相连（0=首层）
    /// </summary>
    public uint linkFromLayerOrder;
    /// <summary>
    /// 是否允许直连该层
    /// </summary>
    public bool allowDirect;
    /// <summary>
    /// 跨层连接件数量下限
    /// </summary>
    public uint linkMin;
    /// <summary>
    /// 跨层连接件数量上限
    /// </summary>
    public uint linkMax;
}
