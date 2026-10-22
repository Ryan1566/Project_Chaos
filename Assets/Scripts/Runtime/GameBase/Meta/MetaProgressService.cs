using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 局外进度（M13 时代 / M13b 深度）的**唯一读写入口**。
///
/// ══════════════ 两条轴（概念案 §5.3）══════════════
/// * **时代 Era**：1~5，**单向**推进（只能 +1，不可回退）。双门槛：本局打过第 3 场景 boss + 局外里程碑达标。
/// * **深度 Depth**：每通关 +1，但**上限 = 当前时代序号**（`depth ≤ era`），由**运行时**截断 —— 表只给参数。
///
/// ══════════════ 没有会话也能用（GM / TestScene 原型）══════════════
/// `GameSession.LoadedPayload` 为 null 时用一份**内存态** `SavePayload`，这样 GM 在 TestScene 里
/// 不读档也能点进度按钮。一旦 `GameSession.Begin(...)` 建立会话，自动改用会话里的 payload。
/// </summary>
public class MetaProgressService : SingletonBase<MetaProgressService>
{
    public const uint MinEra = 1;
    public const uint MaxEra = 5;
    public const uint MinDepth = 1;

    /// <summary>「本局打过第 3 场景 boss」判定用的场景序号（1 起）。</summary>
    public const int FinalSceneIndex = 3;

    /// <summary>无会话时的内存态 payload（GM / TestScene 原型用）。</summary>
    private SavePayload _memoryPayload;

    /// <summary>「本局打过第 3 场景 boss」标记（**每局重置**，不落盘）。</summary>
    private bool _bossClearedThisRun;

    /// <summary>是否有真实会话（有则读写 `GameSession.LoadedPayload`）。</summary>
    public bool HasSession
    {
        get { return GameSession.Instance.HasSession && GameSession.Instance.LoadedPayload != null; }
    }

    /// <summary>当前进度的载体：优先会话 payload，否则内存态（懒建）。</summary>
    public SavePayload Payload
    {
        get
        {
            if (HasSession) return GameSession.Instance.LoadedPayload;
            if (_memoryPayload == null) _memoryPayload = new SavePayload();
            return _memoryPayload;
        }
    }

    public uint Era
    {
        get { return (uint)Mathf.Clamp((int)Payload.era, (int)MinEra, (int)MaxEra); }
    }

    /// <summary>当前深度（已按 `depth ≤ era` 夹紧，展示/查表都安全）。</summary>
    public uint Depth
    {
        get { return (uint)Mathf.Clamp((int)Payload.depth, (int)MinDepth, (int)Era); }
    }

    /// <summary>深度上限 = 当前时代序号（概念案 M13b）。</summary>
    public uint DepthCap { get { return Era; } }

    public bool BossClearedThisRun { get { return _bossClearedThisRun; } }

    /// <summary>原型无存档会话时只使用槽 3；正式流程使用玩家选择的槽位。</summary>
    public int SaveSlot { get { return HasSession ? GameSession.Instance.SelectedSlot : 3; } }

    public bool SaveProgress(out string reason)
    {
        bool saved = SaveSlotService.Instance.Save(SaveSlot, Payload);
        reason = saved ? null : "保存失败，请检查磁盘后重试（槽 " + SaveSlot + "）";
        return saved;
    }

    /// <summary>标记「本局打过第 3 场景 boss」。GM 与将来的结算流程都调它。</summary>
    public void MarkBossCleared()
    {
        _bossClearedThisRun = true;
        ChaosLog.Info(LogChannel.Config, "[进度] 已标记：本局打过第 " + FinalSceneIndex + " 场景 boss");
    }

    /// <summary>记录"通关了一个场景"；打到第 3 场景时顺带置 boss 标记。</summary>
    public void MarkSceneCleared(int sceneIndex)
    {
        SavePayload p = Payload;
        p.scenesCleared++;
        if (sceneIndex >= FinalSceneIndex) MarkBossCleared();
        ChaosLog.Info(LogChannel.Config, "[进度] 通关场景 " + sceneIndex + "（累计场景数 " + p.scenesCleared + "）");
    }

    /// <summary>
    /// **深度 +1**：`depth < era` 才推进，否则**截断**（不变）。
    /// 返回是否真的变了（GM 连点时可据此看"第 2 次起不再增加"）。
    /// </summary>
    public bool TryAdvanceDepth()
    {
        SavePayload p = Payload;
        uint depth = (uint)Mathf.Clamp((int)p.depth, (int)MinDepth, (int)MaxEra);
        uint cap = Era;

        if (depth >= cap)
        {
            p.depth = cap;   //顺手把越界的旧值夹回来
            ChaosLog.Info(LogChannel.Config,
                "[进度] 深度不推进：已是上限（depth=" + depth + " / 上限=" + cap + "，上限=当前时代序号）");
            return false;
        }

        p.depth = depth + 1;
        ChaosLog.Info(LogChannel.Config,
            "[进度] 深度 +1 → " + p.depth + " / 上限 " + cap + "（时代 " + Era + "）");
        return true;
    }

    /// <summary>
    /// **时代 +1（双门槛）**：① 本局打过第 3 场景 boss；② 局外里程碑达标（阈值查 `Era` 表）。
    /// 只能 +1、不可回退；到最后一个时代后拒绝。**深度不重置**（K5），只夹到新上限。
    /// </summary>
    public bool TryAdvanceEra()
    {
        SavePayload p = Payload;
        uint era = Era;

        if (era >= MaxEra)
        {
            ChaosLog.Warn(LogChannel.Config, "[进度] 无法推进时代：已经是最后一个时代（Era=" + era + "）");
            return false;
        }

        if (!_bossClearedThisRun)
        {
            ChaosLog.Warn(LogChannel.Config,
                "[进度] 无法推进时代：本局还没打过第 " + FinalSceneIndex + " 场景 boss（门槛①未满足）");
            return false;
        }

        uint type = EraConfigService.Instance.GetMilestoneType(era);
        uint need = EraConfigService.Instance.GetMilestoneValue(era);
        uint have = type == 2u ? (uint)p.familyTreeNodes.Count : p.clears;

        if (have < need)
        {
            ChaosLog.Warn(LogChannel.Config,
                "[进度] 无法推进时代：局外里程碑未达标（Type=" + type + " 需要 " + need + "，当前 " + have + "）（门槛②未满足）");
            return false;
        }

        p.era = era + 1;
        //深度不重置（概念案 Q1'）；只保证不越新上限
        p.depth = (uint)Mathf.Clamp((int)p.depth, (int)MinDepth, (int)p.era);
        //一次 boss 只能推一次：推完清标记，避免"一次通关连推两个时代"
        _bossClearedThisRun = false;

        ChaosLog.Success(LogChannel.Config,
            "[进度] 时代推进 " + era + " → " + p.era + "（深度 " + p.depth + " / 新上限 " + p.era + "）");
        return true;
    }

    /// <summary>GM：「模拟通关一次」= 累计通关数 +1，并尝试深度 +1（会被上限截断）。</summary>
    public void SimulateClear()
    {
        SavePayload p = Payload;
        p.clears++;
        ChaosLog.Info(LogChannel.Config, "[进度] 模拟通关：clears=" + p.clears);
        TryAdvanceDepth();
    }

    /// <summary>GM：「模拟满足里程碑」= 置 boss 标记 + 把里程碑统计量垫到当前时代阈值。</summary>
    public void SimulateSatisfyMilestone()
    {
        SavePayload p = Payload;
        uint era = Era;
        uint type = EraConfigService.Instance.GetMilestoneType(era);
        uint need = EraConfigService.Instance.GetMilestoneValue(era);

        _bossClearedThisRun = true;
        if (type == 2u)
        {
            while ((uint)p.familyTreeNodes.Count < need) p.familyTreeNodes.Add((uint)(p.familyTreeNodes.Count + 1));
        }
        else if (p.clears < need)
        {
            p.clears = need;
        }

        ChaosLog.Info(LogChannel.Config,
            "[进度] 模拟满足里程碑：boss 标记=true，Type=" + type + " 需要 " + need
            + "，当前 clears=" + p.clears + " familyTreeNodes=" + p.familyTreeNodes.Count);
    }

    /// <summary>调试：把进度恢复到初始值（不删存档文件）。</summary>
    public void ResetAll()
    {
        SavePayload p = Payload;
        p.era = MinEra;
        p.depth = MinDepth;
        p.resMaterial = 0;
        p.resBlueprint = 0;
        p.resGoods = 0;
        p.resMoney = 0;
        if (p.retainers == null) p.retainers = new List<RetainerSave>(); else p.retainers.Clear();
        if (p.buildings == null) p.buildings = new List<BuildingSave>(); else p.buildings.Clear();
        if (p.familyTreeNodes == null) p.familyTreeNodes = new List<uint>(); else p.familyTreeNodes.Clear();
        p.runs = 0;
        p.clears = 0;
        p.scenesCleared = 0;
        _bossClearedThisRun = false;

        ChaosLog.Info(LogChannel.Config, "[进度] 已重置：era=" + p.era + " depth=" + p.depth + " 资源/计数归零");
    }

    /// <summary>带出上限 = 1 + 家族树带出加成，clamp 1~10（W3 带出用）。</summary>
    public int DeployCap
    {
        get { return Mathf.Clamp(1 + EffectAggregator.Instance.Snapshot.DeployCapBonus, 1, 10); }
    }

    /// <summary>扎营「补充减员」是否已解锁（EffectType=5；W3 扎营用）。</summary>
    public bool CampRefillUnlocked
    {
        get { return EffectAggregator.Instance.Snapshot.CampRefillUnlocked; }
    }

    /// <summary>给 GM 打印的一行状态（含难度与里程碑进度）。</summary>
    public string DumpState()
    {
        SavePayload p = Payload;
        uint era = Era;
        uint type = EraConfigService.Instance.GetMilestoneType(era);
        uint need = EraConfigService.Instance.GetMilestoneValue(era);
        uint have = type == 2u ? (uint)p.familyTreeNodes.Count : p.clears;
        float difficulty = EraConfigService.Instance.GetFinalDifficulty(era, Depth);

        return "[进度] 会话=" + (HasSession ? ("槽" + GameSession.Instance.SelectedSlot) : "内存态")
            + " era=" + era + "/5 depth=" + Depth + "/" + DepthCap
            + " 难度=" + EraConfigService.Instance.GetBaseDifficulty(era) + "+"
            + EraConfigService.Instance.GetDepthBonus(era, Depth) + "=" + difficulty
            + " | boss本局=" + _bossClearedThisRun
            + " 里程碑 Type=" + type + " " + have + "/" + need
            + " | clears=" + p.clears + " runs=" + p.runs + " scenesCleared=" + p.scenesCleared
            + " | 资源 材料=" + p.resMaterial + " 图纸=" + p.resBlueprint + " 货物=" + p.resGoods + " 钱=" + p.resMoney;
    }
}
