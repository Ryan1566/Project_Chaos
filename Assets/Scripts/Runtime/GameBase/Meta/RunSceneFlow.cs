using System.Collections.Generic;
using UnityEngine;

/// <summary>Run-specific preparation/adoption. Transition ownership belongs to SceneTransitionCoordinator.</summary>
public static class RunSceneFlow
{
    public const string GameplayScene = "WorldScene";
    public const string LoadingScene = "LoadingScene";
    private static List<uint> team;
    private static uint seed;
    private static int stage;
    private static List<BattleSlotSave> entryLoadout;
    private static ActiveRunSave resume;
    private static bool nextStage;
    public static bool IsTransitioning => SceneTransitionCoordinator.IsBusy;
    public static bool IsFormalRun => SceneTransitionCoordinator.IsFormalRun;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { team = null; stage = 0; seed = 0; entryLoadout = null; resume = null; nextStage = false; }

    public static bool Begin(List<uint> selected, IReadOnlyList<BattleSlotSave> loadout, out string reason)
    {
        reason = null;
        if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady || !GameSession.Instance.IsPersistent)
        { reason = "请先读取存档进入村庄"; return false; }
        if (selected == null || selected.Count == 0) { reason = "出征队伍为空"; return false; }
        if (SceneTransitionCoordinator.IsBusy || SceneTransitionCoordinator.HasRun) { reason = "已有出征或场景切换"; return false; }
        if (RunCheckpoint.Current != null) { reason = "先处理尚未完成的出征"; return false; }
        if (!BattleLoadout.Validate(loadout, true, out reason)) return false;
        resume = null;
        nextStage = false;
        entryLoadout = new List<BattleSlotSave>();
        foreach (var slot in loadout) entryLoadout.Add(slot.Copy());
        team = new List<uint>(selected); seed = RunMap.NaturalMapRuntime.PreviewSeed ?? RunMap.NaturalMapRuntime.CreateRunSeed(); stage = 1;
        if (LoadStage(SceneTransitionCoordinator.TransitionKind.EnterRun, out reason)) return true;
        Reset();
        return false;
    }

    public static bool Resume(out string reason)
    {
        var saved = RunCheckpoint.Current;
        if (!RunCheckpoint.Validate(saved, out reason)) return false;
        if (SceneTransitionCoordinator.IsBusy) { reason = "已有场景切换，请稍候"; return false; }
        Reset();
        resume = saved;
        team = new List<uint>(saved.team);
        seed = saved.seed;
        stage = saved.stage;
        if (LoadStage(SceneTransitionCoordinator.TransitionKind.EnterRun, out reason)) return true;
        Reset();
        return false;
    }
    public static bool BeginNextStage(int index, List<uint> selected, uint runSeed)
    {
        if (!IsFormalRun || index < 2 || index > RunEncounter.MaxSceneIndex || selected == null || selected.Count == 0) return false;
        nextStage = true;
        team = new List<uint>(selected); seed = runSeed; stage = index;
        RunEncounter.SuspendForSceneTransition();
        if (LoadStage(SceneTransitionCoordinator.TransitionKind.NextRunStage, out string reason)) return true;
        FailFormalRun(reason); return false;
    }
    private static bool LoadStage(SceneTransitionCoordinator.TransitionKind kind, out string reason)
    {
        RunMap.RunMapBootstrap.RequestForScene(GameplayScene, seed, stage);
        if (kind == SceneTransitionCoordinator.TransitionKind.EnterRun && UIManager.Instance != null)
            UIManager.Instance.HideAllPanelsForGameplay();
        bool started = SceneTransitionCoordinator.TryBegin(kind, GameplayScene, true, AdoptStage, FailFormalRun, out reason);
        if (!started && kind == SceneTransitionCoordinator.TransitionKind.EnterRun && UIManager.Instance != null)
            UIManager.Instance.RestoreAfterGameplay();
        return started;
    }
    private static void AdoptStage()
    {
        var restored = resume;
        System.Action<ActiveRunSave> commit = null;
        if (entryLoadout != null)
            commit = save =>
            {
                if (!RunCheckpoint.CommitEntry(save, entryLoadout, out string reason))
                    throw new System.InvalidOperationException(reason);
            };
        else if (nextStage)
            commit = save =>
            {
                if (!RunCheckpoint.CommitStage(save, out string reason)) throw new System.InvalidOperationException(reason);
            };
        if (!RunEncounter.EnterPreparedScene(stage, team, seed, restored, entryLoadout, commit))
            throw new System.InvalidOperationException("第 " + stage + " 阶段无法接管已准备地图");
        if (entryLoadout != null) BattlePreparationFlow.Clear();
        entryLoadout = null;
        resume = null;
        nextStage = false;
        if (restored?.outcome > 0) RunEncounter.RestoreSettlement(restored);
    }
    public static bool CanReturnToMeta(out string reason)
    {
        reason = null;
        if (!IsFormalRun || SceneTransitionCoordinator.IsBusy) { reason = "当前不在可返回的正式出征中"; return false; }
        if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MainScene.unity") < 0)
        { reason = "村庄场景未加入构建列表"; return false; }
        return true;
    }
    public static void ReturnToMeta()
    {
        if (!CanReturnToMeta(out string reason)) throw new System.InvalidOperationException(reason);
        if (!SceneTransitionCoordinator.TryBegin(SceneTransitionCoordinator.TransitionKind.ReturnVillage, VillageSceneFlow.SceneName, false,
            () => { team = null; stage = 0; VillageSceneFlow.OnReturnedToVillage(); EventService.Instance.CheckAndShowOnReturnToMeta(); },
            HandleReturnFailure, out reason)) throw new System.InvalidOperationException(reason);
    }

    private static void HandleReturnFailure(string reason)
    {
        var settlement = Chaos.MVP.Model.Gameplay.RunSettlementModel.Current;
        if (settlement?.Completed == true && SceneTransitionCoordinator.IsFormalRun)
        {
            RunSettlementPanel.Open(settlement.Win);
            var panel = UIManager.Instance.panelStack.Count > 0 ? UIManager.Instance.panelStack.Peek() as RunSettlementPanel : null;
            panel?.ShowMessage("结算已保存，返回村庄失败：" + reason, false);
            return;
        }
        VillageSceneFlow.ReportEntryFailure(reason);
    }
    private static void FailFormalRun(string reason)
    {
        Debug.LogError("[RunFlow] " + reason);
        bool failedNewEntry = entryLoadout != null && RunCheckpoint.Current == null;
        Reset();
        RunEncounter.Clear();
        Chaos.MVP.Model.Gameplay.RunControlModel.PauseWorld(false);
        Time.timeScale = 1;
        if (failedNewEntry)
        {
            try
            {
                VillageSceneFlow.EnterFromSave(() => BattlePreparationFlow.ReopenAfterFailure(reason));
                return;
            }
            catch (System.Exception error) { reason += "；回村失败：" + error.Message; }
        }
        VillageSceneFlow.ReportEntryFailure(reason);
    }
}
