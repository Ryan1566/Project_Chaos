using System;
using UnityEngine;

/// <summary>Runs the existing whole-second meta timers in a real save session.</summary>
public sealed class MetaRuntimeDriver : MonoBehaviour
{
    public static event Action Changed;
    private SavePayload _payload;
    private float _elapsed, _saveElapsed;
    private string _savedSnapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListeners() { Changed = null; }

    private void Update()
    {
        if (!GameSession.Instance.HasSession) { _payload = null; return; }
        var current = GameSession.Instance.LoadedPayload;
        if (!ReferenceEquals(_payload, current))
        {
            _payload = current;
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
            if (before != JsonUtility.ToJson(current)) Changed?.Invoke();
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
