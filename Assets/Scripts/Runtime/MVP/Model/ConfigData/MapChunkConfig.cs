using System;

[Serializable]
public class MapChunkConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 外键 → MapBiome.Id
    /// </summary>
    public uint biomeId;
    /// <summary>
    /// 主题 Key
    /// </summary>
    public string themeKey;
    /// <summary>
    /// 类型：1 入口 2 通路 3 战斗 9 层间连接 10 boss 门 11 死路奖励
    /// </summary>
    public uint kind;
    /// <summary>
    /// 所属空间层（0=跨层）
    /// </summary>
    public uint layer;
    /// <summary>
    /// Resources 相对路径（不含扩展名）
    /// </summary>
    public string prefabPath;
    /// <summary>
    /// 抽取权重
    /// </summary>
    public uint weight;
    /// <summary>
    /// 单图最大出现次数
    /// </summary>
    public uint maxCount;
    /// <summary>
    /// 全图是否最多一次
    /// </summary>
    public bool isUnique;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool enable;
}
