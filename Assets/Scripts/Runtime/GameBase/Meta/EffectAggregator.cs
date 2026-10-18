using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>所有"已点亮/已装备"来源聚合后的**只读效果快照**（每次变更整份重算）。</summary>
public class MetaEffectSnapshot
{
    /// <summary>格位加成（EffectType=2 相加）。</summary>
    public int TotalSlotsBonus;
    /// <summary>带出上限加成（EffectType=3 相加；最终上限 = 1 + 这个，clamp 1~10）。</summary>
    public int DeployCapBonus;
    /// <summary>门客属性加成（EffectType=4，按 Target：1 生命 / 2 攻击 / 3 防御 / 4 速度 / 5 信心上限）。</summary>
    public int HpBonus;
    public int AttackBonus;
    public int DefenseBonus;
    public int SpeedBonus;
    public int ConfidenceCapBonus;
    /// <summary>是否解锁"扎营补充减员"（EffectType=5 取最高）。</summary>
    public bool CampRefillUnlocked;
    /// <summary>农田建造时长增量（秒，EffectType=6 Target=2001 相加；负=缩短）。</summary>
    public int FarmBuildSecondsDelta;
    /// <summary>农田收获产量增量（EffectType=7 Target=2001 相加）。</summary>
    public int FarmOutputDelta;
    /// <summary>概率加成（EffectType=8，W1b 未用）。</summary>
    public int ProbabilityBonus;
    /// <summary>重算计数（UI 可据此刷新）。</summary>
    public int Version;

    public string Dump()
    {
        return "[效果快照 v" + Version + "] 格位+" + TotalSlotsBonus + " 带出上限+" + DeployCapBonus
            + " 生命+" + HpBonus + " 攻击+" + AttackBonus + " 防御+" + DefenseBonus + " 速度+" + SpeedBonus
            + " 信心上限+" + ConfidenceCapBonus
            + " 扎营补员=" + (CampRefillUnlocked ? "已解锁" : "未解锁")
            + " 农田建造" + FarmBuildSecondsDelta + "s 农田产量+" + FarmOutputDelta;
    }
}

/// <summary>
/// **唯一的效果聚合器**（08_Effect §5 口径：增量相加 / 上限取最高 / 覆盖取最后）。
///
/// 来源 = `FamilyTreeEffect`（已点亮节点）→ `Effect`（效果本体）；
/// 输出 = 一份 `MetaEffectSnapshot`，供库存（格位）、带出（上限）、门客（属性）、扎营（补员）、村庄（建造/产量）读取。
///
/// ⚠ **不要在别处再算一遍**：`InventoryService.SlotBonus` / `RetainerService` 的加成 getter /
///   `MetaProgressService.DeployCap` 全部读这里，保证只有一份真相。
/// </summary>
public class EffectAggregator : SingletonBase<EffectAggregator>
{
    private MetaEffectSnapshot _snapshot;
    private bool _dirty = true;

    public MetaEffectSnapshot Snapshot
    {
        get
        {
            if (_snapshot == null || _dirty) Recompute();
            return _snapshot;
        }
    }

    /// <summary>标记脏（点亮节点、读档、重置后调用）。</summary>
    public void MarkDirty()
    {
        _dirty = true;
    }

    /// <summary>整份重算（廉价：表已在内存，节点数 15）。</summary>
    public MetaEffectSnapshot Recompute()
    {
        _dirty = false;

        var snap = new MetaEffectSnapshot();
        snap.Version = (_snapshot == null ? 0 : _snapshot.Version) + 1;

        List<uint> unlocked = MetaProgressService.Instance.Payload.familyTreeNodes;
        if (unlocked == null) unlocked = new List<uint>();

        List<FamilyTreeEffectConfig> links = LoadTable<FamilyTreeEffectConfig>("FamilyTreeEffect");
        List<EffectConfig> effects = LoadTable<EffectConfig>("Effect");
        if (links == null || effects == null)
        {
            _snapshot = snap;
            return _snapshot;
        }

        var effectById = new Dictionary<uint, EffectConfig>();
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] != null && effects[i].Enable && !effectById.ContainsKey(effects[i].Index))
                effectById.Add(effects[i].Index, effects[i]);

        for (int i = 0; i < links.Count; i++)
        {
            FamilyTreeEffectConfig link = links[i];
            if (link == null || !link.Enable) continue;
            if (!unlocked.Contains(link.NodeId)) continue;

            EffectConfig effect;
            if (!effectById.TryGetValue(link.EffectId, out effect))
            {
                ChaosLog.Warn(LogChannel.Config, "[效果聚合] 节点 " + link.NodeId + " 的效果 " + link.EffectId + " 在 Effect 表里不存在（跳过）");
                continue;
            }

            float weight = link.Weight <= 0f ? 1f : link.Weight;
            float value = effect.Value * weight;

            switch (effect.EffectType)
            {
                case 1u: break;   //资源产出：W1b 不用
                case 2u: snap.TotalSlotsBonus += Mathf.RoundToInt(value); break;
                case 3u: snap.DeployCapBonus += Mathf.RoundToInt(value); break;
                case 4u:
                    switch (effect.Target)
                    {
                        case 1u: snap.HpBonus += Mathf.RoundToInt(value); break;
                        case 2u: snap.AttackBonus += Mathf.RoundToInt(value); break;
                        case 3u: snap.DefenseBonus += Mathf.RoundToInt(value); break;
                        case 4u: snap.SpeedBonus += Mathf.RoundToInt(value); break;
                        case 5u: snap.ConfidenceCapBonus += Mathf.RoundToInt(value); break;
                        default:
                            ChaosLog.Warn(LogChannel.Config, "[效果聚合] 类型4 的 Target=" + effect.Target + " 未登记（效果 " + effect.Index + "，跳过）");
                            break;
                    }
                    break;
                case 5u:
                    //上限/解锁型：取最高（任一为 1 即解锁）
                    if (effect.Value >= 1f) snap.CampRefillUnlocked = true;
                    break;
                case 6u:
                    if (effect.Target == 2001u) snap.FarmBuildSecondsDelta += Mathf.RoundToInt(value);
                    break;
                case 7u:
                    if (effect.Target == 2001u) snap.FarmOutputDelta += Mathf.RoundToInt(value);
                    break;
                case 8u: snap.ProbabilityBonus += Mathf.RoundToInt(value); break;
                default:
                    ChaosLog.Warn(LogChannel.Config, "[效果聚合] 未登记的 EffectType=" + effect.EffectType + "（效果 " + effect.Index + "，跳过）");
                    break;
            }
        }

        //硬范围 clamp（08_Effect §5.3 + Lead 裁决）
        snap.TotalSlotsBonus = Mathf.Clamp(snap.TotalSlotsBonus, 0, InventoryService.HardMaxSlots - InventoryService.BaseSlots);
        snap.DeployCapBonus = Mathf.Clamp(snap.DeployCapBonus, 0, 9);          //最终 DeployCap = 1+bonus，clamp 1~10
        snap.ConfidenceCapBonus = Mathf.Clamp(snap.ConfidenceCapBonus, 0, 100);
        snap.ProbabilityBonus = Mathf.Clamp(snap.ProbabilityBonus, 0, 100);

        _snapshot = snap;
        ChaosLog.Info(LogChannel.Config, snap.Dump());
        return _snapshot;
    }

    private static List<T> LoadTable<T>(string tableName) where T : class
    {
        string path = GlobalPath.data_JsonPathToRead + tableName + "Config";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[效果聚合] 找不到 " + path + "（按空表处理）");
            return null;
        }
        DataList<T> list = JsonUtility.FromJson<DataList<T>>(asset.text);
        return (list == null || list.datas == null) ? null : list.datas;
    }
}
