using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>检查点和仓库使用同一个存档提交点，避免读条中断造成物品重复扣除。</summary>
public static class RunCheckpoint
{
    public static ActiveRunSave Current
    {
        get
        {
            var save = GameSession.Instance.LoadedPayload?.activeRun;
            return save != null && (save.format != 0 || save.stage != 0 || save.departure != 0
                || save.team?.Count > 0) ? save : null;
        }
    }

    public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public static bool IsPositiveTime(float value) => IsFinite(value) && value > 0;
    internal static bool IsPlayable => Current != null && RunSceneFlow.IsFormalRun && !RunSceneFlow.IsTransitioning
        && (RunEncounter.IsSceneReady || RunEncounter.RunCleared || RunEncounter.Defeated);

    internal static bool Validate(ActiveRunSave save, out string reason)
    {
        reason = "未完成出征的检查点无法读取";
        if (save == null || save.format != ActiveRunSave.CurrentFormat || save.departure != GameSession.Instance.LoadedPayload.runs
            || save.stage < 1 || save.stage > RunEncounter.MaxSceneIndex || save.era < 1 || save.era > 5
            || save.team == null || save.team.Count < 1 || save.team.Count > DispatchService.MaxTeamSize
            || save.team.Distinct().Count() != save.team.Count || save.members == null || save.members.Count != save.team.Count
            || save.buffs == null || save.cooldowns == null || save.quests == null || save.claims == null
            || save.defeatedEnemies == null || save.drops == null || save.startingImportant == null || save.retained == null
            || save.outcome < 0 || save.outcome > 2 || (save.outcome == 1 && save.stage != RunEncounter.MaxSceneIndex)
            || save.settlementPage < 0 || save.settlementPage > 2
            || save.kills < 0 || save.highestDamage < 0 || save.stagesPassed < 0 || save.stagesPassed > RunEncounter.MaxSceneIndex
            || !IsFinite(save.timeMinutes) || !IsFinite(save.playSeconds) || save.playSeconds < 0
            || string.IsNullOrEmpty(save.layoutSignature)) return false;
        for (int i = 0; i < save.team.Count; i++)
        {
            var member = save.members[i];
            if (save.team[i] == 0 || member == null || member.id != save.team[i] || member.hp < 0
                || member.downed != (member.hp == 0) || !RetainerService.Instance.TryGet(member.id, out _)) return false;
        }
        if (save.outcome == 2 ? save.members.Any(member => !member.downed) : save.members.All(member => member.downed))
            return false;
        return BattleLoadout.Validate(save.bag, false, out reason);
    }

    internal static bool CommitEntry(ActiveRunSave save, IReadOnlyList<BattleSlotSave> loadout, out string reason)
    {
        reason = null;
        var payload = GameSession.Instance.LoadedPayload;
        if (save == null || payload == null || Current != null || payload.runs == uint.MaxValue)
        { reason = "无法登记本次出征"; return false; }
        if (!BattleLoadout.Validate(loadout, true, out reason)) return false;
        var previous = payload.activeRun;
        return MetaCommandRunner.Execute("Dispatch.EnterRun", (out string why) =>
        {
            foreach (var slot in loadout)
                if (!RunInventory.IsImportant(slot.itemId)
                    && !InventoryService.Instance.RemoveItem(slot.itemId, slot.count, out why)) return false;
            payload.runTeam = new List<uint>(save.team);
            payload.runs++;
            save.departure = payload.runs;
            payload.activeRun = save;
            why = null;
            return true;
        }, out reason, onRolledBack: () => payload.activeRun = previous);
    }

    internal static bool CommitStage(ActiveRunSave save, out string reason)
    {
        var payload = GameSession.Instance.LoadedPayload;
        var previous = Current;
        if (save == null || previous == null || save.departure != previous.departure)
        { reason = "找不到本局切图前的检查点"; return false; }
        return MetaCommandRunner.Execute("Run.EnterStage", (out string why) =>
        {
            payload.activeRun = save;
            why = null;
            return true;
        }, out reason, onRolledBack: () => payload.activeRun = previous);
    }

    internal static bool RefreshForSave(SavePayload payload, out string reason)
    {
        reason = null;
        var previous = Current;
        if (!IsPlayable || previous.stage != RunEncounter.CurrentSceneIndex
            || previous.seed != RunEncounter.RunSeed) return true;
        var snapshot = RunEncounter.CaptureCheckpoint(previous.departure);
        if (snapshot == null) { reason = "无法捕获当前出征状态"; return false; }
        payload.activeRun = snapshot;
        return true;
    }

    private static int pendingContext = -1;
    internal static void RequestSave() => pendingContext = GameSession.Instance.ContextId;
    internal static void FlushRequested()
    {
        if (pendingContext != GameSession.Instance.ContextId || MetaCommandRunner.IsExecuting) return;
        pendingContext = -1;
        if (IsPlayable && !GameSession.Instance.SaveCurrent(out string reason)) UnityEngine.Debug.LogWarning(reason);
    }

    internal static bool Save(out string reason)
    {
        if (!IsPlayable) { reason = "当前没有可保存的局内场景"; return false; }
        return GameSession.Instance.SaveCurrent(out reason);
    }

    public static bool SaveAndReturnToTitle(out string reason)
    {
        if (!Save(out reason)) return false;
        RunEncounter.Clear();
        Chaos.MVP.Model.Gameplay.RunControlModel.PauseWorld(false);
        UnityEngine.Time.timeScale = 1;
        GameSession.Instance.Clear();
        UnityEngine.SceneManagement.SceneManager.LoadScene("EnterScene");
        return true;
    }

    public static bool Abandon(out string reason)
    {
        if (Current == null || SceneTransitionCoordinator.IsBusy || !GameSession.Instance.IsPersistent)
        { reason = "当前没有可放弃的出征"; return false; }
        var save = Current;
        return MetaCommandRunner.Execute("Run.AbandonCheckpoint", (out string why) =>
        {
            if (save.members != null)
                foreach (var member in save.members)
                    if (member != null && member.downed && RetainerService.Instance.TryGet(member.id, out _))
                        RetainerService.Instance.Wound(member.id, 30);
            GameSession.Instance.LoadedPayload.activeRun = null;
            why = null;
            return true;
        }, out reason);
    }
}
