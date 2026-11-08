using System;
using System.Linq;
using UnityEngine;

/// <summary>Runs the existing whole-second meta timers in a real save session.</summary>
public sealed class MetaRuntimeDriver : MonoBehaviour
{
    public static event Action Changed;
    private SavePayload _payload;
    private float _elapsed, _saveElapsed;
    private string _savedSnapshot;
    private int _contextId;
    private double _officeElapsed;
    private bool _officeReady;
    private static bool _publishing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListeners() { Changed = null; _publishing = false; }
    public static void NotifyChanged() { if (!MetaCommandRunner.IsExecuting) PublishChanged(); }
    internal static void PublishChanged()
    {
        if (_publishing) return;
        _publishing = true;
        try
        {
            var handlers = Changed;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception error) { Debug.LogError("[MetaRuntime] 状态监听失败：" + error); }
            }
        }
        finally { _publishing = false; }
    }

    private void Update()
    {
        if (!GameSession.Instance.HasSession) { _payload = null; return; }
        var current = GameSession.Instance.LoadedPayload;
        if (!ReferenceEquals(_payload, current) || _contextId != GameSession.Instance.ContextId)
        {
            bool sessionReplaced = _payload != null;
            _payload = current;
            _contextId = GameSession.Instance.ContextId;
            _elapsed = _saveElapsed = 0;
            _officeElapsed=0;_officeReady=false;
            _savedSnapshot = JsonUtility.ToJson(current);
            // 家族地点扩展许可只在当前会话有效，换档/重开时清空；首次进入保留已授予状态。
            if (sessionReplaced) WorldLocationService.Instance.Reload();
        }
        _elapsed += Time.deltaTime;
        _saveElapsed += Time.unscaledDeltaTime;
        // 普通面板不冻结任务时限；暂停菜单（含其上层设置页）及读条阶段不推进。
        bool taskWorldReady=!SceneTransitionCoordinator.IsBusy&&(VillageSceneBootstrap.IsReady||RunEncounter.IsSceneReady);
        var ui=UIManager.Instance;
        bool menuPaused=ui.HasPanelInStack(PanelType.PausePanel);
        if (!SceneTransitionCoordinator.IsBusy && !menuPaused)
        {
            if (VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene && !SceneTransitionCoordinator.HasRun)
                DayNightClock.TickVillage(current, Time.deltaTime);
            else if (RunEncounter.IsSceneReady && !RunEncounter.RunCleared && !RunEncounter.Defeated
                && !Chaos.MVP.Model.Gameplay.RunControlModel.IsWorldPaused && !ui.HasPanelInStack(PanelType.RunCampPanel))
                DayNightClock.TickRun(Time.deltaTime);
        }
        if(taskWorldReady&&!menuPaused)
        {
            if(!_officeReady){OfficeTaskService.Instance.RefreshTriggers();_officeReady=true;}
            _officeElapsed+=Time.unscaledDeltaTime;
            if(_officeElapsed>=1){double seconds=Math.Floor(_officeElapsed);_officeElapsed-=seconds;OfficeTaskService.Instance.Tick(seconds);WorldResourceService.Instance.Tick(seconds);}
        }
        if (_elapsed >= 1)
        {
            // Existing timers store uint seconds: fractional frame deltas must be accumulated first.
            int seconds = Mathf.FloorToInt(_elapsed);
            _elapsed -= seconds;
            var before = JsonUtility.ToJson(current);
            RetainerService.Instance.Tick(seconds);
            VillageService.Instance.Tick(seconds);
            if (before != JsonUtility.ToJson(current)) NotifyChanged();
        }
        if (_saveElapsed >= 30) { _saveElapsed = 0; SaveIfChanged(); }
    }

    private void SaveIfChanged()
    {
        if (_payload == null || !GameSession.Instance.HasSession
            || !ReferenceEquals(_payload, GameSession.Instance.LoadedPayload)) return;
        string snapshot = JsonUtility.ToJson(_payload);
        if (snapshot == _savedSnapshot && !RunCheckpoint.IsPlayable) return;
        if (MetaProgressService.Instance.SaveProgress(out string reason)) _savedSnapshot = JsonUtility.ToJson(_payload);
    }

    private void LateUpdate() => RunCheckpoint.FlushRequested();
    private void OnEnable() => Application.wantsToQuit += SaveBeforeQuit;
    private void OnDisable() => Application.wantsToQuit -= SaveBeforeQuit;
    private bool SaveBeforeQuit()
    {
        if (!RunCheckpoint.IsPlayable || RunCheckpoint.Save(out string reason)) return true;
        Debug.LogWarning("[续档] " + reason);
        var ui = UIManager.Instance;
        var panel = ui.panelStack.Count > 0 ? ui.panelStack.Peek() as PausePanel : null;
        if (panel == null) panel = ui.PushPanel(PanelType.PausePanel) as PausePanel;
        panel?.ShowSaveFailure(reason);
        return false;
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveIfChanged(); }
    private void OnApplicationQuit() { SaveIfChanged(); }
}
