using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 门客系统（M2 招募/展示 + M11 重伤/信心）的服务层 —— **唯一读写入口**。
///
/// ══════════════ 表是模板、存档是实例 ══════════════
/// `Retainer` 表 = 模板；`SavePayload.retainers` 里每个 `RetainerSave` = 某一行的**副本实例**
/// （`protoId` → `RetainerConfig.Index`）。属性**不落盘**（模板值 + 将来 Effect 加成），
/// 只落盘会变的东西：当前生命 `hp`、信心 `confidence`、重伤 `wounded`/`woundedRemainSeconds`。
///
/// ══════════════ 可出战（口径原文 05 §3.2）══════════════
/// `可出战 = (!wounded) && (confidence >= ConfidenceThreshold=30)`；重伤**优先**显示。
/// ══════════════ 重伤只算「运行中时间」══════════════
/// `Tick(dt)` 递减 `woundedRemainSeconds`，归零自动恢复；**信心不受重伤影响**。
/// </summary>
public class RetainerService : SingletonBase<RetainerService>
{
    /// <summary>可出战信心阈值（原型期常量，W1 后期考虑移表）。</summary>
    public const int ConfidenceThreshold = 30;

    /// <summary>重伤时长（原型期常量 30 秒）。</summary>
    public const uint DefaultWoundSeconds = 30u;

    /// <summary>门客数量上限（原型）。</summary>
    public const int MaxRetainers = 8;

    /// <summary>重伤/信心不足/可用 三态。</summary>
    public enum DeployState
    {
        Available = 0,
        LowConfidence = 1,
        Wounded = 2,
    }

    private List<RetainerConfig> _defs;
    private bool _loaded;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        string path = GlobalPath.data_JsonPathToRead + "RetainerConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[门客] 找不到 " + path + "（招募/展示会退化，不抛异常）");
            _defs = new List<RetainerConfig>();
            return;
        }

        DataList<RetainerConfig> list = JsonUtility.FromJson<DataList<RetainerConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<RetainerConfig>() : list.datas;
    }

    public void Reload()
    {
        _loaded = false;
        _defs = null;
        EnsureLoaded();
    }

    public int DefinitionCount { get { EnsureLoaded(); return _defs == null ? 0 : _defs.Count; } }

    /// <summary>按 `protoId` 回查模板；找不到返回 null（调用方走占位分支，E3）。</summary>
    public RetainerConfig GetDefinition(uint protoId)
    {
        EnsureLoaded();
        if (_defs == null) return null;
        for (int i = 0; i < _defs.Count; i++)
            if (_defs[i] != null && _defs[i].Index == protoId) return _defs[i];
        return null;
    }

    /// <summary>实例列表（直接返回存档里的 List 引用；面板只读）。</summary>
    public List<RetainerSave> GetAll()
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.retainers == null) p.retainers = new List<RetainerSave>();
        return p.retainers;
    }

    public int Count { get { return GetAll().Count; } }

    public bool TryGet(uint id, out RetainerSave retainer)
    {
        List<RetainerSave> all = GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].id == id) { retainer = all[i]; return true; }
        }
        retainer = null;
        return false;
    }

    private uint NextId()
    {
        uint max = 0;
        List<RetainerSave> all = GetAll();
        for (int i = 0; i < all.Count; i++) if (all[i] != null && all[i].id > max) max = all[i].id;
        return max + 1;
    }

    /// <summary>
    /// 按 `RecruitWeight` 加权随机招募一个门客（**不消耗资源**，GM 免费发放）。
    /// 达到上限 8 / 无可用模板时返回 null 并 Warn（E2）。
    /// </summary>
    public RetainerSave RecruitRandom()
    {
        EnsureLoaded();

        if (GetAll().Count >= MaxRetainers)
        {
            ChaosLog.Warn(LogChannel.Config, "[门客] 已达上限 " + MaxRetainers + "，招募被拒");
            return null;
        }

        var pool = new List<RetainerConfig>();
        uint total = 0;
        if (_defs != null)
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                RetainerConfig d = _defs[i];
                if (d == null || !d.Enable || d.RecruitWeight == 0) continue;
                pool.Add(d);
                total += d.RecruitWeight;
            }
        }

        if (pool.Count == 0 || total == 0)
        {
            ChaosLog.Warn(LogChannel.Config, "[门客] 没有可招募的模板（表为空/全 Enable=false/权重全 0）");
            return null;
        }

        uint roll = (uint)Random.Range(0, (int)total);
        uint acc = 0;
        RetainerConfig picked = null;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].RecruitWeight;
            if (roll < acc) { picked = pool[i]; break; }
        }
        if (picked == null) picked = pool[pool.Count - 1];

        RetainerSave instance = CreateInstance(picked);
        ChaosLog.Success(LogChannel.Config,
            "[门客] 招募到 #" + instance.id + " protoId=" + picked.Index + "（" + picked.NameKey + "）"
            + " 品质=" + instance.quality + " 信心=" + instance.confidence + "，当前 " + GetAll().Count + "/" + MaxRetainers);
        return instance;
    }

    /// <summary>
    /// **按模板 ID 实例化一名门客**（W5.1 剧情事件「给门客」用）。
    ///
    /// 与 <see cref="RecruitRandom"/> **同一条实例化路径**（都走 <see cref="CreateInstance"/>），
    /// 只是不再随机挑模板 —— 事件表里的 `GiveRetainerId` 就指 `RetainerConfig.Index`。
    /// 达到上限 / 模板不存在 / 模板禁用时返回 null 并给出原因（E4：跳过该奖励 + Warn）。
    /// </summary>
    public RetainerSave RecruitByProtoId(uint protoId, out string reason)
    {
        reason = null;
        EnsureLoaded();

        if (GetAll().Count >= MaxRetainers)
        {
            reason = "门客已达上限 " + MaxRetainers;
            ChaosLog.Warn(LogChannel.Config, "[门客] " + reason + "，按模板 " + protoId + " 发放被拒");
            return null;
        }

        RetainerConfig def = GetDefinition(protoId);
        if (def == null)
        {
            reason = "门客模板 " + protoId + " 不存在";
            ChaosLog.Warn(LogChannel.Config, "[门客] " + reason);
            return null;
        }
        if (!def.Enable)
        {
            reason = "门客模板 " + protoId + " 已禁用（Enable=false）";
            ChaosLog.Warn(LogChannel.Config, "[门客] " + reason);
            return null;
        }

        RetainerSave instance = CreateInstance(def);
        ChaosLog.Success(LogChannel.Config,
            "[门客] 按模板发放 #" + instance.id + " protoId=" + def.Index + "（" + def.NameKey + "）"
            + " 品质=" + instance.quality + " 信心=" + instance.confidence + "，当前 " + GetAll().Count + "/" + MaxRetainers);
        return instance;
    }

    /// <summary>模板 → 存档实例（招募与事件发放**共用**这一处，别另写一份）。</summary>
    private RetainerSave CreateInstance(RetainerConfig picked)
    {
        var instance = new RetainerSave
        {
            id = NextId(),
            protoId = picked.Index,
            quality = picked.Quality,
            hp = (int)picked.Hp,
            confidence = (int)Mathf.Clamp((int)picked.InitialConfidence, 0, 100),
            wounded = false,
            woundedRemainSeconds = 0u,
            woundedUntilUtcTicks = 0L,   //保留字段，恒 0 不读（见 05 §6.2）
        };

        GetAll().Add(instance);
        return instance;
    }

    /// <summary>可出战判定：`!wounded && confidence >= 30`。</summary>
    public bool IsDeployable(RetainerSave retainer)
    {
        if (retainer == null) return false;
        return !retainer.wounded && retainer.confidence >= ConfidenceThreshold;
    }

    /// <summary>三态（优先级：重伤 &gt; 信心不足 &gt; 可用）。</summary>
    public DeployState GetState(RetainerSave retainer)
    {
        if (retainer == null) return DeployState.LowConfidence;
        if (retainer.wounded) return DeployState.Wounded;
        if (retainer.confidence < ConfidenceThreshold) return DeployState.LowConfidence;
        return DeployState.Available;
    }

    /// <summary>状态标签文案（GM/面板共用；面板走 LocalizedText 时可用 key 替换）。</summary>
    public string GetStateLabel(RetainerSave retainer)
    {
        switch (GetState(retainer))
        {
            case DeployState.Wounded: return "重伤 " + retainer.woundedRemainSeconds + "s";
            case DeployState.LowConfidence: return "信心不足";
            default: return "可用";
        }
    }

    /// <summary>把门客打成重伤（原型期 GM 触发）。**信心不变**。</summary>
    public bool Wound(uint id, uint seconds = DefaultWoundSeconds)
    {
        RetainerSave retainer;
        if (!TryGet(id, out retainer))
        {
            ChaosLog.Warn(LogChannel.Config, "[门客] 打伤失败：找不到 id=" + id);
            return false;
        }
        if (seconds == 0) seconds = DefaultWoundSeconds;

        retainer.wounded = true;
        retainer.woundedRemainSeconds = seconds;
        retainer.woundedUntilUtcTicks = 0L;
        ChaosLog.Info(LogChannel.Config,
            "[门客] #" + id + " 重伤 " + seconds + "s（信心仍为 " + retainer.confidence + "，重伤不影响信心）");
        return true;
    }

    /// <summary>逐帧递减重伤剩余秒数（**只在运行中走**）；归零自动恢复，信心不变。</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        List<RetainerSave> all = GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            RetainerSave retainer = all[i];
            if (retainer == null || !retainer.wounded) continue;

            float remain = retainer.woundedRemainSeconds - deltaTime;
            if (remain <= 0f)
            {
                retainer.wounded = false;
                retainer.woundedRemainSeconds = 0u;
                ChaosLog.Success(LogChannel.Config,
                    "[门客] #" + retainer.id + " 重伤恢复 → 可用（信心 " + retainer.confidence + " 保持不变）");
            }
            else
            {
                retainer.woundedRemainSeconds = (uint)Mathf.CeilToInt(remain);
            }
        }
    }

    /// <summary>信心增减，clamp 0~100。</summary>
    public bool ChangeConfidence(uint id, int delta)
    {
        RetainerSave retainer;
        if (!TryGet(id, out retainer))
        {
            ChaosLog.Warn(LogChannel.Config, "[门客] 改信心失败：找不到 id=" + id);
            return false;
        }

        int before = retainer.confidence;
        retainer.confidence = Mathf.Clamp(retainer.confidence + delta, 0, 100);
        ChaosLog.Info(LogChannel.Config,
            "[门客] #" + id + " 信心 " + before + " → " + retainer.confidence + "（delta=" + delta + "）");
        return true;
    }

    /// <summary>给 GM 打印的全部门客状态。</summary>
    public string DumpAll()
    {
        List<RetainerSave> all = GetAll();
        var sb = new System.Text.StringBuilder();
        sb.Append("[门客] 共 ").Append(all.Count).Append("/").Append(MaxRetainers).Append(" 位：");
        for (int i = 0; i < all.Count; i++)
        {
            RetainerSave retainer = all[i];
            if (retainer == null) continue;
            sb.Append("\n  #").Append(retainer.id)
              .Append(" proto=").Append(retainer.protoId)
              .Append(" 品质=").Append(retainer.quality)
              .Append(" 信心=").Append(retainer.confidence)
              .Append(" 状态=").Append(GetStateLabel(retainer))
              .Append(" 可出战=").Append(IsDeployable(retainer));
        }
        return sb.ToString();
    }

    // ══════════════ 家族树效果加成（EffectType=4，只读；不改存档字段）══════════════

    /// <summary>门客生命加成。</summary>
    public int BonusHp { get { return EffectAggregator.Instance.Snapshot.HpBonus; } }
    /// <summary>门客攻击加成。</summary>
    public int BonusAttack { get { return EffectAggregator.Instance.Snapshot.AttackBonus; } }
    /// <summary>门客防御加成。</summary>
    public int BonusDefense { get { return EffectAggregator.Instance.Snapshot.DefenseBonus; } }
    /// <summary>门客速度加成。</summary>
    public int BonusSpeed { get { return EffectAggregator.Instance.Snapshot.SpeedBonus; } }

    /// <summary>某门客的最终属性 = 模板值 + 家族树加成（属性不落盘）。</summary>
    public void GetEffectiveStats(RetainerSave retainer, out int hp, out int attack, out int defense, out int speed)
    {
        hp = attack = defense = speed = 0;
        if (retainer == null) return;
        RetainerConfig def = GetDefinition(retainer.protoId);
        if (def == null) return;
        hp = (int)def.Hp + BonusHp;
        attack = (int)def.Attack + BonusAttack;
        defense = (int)def.Defense + BonusDefense;
        speed = (int)def.Speed + BonusSpeed;
    }

    /// <summary>调试：清空门客列表。</summary>
    public void ClearAll()
    {
        GetAll().Clear();
        ChaosLog.Info(LogChannel.Config, "[门客] 已清空");
    }
}
