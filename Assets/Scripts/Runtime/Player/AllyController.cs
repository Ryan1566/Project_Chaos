using System.Collections.Generic;
using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// AI 门客（W3.1e 修正版）。
///
/// **本轮修的两件事**：
/// 1. **跨层跟随**（W3.1d 遗留：玩家上平台后门客留在下层 y≈1.01 不上来）：
///    - 玩家在上方（`playerY - allyY > 1.2`）且在地面 → **跳**（跳跃冷却 0.6s，不连续弹）；
///    - 玩家在下方（`dy < -1.5`）→ 朝玩家水平走，**自然下落**；前方没地面就停下等；
///    - **跟丢兜底**：`|dx|>10` 或 `|dy|>6` 持续 **>2s** → 传送到玩家身边（±1.2 格）+ Warn + `TeleportCount`。
/// 2. **日志命名统一**：一律用 `AllyName` = `Ally_{位序}(id={门客id})`，
///    攻击/掉落/传送日志与 `来源=` 全部一致（修掉 W3.1d 的 `Ally_2` vs `来源=Ally_1` 不一致）。
///
/// 其它：跳跃仅在**前方有墙**或**玩家在上层**时触发；跟随目标 x 钳制在房间矩形内；
/// 前方无地面停下；攻击目标需存活且同房间；**本轮 AI 门客不掉血**（敌人只打受控玩家）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AllyController : MonoBehaviour
{
    /// <summary>场上所有 AI 门客（撤离/切场景统一清理）。</summary>
    public static readonly List<AllyController> All = new List<AllyController>();

    public int followIndex;                 //队伍位序 1、2、3
    public uint memberId;                   //门客 id（存档里的 id）
    public float moveSpeed = 5.5f;
    public float jumpForce = 11f;
    public float attackRange = 2.5f;
    public float attackCooldown = 0.6f;
    public int damage = 6;
    public float followDeadZone = 0.6f;
    public float probeDistance = 0.6f;
    public float fallThresholdY = -20f;
    /// <summary>跳跃冷却（跨层跟随 & 上台阶共用，避免连续弹跳）。</summary>
    public float jumpCooldown = 0.6f;
    public float lostHorizontal = 10f;
    public float lostVertical = 6f;
    public float lostSeconds = 2f;

    private Rigidbody2D _body;
    private SkeletonAnimation _skeleton;
    private float _nextAttack;
    private float _nextJump;
    private string _currentAnimation;
    private int _facing = 1;
    private EnemyPlaceholder _target;
    private float _lostSince = -1f;

    /// <summary>统一显示名：`Ally_{位序}(id={门客id})`。</summary>
    public string AllyName { get { return "Ally_" + followIndex + "(id=" + memberId + ")"; } }
    public int FallCount { get; private set; }
    public int TeleportCount { get; private set; }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton != null) _skeleton.AnimationState.SetAnimation(0, "Idle_Normal", true);
        _currentAnimation = "Idle_Normal";
    }

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void Update()
    {
        GameObject controlled = PlayerSpawner.CurrentPlayer;
        if (controlled == null) return;

        if (transform.position.y < fallThresholdY)
        {
            FallCount++;
            TeleportNear(controlled, "掉出地图");
            return;
        }

        float dx = controlled.transform.position.x - transform.position.x;
        float dy = controlled.transform.position.y - transform.position.y;

        //跟丢兜底：持续 >2s 才传送（避免抖动误判）
        if (Mathf.Abs(dx) > lostHorizontal || Mathf.Abs(dy) > lostVertical)
        {
            if (_lostSince < 0f) _lostSince = Time.time;
            else if (Time.time - _lostSince > lostSeconds)
            {
                TeleportCount++;
                TeleportNear(controlled, "跟丢 " + (Time.time - _lostSince).ToString("F1") + "s");
                return;
            }
        }
        else _lostSince = -1f;

        if (_target == null || _target.IsDead) _target = FindNearestEnemy();
        if (_target != null && Vector2.Distance(_target.transform.position, transform.position) > attackRange * 1.6f) _target = null;

        if (_target != null)
        {
            if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
            SetFacing(_target.transform.position.x >= transform.position.x ? 1 : -1);
            if (Time.time >= _nextAttack)
            {
                _nextAttack = Time.time + attackCooldown;
                _target.TakeDamage(damage);
                ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」攻击「" + _target.name + "」，伤害 " + damage
                    + "（来源=" + AllyName + "）");
            }
            UpdateAnimation(0f);
            return;
        }

        float wantedX = controlled.transform.position.x + (followIndex == 3 ? 2f : (followIndex == 2 ? 0f : -2f));
        Rect room;
        if (RunEncounter.TryGetRoomRect(controlled.transform.position, out room))
            wantedX = Mathf.Clamp(wantedX, room.xMin + 1f, room.xMax - 1f);

        float targetDx = wantedX - transform.position.x;
        int dir = targetDx >= 0f ? 1 : -1;

        //玩家在上方 → 跳（冷却 0.6s，且要在地面）
        if (dy > 1.2f && Time.time >= _nextJump && IsGrounded())
        {
            _nextJump = Time.time + jumpCooldown;
            if (_body != null) _body.velocity = new Vector2(_body.velocity.x, jumpForce);
            ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」起跳追玩家（dy=" + dy.ToString("F2") + "）");
        }

        bool playerBelow = dy < -1.5f;
        if (Mathf.Abs(targetDx) > followDeadZone || playerBelow)
        {
            if (!GroundAhead(dir) && !playerBelow)
            {
                if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
                UpdateAnimation(0f);
                return;
            }

            if (_body != null) _body.velocity = new Vector2(dir * moveSpeed, _body.velocity.y);
            SetFacing(dir);

            if (Time.time >= _nextJump && WallAhead(dir) && IsGrounded())
            {
                _nextJump = Time.time + jumpCooldown;
                if (_body != null) _body.velocity = new Vector2(_body.velocity.x, jumpForce);
            }
            UpdateAnimation(moveSpeed);
        }
        else
        {
            if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
            UpdateAnimation(0f);
        }
    }

    private void TeleportNear(GameObject player, string reason)
    {
        float offset = followIndex == 3 ? 1.2f : (followIndex == 2 ? 0f : -1.2f);
        transform.position = player.transform.position + new Vector3(offset, 0.8f, 0f);
        if (_body != null) _body.velocity = Vector2.zero;
        _lostSince = -1f;
        ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」传送回玩家身边（" + reason + "）");
    }

    private bool IsGrounded()
    {
        Vector2 feet = (Vector2)transform.position + new Vector2(0f, -0.05f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(feet, 0.16f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i].isTrigger) continue;
            if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform)) continue;
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
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 前方是否有墙：只把"命中点高于脚底 ≥0.3 且低于头顶 1.8"的命中当墙
    /// （更严格的胶囊重叠判定成本更高，按 Lead 允许的简化保留，注释已写明）。
    /// </summary>
    private bool WallAhead(int dir)
    {
        if (dir == 0) return false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, new Vector2(dir, 0f), probeDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            float relative = hits[i].point.y - transform.position.y;
            if (relative > 0.3f && relative < 1.8f) return true;
        }
        return false;
    }

    private EnemyPlaceholder FindNearestEnemy()
    {
        EnemyPlaceholder[] enemies = Object.FindObjectsOfType<EnemyPlaceholder>();
        EnemyPlaceholder best = null;
        float bestDistance = attackRange;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyPlaceholder enemy = enemies[i];
            if (enemy == null || enemy.IsDead) continue;
            if (!RunEncounter.IsPlayerInRoom(enemy.roomId)) continue;
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

    /// <summary>撤离/切场景：销毁所有 AI 门客。</summary>
    public static void ClearAll()
    {
        for (int i = 0; i < All.Count; i++) if (All[i] != null) Object.Destroy(All[i].gameObject);
        All.Clear();
    }
}
