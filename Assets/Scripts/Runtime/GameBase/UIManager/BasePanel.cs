using ChaosDebug;
using UnityEngine;

/// <summary>
/// 面板基类：业务逻辑与显隐表现分离。
///
/// - 业务回调：OnEnter() / OnExit()，子类按需覆写（调用 base 是安全的空实现）
/// - 显隐表现：Show() / Hide()，只由 UIManager 调用，负责激活/关闭时机与显隐动画
///
/// 注意：OnEnter / OnExit 是业务回调，请勿直接调用 —— 激活与关闭的时机由本类的
/// Show() / Hide() 统一管理，OnExit 里已经不再负责 SetActive(false)，
/// 真正的关闭动作发生在退场动画播完之后。
/// </summary>
public class BasePanel : MonoBehaviour
{
    protected UIManager uiManager;

    public UIManager setUIManager
    {
        set
        {
            uiManager = value;
        }
    }

    private UIPanelAnimator _animator;

    /// <summary>
    /// 显隐动画组件。Prefab 上没挂时会自动补一个（用默认的淡入淡出参数）。
    /// 惰性获取而不是写在 Awake 里 —— Unity 只会调用最派生类的 Awake，
    /// 子面板一旦自己写了 Awake 就会把基类的吞掉。
    /// </summary>
    public UIPanelAnimator Animator
    {
        get
        {
            if (_animator == null)//Unity 重载了 ==，组件被销毁后会重新查找
            {
                _animator = GetComponent<UIPanelAnimator>();
                if (_animator == null)
                {
                    _animator = gameObject.AddComponent<UIPanelAnimator>();
#if UNITY_EDITOR
                    ChaosLog.Warn($"[UI] 面板 {name} 没有 UIPanelAnimator，已按默认淡入淡出自动添加。" +
                                  "运行时添加的组件不会被保存到预制体，参数调不了，请执行菜单 Tool/UI/为所有面板预制体添加动画组件");
#endif
                }
            }
            return _animator;
        }
    }

    /// <summary>当前是否处于"已显示"状态（由 Show / Hide 维护）。</summary>
    public bool IsShowing { get; private set; }

    /// <summary>当前是否正在播放显隐动画。</summary>
    public bool IsAnimating
    {
        get { return _animator != null && _animator.IsPlaying; }
    }

    /// <summary>
    /// 业务回调：面板显示时调用。请勿直接调用，显隐流程由 UIManager 经 Show() 驱动。
    /// </summary>
    public virtual void OnEnter()
    {
    }

    /// <summary>
    /// 业务回调：面板隐藏时调用（在退场动画开始之前）。
    /// 请勿直接调用，也请勿在这里依赖对象已被关闭 —— 真正的 SetActive(false) 发生在动画之后。
    /// </summary>
    public virtual void OnExit()
    {
    }

    /// <summary>
    /// 显示面板：激活 -> 业务 OnEnter -> 播放进场动画。只由 UIManager 调用。
    /// </summary>
    public void Show(bool instant = false)
    {
        if (IsShowing) return;
        IsShowing = true;

        // 先解析组件：自动补挂时 AddComponent 会同步触发 Awake，此刻面板仍是设计状态
        UIPanelAnimator anim = Animator;

        gameObject.SetActive(true);//必须在创建 tween 之前，否则 DOTween 驱动不了未激活对象上的组件
        enabled = true;

        OnEnter();//业务先跑完，动画开始时内容已经就绪

        anim.PlayEnter(instant);
    }

    /// <summary>
    /// 隐藏面板：业务 OnExit -> 播放退场动画 -> 播完自动关闭。只由 UIManager 调用。
    /// </summary>
    public void Hide(bool instant = false)
    {
        if (!IsShowing) return;
        IsShowing = false;

        // 在隐藏开始时调用（而不是放进动画完成回调）：退场动画若被打断，
        // 完成回调不会触发，放在那里会让业务回调被永久吞掉、订阅者泄漏
        OnExit();

        Animator.PlayExit(instant);
    }

    /// <summary>
    /// 控制鼠标显示
    /// </summary>
    public void CursorEnable()
    {
        Cursor.visible = true;//显示鼠标
        Cursor.lockState = 0;//解除鼠标的限制
    }

    /// <summary>
    /// 控制鼠标隐藏
    /// </summary>
    public void CursorHide()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;//锁定并隐藏鼠标
        Cursor.lockState = CursorLockMode.Confined;//鼠标限制在游戏视图内
    }

    /// <summary>
    /// 暂停
    /// </summary>
    /// <param name="isPauseTime">暂停游戏时间</param>
    /// <param name="isPause">暂停PlayerManager的Update</param>
    public void PauseTime(bool isPauseTime, bool isPause)
    {
        if (isPauseTime)
            Time.timeScale = 0f;

        //if (isPause)
        //{
        //    GameManager.Instance.isPause = true;
        //}
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public void ResumeTime()
    {
        Time.timeScale = 1f;
        //GameManager.Instance.isPause = false;
    }
}
