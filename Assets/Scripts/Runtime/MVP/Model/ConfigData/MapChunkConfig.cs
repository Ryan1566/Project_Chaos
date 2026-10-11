using System;	

[Serializable]	
public class MapChunkConfig
{
	public uint Id;//说明
	public uint biomeId;//外键 → MapBiome.Id
	public string themeKey;//主题 Key
	public uint kind;//类型：1 入口 2 通路 3 战斗 9 层间连接 10 boss 门 11 死路奖励
	public uint layer;//所属空间层（0=跨层）
	public string prefabPath;//Resources 相对路径（不含扩展名）
	public uint weight;//抽取权重
	public uint maxCount;//单图最大出现次数
	public bool isUnique;//全图是否最多一次
	public bool enable;//是否启用
}

