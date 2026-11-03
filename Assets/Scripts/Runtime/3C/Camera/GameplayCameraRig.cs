using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

/// <summary>One output owner, reusable Cinemachine rig, and target-scoped leases across village/run transitions.</summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Camera))]
public sealed class GameplayCameraRig : MonoBehaviour
{
    public enum CameraMode { None, Village, Run }
    static readonly List<GameplayCameraRig> Rigs = new List<GameplayCameraRig>();
    public Camera OutputCamera { get; private set; }
    public Transform Target { get; private set; }
    public CinemachineVirtualCamera VirtualCamera { get; private set; }
    public Rect Bounds { get; private set; }
    public CameraMode Mode { get; private set; }
    public int BoundaryRevision { get; private set; }
    CinemachineBrain _brain;
    CinemachineFramingTransposer _framing;
    CinemachineConfiner2D _confiner;
    CameraViewportBounds _finalBounds;
    Transform _anchor;
    PolygonCollider2D _shape;
    GameObject _root;
    GameplayCameraSettings _settings;
    bool _ownsBrain, _previousBrainEnabled, _ownsSettings;
    float _look, _lookVelocity, _villageY, _aspect = -1, _lens = -1;
    CombatCameraShake _shake;
    DayNightSkyRenderer _sky;

    public static GameplayCameraRig For(Camera output)
    {
        if (output == null) return null;
        var rig = output.GetComponent<GameplayCameraRig>();
        return rig != null ? rig : output.gameObject.AddComponent<GameplayCameraRig>();
    }
    public static bool IsFollowing(Camera output)
    {
        var rig = output == null ? null : output.GetComponent<GameplayCameraRig>();
        return rig != null && rig.enabled && rig.Target != null && rig.Mode != CameraMode.None;
    }
    void Awake()
    {
        OutputCamera = GetComponent<Camera>();
        _brain = GetComponent<CinemachineBrain>(); _ownsBrain = _brain == null;
        if (_ownsBrain) _brain = gameObject.AddComponent<CinemachineBrain>();
        _previousBrainEnabled = !_ownsBrain && _brain.enabled;
        if (_ownsBrain)
        {
            _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            _brain.m_IgnoreTimeScale = false;
            _brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
            _brain.enabled = false;
        }
        _settings = Resources.Load<GameplayCameraSettings>(GameplayCameraSettings.ResourcesPath);
        if (_settings == null)
        {
            _settings = ScriptableObject.CreateInstance<GameplayCameraSettings>();
            _settings.hideFlags = HideFlags.HideAndDontSave; _ownsSettings = true;
        }
    }
    void OnEnable() { if (!Rigs.Contains(this)) Rigs.Add(this); }
    void EnsureRig()
    {
        if (_root != null) return;
        _root = new GameObject("GameplayCinemachineRig");
        if (Application.isPlaying) DontDestroyOnLoad(_root);
        _anchor = new GameObject("FollowAnchor").transform; _anchor.SetParent(_root.transform, false);
        var vgo = new GameObject("GameplayVirtualCamera"); vgo.transform.SetParent(_root.transform, false);
        VirtualCamera = vgo.AddComponent<CinemachineVirtualCamera>();
        VirtualCamera.Follow = _anchor; VirtualCamera.LookAt = null;
        _framing = VirtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
        _framing.m_CameraDistance = 10; _framing.m_ScreenX = _framing.m_ScreenY = .5f;
        _framing.m_LookaheadTime = 0; _framing.m_UnlimitedSoftZone = true;
        var border = new GameObject("CameraBounds"); border.layer = 2; border.transform.SetParent(_root.transform, false);
        _shape = border.AddComponent<PolygonCollider2D>(); _shape.isTrigger = true;
        _confiner = vgo.AddComponent<CinemachineConfiner2D>(); _confiner.m_BoundingShape2D = _shape; _confiner.m_Damping = 0;
        _finalBounds = vgo.AddComponent<CameraViewportBounds>(); _finalBounds.Output = OutputCamera;
        vgo.SetActive(false);
    }
    public void BindRun(Transform target, Rect bounds)
    { Bind(target, bounds, CameraMode.Run, 0); }
    public void BindVillage(Transform target, float width, float fixedY)
    {
        float size = _settings.Size(true);
        Bind(target, new Rect(-width * .5f, fixedY - size, width, size * 2), CameraMode.Village, fixedY);
    }
    void Bind(Transform target, Rect bounds, CameraMode mode, float fixedY)
    {
        if (target == null) throw new System.ArgumentNullException(nameof(target));
        if (!Valid(bounds)) throw new System.ArgumentException("Invalid camera bounds");
        enabled = true; EnsureRig();
        if (_shake != null && Target != null) _shake.Detach(Target);
        _shake = null;
        Target = target; Bounds = bounds; Mode = mode; _villageY = fixedY;
        _sky = DayNightSkyRenderer.Attach(OutputCamera, mode);
        VirtualCamera.Priority = _settings.priority;
        _framing.m_XDamping = GameplayCameraSettings.Finite(_settings.horizontalDamping, .3f, 0, 5);
        _framing.m_YDamping = mode == CameraMode.Village ? 0 : GameplayCameraSettings.Finite(_settings.verticalDamping, .65f, 0, 5);
        _framing.m_ZDamping = 0;
        _framing.m_DeadZoneWidth = GameplayCameraSettings.Finite(_settings.horizontalDeadZone, .08f, 0, .8f);
        _framing.m_DeadZoneHeight = mode == CameraMode.Village ? 0 : GameplayCameraSettings.Finite(_settings.verticalDeadZone, .22f, 0, .8f);
        _framing.m_SoftZoneWidth = _framing.m_SoftZoneHeight = 1;
        RefreshLensAndBoundary(true);
        _shape.enabled = true; VirtualCamera.gameObject.SetActive(true); _brain.enabled = true;
        SnapToTarget();
        if (mode == CameraMode.Run) _shake = CombatCameraShake.Attach(OutputCamera, VirtualCamera, target);
    }
    void RefreshLensAndBoundary(bool force)
    {
        float lens = _settings.Size(Mode == CameraMode.Village);
        float aspect = GameplayCameraSettings.Finite(OutputCamera.aspect, 16f / 9, .1f, 10);
        if (!force && Mathf.Approximately(lens, _lens) && Mathf.Approximately(aspect, _aspect)) return;
        _lens = lens; _aspect = aspect;
        var settings = VirtualCamera.m_Lens; settings.ModeOverride = LensSettings.OverrideModes.Orthographic;
        settings.OrthographicSize = lens; VirtualCamera.m_Lens = settings;
        if (Mode == CameraMode.Village) Bounds = new Rect(Bounds.xMin, _villageY - lens, Bounds.width, lens * 2);
        _shape.SetPath(0, new[] { Bounds.min, new Vector2(Bounds.xMin, Bounds.yMax), Bounds.max, new Vector2(Bounds.xMax, Bounds.yMin) });
        _confiner.m_MaxWindowSize = lens; _confiner.InvalidateCache(); _finalBounds.Bounds = Bounds; BoundaryRevision++;
    }
    Vector3 AnchorPosition()
    {
        Vector3 point = Target.position;
        point.x += _look;
        point.y = Mode == CameraMode.Village ? _villageY : point.y + GameplayCameraSettings.Finite(_settings.runHeightOffset, 1.25f, -20, 20);
        point.z = 0; return point;
    }
    float DesiredLook()
    {
        var movement = Target.GetComponent<PlayerLocomotion>();
        return (movement == null ? 0 : movement.Facing) * GameplayCameraSettings.Finite(_settings.forwardDistance, 1.25f, 0, 10)
            * (Mode == CameraMode.Village ? .25f : 1);
    }
    void Update()
    {
        if (Mode == CameraMode.None) return;
        if (Target == null) { Release(Target); return; }
        RefreshLensAndBoundary(false);
        if (Time.timeScale <= 0 || InputManager.Instance.GameplayInputBlocked) return;
        _look = Mathf.SmoothDamp(_look, DesiredLook(), ref _lookVelocity,
            GameplayCameraSettings.Finite(_settings.turnSmoothing, .35f, .01f, 5), Mathf.Infinity, Time.deltaTime);
        _anchor.position = AnchorPosition();
    }
    public void SnapToTarget()
    {
        if (Target == null || VirtualCamera == null) return;
        RefreshLensAndBoundary(false);
        _look = DesiredLook(); _lookVelocity = 0; _anchor.position = AnchorPosition();
        Vector3 point = _anchor.position; point.z = -10;
        float hy = _lens, hx = hy * _aspect;
        point.x = Bounds.width <= hx * 2 ? Bounds.center.x : Mathf.Clamp(point.x, Bounds.xMin + hx, Bounds.xMax - hx);
        point.y = Bounds.height <= hy * 2 ? Bounds.center.y : Mathf.Clamp(point.y, Bounds.yMin + hy, Bounds.yMax - hy);
        VirtualCamera.ForceCameraPosition(point, Quaternion.identity);
        // ForceCameraPosition retains the previous pipeline corrections. A negative
        // delta explicitly resets them and bypasses Cinemachine's same-frame cache.
        VirtualCamera.PreviousStateIsValid = false;
        VirtualCamera.UpdateCameraState(Vector3.up, -1);
        _brain.ManualUpdate();
    }
    public static void NotifyTargetWarped(Transform target, Vector3 delta)
    {
        if (target == null) return;
        foreach (var rig in Rigs)
            if (rig != null && rig.VirtualCamera != null && ReferenceEquals(rig.Target, target))
            {
                rig.VirtualCamera.OnTargetObjectWarped(rig._anchor, delta);
                rig.SnapToTarget();
            }
    }
    public bool Release(Transform expectedTarget)
    {
        if (Mode == CameraMode.None || !ReferenceEquals(Target, expectedTarget)) return false;
        if (_shake != null) _shake.Detach(expectedTarget); _shake = null;
        Target = null; Mode = CameraMode.None;
        if (_sky != null) _sky.enabled = false;
        if (VirtualCamera != null) VirtualCamera.gameObject.SetActive(false);
        if (_shape != null) _shape.enabled = false;
        Bounds = default; if (_finalBounds != null) _finalBounds.Bounds = default;
        if (_brain != null) _brain.enabled = _previousBrainEnabled;
        return true;
    }
    void OnDisable() { Release(Target); Rigs.Remove(this); }
    void OnDestroy()
    {
        Release(Target); Rigs.Remove(this);
        DestroyOwned(_root); if (_ownsSettings) DestroyOwned(_settings); if (_ownsBrain) DestroyOwned(_brain);
    }
    static bool Valid(Rect b) => !float.IsNaN(b.xMin) && !float.IsInfinity(b.xMin) && !float.IsNaN(b.yMin)
        && !float.IsInfinity(b.yMin) && !float.IsNaN(b.width) && !float.IsInfinity(b.width)
        && !float.IsNaN(b.height) && !float.IsInfinity(b.height) && b.width > 0 && b.height > 0;
    static void DestroyOwned(Object value) { if (value != null) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); } }
}
