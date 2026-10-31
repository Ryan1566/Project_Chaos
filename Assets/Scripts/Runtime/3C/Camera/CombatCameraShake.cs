using System.Collections.Generic;
using ChaosDebug;
using Cinemachine;
using UnityEngine;

/// <summary>
/// 战斗相机抖动：只通过 Cinemachine Impulse 让绑定的虚拟相机产生 XY 位移。
///
/// 所有权约定：
/// - 本组件的 <see cref="_listener"/> 和 <see cref="_source"/> 都是运行时由本组件创建的；
///   本组件只清理自己创建的组件，绝不调用 <see cref="CinemachineImpulseManager.Clear"/>。
/// - 每次事件都记录返回的 ImpulseEvent，取消时先核对 signal/channel，避免误伤已被管理器回收复用的其他事件。
/// - 绑定只有一个当前目标；Detach 必须传入当初的 expectedTarget，旧目标的延迟调用不会解绑新目标。
/// </summary>
public sealed class CombatCameraShake : MonoBehaviour
{
    private static readonly List<CombatCameraShake> Bindings = new List<CombatCameraShake>();
    private static CombatCameraShakeSettings s_fallbackSettings;
    private static AnimationCurve s_shakeCurve;

    private sealed class ActiveImpulse
    {
        public CinemachineImpulseManager.ImpulseEvent Event;
        public object Signal;
        public int Channel;
        public float EndTime;
        public float Amplitude;
    }

    private readonly List<ActiveImpulse> _active = new List<ActiveImpulse>();

    private Camera _output;
    private CinemachineVirtualCamera _virtualCamera;
    private Transform _controlledTarget;
    private CinemachineImpulseSource _source;
    private CinemachineImpulseListener _listener;
    private CombatCameraShakeSettings _settings;
    private CinemachineImpulseDefinition _normalDefinition;
    private CinemachineImpulseDefinition _hurtDefinition;
    private CinemachineImpulseDefinition _heavyDefinition;
    private int _channel;
    private bool _attached;
    private float _nextAllowedTime;

    private static AnimationCurve ShakeCurve
    {
        get
        {
            if (s_shakeCurve == null)
            {
                // 两端为 0，峰值 1；Hermite 切线与端点都取 0，不会过冲，也不会留下漂移。
                s_shakeCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.22f, 1f, 0f, 0f),
                    new Keyframe(1f, 0f, 0f, 0f));
            }
            return s_shakeCurve;
        }
    }

    /// <summary>
    /// 把抖动绑定到“输出相机 + 指定虚拟相机 + 指定受控角色”。
    /// 复用输出相机上已有的 CombatCameraShake 组件；失败返回 null，不改变原绑定。
    /// </summary>
    public static CombatCameraShake Attach(
        Camera output,
        CinemachineVirtualCamera virtualCamera,
        Transform controlledTarget)
    {
        if (output == null || virtualCamera == null || controlledTarget == null)
        {
            ChaosLog.Warn(LogChannel.Battle, "[相机抖动] Attach 参数无效：output/virtualCamera/controlledTarget 不能为空");
            return null;
        }

        CombatCameraShake shake = output.GetComponent<CombatCameraShake>();
        bool created = shake == null;
        if (created) shake = output.gameObject.AddComponent<CombatCameraShake>();

        if (!shake.Bind(output, virtualCamera, controlledTarget))
        {
            if (created) DestroyOwned(shake);
            return null;
        }
        return shake;
    }

    /// <summary>
    /// 解绑。只有 expectedTarget 仍是当前目标时才执行；旧目标的延迟 Detach 不会解绑新目标。
    /// 解绑会取消本组件仍存活的全部冲量，但不触碰全局其他事件。
    /// </summary>
    public void Detach(Transform expectedTarget)
    {
        if (!_attached) return;
        if (!ReferenceEquals(_controlledTarget, expectedTarget)) return;
        DetachInternal();
    }

    /// <summary>攻击实际命中后调用；同一次攻击的多目标命中只应调用一次。</summary>
    public static void NotifyHit(Transform attacker, Vector3 position, bool heavy = false)
    {
        if (attacker == null || !IsFinite(position)) return;
        CombatCameraShake shake = FindFor(attacker);
        if (shake != null) shake.EmitHit(position, heavy);
    }

    /// <summary>受控角色真实扣血后调用；治疗、无敌帧、非正伤害和 GM 倒地不调用。</summary>
    public static void NotifyHurt(Transform victim, Vector3 position)
    {
        if (victim == null || !IsFinite(position)) return;
        CombatCameraShake shake = FindFor(victim);
        if (shake != null) shake.EmitHurt(position);
    }

    private static CombatCameraShake FindFor(Transform target)
    {
        for (int i = Bindings.Count - 1; i >= 0; i--)
        {
            CombatCameraShake shake = Bindings[i];
            if (shake == null)
            {
                Bindings.RemoveAt(i);
                continue;
            }
            if (shake._attached && shake.enabled && shake.gameObject.activeInHierarchy
                && ReferenceEquals(shake._controlledTarget, target))
                return shake;
        }
        return null;
    }

    private bool Bind(Camera output, CinemachineVirtualCamera virtualCamera, Transform controlledTarget)
    {
        if (_attached
            && ReferenceEquals(_output, output)
            && ReferenceEquals(_virtualCamera, virtualCamera)
            && ReferenceEquals(_controlledTarget, controlledTarget)
            && _listener != null)
        {
            enabled = true;
            if (_source != null) _source.enabled = true;
            _listener.enabled = true;
            return true;
        }

        CancelOwnedImpulses();
        Unregister();

        if (_listener != null && !ReferenceEquals(_listener.gameObject, virtualCamera.gameObject))
        {
            ReleaseListener();
            _channel = 0;
        }

        _output = output;
        _virtualCamera = virtualCamera;
        _controlledTarget = controlledTarget;
        _attached = false;

        if (_source == null)
        {
            _source = output.gameObject.AddComponent<CinemachineImpulseSource>();
            if (_source == null)
            {
                ChaosLog.Warn(LogChannel.Battle, "[相机抖动] 无法在输出相机上创建 CinemachineImpulseSource");
                return false;
            }
        }

        if (_listener == null)
        {
            if (_channel == 0 && !TryAllocateChannel(out _channel))
            {
                ChaosLog.Warn(LogChannel.Battle, "[相机抖动] 32 个 Impulse channel 均已被占用，无法保证只影响自己的虚拟相机");
                CleanupFailedBind();
                return false;
            }

            _listener = virtualCamera.gameObject.AddComponent<CinemachineImpulseListener>();
            if (_listener == null)
            {
                ChaosLog.Warn(LogChannel.Battle, "[相机抖动] 无法在虚拟相机上创建 CinemachineImpulseListener");
                CleanupFailedBind();
                return false;
            }
        }

        _settings = LoadSettings();
        if (_settings == null)
        {
            ChaosLog.Warn(LogChannel.Battle, "[相机抖动] 无法取得 CombatCameraShakeSettings");
            CleanupFailedBind();
            return false;
        }

        ConfigureListener(_listener, _channel);
        ConfigureDefinitions(_settings, _channel);
        ConfigureSource(_source);

        _nextAllowedTime = 0f;
        _active.Clear();
        _attached = true;
        Register();
        enabled = true;
        _listener.enabled = true;
        _source.enabled = true;

        ChaosLog.Info(LogChannel.Battle, "[相机抖动] 绑定 " + controlledTarget.name
            + " → vcam " + virtualCamera.name + "，专用 channel=0x" + _channel.ToString("X8"));
        return true;
    }

    private void CleanupFailedBind()
    {
        CancelOwnedImpulses();
        Unregister();
        if (_listener != null)
        {
            _listener.enabled = false;
            DestroyOwned(_listener);
            _listener = null;
        }
        if (_source != null) _source.enabled = false;
        _channel = 0;
        _attached = false;
        _controlledTarget = null;
        _virtualCamera = null;
        _output = null;
        _settings = null;
        ReleaseFallbackSettingsIfUnused();
    }

    private static bool TryAllocateChannel(out int channel)
    {
        int used = 0;
        CinemachineImpulseListener[] listeners =
            FindObjectsOfType<CinemachineImpulseListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null) used |= listeners[i].m_ChannelMask;
        }

        for (int bit = 31; bit >= 0; bit--)
        {
            int mask = 1 << bit;
            if ((used & mask) == 0)
            {
                channel = mask;
                return true;
            }
        }

        channel = 0;
        return false;
    }

    private static void ConfigureListener(CinemachineImpulseListener listener, int channel)
    {
        listener.m_ApplyAfter = CinemachineCore.Stage.Noise;
        listener.m_ChannelMask = channel;
        listener.m_Gain = 1f;
        listener.m_Use2DDistance = true;
        listener.m_UseCameraSpace = false;
        listener.m_ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
        {
            m_SecondaryNoise = null,
            m_AmplitudeGain = 0f,
            m_FrequencyGain = 1f,
            m_Duration = 0f
        };
    }

    private void ConfigureDefinitions(CombatCameraShakeSettings settings, int channel)
    {
        _normalDefinition = CreateDefinition(channel, settings.NormalHitDuration);
        _hurtDefinition = CreateDefinition(channel, settings.HurtDuration);
        _heavyDefinition = CreateDefinition(channel, settings.HeavyHitDuration);
    }

    private static CinemachineImpulseDefinition CreateDefinition(int channel, float duration)
    {
        CinemachineImpulseDefinition definition = new CinemachineImpulseDefinition
        {
            m_ImpulseChannel = channel,
            m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Custom,
            m_CustomImpulseShape = ShakeCurve,
            m_ImpulseDuration = duration,
            m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            m_DissipationDistance = 100f,
            m_DissipationRate = 0f,
            m_PropagationSpeed = 343f,
            m_AmplitudeGain = 1f,
            m_FrequencyGain = 1f
        };
        return definition;
    }

    private static void ConfigureSource(CinemachineImpulseSource source)
    {
        source.m_DefaultVelocity = Vector3.zero;
        if (source.m_ImpulseDefinition == null)
            source.m_ImpulseDefinition = new CinemachineImpulseDefinition();
    }

    private static CombatCameraShakeSettings LoadSettings()
    {
        CombatCameraShakeSettings asset =
            Resources.Load<CombatCameraShakeSettings>(CombatCameraShakeSettings.ResourcesPath);
        if (asset != null) return asset;

        if (s_fallbackSettings == null)
        {
            s_fallbackSettings = ScriptableObject.CreateInstance<CombatCameraShakeSettings>();
            s_fallbackSettings.name = "CombatCameraShakeSettings (Runtime Defaults)";
            s_fallbackSettings.hideFlags = HideFlags.HideAndDontSave;
        }
        return s_fallbackSettings;
    }

    private static void ReleaseFallbackSettingsIfUnused()
    {
        if (Bindings.Count != 0 || s_fallbackSettings == null) return;
        DestroyOwned(s_fallbackSettings);
        s_fallbackSettings = null;
    }

    private void EmitHit(Vector3 position, bool heavy)
    {
        if (!CanEmit()) return;
        if (heavy) Emit(position, _settings.HeavyHitAmplitude * _settings.IntensityScale, _heavyDefinition);
        else Emit(position, _settings.NormalHitAmplitude * _settings.IntensityScale, _normalDefinition);
    }

    private void EmitHurt(Vector3 position)
    {
        if (!CanEmit()) return;
        Emit(position, _settings.HurtAmplitude * _settings.IntensityScale, _hurtDefinition);
    }

    private void Emit(Vector3 position, float requestedAmplitude, CinemachineImpulseDefinition definition)
    {
        if (definition == null || !IsFinitePositive(requestedAmplitude)) return;
        if (_source == null || _settings == null) return;

        float now = CinemachineImpulseManager.Instance.CurrentTime;
        PruneActive(now);
        if (now < _nextAllowedTime) return;

        float amplitude = Mathf.Min(requestedAmplitude, _settings.MaxAmplitude);
        float remaining = _settings.MaxAmplitude - ActiveAmplitude();
        if (remaining <= 0f) return;
        amplitude = Mathf.Min(amplitude, remaining);
        if (amplitude <= 0.0001f) return;

        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        direction.Normalize();
        Vector3 velocity = new Vector3(direction.x * amplitude, direction.y * amplitude, 0f);

        // 让 source 的当前 definition 与本次事件一致；事件自身持有 definition 引用，
        // 三类 definition 各自固定 duration，事件存活期间不再改写，避免影响尚存冲量。
        _source.m_ImpulseDefinition = definition;
        CinemachineImpulseManager.ImpulseEvent impulseEvent =
            definition.CreateAndReturnEvent(position, velocity);
        if (impulseEvent == null) return;

        _active.Add(new ActiveImpulse
        {
            Event = impulseEvent,
            Signal = impulseEvent.m_SignalSource,
            Channel = impulseEvent.m_Channel,
            EndTime = now + definition.m_ImpulseDuration,
            Amplitude = amplitude
        });
        _nextAllowedTime = now + _settings.MinTriggerInterval;
    }

    private bool CanEmit()
    {
        if (!CanEmitContext()) return false;
        if (_settings == null) return false;
        return _settings.IntensityScale > 0f;
    }

    private bool CanEmitContext()
    {
        if (!_attached || !enabled || !gameObject.activeInHierarchy) return false;
        if (_controlledTarget == null) return false;
        if (_output == null || !_output.isActiveAndEnabled) return false;
        if (_virtualCamera == null || !_virtualCamera.isActiveAndEnabled) return false;
        if (_listener == null || !_listener.enabled) return false;
        if (_source == null || !_source.enabled) return false;
        if (Time.timeScale <= 0f) return false;
        if (RunSceneFlow.IsTransitioning) return false;

        InputManager input = InputManager.Instance;
        if (input != null && !input.CanReadGameplayInput) return false;
        return true;
    }

    private float ActiveAmplitude()
    {
        float total = 0f;
        for (int i = 0; i < _active.Count; i++) total += _active[i].Amplitude;
        return total;
    }

    private void PruneActive(float now)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ActiveImpulse impulse = _active[i];
            if (impulse == null
                || impulse.Event == null
                || now >= impulse.EndTime
                || !ReferenceEquals(impulse.Event.m_SignalSource, impulse.Signal)
                || impulse.Event.m_Channel != impulse.Channel)
            {
                _active.RemoveAt(i);
            }
        }
    }

    private void CancelOwnedImpulses()
    {
        if (_active.Count == 0) return;

        CinemachineImpulseManager manager = CinemachineImpulseManager.Instance;
        float now = manager == null ? Time.time : manager.CurrentTime;
        for (int i = 0; i < _active.Count; i++)
        {
            ActiveImpulse impulse = _active[i];
            if (impulse == null || impulse.Event == null) continue;
            if (impulse.Event.m_Channel != impulse.Channel) continue;
            if (!ReferenceEquals(impulse.Event.m_SignalSource, impulse.Signal)) continue;
            if (impulse.Event.Expired) continue;
            impulse.Event.Cancel(now, true);
        }
        _active.Clear();
    }

    private void Register()
    {
        if (!Bindings.Contains(this)) Bindings.Add(this);
    }

    private void Unregister()
    {
        Bindings.Remove(this);
        ReleaseFallbackSettingsIfUnused();
    }

    private void ReleaseListener()
    {
        if (_listener == null) return;
        _listener.enabled = false;
        DestroyOwned(_listener);
        _listener = null;
        _channel = 0;
    }

    private void LateUpdate()
    {
        if (!_attached) return;

        if (_controlledTarget == null)
        {
            Detach(_controlledTarget);
            return;
        }

        if (!CanEmitContext())
        {
            // 暂停、切图、输入阻断、相机被禁用时，取消自己尚未结束的事件，恢复后不会补震。
            CancelOwnedImpulses();
            return;
        }

        PruneActive(CinemachineImpulseManager.Instance.CurrentTime);
    }

    private void OnEnable()
    {
        if (!_attached) return;
        if (_controlledTarget == null || _virtualCamera == null || _output == null)
        {
            Detach(_controlledTarget);
            return;
        }

        if (_listener != null) _listener.enabled = true;
        if (_source != null) _source.enabled = true;
    }

    private void OnDisable()
    {
        CancelOwnedImpulses();
        if (_listener != null) _listener.enabled = false;
        if (_source != null) _source.enabled = false;
    }

    private void OnDestroy()
    {
        _attached = false;
        CancelOwnedImpulses();
        Unregister();

        if (_listener != null)
        {
            _listener.enabled = false;
            DestroyOwned(_listener);
            _listener = null;
        }
        if (_source != null)
        {
            _source.enabled = false;
            DestroyOwned(_source);
            _source = null;
        }

        _normalDefinition = null;
        _hurtDefinition = null;
        _heavyDefinition = null;
        _settings = null;
        _controlledTarget = null;
        _virtualCamera = null;
        _output = null;
        _channel = 0;
        ReleaseFallbackSettingsIfUnused();
    }

    private void DetachInternal()
    {
        _attached = false;
        CancelOwnedImpulses();
        Unregister();

        if (_listener != null)
        {
            _listener.enabled = false;
            DestroyOwned(_listener);
            _listener = null;
        }
        if (_source != null)
        {
            _source.enabled = false;
            _source.m_ImpulseDefinition = null;
        }

        _normalDefinition = null;
        _hurtDefinition = null;
        _heavyDefinition = null;
        _settings = null;
        _controlledTarget = null;
        _virtualCamera = null;
        _output = null;
        _channel = 0;
        _nextAllowedTime = 0f;
        enabled = false;
        ReleaseFallbackSettingsIfUnused();
    }

    private static void DestroyOwned(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinitePositive(float value)
    {
        return IsFinite(value) && value > 0f;
    }
}
