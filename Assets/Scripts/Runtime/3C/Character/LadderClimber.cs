using RunMap;
using UnityEngine;

/// <summary>
/// 局内共享攀梯 motor：玩家、队友、敌人共用。
///
/// 坐标约定（与 <see cref="MapLadder"/> 一致）：
/// · 端点是【脚底】格，世界坐标为 (cell.x + .5, cell.y + .03)；
/// · 先以物理速度对齐最近端点，再沿竖直梯身与顶部落脚口移动；
/// · 只接管重力标量与速度，不关闭碰撞、不改 bodyType，也不靠传送完成路径。
///
/// 输入/AI 分工：
/// · 玩家由 PlayerLocomotion 调 <see cref="Drive"/>（内部）和 <see cref="TryAutoAttach"/>（内部），
///   E 交互由 RunLadderInteractable 调 <see cref="TryAttach"/> / <see cref="Detach"/>；
/// · AI 每帧调 <see cref="Navigate"/>，与玩家自动/手动设置无关；
/// · motor 自身不监听任何按键，不依赖设置。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class LadderClimber : MonoBehaviour
{
    /// <summary>脚底到入口端点的最大附着距离（世界单位）；一格相邻（1.0）可附着，两格不行。</summary>
    internal const float AttachRange = 1.2f;
    /// <summary>上下输入低于该值视为松开，用于跳离后的自动再附着锁。</summary>
    private const float ReleaseDeadzone = 0.2f;
    /// <summary>跳离后的短暂冷却，避免同一帧/连续帧立刻吸附。</summary>
    private const float JumpReattachCooldown = 0.3f;
    /// <summary>端点自动离开后的短冷却；仍需松开上下键。</summary>
    private const float EndpointReattachCooldown = 0.15f;
    // 梯顶横移时给脚底留出越过瓦片棱边的余量。
    private const float LandingClearance = .14f;
    private const float EndpointTolerance = .055f;

    [SerializeField] private float jumpOffSpeed = 11f;

    private Rigidbody2D _body;
    private Collider2D _collider;
    private PlayerHealth _health;
    private NaturalMapPlan _map;
    private float _savedGravityScale;
    private bool _gravitySaved;
    private Vector2 _bottomFoot;
    private Vector2 _topFoot;
    private float _drive;
    private bool _navigate;
    private bool _navigateUp;
    private bool _climbNeutral = true;
    private float _reattachBlockedUntil;
    private MapLadder _completedLadder;
    private bool _completedUpward;
    private bool _aligning;
    private bool _atTopTurn;
    private Vector2 _alignPoint, _lastFoot;
    private float _lastProgressAt;
    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[16];

    /// <summary>当前是否在梯上。</summary>
    public bool IsClimbing { get; private set; }
    /// <summary>当前梯子；不在梯上时为 null。</summary>
    public MapLadder Current { get; private set; }

    /// <summary>自动再附着就绪：已松开上下键、冷却结束且当前不在梯上。</summary>
    internal bool AutoAttachReady
    {
        get { return !IsClimbing && _climbNeutral && Time.time >= _reattachBlockedUntil; }
    }

    internal bool IsAttachedTo(MapLadder ladder)
    {
        return IsClimbing && Current != null && ladder != null && ReferenceEquals(Current, ladder);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (!IsClimbing) return;
        // 切图/地图重建后旧 plan 立即失效；倒地（包括不经过接触伤害的 GM 入口）也退出。
        if (!IsMapCurrent() || (_health != null && _health.IsDowned)) Detach(false);
    }

    private void FixedUpdate()
    {
        if (!IsClimbing) return;
        if (_body == null || !IsMapCurrent())
        {
            Detach(false);
            return;
        }

        float direction = _navigate ? (_navigateUp ? 1f : -1f) : Mathf.Clamp(_drive, -1f, 1f);
        if (!_body.simulated) return;
        if (Mathf.Abs(direction) < ReleaseDeadzone)
        {
            // 无输入停留在梯上：重力已为 0，再清速度防止残余冲量滑走。
            _body.velocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _lastProgressAt=Time.time;
            return;
        }

        Vector2 feet=Feet;
        if((feet-_lastFoot).sqrMagnitude>.000004f){_lastFoot=feet;_lastProgressAt=Time.time;}
        if(Time.time-_lastProgressAt>1.2f){Detach(false);return;}
        if(_aligning)
        {
            if(Vector2.Distance(feet,_alignPoint)>.04f){MoveFoot(_alignPoint);return;}
            _aligning=false;
        }
        bool upward = direction > 0f;
        Vector2 destination = upward ? _topFoot : _bottomFoot;
        if (Vector2.Distance(feet, destination) < EndpointTolerance)
        {
            RecordArrival(upward);
            Detach();
            return;
        }

        // 梯顶按物理落脚顺序行进。先升到棱边上方、横移、最后降到落脚点。
        // 明确保留转弯状态，避免脚底偏离折线时反复退回梯身。
        float cornerY = _topFoot.y + LandingClearance;
        if (upward)
        {
            if (!_atTopTurn && feet.y < cornerY - .025f)
            {
                MoveFoot(new Vector2(_bottomFoot.x, cornerY));
                return;
            }
            _atTopTurn = true;
            if (Mathf.Abs(feet.x - _topFoot.x) >= .04f)
                MoveFoot(new Vector2(_topFoot.x, cornerY));
            else
                MoveFoot(_topFoot);
            return;
        }
        if (_atTopTurn)
        {
            if (feet.y < cornerY - .025f)
            {
                MoveFoot(new Vector2(feet.x, cornerY));
                return;
            }
            if (Mathf.Abs(feet.x - _bottomFoot.x) >= .04f)
            {
                MoveFoot(new Vector2(_bottomFoot.x, cornerY));
                return;
            }
            _atTopTurn = false;
        }
        MoveFoot(_bottomFoot);
    }

    private void OnDisable()
    {
        if (IsClimbing) Detach(false);
    }

    private void OnDestroy()
    {
        if (IsClimbing) Detach(false);
    }

    /// <summary>
    /// 附着到梯子的最近入口。要求梯子属于当前地图、角色未倒地，且脚底距最近端点不超过
    /// <see cref="AttachRange"/>；成功后保存 gravityScale 并置 0，对齐脚底到该端点。
    /// </summary>
    public bool TryAttach(MapLadder ladder)
    {
        // 组件被禁用时不允许进入：否则重力已置 0 却不再有 FixedUpdate 驱动，角色会悬空。
        if (!isActiveAndEnabled || IsClimbing || ladder == null || _body == null) return false;
        if (_health != null && _health.IsDowned) return false;
        if (!NaturalMapRuntime.IsReady || NaturalMapRuntime.Current == null) return false;

        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (!ContainsLadder(plan, ladder)) return false;
        // Top 必须在 Bottom 上方，梯顶可在梯身旁保留落脚平台。
        if (ladder.Top.y <= ladder.Bottom.y) return false;

        Vector2 feet = Feet;
        Vector2 bottomFoot = FootOf(ladder.Bottom);
        Vector2 topFoot = FootOf(ladder.Top);
        float bottomDistance = (feet - bottomFoot).sqrMagnitude;
        float topDistance = (feet - topFoot).sqrMagnitude;
        if (Mathf.Min(bottomDistance, topDistance) > AttachRange * AttachRange) return false;

        _map = plan;
        _bottomFoot = bottomFoot;
        _topFoot = topFoot;
        _savedGravityScale = _body.gravityScale;
        _gravitySaved = true;
        _body.gravityScale = 0f;
        _body.velocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.WakeUp();

        IsClimbing = true;
        Current = ladder;
        _drive = 0f;
        _navigate = false;
        _climbNeutral = false;
        _atTopTurn = topDistance < bottomDistance;
        _alignPoint = _atTopTurn ? _topFoot : _bottomFoot;
        _aligning=true;_lastFoot=feet;_lastProgressAt=Time.time;
        return true;
    }

    /// <summary>
    /// AI 每帧调用：未上梯时先接近并附着，然后朝目标端爬，到达后自动退出。
    /// 与玩家的自动/手动设置完全无关。
    /// </summary>
    public bool Navigate(MapLadder ladder, bool upward)
    {
        if (ladder == null) return false;

        if (!IsClimbing)
        {
            // 刚在目标端离开且人还停在入口范围内时，同一梯子/同一方向的重复调用不再吸附；
            // 换方向、或离开该入口超过附着距离后才允许重新进入，避免 AI 在端点来回空转。
            if (Time.time < _reattachBlockedUntil) return false;
            if (_completedUpward == upward && ReferenceEquals(_completedLadder, ladder))
            {
                Vector2 entry = FootOf(upward ? ladder.Top : ladder.Bottom);
                if ((Feet - entry).sqrMagnitude <= AttachRange * AttachRange) return false;
            }
            if (!TryAttach(ladder)) return false;
        }
        else if (!IsAttachedTo(ladder))
        {
            return false;
        }

        _navigate = true;
        _navigateUp = upward;
        _drive = 0f;
        _climbNeutral = false;
        return true;
    }

    /// <summary>
    /// 离开梯子并恢复进入前的 gravityScale。jump=true 时施加向上跳离速度；
    /// 两种情况都会锁住自动再附着，直到上下键松开并经过短暂冷却。
    /// </summary>
    public void Detach(bool jump = false)
    {
        if (!IsClimbing) return;

        IsClimbing = false;
        Current = null;
        _drive = 0f;
        _navigate = false;
        _map = null;

        if (_body != null)
        {
            if (_gravitySaved) _body.gravityScale = _savedGravityScale;
            if (jump)
            {
                Vector2 velocity = _body.velocity;
                velocity.x = 0f;
                velocity.y = Mathf.Max(velocity.y, jumpOffSpeed);
                _body.velocity = velocity;
                _body.WakeUp();
            }
            else
                _body.velocity = Vector2.zero;
        }
        _gravitySaved = false;

        _climbNeutral = false;
        _reattachBlockedUntil = Time.time + (jump ? JumpReattachCooldown : EndpointReattachCooldown);
    }

    /// <summary>玩家每帧写入攀爬方向（-1 下 / 1 上）；非梯上调用无效。</summary>
    internal void Drive(float vertical)
    {
        if (IsClimbing) _drive = Mathf.Clamp(vertical, -1f, 1f);
    }

    /// <summary>玩家每帧上报上下输入；只有回到死区才视为“已松开”。</summary>
    internal void NoteClimbInput(float vertical)
    {
        if (Mathf.Abs(vertical) < ReleaseDeadzone) _climbNeutral = true;
    }

    /// <summary>
    /// 玩家自动模式：按上下方向找最近的对应入口并附着。
    /// 向上只认 Bottom 入口，向下只认 Top 入口，避免在端点立即又吸附成环。
    /// </summary>
    internal bool TryAutoAttach(float vertical)
    {
        if (!AutoAttachReady || Mathf.Abs(vertical) < ReleaseDeadzone) return false;
        if (!NaturalMapRuntime.IsReady || NaturalMapRuntime.Current == null) return false;

        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (plan.Ladders == null) return false;
        Vector2 feet = Feet;
        bool upward = vertical > 0f;
        MapLadder best = null;
        float bestDistance = AttachRange * AttachRange;

        for (int i = 0; i < plan.Ladders.Count; i++)
        {
            MapLadder ladder = plan.Ladders[i];
            if (ladder == null || ladder.Top.y <= ladder.Bottom.y) continue;
            Vector2 entrance = FootOf(upward ? ladder.Bottom : ladder.Top);
            float d = (entrance - feet).sqrMagnitude;
            if (d > AttachRange * AttachRange) continue;
            if (best != null)
            {
                if (d > bestDistance + 0.0001f) continue;
                if (Mathf.Abs(d - bestDistance) <= 0.0001f
                    && string.CompareOrdinal(ladder.StableId, best.StableId) >= 0) continue;
            }
            best = ladder;
            bestDistance = d;
        }

        return best != null && TryAttach(best);
    }

    private void RecordArrival(bool upward)
    {
        _completedLadder = Current;
        _completedUpward = upward;
    }

    private bool IsMapCurrent()
    {
        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (!NaturalMapRuntime.IsReady || plan == null || !ReferenceEquals(plan, _map)) return false;
        return ContainsLadder(plan, Current);
    }

    private static bool ContainsLadder(NaturalMapPlan plan, MapLadder ladder)
    {
        if (plan == null || ladder == null || plan.Ladders == null) return false;
        for (int i = 0; i < plan.Ladders.Count; i++)
            if (ReferenceEquals(plan.Ladders[i], ladder)) return true;
        return false;
    }

    private static Vector2 FootOf(Vector2Int cell)
    {
        return new Vector2(cell.x + 0.5f, cell.y + 0.03f);
    }

    private Vector2 Feet
    {
        get
        {
            if (_collider != null)
            {
                Bounds bounds = _collider.bounds;
                return new Vector2(bounds.center.x, bounds.min.y);
            }
            return transform.position;
        }
    }

    private void MoveFoot(Vector2 foot)
    {
        if (_body == null) return;
        Vector2 delta=foot-Feet;
        float speed=Current==null?3.5f:Mathf.Max(.1f,Current.Speed);
        Vector2 step=Vector2.ClampMagnitude(delta,speed*Time.fixedDeltaTime);
        if(_collider!=null&&step.sqrMagnitude>.000001f)
        {
            var filter=new ContactFilter2D();filter.useTriggers=false;
            int count=_collider.Cast(step.normalized,filter,_castHits,step.magnitude+.015f);
            float allowed=step.magnitude;
            for(int i=0;i<count;i++)
            {
                var hit=_castHits[i];if(hit.collider==null||hit.collider.attachedRigidbody==_body)continue;
                if(hit.collider.attachedRigidbody!=null&&hit.collider.attachedRigidbody.bodyType==RigidbodyType2D.Dynamic)continue;
                if(Vector2.Dot(hit.normal,step.normalized)<-.01f)allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-.01f));
            }
            step=step.normalized*allowed;
        }
        _body.velocity = step/Mathf.Max(.001f,Time.fixedDeltaTime);
        _body.angularVelocity = 0f;
    }
}
