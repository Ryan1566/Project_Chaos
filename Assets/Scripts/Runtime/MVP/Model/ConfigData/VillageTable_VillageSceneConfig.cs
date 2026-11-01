using System;

[Serializable]
public class VillageTable_VillageSceneConfig
{
    /// <summary>
    /// 场景配置编号
    /// </summary>
    public uint Id;
    /// <summary>
    /// 地面宽度 格
    /// </summary>
    public int widthTiles;
    /// <summary>
    /// 地面厚度 格
    /// </summary>
    public int groundDepth;
    /// <summary>
    /// 玩家出生横坐标 米
    /// </summary>
    public float playerSpawnX;
    /// <summary>
    /// 地面顶面高度 米
    /// </summary>
    public float groundY;
    /// <summary>
    /// 固定岗位NPC人数
    /// </summary>
    public int workerCount;
    /// <summary>
    /// 工人速度 米每秒
    /// </summary>
    public float workerMoveSpeed;
    /// <summary>
    /// 单次连续工作 秒
    /// </summary>
    public float workSeconds;
    /// <summary>
    /// 单次休息 秒
    /// </summary>
    public float restSeconds;
    /// <summary>
    /// 最少闲逛 秒
    /// </summary>
    public float wanderMinSeconds;
    /// <summary>
    /// 最多闲逛 秒
    /// </summary>
    public float wanderMaxSeconds;
    /// <summary>
    /// 地块中心间距 米
    /// </summary>
    public float plotSpacing;
    /// <summary>
    /// 首个地块横坐标 米
    /// </summary>
    public float firstPlotX;
    /// <summary>
    /// 地面瓦片资源池
    /// </summary>
    public string groundTilePool;
}
