using System;
using UnityEngine;

/// <summary>Runs the existing whole-second meta timers in a real save session.</summary>
public sealed class MetaRuntimeDriver : MonoBehaviour
{
    public static event Action Changed;
    private SavePayload _payload;
    private float _elapsed, _saveElapsed;
    private string _savedSnapshot;
    private int _contextId;
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
            _payload = current;
            _contextId = GameSession.Instance.ContextId;
            _elapsed = _saveElapsed = 0;
            _savedSnapshot = JsonUtility.ToJson(current);
        }
        _elapsed += Time.deltaTime;
        _saveElapsed += Time.deltaTime;
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
        if (snapshot == _savedSnapshot) return;
        if (MetaProgressService.Instance.SaveProgress(out string reason)) _savedSnapshot = snapshot;
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveIfChanged(); }
    private void OnApplicationQuit() { SaveIfChanged(); }
}
