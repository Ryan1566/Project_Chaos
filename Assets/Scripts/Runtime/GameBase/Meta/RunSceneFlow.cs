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
    public static bool IsTransitioning => SceneTransitionCoordinator.IsBusy;
    public static bool IsFormalRun => SceneTransitionCoordinator.IsFormalRun;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { team = null; stage = 0; seed = 0; }

    public static bool Begin(List<uint> selected, out string reason)
    {
        reason = null;
        if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady || !GameSession.Instance.IsPersistent)
        { reason = "请先读取存档进入村庄"; return false; }
        if (selected == null || selected.Count == 0) { reason = "出征队伍为空"; return false; }
        if (SceneTransitionCoordinator.IsBusy || SceneTransitionCoordinator.HasRun) { reason = "已有出征或场景切换"; return false; }
        team = new List<uint>(selected); seed = RunMap.NaturalMapRuntime.PreviewSeed ?? RunMap.NaturalMapRuntime.CreateRunSeed(); stage = 1;
        return LoadStage(SceneTransitionCoordinator.TransitionKind.EnterRun, out reason);
    }
    public static bool BeginNextStage(int index, List<uint> selected, uint runSeed)
    {
        if (!IsFormalRun || index < 2 || index > RunEncounter.MaxSceneIndex || selected == null || selected.Count == 0) return false;
        team = new List<uint>(selected); seed = runSeed; stage = index;
        RunEncounter.SuspendForSceneTransition();
        if (LoadStage(SceneTransitionCoordinator.TransitionKind.NextRunStage, out string reason)) return true;
        FailFormalRun(reason); return false;
    }
    private static bool LoadStage(SceneTransitionCoordinator.TransitionKind kind, out string reason)
    {
        RunMap.RunMapBootstrap.RequestForScene(GameplayScene, seed, stage);
        UIManager.Instance.HideAllPanelsForGameplay();
        bool started = SceneTransitionCoordinator.TryBegin(kind, GameplayScene, true, AdoptStage, FailFormalRun, out reason);
        if (!started) UIManager.Instance.RestoreAfterGameplay();
        return started;
    }
    private static void AdoptStage()
    {
        if (!RunEncounter.EnterPreparedScene(stage, team, seed)) FailFormalRun("第 " + stage + " 阶段无法接管已准备地图");
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
            VillageSceneFlow.ReportEntryFailure, out reason)) throw new System.InvalidOperationException(reason);
    }
    private static void FailFormalRun(string reason)
    {
        Debug.LogError("[RunFlow] " + reason);
        if (!DispatchService.Instance.ReturnToMeta(out string failure))
        {
            UIManager.Instance.RestoreAfterGameplay();
            Debug.LogError("[RunFlow] 返回未完成，可重试：" + failure);
        }
    }
}
