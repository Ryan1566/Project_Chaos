using System;

[Serializable]
public class LoadingTableConfig
{
    /// <summary>
    /// 序号
    /// </summary>
    public uint Id;
    /// <summary>
    /// 轮播图资源路径
    /// </summary>
    public string srcImg;
    /// <summary>
    /// 播放顺序
    /// </summary>
    public uint sort;
    /// <summary>
    /// 单张停留秒数
    /// </summary>
    public float showSeconds;
    /// <summary>
    /// 切换淡入淡出秒数
    /// </summary>
    public float fadeSeconds;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool enable;
}
