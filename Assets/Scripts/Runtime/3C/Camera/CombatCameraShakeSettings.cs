using UnityEngine;

/// <summary>
/// 战斗相机抖动的集中配置。资源约定加载路径：Resources/3C/CombatCameraShakeSettings。
/// 所有幅度都是 Cinemachine Impulse 的位置偏移（世界单位），不是相机自身的 Transform 偏移。
/// </summary>
[CreateAssetMenu(
    menuName = "Project Chaos/Combat Camera Shake Settings",
    fileName = "CombatCameraShakeSettings")]
public sealed class CombatCameraShakeSettings : ScriptableObject
{
    public const string ResourcesPath = "3C/CombatCameraShakeSettings";

    [Header("普通命中")]
    [Tooltip("普通攻击命中时的峰值偏移（世界单位）。")]
    [SerializeField, Min(0f)] private float normalHitAmplitude = 0.05f;

    [Tooltip("普通命中抖动持续时间（秒）。")]
    [SerializeField, Min(0.01f)] private float normalHitDuration = 0.10f;

    [Header("受击")]
    [Tooltip("受控角色真实扣血时的峰值偏移（世界单位）。")]
    [SerializeField, Min(0f)] private float hurtAmplitude = 0.12f;

    [Tooltip("受击抖动持续时间（秒）。")]
    [SerializeField, Min(0.01f)] private float hurtDuration = 0.14f;

    [Header("重击（预留）")]
    [Tooltip("重击命中时的峰值偏移（世界单位）。没有重击玩法时可保持配置但不调用。")]
    [SerializeField, Min(0f)] private float heavyHitAmplitude = 0.20f;

    [Tooltip("重击抖动持续时间（秒）。")]
    [SerializeField, Min(0.01f)] private float heavyHitDuration = 0.18f;

    [Header("保护")]
    [Tooltip("同时存在（尚未结束）的自身冲量峰值总和上限。达到上限后不再叠加。")]
    [SerializeField, Min(0f)] private float maxAmplitude = 0.28f;

    [Tooltip("两次新增抖动之间的最小间隔（秒），多目标命中仍只调用一次。")]
    [SerializeField, Min(0f)] private float minTriggerInterval = 0.06f;

    [Tooltip("总强度倍率。0 表示完全关闭抖动；负值按 0 处理。")]
    [SerializeField, Min(0f)] private float intensityScale = 1f;

    public float NormalHitAmplitude { get { return SanitizeAmplitude(normalHitAmplitude, 0.05f); } }
    public float NormalHitDuration { get { return SanitizeDuration(normalHitDuration, 0.10f); } }
    public float HurtAmplitude { get { return SanitizeAmplitude(hurtAmplitude, 0.12f); } }
    public float HurtDuration { get { return SanitizeDuration(hurtDuration, 0.14f); } }
    public float HeavyHitAmplitude { get { return SanitizeAmplitude(heavyHitAmplitude, 0.20f); } }
    public float HeavyHitDuration { get { return SanitizeDuration(heavyHitDuration, 0.18f); } }
    public float MaxAmplitude { get { return SanitizeAmplitude(maxAmplitude, 0.28f); } }
    public float MinTriggerInterval { get { return SanitizeInterval(minTriggerInterval, 0.06f); } }
    public float IntensityScale { get { return SanitizeScale(intensityScale, 1f); } }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float SanitizeAmplitude(float value, float fallback)
    {
        if (!IsFinite(value)) return fallback;
        return Mathf.Clamp(value, 0f, 2f);
    }

    private static float SanitizeDuration(float value, float fallback)
    {
        if (!IsFinite(value)) return fallback;
        return Mathf.Clamp(value, 0.01f, 2f);
    }

    private static float SanitizeInterval(float value, float fallback)
    {
        if (!IsFinite(value)) return fallback;
        return Mathf.Clamp(value, 0f, 2f);
    }

    private static float SanitizeScale(float value, float fallback)
    {
        if (!IsFinite(value)) return fallback;
        return Mathf.Clamp(value, 0f, 10f);
    }

    private void OnValidate()
    {
        // Inspector 中直接挡住明显的坏值；运行时 getter 仍会再做一次防御，
        // 避免运行时加载到被脚本或旧资源写坏的数值。
        if (!IsFinite(normalHitAmplitude)) normalHitAmplitude = 0.05f;
        if (!IsFinite(normalHitDuration)) normalHitDuration = 0.10f;
        if (!IsFinite(hurtAmplitude)) hurtAmplitude = 0.12f;
        if (!IsFinite(hurtDuration)) hurtDuration = 0.14f;
        if (!IsFinite(heavyHitAmplitude)) heavyHitAmplitude = 0.20f;
        if (!IsFinite(heavyHitDuration)) heavyHitDuration = 0.18f;
        if (!IsFinite(maxAmplitude)) maxAmplitude = 0.28f;
        if (!IsFinite(minTriggerInterval)) minTriggerInterval = 0.06f;
        if (!IsFinite(intensityScale)) intensityScale = 1f;

        normalHitAmplitude = Mathf.Max(0f, normalHitAmplitude);
        normalHitDuration = Mathf.Max(0.01f, normalHitDuration);
        hurtAmplitude = Mathf.Max(0f, hurtAmplitude);
        hurtDuration = Mathf.Max(0.01f, hurtDuration);
        heavyHitAmplitude = Mathf.Max(0f, heavyHitAmplitude);
        heavyHitDuration = Mathf.Max(0.01f, heavyHitDuration);
        maxAmplitude = Mathf.Max(0f, maxAmplitude);
        minTriggerInterval = Mathf.Max(0f, minTriggerInterval);
        intensityScale = Mathf.Max(0f, intensityScale);
    }
}
