using System;	

[Serializable]	
public class MapBiomeConfig
{
	public uint Id;//说明
	public string themeKey;//美术/瓦片主题 Key（与 TileMap 目录名一致）
	public string nameCN;//中文名
	public uint layer;//空间层：1 地下 2 地表 3 地表建筑
	public uint retryMax;//整图重生成上限
	public bool enable;//是否启用
	public float EnemyHpMul;//敌人生命倍率
	public float EnemyDmgMul;//敌人伤害倍率
	public uint DropGroupBase;//掉落组基号
}

