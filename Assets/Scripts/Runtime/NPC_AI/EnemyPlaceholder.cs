using ChaosDebug;
using UnityEngine;

/// <summary>
/// Enemy with visible melee windup and a region-bound patrol/chase/return lifecycle.
/// Public health, damage and encounter APIs remain compatible with existing saves and QA.
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
    private RunMap.RunBehaviorSettings _settings;
    private RunMap.RunMapNavigator _navigator;
    private RunCombatFeedback _feedback;
    private Vector2 _homeFeet;
    private Vector2 _patrolFeet;
    private Vector2 _lastSeenFeet;
    private float _lastSeenAt;
    private float _stateUntil;
    private bool _patrolDestinationReady;
    private Transform _windupTarget;

    public enum BehaviorState { Patrol, Chase, Windup, Recover, Return }
    public BehaviorState State { get; private set; }
    public int AttackCount { get; private set; }
    public int InterruptedAttackCount { get; private set; }

    private void Start()
    {
        try { _settings = RunMap.RunBehaviorSettings.Load("Enemy"); }
        catch (System.Exception exception)
        {
            Debug.LogError("[Enemy] RunBehaviorConfig Enemy is unavailable: " + exception.Message, this);
            enabled = false;
            return;
        }
        if (_settings == null)
        {
            Debug.LogError("[Enemy] RunBehaviorConfig has no Enemy row.", this);
            enabled = false;
            return;
        }
        chaseRange = _settings.detectRange;
        chaseSpeed = _settings.moveSpeed;
        patrolSpeed = _settings.patrolSpeed;
        contactDamageInterval = _settings.attackCooldown;
        _homeFeet = CombatQuery.Feet(transform);
        _feedback = RunCombatFeedback.For(this);
        if (RunMap.NaturalMapRuntime.Current != null)
        {
            _navigator = GetComponent<RunMap.RunMapNavigator>();
            if (_navigator == null) _navigator = gameObject.AddComponent<RunMap.RunMapNavigator>();
            _navigator.Configure(chaseSpeed, _settings.jumpForce, _settings.acceleration);
            Vector2 safe;
            if (_navigator.TryFindSafePosition(_homeFeet, out safe, _settings.searchRadius)
                && AllowedBounds.Contains(safe)) _homeFeet = safe;
        }
        State = BehaviorState.Patrol;
    }

    private Rect AllowedBounds
    {
        get
        {
            float padding = _settings != null ? _settings.leashPadding : 0f;
            return new Rect(roomX0 - padding, roomY0 - padding,
                roomW + padding * 2f, roomH + padding * 2f);
        }
    }

    private void OnDisable()
    {
        StopMoving();
        if (_feedback != null) _feedback.Clear();
        if (State == BehaviorState.Windup)
        {
            _windupTarget = null;
            State = BehaviorState.Recover;
            _stateUntil = Time.time + (_settings != null ? _settings.attackRecovery : 0f);
        }
    }

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

        uint era = FactionService.ResolveEra();
        float factionMul = FactionService.GetFactionMul(era);

        float hpMul = 1f, dmgMul = 1f;
        if (biome != null)
        {
            hpMul = biome.EnemyHpMul > 0f ? biome.EnemyHpMul : 1f;
            dmgMul = biome.EnemyDmgMul > 0f ? biome.EnemyDmgMul : 1f;
        }

        int hp = Mathf.Max(1, Mathf.RoundToInt(BaseMaxHp * hpMul * factionMul));
        int damage = Mathf.Max(1, Mathf.RoundToInt(BaseContactDamage * dmgMul * factionMul));

        maxHp = hp;
        contactDamage = damage;

    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Hp -= amount;
        if (State == BehaviorState.Windup)
        {
            InterruptedAttackCount++;
            _nextContactDamage = Mathf.Max(_nextContactDamage, Time.time + contactDamageInterval);
            BeginRecovery();
        }
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
        CurrentTarget = null;
        _windupTarget = null;
        StopMoving();
        if (_feedback != null) _feedback.Clear();
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
        if (_body == null || _settings == null || Time.time < _knockUntil) return;
        if (State == BehaviorState.Windup)
        {
            StopMoving();
            if (!ValidTarget(_windupTarget)) { BeginRecovery(); return; }
            _feedback.Windup(CombatQuery.Center(transform), CombatQuery.Center(_windupTarget));
            if (Time.time < _stateUntil) return;
            Transform victim = _windupTarget;
            if (CanHit(victim))
            {
                _feedback.Strike(CombatQuery.Center(transform), CombatQuery.Center(victim));
                // The windup owns one target. No collision callback can add a second hit.
                AllyController ally = victim.GetComponent<AllyController>();
                if (ally != null) ally.TakeDamage(contactDamage);
                else
                {
                    PlayerHealth health = victim.GetComponent<PlayerHealth>();
                    if (health != null) health.ApplyContactDamage(contactDamage);
                }
                AttackCount++;
            }
            BeginRecovery(false);
            return;
        }
        if (State == BehaviorState.Recover)
        {
            StopMoving();
            if (Time.time < _stateUntil) return;
            State = CurrentTarget != null ? BehaviorState.Chase : BehaviorState.Return;
        }

        Transform visible = SelectTarget();
        if (visible != null)
        {
            CurrentTarget = visible;
            _lastSeenFeet = CombatQuery.Feet(visible);
            _lastSeenAt = Time.time;
            State = BehaviorState.Chase;
        }
        else if (CurrentTarget != null)
        {
            bool valid = ValidTarget(CurrentTarget)
                && Vector2.Distance(CombatQuery.Center(transform), CombatQuery.Center(CurrentTarget)) <= _settings.loseRange;
            if (valid && CombatQuery.HasLineOfSight(transform, CurrentTarget))
            {
                _lastSeenFeet = CombatQuery.Feet(CurrentTarget);
                _lastSeenAt = Time.time;
            }
            if (!valid || Time.time - _lastSeenAt > _settings.memorySeconds)
            {
                CurrentTarget = null;
                State = BehaviorState.Return;
            }
        }
        else if (State == BehaviorState.Chase) State = BehaviorState.Return;

        if (State == BehaviorState.Chase && CurrentTarget != null)
        {
            if (CanHit(CurrentTarget))
            {
                StopMoving();
                if (Time.time >= _nextContactDamage)
                {
                    _windupTarget = CurrentTarget;
                    _nextContactDamage = Time.time + contactDamageInterval;
                    _stateUntil = Time.time + _settings.attackWindup;
                    State = BehaviorState.Windup;
                    _feedback.Windup(CombatQuery.Center(transform), CombatQuery.Center(_windupTarget));
                }
                return;
            }
            MoveTo(_lastSeenFeet, chaseSpeed, _settings.waypointTolerance);
            return;
        }

        if (State == BehaviorState.Return)
        {
            if (MoveTo(_homeFeet, chaseSpeed, _settings.returnTolerance))
            {
                State = BehaviorState.Patrol;
                _stateUntil = Time.time + _settings.patrolWait;
                _patrolDestinationReady = false;
            }
            return;
        }
        Patrol();
    }

    private bool ValidTarget(Transform target)
    {
        return CombatQuery.IsStandingTarget(target)
            && RunEncounter.IsPositionInRoom(roomId, target.position);
    }

    private bool CanHit(Transform target)
    {
        return ValidTarget(target)
            && CombatQuery.CanMelee(transform, target, _settings.attackRange, _settings.attackHeight);
    }

    private void BeginRecovery(bool clearFeedback = true)
    {
        _windupTarget = null;
        State = BehaviorState.Recover;
        _stateUntil = Time.time + (_settings != null ? _settings.attackRecovery : 0f);
        StopMoving();
        if (clearFeedback && _feedback != null) _feedback.Clear();
    }

    private void StopMoving()
    {
        if (_navigator != null) _navigator.Stop();
        if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
    }

    private bool MoveTo(Vector2 feet, float speed, float tolerance)
    {
        if (_navigator != null)
        {
            _navigator.Configure(speed, _settings.jumpForce, _settings.acceleration);
            _navigator.MoveTo(feet, tolerance, AllowedBounds);
            return _navigator.Reached;
        }
        Vector2 current = CombatQuery.Feet(transform);
        if (Mathf.Abs(feet.x - current.x) <= tolerance) { StopMoving(); return true; }
        int direction = feet.x >= current.x ? 1 : -1;
        if (!GroundAhead(direction) || WallAhead(direction)) { StopMoving(); return false; }
        _body.velocity = new Vector2(direction * speed, _body.velocity.y);
        return false;
    }

    private void Patrol()
    {
        if (Time.time < _stateUntil) { StopMoving(); return; }
        if (!_patrolDestinationReady)
        {
            Rect bounds = AllowedBounds;
            Vector2 desired = new Vector2(Mathf.Clamp(_homeFeet.x + _patrolDir * roomW * 0.35f,
                bounds.xMin + 0.6f, bounds.xMax - 0.6f), _homeFeet.y);
            if (_navigator != null)
            {
                Vector2 safe;
                if (!_navigator.TryFindSafePosition(desired, out safe, _settings.searchRadius)
                    || !bounds.Contains(safe))
                {
                    _patrolDir = -_patrolDir;
                    _stateUntil = Time.time + _settings.patrolWait;
                    StopMoving();
                    return;
                }
                desired = safe;
            }
            _patrolFeet = desired;
            _patrolDestinationReady = true;
        }
        bool arrived = MoveTo(_patrolFeet, patrolSpeed, _settings.waypointTolerance);
        if (arrived || (_navigator != null && _navigator.IsStuck)
            || (_navigator == null && Mathf.Abs(_body.velocity.x) < 0.01f))
        {
            StopMoving();
            _patrolDestinationReady = false;
            _patrolDir = -_patrolDir;
            _stateUntil = Time.time + _settings.patrolWait;
        }
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
            if (ally == null || !ValidTarget(ally.transform)) continue;
            if (!RunEncounter.IsPositionInRoom(roomId, ally.transform.position)) continue;
            float distance = Vector2.Distance(ally.transform.position, transform.position);
            if (distance > bestDistance) continue;
            if (!CombatQuery.HasLineOfSight(transform, ally.transform)) continue;
            bestDistance = distance;
            best = ally.transform;
        }

        //② 受控角色（同距优先）
        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            bool playerDowned = health == null || health.IsDowned;
            if (!playerDowned && RunEncounter.IsPositionInRoom(roomId, player.transform.position))
            {
                float distance = Vector2.Distance(player.transform.position, transform.position);
                if (distance <= bestDistance && CombatQuery.HasLineOfSight(transform, player.transform))
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
            if (!RunMap.RunMapPhysics.IsTerrain(hits[i].collider)) continue;
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
            if (!RunMap.RunMapPhysics.IsTerrain(hits[i].collider)) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            if (hits[i].point.y > transform.position.y + 0.3f) return true;
        }
        return false;
    }

}
