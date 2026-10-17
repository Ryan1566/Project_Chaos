using System;	

[Serializable]	
public class EffectConfig
{
	public uint Index;//序号
	public uint EffectType;//效果类型
	public uint Target;//目标
	public float Value;//数值
	public string Note;//说明
	public bool Enable;//是否启用
}

