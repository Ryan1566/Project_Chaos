using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the single in-flight scene request, its session identity and explicit readiness acknowledgement.</summary>
public static class SceneTransitionCoordinator
{
    public enum GamePhase { Outside, Village, EnteringVillage, LoadingRun, Run, ReturningVillage, PrototypeRun, Cancelling }
    public enum TransitionKind { EnterVillage, EnterRun, NextRunStage, ReturnVillage }
    public sealed class Request
    {
        public int Id { get; internal set; }
        public int ContextId { get; internal set; }
        public string Target { get; internal set; }
        public TransitionKind Kind { get; internal set; }
        public int SceneHandle { get; internal set; } = -1;
        internal AsyncOperation Operation;
        internal GameObject PendingRoot;
        internal string Error;
        internal Action Ready;
        internal Action<string> Failed;
        public bool Cancelled => Error != null;
    }
    public static Request Active { get; private set; }
    public static GamePhase Phase { get; private set; }
    public static bool IsBusy => Active != null;
    public static bool HasRun => Phase == GamePhase.LoadingRun || Phase == GamePhase.Run || Phase == GamePhase.ReturningVillage || Phase == GamePhase.PrototypeRun;
    public static bool IsFormalRun => Phase == GamePhase.Run;
    public static int ActiveId => Active == null ? 0 : Active.Id;
    private static int sequence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= Loaded;
        Active = null; Phase = GamePhase.Outside; sequence++;
    }

    public static bool TryBegin(TransitionKind kind, string target, bool loadingScreen, Action ready, Action<string> failed, out string reason)
    {
        reason = null;
        if (IsBusy) { reason = "已有场景切换正在进行，请稍候"; return false; }
        if (!GameSession.Instance.IsPersistent) { reason = "正式切图需要已选择的存档会话"; return false; }
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + target + ".unity") < 0
            || (loadingScreen && SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/LoadingScene.unity") < 0))
        { reason = "目标或读条场景未加入构建列表"; return false; }
        Active = new Request { Id = ++sequence, ContextId = GameSession.Instance.ContextId, Target = target, Kind = kind, Ready = ready, Failed = failed };
        Phase = kind == TransitionKind.EnterVillage ? GamePhase.EnteringVillage : kind == TransitionKind.ReturnVillage ? GamePhase.ReturningVillage : GamePhase.LoadingRun;
        GameSession.Instance.ContextChanged -= ContextChanged;
        GameSession.Instance.ContextChanged += ContextChanged;
        GameSession.Instance.SetTargetScene(target);
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
        try { SceneManager.LoadScene(loadingScreen ? "LoadingScene" : target); return true; }
        catch (Exception error)
        {
            var request = Active; Active = null; Detach();
            Phase = kind == TransitionKind.EnterVillage ? GamePhase.Outside : kind == TransitionKind.ReturnVillage ? GamePhase.Run : GamePhase.Village;
            GameSession.Instance.SetTargetScene(null); Cleanup(request);
            reason = "无法开始场景切换：" + error.Message; return false;
        }
    }

    public static bool Owns(int id) => Active != null && Active.Id == id;
    public static bool IsCurrent(int id) => Owns(id) && !Active.Cancelled && Active.ContextId == GameSession.Instance.ContextId;
    public static bool IsCancelled(int id) => Owns(id) && Active.Cancelled;
    public static void RegisterOperation(int id, AsyncOperation operation) { if (Owns(id)) Active.Operation = operation; }
    public static void RegisterPendingRoot(int id, GameObject root) { if (IsCurrent(id)) Active.PendingRoot = root; }

    private static void ContextChanged()
    {
        if (Active != null && Active.ContextId != GameSession.Instance.ContextId) Fail(Active.Id, "存档会话已切换，旧加载已取消");
        else if (Active == null) Phase = GamePhase.Outside;
    }

    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        var request = Active;
        if (request == null || scene.name != request.Target) return;
        request.SceneHandle = scene.handle;
        if (request.Cancelled || request.ContextId != GameSession.Instance.ContextId) { FinishFailure(request); return; }
        if (scene.name == RunSceneFlow.GameplayScene)
        {
            var probe = new GameObject("RunSceneReadiness").AddComponent<RunSceneReadiness>();
            probe.Initialize(request.Id);
        }
        // MainScene's bootstrap reports its own validated readiness, not an assumed one-frame delay.
    }

    public static bool ReportReady(int id, Scene scene)
    {
        if (!IsCurrent(id) || scene.handle != Active.SceneHandle || scene.name != Active.Target) return false;
        var request = Active;
        Active = null; Detach(); GameSession.Instance.SetTargetScene(null);
        Phase = request.Kind == TransitionKind.EnterRun || request.Kind == TransitionKind.NextRunStage ? GamePhase.Run : GamePhase.Village;
        // The receiver now adopts the prepared root into its scene/run scope.
        try { request.Ready?.Invoke(); }
        catch (Exception error)
        {
            Cleanup(request);
            request.Failed?.Invoke("目标场景接管失败：" + error.Message);
        }
        return true;
    }

    public static void CancelActive(string reason = "场景切换已取消") { if (Active != null) Fail(Active.Id, reason); }
    public static void Fail(int id, string reason)
    {
        if (!Owns(id) || Active.Cancelled) return;
        var request = Active; request.Error = string.IsNullOrEmpty(reason) ? "场景切换失败" : reason;
        // Unity cannot cancel a queued LoadSceneAsync. Drain it before recovery/new requests.
        if (request.Operation != null && !request.Operation.isDone && request.SceneHandle < 0)
        { Phase = GamePhase.Cancelling; request.Operation.allowSceneActivation = true; return; }
        FinishFailure(request);
    }

    private static void FinishFailure(Request request)
    {
        if (!Owns(request.Id)) return;
        Active = null; Detach(); GameSession.Instance.SetTargetScene(null); Cleanup(request);
        bool sameSession = request.ContextId == GameSession.Instance.ContextId && GameSession.Instance.IsPersistent;
        Phase = sameSession && (request.Kind == TransitionKind.EnterRun || request.Kind == TransitionKind.NextRunStage) ? GamePhase.Run : GamePhase.Outside;
        if (sameSession) request.Failed?.Invoke(request.Error ?? "场景切换未完成");
        else SceneManager.LoadScene("EnterScene");
    }
    private static void Cleanup(Request request)
    {
        if (request == null || request.PendingRoot == null) return;
        var current = RunMap.RunMapBootstrap.CurrentMapRoot;
        if (current != null && (current == request.PendingRoot || current.transform.IsChildOf(request.PendingRoot.transform))) RunMap.RunMapBootstrap.ClearCurrent();
        UnityEngine.Object.Destroy(request.PendingRoot);
    }
    private static void Detach() { SceneManager.sceneLoaded -= Loaded; }
    public static void SetPrototypeRun(bool active)
    {
        if (IsBusy || !GameSession.Instance.IsSandbox) throw new InvalidOperationException("测试出征需要空闲的测试会话");
        Phase = active ? GamePhase.PrototypeRun : GamePhase.Outside;
    }
}

/// <summary>LateUpdate occurs after scene Start callbacks. The prepared map is checked before combat receives control.</summary>
public sealed class RunSceneReadiness : MonoBehaviour
{
    private int requestId;
    public void Initialize(int id) { requestId = id; }
    private void LateUpdate()
    {
        if (!SceneTransitionCoordinator.IsCurrent(requestId)) { Destroy(gameObject); return; }
        var root = RunMap.RunMapBootstrap.CurrentMapRoot;
        if (root == null || root.scene != gameObject.scene || !RunMap.NaturalMapRuntime.IsReady)
            SceneTransitionCoordinator.Fail(requestId, "目标场景未收到有效的已生成地图");
        else SceneTransitionCoordinator.ReportReady(requestId, gameObject.scene);
        Destroy(gameObject);
    }
}
