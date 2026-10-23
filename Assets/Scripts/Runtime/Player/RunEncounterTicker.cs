using UnityEngine;
using ChaosDebug;

/// <summary>清房重算的驱动（0.3s 一次）。</summary>
public class RunEncounterTicker : MonoBehaviour
{
    private float _next;

    private void Awake()
    {
        if (GetComponent<GameplayShortcutDriver>() == null) gameObject.AddComponent<GameplayShortcutDriver>();
        ChaosLog.Warn(LogChannel.UI, "[诊断] RunEncounterTicker 创建 id=" + GetInstanceID() + " frameCount=" + Time.frameCount);
    }

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + RunEncounter.ScanInterval;
        RunEncounter.ProcessPendingScene();
        int before = RunEncounter.CurrentSceneIndex;
        RunEncounter.RefreshNow();
        if (RunEncounter.CurrentSceneIndex != before)
            ChaosLog.Warn(LogChannel.UI, "[诊断] Ticker(id=" + GetInstanceID() + ") 调用清房导致场景切换 " + before + " → " + RunEncounter.CurrentSceneIndex + " frameCount=" + Time.frameCount);
    }
}

