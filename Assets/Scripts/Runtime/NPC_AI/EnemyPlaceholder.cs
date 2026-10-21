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
/// 接触伤害见 `OnCollisionStay2D`（W3.1b / **W3.3 改**：受控角色与未倒地门客都会被打）。
/// **W3.3 目标选择**：最近者（受控角色 + 未倒地门客），**同距时受控角色优先**；倒地单位不进候选。
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

    /// <summary>
    /// W5.2（M14）**基础**数值：地形/阵营倍率乘在它上面。
    /// 刻意不用 `maxHp`/`contactDamage` 字段当基数 —— 那两个是 public、会被别的系统改，
    /// 拿它们当基数会在重复生成/重复应用时叠乘。
    /// </summary>
    public const int BaseMaxHp = 30;
    public const int BaseContactDamage = 5;

    public int Hp { get; private set; }
    public bool IsDead { get; private set; }
    /// <summary>死亡原因（"击杀" / "掉落死亡"）。</summary>
    public string DeathReason { get; private set; }

    /// <summary>W3.3：当前追击目标（受控角色或未倒地门客；GM/验收打印用）。</summary>
    public Transform CurrentTarget { get; private set; }

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
        ApplyTerrainScaling();   //W5.2：地形/阵营倍率（**必须在 Hp = maxHp 之前**）
        Hp = maxHp;
    }

    /// <summary>
    /// W5.2 / M14 影响面 M1：**生成时**按当前场景地形（`MapBiome`）+ 阵营（`Era` 映射）覆盖
    /// `maxHp` / `contactDamage`：
    /// `maxHp = max(1, Round(30 × EnemyHpMul × FactionMul))`、
    /// `contactDamage = max(1, Round(5 × EnemyDmgMul × FactionMul))`（§9 E4：至少 1）。
    ///
    /// 地形取「当前场景」：`RunMapBootstrap.CurrentLayout.PresetId → MapPreset.biomeId → MapBiome` 行；
    /// 读不到会退回地表（倍率 1）并 Warn，不会 0 血 0 伤。
    /// </summary>
    private void ApplyTerrainScaling()
    {
        RunMap.BiomeInfo biome = RunMap.RunMapConfigLoader.GetCurrentBiome();
        int sceneIndex = RunEncounter.CurrentSceneIndex;

        uint era = FactionService.ResolveEra();
        float factionMul = FactionService.GetFactionMul(era);

        float hpMul = 1f, dmgMul = 1f;
        uint dropGroup = 1u;
        if (biome != null)
        {
            hpMul = biome.EnemyHpMul > 0f ? biome.EnemyHpMul : 1f;
            dmgMul = biome.EnemyDmgMul > 0f ? biome.EnemyDmgMul : 1f;
            dropGroup = biome.DropGroupBase == 0u ? 1u : biome.DropGroupBase;
        }

        int hp = Mathf.Max(1, Mathf.RoundToInt(BaseMaxHp * hpMul * factionMul));
        int damage = Mathf.Max(1, Mathf.RoundToInt(BaseContactDamage * dmgMul * factionMul));

        maxHp = hp;
        contactDamage = damage;

        ChaosLog.Info(LogChannel.UI,
            "[地形] 场景" + sceneIndex + " " + (biome == null ? "未知" : biome.NameCN) + "(biome=" + (biome == null ? 0u : biome.Id) + ")"
            + "：HP倍率=" + hpMul.ToString("F2") + " 伤害倍率=" + dmgMul.ToString("F2") + " 掉落组=" + dropGroup
            + " 阵营=" + FactionService.GetFactionName(era) + " FactionMul=" + factionMul.ToString("F2")
            + " → 敌人 HP=" + hp + " 伤害=" + damage);
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
        bool allowDrop = reason != "掉落死亡";
        if (allowDrop)
            ChaosLog.Info(LogChannel.UI, "[敌人] 「" + name + "」死亡（" + reason + "）");
        else
            ChaosLog.Warn(LogChannel.UI, "[敌人] 「" + name + "」掉落死亡（不掉落）");
        RunEncounter.NotifyEnemyKilled(roomId, this, allowDrop);
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

        Transform target = SelectTarget();
        CurrentTarget = target;
        bool chasing = false;
        int dir = _patrolDir;

        if (target != null)
        {
            chasing = true;
            dir = target.position.x >= transform.position.x ? 1 : -1;
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

    /// <summary>
    /// W3.3：目标 = **最近者**（受控角色 + 未倒地门客）。**同距时受控角色优先**
    /// （门客先选、玩家用 `&lt;=` 覆盖）。倒地单位（门客 `IsDowned` / 玩家 `Hp&lt;=0`）不进候选；
    /// 目标必须在本敌人所属房间内。返回 null = 没目标 → 巡逻。
    /// </summary>
    private Transform SelectTarget()
    {
        Transform best = null;
        float bestDistance = chaseRange;

        //① 未倒地门客
        var allies = AllyController.All;
        for (int i = 0; i < allies.Count; i++)
        {
            AllyController ally = allies[i];
            if (ally == null || ally.IsDowned) continue;
            if (!RunEncounter.IsPositionInRoom(roomId, ally.transform.position)) continue;
            float distance = Vector2.Distance(ally.transform.position, transform.position);
            if (distance > bestDistance) continue;
            bestDistance = distance;
            best = ally.transform;
        }

        //② 受控角色（同距优先）
        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            bool playerDowned = health != null && health.IsDowned;
            if (!playerDowned && RunEncounter.IsPositionInRoom(roomId, player.transform.position))
            {
                float distance = Vector2.Distance(player.transform.position, transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = player.transform;
                }
            }
        }

        return best;
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

    /// <summary>接触伤害：受控角色与**未倒地门客**都打（倒地门客不可被攻击）。</summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;
        if (Time.time < _nextContactDamage) return;
        if (collision.collider == null) return;

        //① 门客（倒地则跳过；门客自己还有 0.5s 无敌帧）
        AllyController ally = collision.collider.GetComponentInParent<AllyController>();
        if (ally != null)
        {
            if (ally.IsDowned) return;
            ally.TakeDamage(contactDamage);
            _nextContactDamage = Time.time + contactDamageInterval;
            return;
        }

        //② 受控角色
        PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (health.ApplyContactDamage(contactDamage))
            _nextContactDamage = Time.time + contactDamageInterval;
    }
}
