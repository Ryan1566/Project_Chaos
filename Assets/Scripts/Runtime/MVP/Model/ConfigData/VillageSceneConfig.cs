using System;	

[Serializable]	
public class VillageSceneConfig
{
	public uint Id;//场景配置编号
	public int widthTiles;//地面宽度 格
	public int groundDepth;//地面厚度 格
	public float playerSpawnX;//玩家出生横坐标 米
	public float groundY;//地面顶面高度 米
	public int workerCount;//固定岗位NPC人数
	public float workerMoveSpeed;//工人速度 米每秒
	public float workSeconds;//单次连续工作 秒
	public float restSeconds;//单次休息 秒
	public float wanderMinSeconds;//最少闲逛 秒
	public float wanderMaxSeconds;//最多闲逛 秒
	public float plotSpacing;//地块中心间距 米
	public float firstPlotX;//首个地块横坐标 米
	public string groundTilePool;//地面瓦片资源池
}

