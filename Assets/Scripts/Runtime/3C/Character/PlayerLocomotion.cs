using Chaos.Gameplay.Items;
using ChaosDebug;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 受控角色运动（W3.0 spike）：水平移动 / 跳跃 / 朝向 / 动画切换。
///
/// 输入统一读取InputManager的Move/Jump动作，和设置面板重绑定共用运行时动作表。
/// 扎营/菜单快捷键由局级GameplayShortcutDriver处理，角色倒地后仍可打开菜单。
///
/// 待机与移动片段由骨骼数据按动作语义解析。
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
    /// <summary>落地后允许的残余下降速度；高速擦过台阶圆角不算真正着陆。</summary>
    private const float LandingSettleSpeed = 0.5f;

    private Rigidbody2D _body;
    private PlayerSurfacePhysics _surface;
    private LadderClimber _ladder;
    private SkeletonAnimation _skeleton;
    private CharacterAttack _attack;
    private int _facing = 1;
    private bool _isGrounded;
    private bool _jumpRequested;
    /// <summary>本次腾空是否已用掉空中跳；不随面板开关或组件启停重置。</summary>
    private bool _airJumpUsed;

    /// <summary>朝向：1 = 右，-1 = 左。</summary>
    public int Facing { get { return _facing; } }
    public bool IsGrounded { get { return _isGrounded; } }
    public Vector2 Velocity { get { return _body == null ? Vector2.zero : _body.velocity; } }
    public SkeletonAnimation Skeleton { get { return _skeleton; } }

    /// <summary>外部（SpawnPoint 等）指定初始朝向。</summary>
    public void SetFacing(int facing)
    {
        if (facing == 0 || _attack?.IsAttacking == true) return;
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
        _surface = GetComponent<PlayerSurfacePhysics>();
        if (_surface == null) _surface = gameObject.AddComponent<PlayerSurfacePhysics>();
        _ladder = GetComponent<LadderClimber>();
        // 玩家默认具备攀梯 motor；队友/敌人由各自预制体挂同一组件后走 Navigate。
        if (_ladder == null) _ladder = gameObject.AddComponent<LadderClimber>();
        _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_skeleton != null)
            CharacterSpineClips.PlayLocomotion(_skeleton.AnimationState, _skeleton.Skeleton?.Data, false);
        else
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 子节点里找不到 SkeletonAnimation（动画不可用）");
        }
    }

    private void Update()
    {
        if (_attack == null) _attack = GetComponent<CharacterAttack>();
        if (_attack?.IsAttacking == true)
        {
            _horizontalInput = 0f;
            _jumpRequested = false;
            return;
        }
        if (_ladder != null && _ladder.IsClimbing) { UpdateLadderClimbing(); return; }
        if (!InputManager.Instance.CanReadGameplayInput) { ClearInput(); UpdateAnimation(); return; }
        float horizontal = InputManager.Instance.ReadMove();
        bool jumpPressed = InputManager.Instance.WasGameplayPressed(InputManager.ActionJump);
        if (jumpPressed) _jumpRequested = true;

        // 自动模式：接近入口后按 W/S 吸附；手动模式不在这里抢 E。
        // 跳键同帧不吸附，保证梯子旁正常起跳不回归。
        float climb = InputManager.Instance.ReadClimb();
        _ladder.NoteClimbInput(climb);
        if (!jumpPressed && Mathf.Abs(climb) > 0.2f && !LadderManualMode
            && _ladder.AutoAttachReady && _ladder.TryAutoAttach(climb))
        {
            _ladder.Drive(climb);
            _horizontalInput = 0f;
            _jumpRequested = false;
            UpdateAnimation();
            return;
        }

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

    /// <summary>
    /// 梯上分支：空格跳离；W/S 上下；菜单只阻止输入、不强制退出。
    /// 端点自动离开和受击/倒地退出由 LadderClimber / PlayerHealth 处理。
    /// </summary>
    private void UpdateLadderClimbing()
    {
        _horizontalInput = 0f;
        _jumpRequested = false;

        if (!InputManager.Instance.CanReadGameplayInput)
        {
            _ladder.Drive(0f);
            UpdateAnimation();
            return;
        }

        if (InputManager.Instance.WasGameplayPressed(InputManager.ActionJump))
        {
            _ladder.Detach(true);
            UpdateAnimation();
            return;
        }

        float climb = InputManager.Instance.ReadClimb();
        _ladder.NoteClimbInput(climb);
        _ladder.Drive(climb);
        UpdateAnimation();
    }

    /// <summary>只在“未上梯、决定是否自动吸附”时读取；切换设置不会打断正在进行的攀爬。</summary>
    private static bool LadderManualMode
    {
        get
        {
            SettingsManager manager = SettingsManager.Instance;
            return manager != null && manager.Applied != null && manager.Applied.ladderEntryMode == 1;
        }
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
        // 地图、扎营和结算会停用输入组件，梯子本身的生命周期由 motor 和受击处理。
        if (_ladder != null) _ladder.Drive(0f);
        ClearInput();
    }
    private void OnInputBlocked(bool blocked) { if (blocked) ClearInput(); }
    private void ClearInput()
    {
        _horizontalInput = 0;
        _jumpRequested = false;
        _injectedInput = 0;
        _injectedInputUntil = 0;
        if (_surface != null) _surface.SetSlideMaterial(false);
        if (_body != null) _body.velocity = new Vector2(0, _body.velocity.y);
    }

    private float _horizontalInput;

    //调试注入（GM/自动化验证用：没有真实键盘时也能驱动运动与动画）
    private float _injectedInput;
    private float _injectedInputUntil;

    private void FixedUpdate()
    {
        if (_body == null) return;
        if (_attack?.IsAttacking == true) return;
        // 攀梯期间由 LadderClimber 接管位置与速度；菜单阻止输入也不强制退出。
        if (_ladder != null && _ladder.IsClimbing)
        {
            _horizontalInput = 0f;
            _jumpRequested = false;
            return;
        }
        if (InputManager.Instance.GameplayInputBlocked) { ClearInput(); return; }

        _isGrounded = CheckGrounded();
        // 起跳的第一物理帧也要关闭侧墙摩擦，避免离地前先损失向上速度。
        if (_surface != null) _surface.SetSlideMaterial((!_isGrounded || _jumpRequested) && _surface.HasSideContact());

        //水平移动：基础速度只作为输入，最终速度叠加家族树永久加成。
        Vector2 velocity = _body.velocity;
        // 只有脚底可站立面且竖直速度已停稳才算真正着陆；上升或高速擦过墙角/台阶不恢复空中次数。
        bool groundedAndSettled = _isGrounded && velocity.y <= 0f && velocity.y >= -LandingSettleSpeed;
        if (groundedAndSettled) _airJumpUsed = false;
        velocity.x = _horizontalInput * (RunEncounter.OwnsRuntimeObject(gameObject) ? MetaCharacterStats.MoveSpeed(moveSpeed) : moveSpeed);

        //跳跃：地面起跳保留原逻辑；腾空后拥有“上云术”时追加一次，第三次点按无效。
        if (_jumpRequested)
        {
            if (_isGrounded)
            {
                velocity.y = jumpForce;
                _airJumpUsed = false;
                ChaosLog.Info(LogChannel.UI, "[玩家] 跳跃（无跳跃动画，占位表现）");
            }
            else if (!_airJumpUsed && RunItemUseService.HasPermanentEffect(ItemUseEffect.DoubleJump))
            {
                velocity.y = jumpForce;
                _airJumpUsed = true;
                ChaosLog.Info(LogChannel.UI, "[玩家] 二段跳（上云术，无跳跃动画，占位表现）");
            }
        }
        _jumpRequested = false;

        _body.velocity = velocity;
    }

    /// <summary>脚底盒形检测：只接受朝上法线；侧墙/过陡斜面不算落地。</summary>
    private bool CheckGrounded()
    {
        if (_surface != null) return _surface.ProbeGround();
        return PlayerSurfacePhysics.ProbeGround(GetComponent<Collider2D>(), _body);
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
        if (_skeleton == null || _attack?.IsAttacking == true) return;

        float vx = Mathf.Abs(_body == null ? 0f : _body.velocity.x);
        CharacterSpineClips.PlayLocomotion(_skeleton.AnimationState, _skeleton.Skeleton?.Data, vx >= IdleThreshold);
    }
}
