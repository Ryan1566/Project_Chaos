using System;	

[Serializable]	
public class MapLoadingConfig
{
	public uint Id;//说明
	public uint stage;//阶段码：1 参数装配 2 生成房间图 3 排布网格 4 挑选房间块 5 实例化 6 封堵填充 7 校验 8 完成
	public uint sort;//展示顺序
	public string descCN;//简体中文描述（禁止英文逗号/双引号/换行）
	public string descTW;//繁体中文描述
	public string descEN;//英语描述
	public string descJP;//日语描述
	public string descKR;//韩语描述
}

