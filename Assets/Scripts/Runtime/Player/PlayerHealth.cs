using ChaosDebug;
using UnityEngine;

/// <summary>
/// 受控玩家血量（W3.1b）：hp=100，接触伤害 5 点带 0.5s 无敌帧；归零 → 回出生点并回满。
/// ⚠ **本轮不做死亡结算**（W3.2 再做）。
/// ⚠ Spine 材质没有 `_Color`（W3.0 实测会报 error）→ "受击闪红"用**缩放脉冲**占位，不是真变色。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public float invulnSeconds = 0.5f;
    public float flashSeconds = 0.15f;

    public int Hp { get; private set; }
    public bool IsInvulnerable { get { return Time.time < _invulnUntil; } }

    private float _invulnUntil;
    private float _flashUntil;
    private Vector3 _baseScale;
    private Rigidbody2D _body;

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

        if (Hp <= 0) Respawn();
        return true;
    }

    private void Respawn()
    {
        SpawnPoint spawn = PlayerSpawner.CurrentSpawnPoint;
        if (spawn != null) transform.position = spawn.transform.position + new Vector3(0f, 1f, 0f);
        if (_body != null) _body.velocity = Vector2.zero;

        Hp = maxHp;
        _invulnUntil = Time.time + invulnSeconds;
        ChaosLog.Warn(LogChannel.UI, "[玩家] HP 归零 → 已回出生点 " + transform.position + " 并回满 HP=" + Hp
            + "（死亡结算留 W3.2）");
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
