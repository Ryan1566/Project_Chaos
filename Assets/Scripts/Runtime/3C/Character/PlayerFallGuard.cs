using ChaosDebug;
using UnityEngine;

/// <summary>
/// 掉落重生兜底（W3.0b）：每 0.2s 检查 y，掉到阈值以下就放回出生点。
/// 挂在玩家身上，随玩家一起销毁。
/// </summary>
public class PlayerFallGuard : MonoBehaviour
{
    /// <summary>掉落判定阈值（世界 y）。</summary>
    public float fallThresholdY = -20f;
    /// <summary>检查间隔（秒）。</summary>
    public float checkInterval = 0.2f;
    /// <summary>重生目标（出生点）。</summary>
    public Transform respawnPoint;

    private float _nextCheck;
    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + Mathf.Max(0.05f, checkInterval);

        if (transform.position.y >= fallThresholdY) return;

        Vector3 target = respawnPoint == null ? transform.position : respawnPoint.position + new Vector3(0f, 1f, 0f);
        Vector3 delta = target - transform.position;
        transform.position = target;
        GameplayCameraRig.NotifyTargetWarped(transform, delta);
        if (_body != null) _body.velocity = Vector2.zero;

        ChaosLog.Warn(LogChannel.UI,
            "[局内] 玩家掉落，已重生于出生点：触发 y=" + fallThresholdY + " → 回到 " + target);
    }
}
