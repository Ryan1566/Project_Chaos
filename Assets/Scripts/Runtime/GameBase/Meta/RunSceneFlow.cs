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
    }

    public static bool Begin(List<uint> team, out string reason)
    {
        reason = null;
        if (_phase != Phase.Idle) { reason = "正在出征或返回，请稍候"; return false; }
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + GameplayScene + ".unity") < 0
            || SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + LoadingScene + ".unity") < 0)
        { reason = "局内或读条场景未加入构建列表"; return false; }

        _returnScene = SceneManager.GetActiveScene().name;
        _team = new List<uint>(team);
        _seed = (uint)System.DateTime.UtcNow.Ticks;
        _phase = Phase.LoadingRun;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RunMap.RunMapBootstrap.RequestForScene(GameplayScene, _seed, 1);
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
            if (UIManager.Instance != null) UIManager.Instance.RestoreAfterGameplay();
            reason = "无法进入读条场景：" + error.Message;
            return false;
        }
    }

    public static void ReturnToMeta()
    {
        _phase = Phase.Returning;
        SceneManager.LoadScene(_returnScene);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((_phase == Phase.LoadingRun && scene.name == GameplayScene)
            || (_phase == Phase.Returning && scene.name == _returnScene))
            MonoManager.Instance.StartCoroutine(CompleteTransition(scene.name));
    }

    private static IEnumerator CompleteTransition(string sceneName)
    {
        // sceneLoaded 在 Start 之前发生；下一帧再接管，等加载器搬入地图、Entry 初始化完毕。
        yield return null;
        if (_phase == Phase.LoadingRun && sceneName == GameplayScene)
        {
            bool started = RunEncounter.EnterPreparedScene(1, _team, _seed);
            _phase = Phase.Active;
            if (!started)
            {
                ChaosLog.Error(LogChannel.Scene, "[正式流程] 局内初始化失败，返回局外");
                string reason;
                DispatchService.Instance.ReturnToMeta(out reason);
                yield break;
            }
            ChaosLog.Info(LogChannel.Scene, "[正式流程] WorldScene 已接入战斗：队伍=" + _team.Count + " seed=" + _seed);
        }
        else if (_phase == Phase.Returning && sceneName == _returnScene)
        {
            _phase = Phase.Idle;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSession.Instance.SetTargetScene(null);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RestoreAfterGameplay();
                UIManager.Instance.SetRootPanel(PanelType.WorldMapPanel);
            }
            EventService.Instance.CheckAndShowOnReturnToMeta();
            ChaosLog.Info(LogChannel.Scene, "[正式流程] 已返回 " + sceneName + " 的局外地图");
        }
    }
}
