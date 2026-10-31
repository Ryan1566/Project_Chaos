using System.Collections.Generic;
using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// AI retainer: terrain-aware following and single-target melee; recovery warps need verified ground.
/// Existing team, health and QA counters remain available.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AllyController : MonoBehaviour
{
    public const float DefaultMoveSpeed = 5f;
    public const float DefaultJumpForce = 11f;
    public static readonly List<AllyController> All = new List<AllyController>();

    public int followIndex;
    public uint memberId;
    public float moveSpeed = DefaultMoveSpeed;
    public float acceleration = 30f;
    public float jumpForce = DefaultJumpForce;
    public float attackRange = 2.5f;
    public float attackCooldown = 0.6f;
    public int damage = 6;
    public float probeDistance = 0.6f;
    public float jumpCooldown = 0.8f;
    public float teleportDy = 1.5f;
    public float teleportHoldSeconds = 1.5f;
    public float fallThresholdY = -20f;

    private static readonly float[] FollowOffsets = { -2.6f, 2.6f, -4.3f };

    private Rigidbody2D _body;
    private SkeletonAnimation _skeleton;
    private float _nextAttack;
    private float _nextJump;
    private float _stopJumpUntil;
    private string _currentAnimation;
    private int _facing = 1;
    private EnemyPlaceholder _target;
    private float _nextRecoveryAttempt;
    private float _stuckSince = -1f;
    private float _jumpCheckAt = -1f;
    private float _jumpStartY;
    private int _futileJumps;

    public string AllyName { get { return "Ally_" + followIndex + "(id=" + memberId + ")"; } }
    public int FallCount { get; private set; }
    public int TeleportCount { get; private set; }
    /// <summary>验收计数器：起跳次数 / 停跳 Warn 次数 / 贴近正下方日志次数。</summary>
    public int JumpCount { get; private set; }
    public int StopJumpWarnCount { get; private set; }
    public int NearBelowCount { get; private set; }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton != null) _skeleton.AnimationState.SetAnimation(0, "Idle_Normal", true);
        _currentAnimation = "Idle_Normal";
    }

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    //⚠ W3.3：**不要**在 OnDisable 里移除。倒地 = `enabled=false`，结算暂停世界也是 `enabled=false`；
    //  若 OnDisable 就移出 All，倒地门客会从 All 消失 → 观战判定/补员/结算重伤全部漏掉。
    //   统一只在 OnDestroy 移除。
    private void OnDestroy() { All.Remove(this); }

    private RunMap.RunBehaviorSettings _settings;
    private RunMap.RunMapNavigator _navigator;
    private RunCombatFeedback _feedback;
    private EnemyPlaceholder _attackTarget;
    private float _attackAt;
    private float _recoverUntil;
    private float _lastTargetSeen;
    private Vector2 _targetFeet;
    private Vector2 _progressFeet;
    private float _progressAt;
    private int _observedNavigationJumps;
    private bool _belowFallThreshold;

    public EnemyPlaceholder CurrentTarget { get { return _target; } }
    public bool IsWindingUp { get { return _attackTarget != null; } }

    private void Start()
    {
        try { _settings = RunMap.RunBehaviorSettings.Load("Ally"); }
        catch (System.Exception exception)
        {
            Debug.LogError("[Ally] RunBehaviorConfig Ally is unavailable: " + exception.Message, this);
            enabled = false;
            return;
        }
        if (_settings == null)
        {
            Debug.LogError("[Ally] RunBehaviorConfig has no Ally row.", this);
            enabled = false;
            return;
        }
        moveSpeed = _settings.moveSpeed;
        acceleration = _settings.acceleration;
        jumpForce = _settings.jumpForce;
        attackRange = _settings.attackRange;
        attackCooldown = _settings.attackCooldown;
        _feedback = RunCombatFeedback.For(this);
        if (RunMap.NaturalMapRuntime.Current != null)
        {
            _navigator = GetComponent<RunMap.RunMapNavigator>();
            if (_navigator == null) _navigator = gameObject.AddComponent<RunMap.RunMapNavigator>();
            _navigator.Configure(moveSpeed, jumpForce, acceleration);
        }
        _progressFeet = CombatQuery.Feet(transform);
        _progressAt = Time.time;
    }

    private void OnDisable()
    {
        StopMoving();
        CancelAttack();
    }

    private void Update()
    {
        if (IsDowned || _settings == null) return;
        GameObject controlled = PlayerSpawner.CurrentPlayer;
        if (controlled == null) { StopMoving(); CancelAttack(); return; }
        if (transform.position.y < fallThresholdY)
        {
            if (!_belowFallThreshold) FallCount++;
            _belowFallThreshold = true;
            TryRecover(controlled, "掉出地图");
            return;
        }
        _belowFallThreshold = false;

        if (_attackTarget != null)
        {
            StopMoving();
            if (_attackTarget.IsDead) { CancelAttack(); return; }
            SetFacing(_attackTarget.transform.position.x >= transform.position.x ? 1 : -1);
            _feedback.Windup(CombatQuery.Center(transform), CombatQuery.Center(_attackTarget.transform));
            if (Time.time < _attackAt) return;
            EnemyPlaceholder victim = _attackTarget;
            _attackTarget = null;
            _feedback.Clear();
            _recoverUntil = Time.time + _settings.attackRecovery;
            if (CanHit(victim))
            {
                _feedback.Strike(CombatQuery.Center(transform), CombatQuery.Center(victim.transform));
                victim.TakeDamage(damage);
                ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」攻击「" + victim.name + "」，伤害 " + damage);
            }
            return;
        }
        if (Time.time < _recoverUntil) { StopMoving(); return; }

        if (!IsEligibleEnemy(_target)
            || Vector2.Distance(CombatQuery.Center(transform), CombatQuery.Center(_target.transform)) > _settings.loseRange)
            _target = null;
        if (_target != null && CombatQuery.HasLineOfSight(transform, _target.transform))
        {
            _targetFeet = CombatQuery.Feet(_target.transform);
            _lastTargetSeen = Time.time;
        }
        else if (_target != null && Time.time - _lastTargetSeen > _settings.memorySeconds)
            _target = null;
        if (_target == null)
        {
            _target = FindNearestEnemy();
            if (_target != null)
            {
                _targetFeet = CombatQuery.Feet(_target.transform);
                _lastTargetSeen = Time.time;
            }
        }
        if (CanHit(_target))
        {
            StopMoving();
            SetFacing(_target.transform.position.x >= transform.position.x ? 1 : -1);
            if (Time.time >= _nextAttack)
            {
                _nextAttack = Time.time + attackCooldown;
                _attackTarget = _target;
                _attackAt = Time.time + _settings.attackWindup;
                _feedback.Windup(CombatQuery.Center(transform), CombatQuery.Center(_target.transform));
            }
            return;
        }

        Vector2 playerFeet = CombatQuery.Feet(controlled.transform);
        float offset = FollowOffsets[Mathf.Clamp(followIndex - 1, 0, FollowOffsets.Length - 1)];
        Vector2 destination = _target != null ? _targetFeet : playerFeet + Vector2.right * offset;
        if (_navigator != null)
        {
            // A formation offset may fall inside a wall or over a cave opening. Resolve it
            // onto graph ground before asking for a path, preserving the player's floor.
            if (_target == null)
            {
                Vector2 safe;
                if (_navigator.TryFindSafePosition(destination, out safe, _settings.searchRadius)) destination = safe;
                else destination = playerFeet;
            }
            _navigator.MoveTo(destination, _target != null ? _settings.waypointTolerance : 1.4f);
            int jumps = _navigator.JumpCount;
            JumpCount += Mathf.Max(0, jumps - _observedNavigationJumps);
            _observedNavigationJumps = jumps;
            float speed = _body != null ? Mathf.Abs(_body.velocity.x) : 0f;
            if (speed > 0.1f) SetFacing(_body.velocity.x >= 0f ? 1 : -1);
            UpdateAnimation(speed);
            if (_navigator.IsStuck)
            {
                if (_stuckSince < 0f) _stuckSince = Time.time - _settings.stuckSeconds;
                if (Time.time - _stuckSince >= _settings.recoverySeconds)
                    TryRecover(controlled, "导航持续卡住");
            }
            else _stuckSince = -1f;
            return;
        }
        MoveLegacy(destination, controlled);
    }

    private bool IsEligibleEnemy(EnemyPlaceholder enemy)
    {
        return enemy != null && !enemy.IsDead && enemy.Hp > 0
            && RunEncounter.IsPlayerInRoom(enemy.roomId);
    }

    private bool CanHit(EnemyPlaceholder enemy)
    {
        return IsEligibleEnemy(enemy)
            && CombatQuery.CanMelee(transform, enemy.transform, attackRange, _settings.attackHeight);
    }

    private void CancelAttack()
    {
        if (_attackTarget != null && _settings != null)
        {
            _nextAttack = Mathf.Max(_nextAttack, Time.time + attackCooldown);
            _recoverUntil = Time.time + _settings.attackRecovery;
        }
        _attackTarget = null;
        if (_feedback != null) _feedback.Clear();
    }

    private void StopMoving()
    {
        if (_navigator != null) _navigator.Stop();
        if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
        _stuckSince = -1f;
        _progressAt = Time.time;
        _progressFeet = CombatQuery.Feet(transform);
        UpdateAnimation(0f);
    }

    private void MoveLegacy(Vector2 destination, GameObject controlled)
    {
        if (_jumpCheckAt > 0f && Time.time >= _jumpCheckAt)
        {
            _jumpCheckAt = -1f;
            if (Mathf.Abs(transform.position.y - _jumpStartY) < 0.3f)
            {
                _futileJumps++;
                if (_futileJumps >= 3)
                {
                    _stopJumpUntil = Time.time + 3f;
                    StopJumpWarnCount++;
                    _futileJumps = 0;
                    ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」旧地图连续起跳无效，暂缓重试");
                }
            }
            else _futileJumps = 0;
        }
        Vector2 feet = CombatQuery.Feet(transform);
        float delta = destination.x - feet.x;
        int direction = delta >= 0f ? 1 : -1;
        bool moving = _body != null && Mathf.Abs(_body.velocity.x) > 0.1f;
        bool needsMovement = Mathf.Abs(delta) > (moving ? 1.4f : 1.6f)
            || Mathf.Abs(destination.y - feet.y) > 1f;
        if (needsMovement)
        {
            if (_body != null)
            {
                float desiredSpeed = GroundAhead(direction) ? direction * moveSpeed : 0f;
                _body.velocity = new Vector2(Mathf.MoveTowards(_body.velocity.x,
                    desiredSpeed, acceleration * Time.deltaTime), _body.velocity.y);
            }
            SetFacing(direction);
            TryJumpIfBlocked(direction);
            if (Vector2.Distance(feet, _progressFeet) > 0.22f)
            { _progressFeet = feet; _progressAt = Time.time; }
            else if (Time.time - _progressAt > _settings.recoverySeconds)
                TryRecover(controlled, "旧地图持续卡住");
        }
        else { StopMoving(); _progressAt = Time.time; _progressFeet = feet; }
        UpdateAnimation(_body != null ? Mathf.Abs(_body.velocity.x) : 0f);
    }

    private bool TryRecover(GameObject controlled, string reason)
    {
        // No warp on height difference or distance alone. Natural-map recovery requires
        // Navigator.IsStuck (except falling), a sustained timeout and a verified footprint.
        if (Time.time < _nextRecoveryAttempt) return false;
        Vector2 playerFeet = CombatQuery.Feet(controlled.transform);
        Vector2 desired = playerFeet + Vector2.right * FollowOffsets[Mathf.Clamp(followIndex - 1, 0, FollowOffsets.Length - 1)];
        Vector2 safe;
        bool found = false;
        if (_navigator != null)
        {
            found = _navigator.TryFindSafePosition(desired, out safe, _settings.searchRadius)
                || _navigator.TryFindSafePosition(playerFeet, out safe, _settings.searchRadius);
            if (found) _navigator.WarpToFeet(safe);
        }
        else
        {
            safe = default(Vector2);
            Collider2D shape = CombatQuery.Body(transform);
            if (shape != null)
            {
                for (int x = 0; x <= _settings.searchRadius * 2 && !found; x++)
                {
                    float dx = x == 0 ? 0f : (x % 2 == 0 ? 1f : -1f) * ((x + 1) / 2);
                    Vector2 origin = playerFeet + new Vector2(dx, 2f);
                    RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 4f);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (!RunMap.RunMapPhysics.IsTerrain(hits[i].collider) || hits[i].normal.y < 0.5f) continue;
                        Vector2 candidate = hits[i].point + Vector2.up * 0.03f;
                        if (!RunMap.RunMapPhysics.CanOccupy(candidate, shape.bounds.size)) continue;
                        safe = candidate; found = true; break;
                    }
                }
                if (found)
                {
                    Vector2 offset = (Vector2)transform.position - RunMap.RunMapPhysics.Feet(shape);
                    _body.position = safe + offset;
                    _body.velocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                }
            }
        }
        _nextRecoveryAttempt = Time.time + _settings.recoverySeconds;
        if (!found) return false;
        TeleportCount++;
        _stuckSince = -1f;
        _progressFeet = CombatQuery.Feet(transform);
        _progressAt = Time.time;
        CancelAttack();
        ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」回到已验证的安全地面（" + reason + "）");
        return true;
    }

    /// <summary>
    /// **唯一允许跳的理由**：前方有墙，且墙顶上方 0.6×0.6 严格 OverlapBox 命中真实地面。
    /// </summary>
    private void TryJumpIfBlocked(int dir)
    {
        if (Time.time < _nextJump) return;
        if (Time.time < _stopJumpUntil) return;
        if (!IsGrounded()) return;

        RaycastHit2D wall = FindWall(dir);
        if (wall.collider == null) return;

        Vector2 boxCenter = new Vector2(wall.point.x + dir * 0.3f, wall.collider.bounds.max.y + 0.3f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, new Vector2(0.6f, 0.6f), 0f);
        bool groundAbove = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!RunMap.RunMapPhysics.IsTerrain(hits[i])) continue;
            if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform)) continue;
            groundAbove = true;
            break;
        }
        if (!groundAbove) return;

        _nextJump = Time.time + jumpCooldown;
        JumpCount++;
        _jumpStartY = transform.position.y;
        _jumpCheckAt = Time.time + 1f;
        if (_body != null) _body.velocity = new Vector2(_body.velocity.x, jumpForce);
        ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」起跳跨墙（墙顶有地面）");
    }

    private RaycastHit2D FindWall(int dir)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, new Vector2(dir, 0f), probeDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!RunMap.RunMapPhysics.IsTerrain(hits[i].collider)) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            float relative = hits[i].point.y - transform.position.y;
            if (relative > 0.3f && relative < 1.8f) return hits[i];
        }
        return default(RaycastHit2D);
    }

    private bool IsGrounded()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.05f);
        Vector2 size = new Vector2(Mathf.Max(0.1f, b.size.x * 0.8f), 0.04f);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, Vector2.down, 0.12f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!RunMap.RunMapPhysics.IsTerrain(hits[i].collider)) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

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

    private EnemyPlaceholder FindNearestEnemy()
    {
        EnemyPlaceholder[] enemies = Object.FindObjectsOfType<EnemyPlaceholder>();
        EnemyPlaceholder best = null;
        float bestDistance = _settings.detectRange;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyPlaceholder enemy = enemies[i];
            if (!IsEligibleEnemy(enemy)) continue;
            if (!CombatQuery.HasLineOfSight(transform, enemy.transform)) continue;
            float distance = Vector2.Distance(enemy.transform.position, transform.position);
            if (distance < bestDistance) { bestDistance = distance; best = enemy; }
        }
        return best;
    }

    private void SetFacing(int facing)
    {
        if (facing == 0 || facing == _facing) return;
        _facing = facing;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * _facing;
        transform.localScale = scale;
    }

    private void UpdateAnimation(float speed)
    {
        if (_skeleton == null) return;
        string wanted = speed < 0.1f ? "Idle_Normal" : "Run_Normal";
        if (wanted == _currentAnimation) return;
        _currentAnimation = wanted;
        _skeleton.AnimationState.SetAnimation(0, wanted, true);
    }

    public static void ClearAll()
    {
        for (int i = 0; i < All.Count; i++) if (All[i] != null) Object.Destroy(All[i].gameObject);
        All.Clear();
    }

    // ══════════════ W3.3：门客运行时 HP / 倒地 / 复活 ══════════════
    /// <summary>
    /// 血量上限 = `RetainerConfig.Hp` + `EffectAggregator` 的 **EffectType=4 / Target=1** 加成。
    /// **懒解析**：`memberId` 是 Instantiate 之后由 `PlayerSpawner.SpawnAllies` 写入的，Awake 里还是 0。
    /// 取不到模板 / 服务异常 → Warn 并用 100。
    /// </summary>
    public int MaxHp { get { EnsureMaxHp(); return _maxHp + _runHealthBonus; } }

    private int _runHealthBonus;
    /// <summary>Freeze lazy current HP first. Raising maximum never heals or revives.</summary>
    public void SetRunHealthBonus(int bonus)
    {
        int current = Hp;
        _runHealthBonus = Mathf.Max(0, bonus);
        _hp = Mathf.Min(current, MaxHp); _hpInited = true;
    }

    /// <summary>倒地时的半透明 alpha（Spine 材质没有 `_Color`，用 `skeleton.A`）。</summary>
    public const float DownedAlpha = 0.4f;

    private int _maxHp = 100;
    private bool _maxHpInited;
    private int _hp;
    private bool _hpInited;
    private float _invulnUntil;

    /// <summary>是否已倒地（不可操作 / 不可被攻击 / 不跟随）。</summary>
    public bool IsDowned { get; private set; }

    /// <summary>当前 HP（首次访问时初始化为 MaxHp）。</summary>
    public int Hp
    {
        get { if (!_hpInited && !IsDowned) { _hp = MaxHp; _hpInited = true; } return _hp; }
    }

    /// <summary>按 `memberId` 把 MaxHp 解析成「Retainer 表值 + 家族树生命加成」（只解析一次）。</summary>
    public void EnsureMaxHp()
    {
        if (_maxHpInited) return;
        _maxHpInited = true;

        int tableHp = 0;
        int bonusHp = 0;
        try
        {
            RetainerService service = RetainerService.Instance;
            RetainerSave retainer;
            if (service != null && service.TryGet(memberId, out retainer))
            {
                RetainerConfig definition = service.GetDefinition(retainer.protoId);
                if (definition != null) tableHp = (int)definition.Hp;
                bonusHp = service.BonusHp;
            }
        }
        catch (System.Exception exception)
        {
            ChaosLog.Warn(LogChannel.UI, "[门客] " + AllyName + " 读取 Retainer 表异常：" + exception.Message);
        }

        if (tableHp <= 0)
        {
            _maxHp = 100 + bonusHp;
            ChaosLog.Warn(LogChannel.UI, "[门客] " + AllyName + " 取不到 Retainer.Hp（memberId=" + memberId
                + "）→ 基数用 100，MaxHp=" + _maxHp + "（加成 " + bonusHp + "）");
        }
        else
        {
            _maxHp = tableHp + bonusHp;
            ChaosLog.Info(LogChannel.UI, "[门客] " + AllyName + " MaxHp=" + _maxHp
                + "（Retainer.Hp=" + tableHp + " + 家族树生命加成=" + bonusHp + "）");
        }
    }

    /// <summary>受击：0.5s 无敌帧；归零 → 倒地。</summary>
    public void TakeDamage(int damage)
    {
        if (IsDowned || damage <= 0) return;
        if (Time.time < _invulnUntil) return;
        _invulnUntil = Time.time + 0.5f;
        CancelAttack();
        if (!_hpInited) { _hp = MaxHp; _hpInited = true; }
        _hp -= damage;
        ChaosLog.Info(LogChannel.UI, "[门客] " + AllyName + " 受伤 -" + damage + " → HP " + _hp + "/" + MaxHp);
        if (_hp <= 0) Down();
    }

    /// <summary>GM/验收：无视 0.5s 无敌帧造成伤害（普通受击走 `TakeDamage`）。</summary>
    public void TakeDamageDirect(int damage)
    {
        if (IsDowned || damage <= 0) return;
        CancelAttack();
        int before = Hp;   //触发懒初始化
        _hp = Mathf.Max(0, before - damage);
        _hpInited = true;
        ChaosLog.Info(LogChannel.UI, "[门客] " + AllyName + " 受伤 -" + damage + " → HP " + _hp + "/" + MaxHp + "（GM 直伤）");
        if (_hp <= 0) Down();
    }

    /// <summary>GM/验收：无视无敌帧直接打空 HP 并倒地。</summary>
    public void ForceDown()
    {
        if (IsDowned) return;
        EnsureMaxHp();
        _hp = 0;
        _hpInited = true;
        Down();
    }

    /// <summary>倒地：停止移动/攻击、不可操作（禁用组件）、从敌人目标中移除、半透明。</summary>
    public void Down()
    {
        if (IsDowned) return;
        EnsureMaxHp();
        IsDowned = true;
        _hp = 0;
        _hpInited = true;
        StopMoving();
        CancelAttack();
        if (_body != null) _body.velocity = Vector2.zero;
        SetDownedVisual(true);
        enabled = false;   //停掉 Update（不跟随、不攻击）
        ChaosLog.Warn(LogChannel.UI, "[门客] " + AllyName + " 倒地（不可操作/不可被攻击/不跟随）位置="
            + transform.position.x.ToString("F2") + "," + transform.position.y.ToString("F2"));

        //W3.3：受控角色已在观战 + 这是最后一个站着的门客 → 全队倒下 → 失败
        RunEncounter.CheckTeamWipe();
    }

    /// <summary>复活：回到指定 HP 并恢复跟随/攻击。</summary>
    public void Revive(int hp)
    {
        EnsureMaxHp();
        IsDowned = false;
        _hp = Mathf.Clamp(hp, 1, MaxHp);
        _hpInited = true;
        _invulnUntil = Time.time + 0.5f;
        SetDownedVisual(false);
        enabled = true;
        ChaosLog.Success(LogChannel.UI, "[门客] " + AllyName + " 复活 HP " + _hp + "/" + MaxHp);
    }

    /// <summary>倒地视觉：Spine 没有 `_Color` → 改 `skeleton.A`（0.4 = 半透明）。</summary>
    public void SetDownedVisual(bool downed)
    {
        if (_skeleton == null) _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton == null || _skeleton.skeleton == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[门客] " + AllyName + " 找不到 SkeletonAnimation，倒地半透明不可用（仅日志标记）");
            return;
        }
        _skeleton.skeleton.A = downed ? DownedAlpha : 1f;
        ChaosLog.Info(LogChannel.UI, "[门客] " + AllyName + " 倒地视觉 alpha=" + (downed ? DownedAlpha : 1f));
    }
}
