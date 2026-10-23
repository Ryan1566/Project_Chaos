using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 受控角色运动（W3.0 spike）：水平移动 / 跳跃 / 朝向 / 动画切换。
///
/// 输入统一读取InputManager的Move/Jump动作，和设置面板重绑定共用运行时动作表。
/// 扎营/菜单快捷键由局级GameplayShortcutDriver处理，角色倒地后仍可打开菜单。
///
/// ══════════════ 动画只有 3 个 ══════════════
/// `Idle_Normal` / `Run_Normal` / `Walk_Normal` —— **没有跳跃/攻击动画**，
/// 所以跳跃/攻击期间**保持当前动画**（占位表现，见 PlayerAttack）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLocomotion : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 11f;
    /// <summary>脚底地面检测半径。</summary>
    public float groundCheckRadius = 0.16f;
    /// <summary>脚底相对根节点的偏移。</summary>
    public Vector2 feetOffset = new Vector2(0f, -0.05f);

    private const float IdleThreshold = 0.1f;

    private Rigidbody2D _body;
    private SkeletonAnimation _skeleton;
    private int _facing = 1;
    private bool _isGrounded;
    private bool _jumpRequested;
    private string _currentAnimation;

    /// <summary>朝向：1 = 右，-1 = 左。</summary>
    public int Facing { get { return _facing; } }
    public bool IsGrounded { get { return _isGrounded; } }
    public Vector2 Velocity { get { return _body == null ? Vector2.zero : _body.velocity; } }
    public SkeletonAnimation Skeleton { get { return _skeleton; } }

    /// <summary>外部（SpawnPoint 等）指定初始朝向。</summary>
    public void SetFacing(int facing)
    {
        if (facing == 0) return;
        _facing = facing > 0 ? 1 : -1;
        ApplyFacing();
    }

    /// <summary>
    /// **调试/GM 用**：直接注入运动输入（没有真实键盘时验证运动/动画）。
    /// 真实输入存在时会被覆盖回去；仅在 `duration` 秒内生效。
    /// </summary>
    public void InjectInput(float horizontal, bool jump, float duration = 0.3f)
    {
        _injectedInput = Mathf.Clamp(horizontal, -1f, 1f);
        _injectedInputUntil = Time.time + duration;
        if (jump) _jumpRequested = true;
        SetFacing(_injectedInput >= 0f ? 1 : -1);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton != null)
        {
            _skeleton.AnimationState.SetAnimation(0, "Idle_Normal", true);
            _currentAnimation = "Idle_Normal";
        }
        else
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 子节点里找不到 SkeletonAnimation（动画不可用）");
        }
    }

    private void Update()
    {
        if (!InputManager.Instance.CanReadGameplayInput) { ClearInput(); UpdateAnimation(); return; }
        float horizontal = InputManager.Instance.ReadMove();
        if (InputManager.Instance.WasGameplayPressed(InputManager.ActionJump)) _jumpRequested = true;

        if (Time.time < _injectedInputUntil) horizontal = _injectedInput;   //调试注入优先
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        _horizontalInput = horizontal;

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            int newFacing = horizontal > 0f ? 1 : -1;
            if (newFacing != _facing) { _facing = newFacing; ApplyFacing(); }
        }

        UpdateAnimation();
    }

    private InputManager _inputManager;
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        _inputManager = InputManager.Instance;
        _inputManager.GameplayInputBlockedChanged += OnInputBlocked;
        ClearInput();
    }
    private void OnDisable()
    {
        if (_inputManager != null) _inputManager.GameplayInputBlockedChanged -= OnInputBlocked;
        ClearInput();
    }
    private void OnInputBlocked(bool blocked) { if (blocked) ClearInput(); }
    private void ClearInput()
    {
        _horizontalInput = 0;
        _jumpRequested = false;
        _injectedInput = 0;
        _injectedInputUntil = 0;
        if (_body != null) _body.velocity = new Vector2(0, _body.velocity.y);
    }

    private float _horizontalInput;

    //调试注入（GM/自动化验证用：没有真实键盘时也能驱动运动与动画）
    private float _injectedInput;
    private float _injectedInputUntil;

    private void FixedUpdate()
    {
        if (_body == null) return;
        if (InputManager.Instance.GameplayInputBlocked) { ClearInput(); return; }

        _isGrounded = CheckGrounded();

        //水平移动
        Vector2 velocity = _body.velocity;
        velocity.x = _horizontalInput * moveSpeed;

        //跳跃（落地才可跳）
        if (_jumpRequested && _isGrounded)
        {
            velocity.y = jumpForce;
            ChaosLog.Info(LogChannel.UI, "[玩家] 跳跃（无跳跃动画，占位表现）");
        }
        _jumpRequested = false;

        _body.velocity = velocity;
    }

    /// <summary>脚底圆形检测：忽略自身碰撞体与触发器。</summary>
    /// <summary>W3.1f：改用胶囊底部 BoxCast（与门客同一套）。</summary>
    private bool CheckGrounded()
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

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * _facing;
        transform.localScale = scale;
    }

    /// <summary>Idle/Run 切换（无跳跃/攻击动画 → 这两种状态保持当前动画）。</summary>
    private void UpdateAnimation()
    {
        if (_skeleton == null) return;

        float vx = Mathf.Abs(_body == null ? 0f : _body.velocity.x);
        string wanted = vx < IdleThreshold ? "Idle_Normal" : "Run_Normal";
        if (wanted == _currentAnimation) return;

        _currentAnimation = wanted;
        _skeleton.AnimationState.SetAnimation(0, wanted, true);
    }
}
