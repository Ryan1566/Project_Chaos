using UnityEngine;

[CreateAssetMenu(menuName = "Project Chaos/Gameplay Camera Settings", fileName = "GameplayCameraSettings")]
public sealed class GameplayCameraSettings : ScriptableObject
{
    public const string ResourcesPath = "3C/GameplayCameraSettings";
    [Header("取景")]
    [Min(.1f)] public float runSize = 6;
    [Min(.1f)] public float villageSize = 8;
    public float runHeightOffset = 1.25f;
    [Header("跟随缓冲（秒 / 视口比例）")]
    [Min(0)] public float horizontalDamping = .3f;
    [Min(0)] public float verticalDamping = .65f;
    [Range(0, .8f)] public float horizontalDeadZone = .08f;
    [Range(0, .8f)] public float verticalDeadZone = .22f;
    [Header("朝向前视")]
    [Min(0)] public float forwardDistance = 1.25f;
    [Min(.01f)] public float turnSmoothing = .35f;
    public int priority = 100;
    public static float Finite(float value, float fallback, float minimum, float maximum)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
    public float Size(bool village) => village ? Finite(villageSize, 8, .1f, 100) : Finite(runSize, 6, .1f, 100);
}
