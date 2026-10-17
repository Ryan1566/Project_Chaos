using System;	

[Serializable]	
public class EraConfig
{
	public uint Index;//序号
	public uint Era;//时代
	public string NameKey;//名称key
	public uint BaseDifficulty;//基础难度档
	public uint MilestoneType;//里程碑类型
	public uint MilestoneValue;//里程碑阈值
	public bool Enable;//是否启用
}

