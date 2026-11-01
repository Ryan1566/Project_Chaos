using System;

[Serializable]
public class MapGenerationConfig
{
    /// <summary>
    /// 参数序号
    /// </summary>
    public uint Id;
    /// <summary>
    /// 地图预设编号
    /// </summary>
    public uint presetId;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool enable;
    /// <summary>
    /// 地图宽度格数
    /// </summary>
    public int widthTiles;
    /// <summary>
    /// 地图高度格数
    /// </summary>
    public int heightTiles;
    /// <summary>
    /// 边缘安全留白
    /// </summary>
    public int edgeMargin;
    /// <summary>
    /// 地表基准高度
    /// </summary>
    public int surfaceBaseY;
    /// <summary>
    /// 天空净空高度
    /// </summary>
    public int skyClearance;
    /// <summary>
    /// 地下最低高度
    /// </summary>
    public int undergroundMinY;
    /// <summary>
    /// 地表起伏幅度
    /// </summary>
    public int surfaceAmplitude;
    /// <summary>
    /// 地表变化步长
    /// </summary>
    public int surfaceStepLength;
    /// <summary>
    /// 路线最大台阶高度
    /// </summary>
    public int maxStepHeight;
    /// <summary>
    /// 路线头顶净空
    /// </summary>
    public int routeHeadroom;
    /// <summary>
    /// 最小站立平台宽度
    /// </summary>
    public int platformMinWidth;
    /// <summary>
    /// 野外非必经缺口宽度
    /// </summary>
    public int maxGapWidth;
    /// <summary>
    /// 路线最大落差
    /// </summary>
    public int maxDropHeight;
    /// <summary>
    /// 战斗区域最小宽度
    /// </summary>
    public int regionWidthMin;
    /// <summary>
    /// 战斗区域最大宽度
    /// </summary>
    public int regionWidthMax;
    /// <summary>
    /// 最少支路数量
    /// </summary>
    public int branchCountMin;
    /// <summary>
    /// 最多支路数量
    /// </summary>
    public int branchCountMax;
    /// <summary>
    /// 支路最小长度
    /// </summary>
    public int branchLengthMin;
    /// <summary>
    /// 支路最大长度
    /// </summary>
    public int branchLengthMax;
    /// <summary>
    /// 回环生成概率百分比
    /// </summary>
    public int loopChance;
    /// <summary>
    /// 洞穴目标深度
    /// </summary>
    public int caveDepth;
    /// <summary>
    /// 洞穴通道高度
    /// </summary>
    public int caveHeight;
    /// <summary>
    /// 洞穴通道宽度
    /// </summary>
    public int caveWidth;
    /// <summary>
    /// 洞室目标半径
    /// </summary>
    public int chamberRadius;
    /// <summary>
    /// 最少洞口数量
    /// </summary>
    public int caveEntranceMin;
    /// <summary>
    /// 最多洞口数量
    /// </summary>
    public int caveEntranceMax;
    /// <summary>
    /// 洞穴平滑次数
    /// </summary>
    public int caveSmoothPasses;
    /// <summary>
    /// 最少建筑数量
    /// </summary>
    public int buildingCountMin;
    /// <summary>
    /// 最多建筑数量
    /// </summary>
    public int buildingCountMax;
    /// <summary>
    /// 建筑最小间距
    /// </summary>
    public int buildingMinSpacing;
    /// <summary>
    /// 手工结构资源池
    /// </summary>
    public string buildingPool;
    /// <summary>
    /// 每战斗区域敌人数
    /// </summary>
    public int enemiesPerRegion;
    /// <summary>
    /// NPC预留点数量
    /// </summary>
    public int npcCount;
    /// <summary>
    /// 杂物预留点数量
    /// </summary>
    public int propCount;
    /// <summary>
    /// 交互预留点数量
    /// </summary>
    public int interactiveCount;
    /// <summary>
    /// 装饰预留点数量
    /// </summary>
    public int decorationCount;
    /// <summary>
    /// 内容点最小间距
    /// </summary>
    public int contentMinSpacing;
    /// <summary>
    /// 入口安全半径
    /// </summary>
    public int entrySafetyRadius;
    /// <summary>
    /// 最大生成重试次数
    /// </summary>
    public int retryMax;
    /// <summary>
    /// 最大可达修复次数
    /// </summary>
    public int repairMax;
    /// <summary>
    /// 最大格子预算
    /// </summary>
    public int maxTileCells;
    /// <summary>
    /// 最大内容对象预算
    /// </summary>
    public int maxContentObjects;
    /// <summary>
    /// 每帧工作列数
    /// </summary>
    public int workColumnsPerFrame;
    /// <summary>
    /// 每帧时间预算毫秒
    /// </summary>
    public float frameBudgetMs;
    /// <summary>
    /// 主路实际最短长度
    /// </summary>
    public float mainRouteMinLength;
    /// <summary>
    /// 主路实际最长长度
    /// </summary>
    public float mainRouteMaxLength;
    /// <summary>
    /// 入口在右侧概率百分比
    /// </summary>
    public int startOnRightChance;
}
