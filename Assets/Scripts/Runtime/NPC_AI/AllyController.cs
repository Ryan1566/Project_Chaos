using System.Collections.Generic;
using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// AI 门客（W3.1q 换设计版）。
///
/// **本轮把"玩家在上方就跳"整条删掉**（W3.1p 实测：17 次起跳、0 次防抖生效、靠传送才上去）。
/// 新设计：
/// 1. **跳跃只剩一个理由**：前方有墙挡路（`FindWall`）**且**墙顶上方 0.6×0.6 的严格 OverlapBox 命中真实地面；
/// 2. **玩家在上方** → 门客**贴到玩家正下方**（目标 x = 玩家 x），每 1s 一条低频日志；
/// 3. **短传送兜底**：`dy > 1.5` 持续 1.5s / 卡在平台下（`|dx|<0.8 且 dy>1.0`）持续 1.5s / 跟丢 >2s → 传送 + Warn；
/// 4. 起跳间隔 ≥ 0.8s；`_futileJumps >= 3` → **停跳 3s** + Warn。
///
/// 其它保持：跟随偏移 `{-2.6,+2.6,-4.3}`、死区 1.6/1.4、速度 5.0 + 加速度、目标 x 钳制房间内、
/// 攻击 2.5 格内每 0.6s 打 6（同房间）、**AI 门客不掉血**、掉落兜底回玩家身边。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AllyController : MonoBehaviour
{
    public static readonly List<AllyController> All = new List<AllyController>();

    public int followIndex;
    public uint memberId;
    public float moveSpeed = 5.0f;
    public float acceleration = 30f;
    public float jumpForce = 11f;
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
    private float _nextNearLog;
    private float _stopJumpUntil;
    private string _currentAnimation;
    private int _facing = 1;
    private EnemyPlaceholder _target;
    private float _lostSince = -1f;
    private float _highDySince = -1f;
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
    private void OnDisable() { All.Remove(this); }
    private void OnDestroy() { All.Remove(this); }

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

        //复查上次起跳是否"白跳"（起跳后 1s 比较 y）
        if (_jumpCheckAt > 0f && Time.time >= _jumpCheckAt)
        {
            _jumpCheckAt = -1f;
            if (Mathf.Abs(transform.position.y - _jumpStartY) < 0.3f)
            {
                _futileJumps++;
                ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」起跳无效（Δy<0.3），累计 " + _futileJumps + "/3");
                if (_futileJumps >= 3)
                {
                    _stopJumpUntil = Time.time + 3f;
                    StopJumpWarnCount++;
                    ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」连续 3 次起跳没升高，停跳 3s");
                    _futileJumps = 0;
                }
            }
            else _futileJumps = 0;
        }

        bool playerAbove = dy > teleportDy;

        //② 玩家在上方 → 贴正下方（1s 一条低频日志）
        if (playerAbove && Time.time >= _nextNearLog)
        {
            _nextNearLog = Time.time + 1f;
            NearBelowCount++;
            ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」贴近玩家正下方（dx=" + dx.ToString("F2")
                + " dy=" + dy.ToString("F2") + "）");
        }

        //③ 短传送兜底
        if (playerAbove) { if (_highDySince < 0f) _highDySince = Time.time; }
        else _highDySince = -1f;

        bool stuckUnderPlatform = Mathf.Abs(dx) < 0.8f && dy > 1.0f;
        if (stuckUnderPlatform) { if (_stuckSince < 0f) _stuckSince = Time.time; }
        else _stuckSince = -1f;

        if ((_highDySince > 0f && Time.time - _highDySince > teleportHoldSeconds)
            || (_stuckSince > 0f && Time.time - _stuckSince > teleportHoldSeconds))
        {
            TeleportCount++;
            TeleportNear(controlled, "垂直差 " + dy.ToString("F2") + " 持续 >" + teleportHoldSeconds + "s");
            return;
        }

        if (Mathf.Abs(dx) > 10f || Mathf.Abs(dy) > 6f)
        {
            if (_lostSince < 0f) _lostSince = Time.time;
            else if (Time.time - _lostSince > 2f)
            {
                TeleportCount++;
                TeleportNear(controlled, "跟丢 >2s");
                return;
            }
        }
        else _lostSince = -1f;

        //攻击
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
                ChaosLog.Info(LogChannel.UI, "[门客] 「" + AllyName + "」攻击「" + _target.name + "」，伤害 " + damage + "（来源=" + AllyName + "）");
            }
            UpdateAnimation(0f);
            return;
        }

        //① 目标位置：玩家在上方 → 正下方；否则按队形偏移
        float wantedX = playerAbove
            ? controlled.transform.position.x
            : controlled.transform.position.x + FollowOffsets[Mathf.Clamp(followIndex - 1, 0, FollowOffsets.Length - 1)];

        Rect room;
        if (RunEncounter.TryGetRoomRect(controlled.transform.position, out room))
            wantedX = Mathf.Clamp(wantedX, room.xMin + 1f, room.xMax - 1f);

        float targetDx = wantedX - transform.position.x;
        int dir = targetDx >= 0f ? 1 : -1;

        bool alreadyMoving = _body != null && Mathf.Abs(_body.velocity.x) > 0.1f;
        float deadZone = alreadyMoving ? 1.4f : 1.6f;

        if (Mathf.Abs(targetDx) > deadZone)
        {
            if (!GroundAhead(dir))
            {
                if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
                UpdateAnimation(0f);
                TryJumpIfBlocked(dir);
                return;
            }
            if (_body != null)
            {
                float vx = Mathf.MoveTowards(_body.velocity.x, dir * moveSpeed, acceleration * Time.deltaTime);
                _body.velocity = new Vector2(vx, _body.velocity.y);
            }
            SetFacing(dir);
            UpdateAnimation(moveSpeed);
            TryJumpIfBlocked(dir);
        }
        else
        {
            if (_body != null) _body.velocity = new Vector2(0f, _body.velocity.y);
            UpdateAnimation(0f);
            TryJumpIfBlocked(dir);
        }
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
            if (hits[i] == null || hits[i].isTrigger) continue;
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
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            float relative = hits[i].point.y - transform.position.y;
            if (relative > 0.3f && relative < 1.8f) return hits[i];
        }
        return default(RaycastHit2D);
    }

    private void TeleportNear(GameObject player, string reason)
    {
        float offset = followIndex == 3 ? 1.2f : (followIndex == 2 ? 0f : -1.2f);
        transform.position = player.transform.position + new Vector3(offset, 0.8f, 0f);
        if (_body != null) _body.velocity = Vector2.zero;
        _lostSince = -1f;
        _highDySince = -1f;
        _stuckSince = -1f;
        ChaosLog.Warn(LogChannel.UI, "[门客] 「" + AllyName + "」传送回玩家身边（" + reason + "）");
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
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
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
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
            return true;
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

    public static void ClearAll()
    {
        for (int i = 0; i < All.Count; i++) if (All[i] != null) Object.Destroy(All[i].gameObject);
        All.Clear();
    }
}
