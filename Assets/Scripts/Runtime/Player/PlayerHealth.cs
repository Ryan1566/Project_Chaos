using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 受控玩家血量（W3.1b / **W3.3 改**）：hp=100，接触伤害 5 点带 0.5s 无敌帧。
///
/// **W3.3 关键改动**：HP 归零**不再直接判负** —— 交给 `RunEncounter.OnPlayerDowned()`：
/// 门客还活着 → 进入「观战模式」（本角色不可操作、不可被攻击，门客 AI 继续）；全部门客也倒地 → 失败。
/// ⚠ Spine 材质没有 `_Color`（W3.0 实测会报 error）→ 倒地半透明用 `skeleton.A`，不是材质色。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public float invulnSeconds = 0.5f;
    public float flashSeconds = 0.15f;

    /// <summary>倒地时的半透明 alpha（Spine 没有 _Color → 用 skeleton.A）。</summary>
    public const float DownedAlpha = 0.4f;

    public int Hp { get; private set; }
    public bool IsInvulnerable { get { return Time.time < _invulnUntil; } }
    /// <summary>W3.3：受控角色是否已倒地（Hp 归零）。倒地后敌人不再把它当目标。</summary>
    public bool IsDowned { get { return Hp <= 0; } }

    private float _invulnUntil;
    private float _flashUntil;
    private Vector3 _baseScale;
    private Rigidbody2D _body;
    private int _runHealthBonus;

    /// <summary>Adjust maximum only; never heal, revive or reset invulnerability.</summary>
    public void SetRunHealthBonus(int bonus)
    {
        bonus = Mathf.Max(0, bonus);
        maxHp = Mathf.Max(1, maxHp - _runHealthBonus + bonus);
        _runHealthBonus = bonus;
        Hp = Mathf.Min(Hp, maxHp);
    }

    private void Awake()
    {
        Hp = maxHp;
        _baseScale = transform.localScale;
        _body = GetComponent<Rigidbody2D>();
    }

    /// <summary>接触伤害入口（由敌人调用）。无敌帧内会被忽略。</summary>
    public bool ApplyContactDamage(int amount)
    {
        if (amount <= 0) return false;
        if (IsInvulnerable) return false;
        if (Hp <= 0) return false;

        Hp -= amount;
        _invulnUntil = Time.time + invulnSeconds;
        _flashUntil = Time.time + flashSeconds;

        ChaosLog.Info(LogChannel.UI, "[玩家] 受到接触伤害 " + amount + "，HP=" + Mathf.Max(0, Hp) + "/" + maxHp
            + "（无敌帧 " + invulnSeconds + "s）");

        if (Hp <= 0)
        {
            Hp = 0;
            OnDowned();
        }
        return true;
    }

    /// <summary>W3.3 GM/验收用：直接把受控角色打到倒地（无视无敌帧）。</summary>
    public bool ForceDown()
    {
        if (Hp <= 0) return false;
        Hp = 0;
        _flashUntil = 0f;
        transform.localScale = _baseScale;
        ChaosLog.Warn(LogChannel.UI, "[玩家] GM：受控角色被强制打到倒地（HP 0/" + maxHp + "）");
        OnDowned();
        return true;
    }

    /// <summary>W3.3：HP 归零 → 交给 RunEncounter 判定「观战」还是「全队倒下 → 失败」。</summary>
    private void OnDowned()
    {
        ChaosLog.Warn(LogChannel.UI, "[玩家] 受控角色 HP 归零 → 倒地（等待 RunEncounter 判定观战/失败）");
        RunEncounter.OnPlayerDowned();
    }

    /// <summary>倒地/复活的视觉：Spine 没有 _Color → 改 `skeleton.A`（0.4 = 半透明）。</summary>
    public void MarkDownedVisual(bool downed)
    {
        PlayerLocomotion locomotion = GetComponent<PlayerLocomotion>();
        SkeletonAnimation skeleton = locomotion == null ? null : locomotion.Skeleton;
        if (skeleton == null) skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (skeleton == null || skeleton.skeleton == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 找不到 SkeletonAnimation，倒地半透明不可用（仅日志标记）");
            return;
        }
        skeleton.skeleton.A = downed ? DownedAlpha : 1f;
        ChaosLog.Info(LogChannel.UI, "[玩家] 倒地视觉 alpha=" + (downed ? DownedAlpha : 1f));
    }

    /// <summary>扎营：hp 直接回满（W3.2i）。W3.3：若之前处于观战，回满后恢复操作。</summary>
    public void RefillFullHp()
    {
        bool wasDowned = Hp <= 0;
        Hp = maxHp;
        _invulnUntil = Time.time + invulnSeconds;
        ChaosLog.Info(LogChannel.UI, "[扎营] 完全治疗：hp 回满 " + Hp + "/" + maxHp);
        if (wasDowned)
        {
            MarkDownedVisual(false);
            RunEncounter.ResetSpectator();
        }
    }

    private void Respawn()
    {
        SpawnPoint spawn = PlayerSpawner.CurrentSpawnPoint;
        if (spawn != null) transform.position = spawn.transform.position + new Vector3(0f, 1f, 0f);
        if (_body != null) _body.velocity = Vector2.zero;

        Hp = maxHp;
        _invulnUntil = Time.time + invulnSeconds;
        ChaosLog.Warn(LogChannel.UI, "[玩家] HP 归零 → 已回出生点 " + transform.position + " 并回满 HP=" + Hp);
    }

    private void Update()
    {
        //受击表现：缩放脉冲（Spine 无 _Color，不能染红）
        if (_flashUntil > 0f && Time.time < _flashUntil)
            transform.localScale = _baseScale * 1.12f;
        else if (_flashUntil > 0f)
        {
            _flashUntil = 0f;
            transform.localScale = _baseScale;
        }
    }
}
