using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 受控角色攻击（W3.0 spike）：冷却 0.35s，面前 0.8×0.6 区域取 <see cref="IDamageable"/> 造成 10 伤害。
/// **没有攻击动画** → 占位表现：Spine 整体轻微前移 + 短暂变色（0.1s 后恢复）。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 0.35f;
    public Vector2 hitBoxSize = new Vector2(0.8f, 0.6f);
    public float hitBoxOffsetX = 0.5f;

    private PlayerLocomotion _locomotion;
    private SkeletonAnimation _skeleton;
    private MeshRenderer _meshRenderer;
    private Color _baseColor;
    private float _nextAttackTime;
    private float _restoreUntil;
    private Vector3 _spineBasePosition;

    private void Awake()
    {
        _locomotion = GetComponent<PlayerLocomotion>();
        if (_locomotion != null) _skeleton = _locomotion.Skeleton;
        if (_skeleton == null) _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton != null)
        {
            _meshRenderer = _skeleton.GetComponent<MeshRenderer>();
            //⚠ Spine/Skeleton 着色器没有 _Color 属性，不能读 material.color（会报 error）；
            //   → 攻击表现只做前移占位，颜色变化留待能拿到 Skeleton.SetColor 时再补
            _spineBasePosition = _skeleton.transform.localPosition;
        }
    }

    private void Update()
    {
        RestoreVisual();
        bool pressed = InputManager.Instance.WasGameplayPressed(InputManager.ActionAttack);
        if (SettingsManager.Instance.Applied?.attackTriggerMode == (int)MouseTriggerMode.Hold)
            pressed |= InputManager.Instance.IsPointerAttackHeld();
        if (!pressed) { RestoreVisual(); return; }

        if (Time.time < _nextAttackTime) return;   //冷却中
        _nextAttackTime = Time.time + attackCooldown;
        DoAttack();
    }

    private void OnDisable()
    {
        _restoreUntil = 0;
        if (_skeleton != null) _skeleton.transform.localPosition = _spineBasePosition;
    }

    private void RestoreVisual()
    {
        if (_restoreUntil <= 0f || Time.time < _restoreUntil) return;
        _restoreUntil = 0f;
        if (_skeleton != null) _skeleton.transform.localPosition = _spineBasePosition;
    }

    private void DoAttack()
    {
        int facing = _locomotion == null ? 1 : _locomotion.Facing;
        Vector2 center = (Vector2)transform.position + new Vector2(hitBoxOffsetX * facing, 0f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitBoxSize, 0f);
        int damaged = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform == transform) continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target is PlayerAttack) continue;

            target.TakeDamage(damage);
            damaged++;
        }

        //占位表现：前移 + 变色（无攻击动画）
        if (_skeleton != null) _skeleton.transform.localPosition = _spineBasePosition + new Vector3(0.12f * facing, 0f, 0f);
        _restoreUntil = Time.time + 0.1f;

        ChaosLog.Info(LogChannel.UI, "[玩家] 攻击（占位表现），命中 " + damaged + " 个目标");
    }
}
