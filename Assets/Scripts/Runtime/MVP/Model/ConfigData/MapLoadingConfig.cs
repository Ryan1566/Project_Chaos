using System;

[Serializable]
public class MapLoadingConfig
{
    /// <summary>
    /// 说明
    /// </summary>
    public uint Id;
    /// <summary>
    /// 阶段码：1 参数装配 2 生成房间图 3 排布网格 4 挑选房间块 5 实例化 6 封堵填充 7 校验 8 完成
    /// </summary>
    public uint stage;
    /// <summary>
    /// 展示顺序
    /// </summary>
    public uint sort;
    /// <summary>
    /// 简体中文描述（禁止英文逗号/双引号/换行）
    /// </summary>
    public string descCN;
    /// <summary>
    /// 繁体中文描述
    /// </summary>
    public string descTW;
    /// <summary>
    /// 英语描述
    /// </summary>
    public string descEN;
    /// <summary>
    /// 日语描述
    /// </summary>
    public string descJP;
    /// <summary>
    /// 韩语描述
    /// </summary>
    public string descKR;
}
