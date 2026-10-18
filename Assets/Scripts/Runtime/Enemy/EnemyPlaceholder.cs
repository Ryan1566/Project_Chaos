using ChaosDebug;
using UnityEngine;

/// <summary>
/// 局内敌人占位（W3.1d 修正版）。
///
/// **本轮修的三件事**（W3.1c 实测：敌人频繁走出平台掉图 → 房间"0/2 但已清 0"）：
/// 1. 巡逻范围改用**所属房间矩形**（`roomX0/roomW`），不再固定 ±3；
/// 2. 前方 **0.6 格做"向下探地 + 水平探墙"** 两条 Raycast：没地面或遇墙就折返；
/// 3. 真掉出地图 → 走 **`Die()`（算掉落死亡，计入房间清零）**，不再"销毁不计入"。
///
/// 接触伤害见 `OnCollisionStay2D`（W3.1b）。敌人**不打 AI 门客**（门客没有 PlayerHealth）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyPlaceholder : MonoBehaviour, IDamageable
{
    public int maxHp = 30;
    public float chaseRange = 8f;
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    /// <summary>前方探测距离（探地/探墙共用）。</summary>
    public float probeDistance = 0.6f;
    public float fallThresholdY = -20f;

    public float contactDamageInterval = 0.8f;
    public int contactDamage = 5;

    /// <summary>所属房间（用于清房统计）。</summary>
    public ushort roomId;
    /// <summary>所属房间世界矩形（由 RunEncounter 生成时写入）。</summary>
    public float roomX0, roomY0, roomW, roomH;

    public int Hp { get; private set; }
    public bool IsDead { get; private set; }
    /// <summary>死亡原因（"击杀" / "掉落死亡"）。</summary>
    public string DeathReason { get; private set; }

    private Rigidbody2D _body;
    private SpriteRenderer _renderer;
    private Color _baseColor;
    private int _patrolDir = 1;
    private float _flashUntil;
    private float _knockUntil;
    private float _deathStart = -1f;
    private float _nextContactDamage;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null) _baseColor = _renderer.color;
        Hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Hp -= amount;
        _flashUntil = Time.time + 0.08f;
        if (_renderer != null) _renderer.color = Color.white;

        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player != null && _body != null)
        {
            int dir = transform.position.x >= player.transform.position.x ? 1 : -1;
            if (!GroundAhead(dir)) dir = -dir;   //靠近边缘时强制朝内，别被击退出平台
            _body.velocity = new Vector2(dir * 4f, _body.velocity.y);
            _knockUntil = Time.time + 0.1f;
        }

        ChaosLog.Info(LogChannel.UI, "[敌人] 「" + name + "」受到 " + amount + " 伤害，剩余 HP " + Mathf.Max(0, Hp) + "/" + maxHp);
        if (Hp <= 0) Die("击杀");
    }

    /// <summary>统一死亡路径：击杀与掉落死亡都走这里（会计入房间清零）。</summary>
    public void Die(string reason)
    {
        if (IsDead) return;
        IsDead = true;
        DeathReason = reason;
        _deathStart = Time.time;
        ChaosLog.Info(LogChannel.UI, "[敌人] 「" + name + "」死亡（" + reason + "）");
        RunEncounter.NotifyEnemyKilled(roomId, this);
    }

    private void Update()
    {
        if (_renderer != null && _flashUntil > 0f && Time.time >= _flashUntil)
        {
            _flashUntil = 0f;
            _renderer.color = _baseColor;
        }

        if (IsDead)
        {
            float t = Time.time - _deathStart;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, Mathf.Clamp01(t / 0.25f));
            if (t >= 0.25f) Destroy(gameObject);
            return;
        }

        if (transform.position.y < fallThresholdY)
        {
            ChaosLog.Warn(LogChannel.UI, "[敌人] 「" + name + "」掉落死亡（y=" + transform.position.y.ToString("F1") + "）");
            Die("掉落死亡");
            return;
        }

        UpdateAi();
    }

    private void UpdateAi()
    {
        if (_body == null) return;
        if (Time.time < _knockUntil) return;

        GameObject player = PlayerSpawner.CurrentPlayer;
        bool chasing = false;
        int dir = _patrolDir;

        if (player != null)
        {
            float distance = Vector2.Distance(player.transform.position, transform.position);
            if (distance <= chaseRange && RunEncounter.IsPlayerInRoom(roomId))
            {
                chasing = true;
                dir = player.transform.position.x >= transform.position.x ? 1 : -1;
            }
        }

        //巡逻：限制在所属房间矩形内（不再用固定 ±3）
        if (!chasing)
        {
            float left = roomX0 + 1.5f;
            float right = roomX0 + roomW - 1.5f;
            if (transform.position.x <= left) _patrolDir = 1;
            else if (transform.position.x >= right) _patrolDir = -1;
            dir = _patrolDir;
        }

        //边缘/墙检测：没地面或遇墙就折返
        if (!GroundAhead(dir) || WallAhead(dir))
        {
            dir = -dir;
            if (!chasing) _patrolDir = dir;
        }

        float speed = chasing ? chaseSpeed : patrolSpeed;
        _body.velocity = new Vector2(dir * speed, _body.velocity.y);
    }

    /// <summary>前方 probeDistance 处向下探地（跳过自身碰撞体）。</summary>
    private bool GroundAhead(int dir)
    {
        if (dir == 0) return true;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * probeDistance, 0.2f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 2.0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    /// <summary>前方 probeDistance 处是否有墙（只把明显高于脚下的命中当墙）。</summary>
    private bool WallAhead(int dir)
    {
        if (dir == 0) return false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, new Vector2(dir, 0f), probeDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            if (hits[i].point.y > transform.position.y + 0.3f) return true;
        }
        return false;
    }

    /// <summary>接触伤害：只打受控玩家（AI 门客没有 PlayerHealth → 天然不受伤）。</summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;
        if (Time.time < _nextContactDamage) return;

        PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (health.ApplyContactDamage(contactDamage))
            _nextContactDamage = Time.time + contactDamageInterval;
    }
}
