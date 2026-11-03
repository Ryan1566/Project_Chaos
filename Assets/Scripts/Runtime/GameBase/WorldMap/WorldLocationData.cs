using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>地点类型；与配置 Location.Kind 一一对应。</summary>
public enum WorldLocationKind
{
    Capital = 1,
    Trade = 2,
    Tribe = 3,
    Resource = 4,
    Temporary = 5,
    Task = 6,
}

/// <summary>玩家身份；官员只处理同属既有事务，家族按关系贸易与经营。</summary>
public enum WorldLocationIdentity
{
    Official = 0,
    Family = 1,
}

/// <summary>
/// 地点聚合快照（地图/详情/交易共用）。所有字段均为运行时只读数据，
/// 不直接暴露配置表或存档记录，避免 UI 层绕过服务改状态。
/// </summary>
[Serializable]
public sealed class WorldLocationSnapshot
{
    public uint Id;
    public uint FactionId;
    public uint Era;
    public string Name;
    public string Description;
    public string IconKey;
    public string FactionName;
    public string RelationName;
    public WorldLocationKind Kind;
    public Vector2 Position;
    public bool CanTrade;
    public bool CanBuild;
    public bool CanHarvest;
    public bool CanResolve;
    public bool CanOpenOffice;
    public bool Built;
    public string LockReason;
    public uint PendingOutput;
    public uint OutputItemId;
    public double ProductionSeconds;
    public int BuyPercent;
    public string ConditionText;
}

