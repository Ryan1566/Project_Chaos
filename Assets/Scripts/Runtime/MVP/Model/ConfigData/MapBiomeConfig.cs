using System;

[Serializable]
public class MapBiomeConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 美术/瓦片主题 Key（与 TileMap 目录名一致）
    /// </summary>
    public string themeKey;
    /// <summary>
    /// 中文名
    /// </summary>
    public string nameCN;
    /// <summary>
    /// 空间层：1 地下 2 地表 3 地表建筑
    /// </summary>
    public uint layer;
    /// <summary>
    /// 整图重生成上限
    /// </summary>
    public uint retryMax;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool enable;
    /// <summary>
    /// 敌人生命倍率
    /// </summary>
    public float EnemyHpMul;
    /// <summary>
    /// 敌人伤害倍率
    /// </summary>
    public float EnemyDmgMul;
    /// <summary>
    /// 掉落组基号
    /// </summary>
    public uint DropGroupBase;
}
