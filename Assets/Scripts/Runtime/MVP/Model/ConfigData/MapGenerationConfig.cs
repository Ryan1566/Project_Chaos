using System;	

[Serializable]	
public class MapGenerationConfig
{
	public uint Id;//参数序号
	public uint presetId;//地图预设编号
	public bool enable;//是否启用
	public int widthTiles;//地图宽度格数
	public int heightTiles;//地图高度格数
	public int edgeMargin;//边缘安全留白
	public int surfaceBaseY;//地表基准高度
	public int skyClearance;//天空净空高度
	public int undergroundMinY;//地下最低高度
	public int surfaceAmplitude;//地表起伏幅度
	public int surfaceStepLength;//地表变化步长
	public int maxStepHeight;//路线最大台阶高度
	public int routeHeadroom;//路线头顶净空
	public int platformMinWidth;//最小站立平台宽度
	public int maxGapWidth;//野外非必经缺口宽度
	public int maxDropHeight;//路线最大落差
	public int regionWidthMin;//战斗区域最小宽度
	public int regionWidthMax;//战斗区域最大宽度
	public int branchCountMin;//最少支路数量
	public int branchCountMax;//最多支路数量
	public int branchLengthMin;//支路最小长度
	public int branchLengthMax;//支路最大长度
	public int loopChance;//回环生成概率百分比
	public int caveDepth;//洞穴目标深度
	public int caveHeight;//洞穴通道高度
	public int caveWidth;//洞穴通道宽度
	public int chamberRadius;//洞室目标半径
	public int caveEntranceMin;//最少洞口数量
	public int caveEntranceMax;//最多洞口数量
	public int caveSmoothPasses;//洞穴平滑次数
	public int buildingCountMin;//最少建筑数量
	public int buildingCountMax;//最多建筑数量
	public int buildingMinSpacing;//建筑最小间距
	public string buildingPool;//手工结构资源池
	public int enemiesPerRegion;//每战斗区域敌人数
	public int npcCount;//NPC预留点数量
	public int propCount;//杂物预留点数量
	public int interactiveCount;//交互预留点数量
	public int decorationCount;//装饰预留点数量
	public int contentMinSpacing;//内容点最小间距
	public int entrySafetyRadius;//入口安全半径
	public int retryMax;//最大生成重试次数
	public int repairMax;//最大可达修复次数
	public int maxTileCells;//最大格子预算
	public int maxContentObjects;//最大内容对象预算
	public int workColumnsPerFrame;//每帧工作列数
	public float frameBudgetMs;//每帧时间预算毫秒
	public float mainRouteMinLength;//主路实际最短长度
	public float mainRouteMaxLength;//主路实际最长长度
	public int startOnRightChance;//入口在右侧概率百分比
}

