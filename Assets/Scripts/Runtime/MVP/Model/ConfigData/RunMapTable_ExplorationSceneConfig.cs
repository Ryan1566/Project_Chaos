using System;

[Serializable]
public class RunMapTable_ExplorationSceneConfig
{
    /// <summary>
    /// 编号
    /// </summary>
    public uint Id;
    /// <summary>
    /// 场景编号
    /// </summary>
    public int stage;
    /// <summary>
    /// 场景键
    /// </summary>
    public string key;
    /// <summary>
    /// 中文名称
    /// </summary>
    public string nameCN;
    /// <summary>
    /// 权重
    /// </summary>
    public int weight;
    /// <summary>
    /// 装饰风格
    /// </summary>
    public int style;
}
