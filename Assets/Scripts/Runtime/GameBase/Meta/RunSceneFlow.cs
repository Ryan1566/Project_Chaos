using System.Collections;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>正式入口的场景衔接；地图仍由既有 LoadingController 在读条期间准备。</summary>
public static class RunSceneFlow
{
    private enum Phase { Idle, LoadingRun, Active, Returning }
    private static Phase _phase;
    private static string _returnScene;
    private static List<uint> _team;
    private static uint _seed;
    private static int _stageIndex;
    private static int _transitionVersion;

    public const string GameplayScene = "WorldScene";
    public const string LoadingScene = "LoadingScene";
    public static bool IsTransitioning { get { return _phase == Phase.LoadingRun || _phase == Phase.Returning; } }
    public static bool IsFormalRun { get { return _phase == Phase.Active; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _phase = Phase.Idle;
        _team = null;
        _returnScene = null;
        _stageIndex = 0;
        _transitionVersion = 0;
    }

    public static bool Begin(List<uint> team, out string reason)
    {
        reason = null;
        if (_phase != Phase.Idle) { reason = "正在出征或返回，请稍候"; return false; }
        if (!VillageSceneFlow.IsVillageScene || VillageSceneFlow.IsLoading || !GameSession.Instance.HasSession)
        { reason = "请先读取存档进入村庄，再从地图选择队伍出征"; return false; }
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + GameplayScene + ".unity") < 0
            || SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + LoadingScene + ".unity") < 0
            || SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + VillageSceneFlow.SceneName + ".unity") < 0)
        { reason = "村庄、局内或读条场景未加入构建列表"; return false; }

        _returnScene = VillageSceneFlow.SceneName;
        _team = new List<uint>(team);
        _seed = RunMap.NaturalMapRuntime.PreviewSeed ?? RunMap.NaturalMapRuntime.CreateRunSeed();
        _stageIndex = 1;
        _transitionVersion++;
        _phase = Phase.LoadingRun;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RunMap.RunMapBootstrap.RequestForScene(GameplayScene, _seed, _stageIndex);
        GameSession.Instance.SetTargetScene(GameplayScene);
        try
        {
            if (UIManager.Instance != null) UIManager.Instance.HideAllPanelsForGameplay();
            ScenesLoadManager.Instance.LoadScene(LoadingScene, () => { });
            return true;
        }
        catch (System.Exception error)
        {
            _phase = Phase.Idle;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSession.Instance.SetTargetScene(null);
            if (UIManager.Instance != null) UIManager.Instance.RestoreAfterGameplay();
            reason = "无法进入读条场景：" + error.Message;
            return false;
        }
    }

    /// <summary>正式第 2/3 阶段重新走读条分帧构建，保留整局的返回场景与种子。</summary>
    public static bool BeginNextStage(int index, List<uint> team, uint seed)
    {
        if (_phase != Phase.Active) return false;
        if (index < 2 || index > RunEncounter.MaxSceneIndex || team == null || team.Count == 0)
        {
            FailFormalRun("下一阶段或队伍无效");
            return false;
        }
        _team = new List<uint>(team);
        _seed = seed;
        _stageIndex = index;
        _phase = Phase.LoadingRun;
        _transitionVersion++;
        try
        {
            // Clear the old map before registering/building its replacement. The loader
            // publishes the new CurrentPlan only after assembly has completed.
            RunEncounter.SuspendForSceneTransition();
            RunMap.RunMapBootstrap.RequestForScene(GameplayScene, _seed, _stageIndex);
            GameSession.Instance.SetTargetScene(GameplayScene);
            // Outside UI was hidden at departure; hiding it again would lose its restore list.
            ScenesLoadManager.Instance.LoadScene(LoadingScene, () => { });
            return true;
        }
        catch (System.Exception error)
        {
            FailFormalRun("无法加载下一阶段：" + error.Message);
            return false;
        }
    }

    public static void ReturnToMeta()
    {
        if (!CanReturnToMeta(out string reason)) throw new System.InvalidOperationException(reason);
        _returnScene = VillageSceneFlow.SceneName;
        _phase = Phase.Returning;
        _transitionVersion++;
        try { SceneManager.LoadScene(_returnScene); }
        catch
        {
            _phase = Phase.Active;
            throw;
        }
    }

    public static bool CanReturnToMeta(out string reason)
    {
        reason = null;
        if (_phase != Phase.Active) { reason = "当前不在可返回的正式出征中"; return false; }
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + VillageSceneFlow.SceneName + ".unity") < 0)
        { reason = "村庄场景未加入构建列表，可修复后重试返回"; return false; }
        return true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((_phase == Phase.LoadingRun && scene.name == GameplayScene)
            || (_phase == Phase.Returning && scene.name == _returnScene))
            MonoManager.Instance.StartCoroutine(CompleteTransition(scene.name, _transitionVersion));
    }

    private static IEnumerator CompleteTransition(string sceneName, int version)
    {
        // sceneLoaded 在 Start 之前发生；下一帧再接管，等加载器搬入地图、Entry 初始化完毕。
        yield return null;
        if (version != _transitionVersion) yield break;
        if (_phase == Phase.LoadingRun && sceneName == GameplayScene)
        {
            bool started = false;
            string error = null;
            try { started = RunEncounter.EnterPreparedScene(_stageIndex, _team, _seed); }
            catch (System.Exception exception) { error = exception.Message; }
            _phase = Phase.Active;
            if (!started)
            {
                FailFormalRun("第 " + _stageIndex + " 阶段初始化失败" + (error == null ? "" : "：" + error));
                yield break;
            }
            ChaosLog.Info(LogChannel.Scene, "[正式流程] WorldScene 已接入战斗：阶段=" + _stageIndex + " 队伍=" + _team.Count + " seed=" + _seed);
        }
        else if (_phase == Phase.Returning && sceneName == _returnScene)
        {
            _phase = Phase.Idle;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSession.Instance.SetTargetScene(null);
            if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady)
            {
                _team = null; _stageIndex = 0;
                if (VillageSceneFlow.IsVillageScene) VillageSceneFlow.ReportEntryFailure(VillageSceneBootstrap.LastError ?? "村庄未初始化");
                yield break;
            }
            VillageSceneFlow.OnReturnedToVillage();
            EventService.Instance.CheckAndShowOnReturnToMeta();
            _team = null;
            _stageIndex = 0;
            ChaosLog.Info(LogChannel.Scene, "[正式流程] 已返回 " + sceneName + " 村庄；地图保持关闭");
        }
    }

    private static void FailFormalRun(string failure)
    {
        ChaosLog.Error(LogChannel.Scene, "[正式流程] " + failure + "，返回局外");
        // Dispatch saves and cleans the run before returning. It must see Active rather
        // than LoadingRun, otherwise its transition guard would reject this recovery.
        _phase = Phase.Active;
        string reason;
        if (!DispatchService.Instance.ReturnToMeta(out reason))
        {
            if (UIManager.Instance != null) UIManager.Instance.RestoreAfterGameplay();
            ChaosLog.Error(LogChannel.Scene, "[正式流程] 返回局外未完成，可重试：" + reason);
        }
    }
}
