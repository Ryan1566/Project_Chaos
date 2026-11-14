using UnityEngine;

/// <summary>将玩家的攻击绑定转换成轻击、重击或蓄力释放。</summary>
[DefaultExecutionOrder(-60)]
public class PlayerAttack : MonoBehaviour
{
    public int damage = 10;
    public int LastHitCount => _attack == null ? 0 : _attack.LastHitCount;
    CharacterAttack _attack;
    PlayerLocomotion _locomotion;
    bool _pending, _charging;
    float _pressedAt;

    void Awake()
    {
        _attack = CharacterAttack.For(this);
        _locomotion = GetComponent<PlayerLocomotion>();
    }

    void Update()
    {
        var input = InputManager.Instance;
        if (!input.CanReadGameplayInput || GetComponent<LadderClimber>()?.IsClimbing == true)
        {
            _pending = _charging = false;
            return;
        }
        ReadAttack(input.WasGameplayPressed(InputManager.ActionAttack),
            input.IsGameplayAttackHeld(), input.WasGameplayAttackReleased(), Time.time);
    }

    // 以输入边沿判定，持续按住只会消费一次。
    void ReadAttack(bool pressed, bool heldDown, bool released, float now)
    {
        var profile = _attack.Profile;
        if (profile == null) return;
        int facing = _locomotion == null ? 1 : _locomotion.Facing;
        if (pressed)
        {
            if (profile.heavyMode == "none" || _attack.IsAttacking)
            {
                _attack.TryLight(facing, damage, true);
                return;
            }
            _pending = true;
            _charging = false;
            _pressedAt = now;
        }
        if (!_pending) return;
        float held = now - _pressedAt;
        if (!heldDown)
        {
            if (released)
            {
                if (profile.heavyMode == "animation" && held >= profile.holdSeconds)
                    _attack.TryHeavy(facing, damage);
                else
                {
                    float strength = profile.heavyMode == "chargeLight" && (_charging || held >= profile.holdSeconds)
                        ? Mathf.Lerp(1f, profile.chargeDamageMultiplier, Mathf.Clamp01(held / profile.chargeFullSeconds))
                        : 1f;
                    _attack.TryLight(facing, damage, true, strength);
                }
            }
            _pending = _charging = false;
            return;
        }
        if (held < profile.holdSeconds) return;
        if (profile.heavyMode == "chargeLight") _charging = true;
        else
        {
            _pending = false;
            _attack.TryHeavy(facing, damage);
        }
    }

    void OnDisable()
    {
        _pending = _charging = false;
        if (_attack != null) _attack.Cancel();
    }
}
