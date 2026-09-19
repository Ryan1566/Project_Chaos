using System;
using System.Collections.Generic;
using ChaosDebug;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 面板显隐动画组件：淡入淡出、四方向平移、缩放弹出，以及子元素依次入场（Stagger）。
/// 只负责"怎么动"，不含任何业务逻辑；显隐时机由 BasePanel.Show()/Hide() 驱动。
///
/// 两条必须遵守的不变式：
/// 1. 进场一定从"起始状态"补间到"静止状态"，退场一定从"静止状态"补间到"起始状态"，
///    绝不从当前值接着动 —— 被同帧 Kill 的 tween 一个值都没写过，接着动会闪一帧错误姿态。
/// 2. 所有 tween 都 Insert 进同一个 Sequence，因此 KillTweens() 一次就能杀干净。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelAnimator : MonoBehaviour
{
    #region Inspector 配置

    [Header("进场")]
    public PanelAnimType enterType = PanelAnimType.Fade;

    [Tooltip("位移/缩放类动画是否叠加透明度变化，变成淡入 + 位移的组合效果")]
    public bool enterFadeAlong = true;

    [Min(0f)] public float enterDuration = 0.25f;
    [Min(0f)] public float enterDelay = 0f;
    public Ease enterEase = Ease.OutQuad;

    [Header("退场")]
    [Tooltip("勾选 = 退场自动反向播放进场动画（推荐）。注意：从左滑进的面板也会向左滑出")]
    public bool autoReverseExit = true;

    [Tooltip("仅当 autoReverseExit 取消勾选时生效")]
    public PanelAnimType exitType = PanelAnimType.Fade;

    public bool exitFadeAlong = true;
    [Min(0f)] public float exitDuration = 0.2f;
    [Min(0f)] public float exitDelay = 0f;
    public Ease exitEase = Ease.InQuad;

    [Header("位移 / 缩放")]
    [Tooltip("做位移和缩放的节点，留空 = 面板自身。全屏面板建议指向 Content 子节点，否则背景会一起滑动、露出空画布")]
    public RectTransform animTarget;

    [Tooltip("平移距离（像素），<= 0 表示自动取节点的宽 / 高")]
    public float slideDistance = 200f;

    [Range(0.01f, 1f)] public float scaleFromSmall = 0.85f;
    [Min(1f)] public float scaleFromLarge = 1.15f;

    [Header("输入")]
    [Tooltip("过渡期间关闭 blocksRaycasts，防止点到正在滑入/消失的按钮。不会修改 interactable（改它会让按钮整段动画期间变灰）")]
    public bool blockInputDuringTransition = true;

    [Header("子元素依次入场")]
    [Tooltip("默认关闭，未配置的面板不参与依次入场")]
    public bool staggerEnabled = false;

    [Tooltip("收集其直接子物体；留空 = 面板自身。若子树中挂了 UIPanelStaggerItem 标记，则只动被标记的项")]
    public Transform staggerRoot;

    [Min(0f)] public float staggerDelay = 0.05f;

    [Tooltip("相邻子元素的间隔。子元素很多时注意 间隔 x 数量 会把进场拉得很长")]
    [Min(0f)] public float staggerInterval = 0.06f;

    [Min(0f)] public float staggerDuration = 0.2f;
    public PanelAnimType staggerType = PanelAnimType.Fade;
    public bool staggerFadeAlong = true;
    public Ease staggerEase = Ease.OutQuad;

    [Tooltip("子元素平移的最远距离，通常远小于面板自身的 slideDistance")]
    public float staggerSlideDistance = 40f;

    [Tooltip("退场时子元素是否也依次消失。默认关闭：退场序列会随子元素数量线性变长")]
    public bool staggerOnExit = false;

    [Tooltip("不参与依次入场的子物体（如全屏遮罩背景）。仅在自动收集模式下生效")]
    public List<Transform> staggerExclude = new List<Transform>();

    [Header("其他")]
    [Tooltip("面板内有 LayoutGroup / ContentSizeFitter 时切成 Late 可避免一帧抖动")]
    public UpdateType updateType = UpdateType.Normal;

    #endregion

    #region 运行时状态

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private RectTransform _moveTarget;
    private RectTransform _restTarget;   // _restPos / _restScale 是从哪个节点录的
    private Transform _staggerOrigin;
    private Transform _staggerOriginSource;   // _staggerOrigin 是从哪个节点解析出来的

    private bool _initialized;
    private Tween _tween;

    // 录制下来的静止状态（Prefab 的设计值）
    private Vector2 _restPos;
    private Vector3 _restScale;
    private float _restAlpha;
    private bool _restBlocksRaycasts = true;

    private readonly Dictionary<Transform, StaggerItem> _itemCache = new Dictionary<Transform, StaggerItem>();
    private readonly List<StaggerItem> _activeItems = new List<StaggerItem>();
    private readonly List<Transform> _pruneBuffer = new List<Transform>();

    /// <summary>一次动画的完整姿态：位置 + 缩放 + 透明度。</summary>
    private struct Pose
    {
        public Vector2 pos;
        public Vector3 scale;
        public float alpha;
    }

    /// <summary>子元素的静止状态缓存。rest 值只在首次收集时录制，之后一直复用。</summary>
    private class StaggerItem
    {
        public Transform tr;
        public RectTransform rt;
        public CanvasGroup cg;
        public Vector2 restPos;
        public Vector3 restScale;
        public float restAlpha;
    }

    #endregion

    /// <summary>当前是否有动画在播。</summary>
    public bool IsPlaying
    {
        get { return _tween != null && _tween.IsActive() && _tween.IsPlaying(); }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    #region 公开接口

    /// <summary>播放进场动画。instant = true 或类型为 None 时立即显示。</summary>
    public void PlayEnter(bool instant = false)
    {
        EnsureInitialized();
        SyncMoveTarget();   // 进场时面板必定处于静止姿态，是唯一可以安全录制基准姿态的时机
        SyncStaggerOrigin();

        // 防御：DOTween 无法驱动未激活对象上的组件，tween 会静默不推进
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        KillTweens();

        // 上一次退场若被打断，子元素可能停在半透明，这里先复位
        ResetStaggerItems();

        float duration = Mathf.Max(0f, enterDuration);
        if (instant || enterType == PanelAnimType.None || duration <= 0f)
        {
            ApplyRestState();
            ResetStaggerItems();
            SetTransitionRaycasts(false);
            return;
        }

        Pose start = ComputeStartPose(enterType, enterFadeAlong, slideDistance,
                                      scaleFromSmall, scaleFromLarge,
                                      _moveTarget, _restPos, _restScale, _restAlpha);
        ApplyState(start.pos, start.scale, start.alpha);
        SetTransitionRaycasts(true);

        Sequence seq = DOTween.Sequence();
        float delay = Mathf.Max(0f, enterDelay);

        bool any = AppendChannelTweens(seq, _canvasGroup, _moveTarget,
                                       start.pos, _restPos, start.scale, _restScale,
                                       start.alpha, _restAlpha, duration, Sanitize(enterEase), delay);

        if (staggerEnabled) any |= BuildStaggerTweens(seq, false, delay);

        if (!any)
        {
            seq.Kill(false);
            ApplyRestState();
            SetTransitionRaycasts(false);
            return;
        }

        _tween = Configure(seq);
        seq.OnComplete(() =>
        {
            if (this == null) return;
            _tween = null;
            ApplyRestState();
            ResetStaggerItems();
            SetTransitionRaycasts(false);
        });
        seq.OnKill(() => { if (ReferenceEquals(_tween, seq)) _tween = null; });
    }

    /// <summary>播放退场动画，播完自动 SetActive(false)。</summary>
    public void PlayExit(bool instant = false)
    {
        EnsureInitialized();
        SyncStaggerOrigin();   // staggerOnExit 时退场也要按当前的 staggerRoot 收集
        KillTweens();

        // 退场一律从静止姿态出发：即使进场播到一半被打断，也不会从中间姿态开始退场
        ApplyRestState();
        ResetStaggerItems();

        PanelAnimType type = autoReverseExit ? enterType : exitType;
        bool fadeAlong = autoReverseExit ? enterFadeAlong : exitFadeAlong;
        float duration = Mathf.Max(0f, exitDuration);

        Pose target = ComputeStartPose(type, fadeAlong, slideDistance,
                                       scaleFromSmall, scaleFromLarge,
                                       _moveTarget, _restPos, _restScale, _restAlpha);

        if (instant || type == PanelAnimType.None || duration <= 0f)
        {
            ApplyState(target.pos, target.scale, target.alpha);
            SetTransitionRaycasts(false);
            FinishExit();
            return;
        }

        SetTransitionRaycasts(true);

        Sequence seq = DOTween.Sequence();
        float delay = Mathf.Max(0f, exitDelay);

        bool any = AppendChannelTweens(seq, _canvasGroup, _moveTarget,
                                       _restPos, target.pos, _restScale, target.scale,
                                       _restAlpha, target.alpha, duration, Sanitize(exitEase), delay);

        if (staggerEnabled && staggerOnExit) any |= BuildStaggerTweens(seq, true, delay);

        if (!any)
        {
            seq.Kill(false);
            ApplyState(target.pos, target.scale, target.alpha);
            SetTransitionRaycasts(false);
            FinishExit();
            return;
        }

        _tween = Configure(seq);
        seq.OnComplete(() =>
        {
            if (this == null) return;
            _tween = null;
            // 停在本次退场真正到达的姿态。原先这里固定写进场姿态，一旦 exitType != enterType
            // （autoReverseExit=false）面板就会在播完的瞬间瞬移到另一个方向的位置
            ApplyState(target.pos, target.scale, target.alpha);
            SetTransitionRaycasts(false);
            FinishExit();
        });
        seq.OnKill(() => { if (ReferenceEquals(_tween, seq)) _tween = null; });
    }

    /// <summary>杀掉当前动画，不触发完成回调。</summary>
    public void KillTweens()
    {
        if (_tween != null)
        {
            _tween.Kill(false);
            _tween = null;
        }
    }

    /// <summary>立即复位到录制下来的静止状态。</summary>
    public void SnapToRest()
    {
        EnsureInitialized();
        KillTweens();
        ApplyRestState();
        ResetStaggerItems();
        SetTransitionRaycasts(false);
    }

    /// <summary>重新录制静止状态（改了 Prefab 布局后不用重进 Play）。</summary>
    [ContextMenu("重新录制静止状态")]
    public void RecaptureRest()
    {
        _initialized = false;
        _restTarget = null;      // 置空才会重新录制，否则 SyncMoveTarget 认为是同一个节点直接跳过
        _staggerOriginSource = null;
        _itemCache.Clear();
        _activeItems.Clear();
        EnsureInitialized();
        ChaosLog.Info($"[UIPanelAnimator] 已重新录制 {name} 的静止状态");
    }

    #endregion

    #region 初始化与静止状态

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rect = transform as RectTransform;
        if (_rect == null)
            ChaosLog.Error($"[UIPanelAnimator] {name} 上找不到 RectTransform，位移动画将不可用");

        SyncStaggerOrigin();

        SyncMoveTarget();

        _restAlpha = _canvasGroup.alpha;
        _restBlocksRaycasts = _canvasGroup.blocksRaycasts;   // 只临时改，用完还原成设计值
    }

    /// <summary>
    /// 校准位移/缩放的作用节点，并在它第一次被使用时录下静止状态。
    ///
    /// 不能只在初始化时解析一次：enterType / slideDistance 等字段都是每次播放现读的，
    /// animTarget 若被缓存，运行时代码改了它会静默不生效 —— 表现为"设了 animTarget，
    /// 动的还是根节点"，而且没有任何报错。这里让它的行为和其他字段一致。
    ///
    /// 只在进场时调用：进场时面板必定处于静止姿态，录下来的才是设计值。
    /// 退场可能发生在进场播到一半时，那时录到的是动画中间帧，会把基准姿态带偏。
    /// </summary>
    private void SyncMoveTarget()
    {
        RectTransform target = animTarget != null ? animTarget : _rect;
        if (target == null || target == _restTarget) return;

        _moveTarget = target;
        _restTarget = target;
        _restPos = target.anchoredPosition;
        _restScale = target.localScale;
    }

    /// <summary>
    /// 校准依次入场的收集起点。和 SyncMoveTarget 同理：staggerRoot 是每次播放现读的字段，
    /// 若把解析结果缓存在初始化那一次，运行时改 staggerRoot 会静默不生效 —— 表现为
    /// "把 staggerRoot 指到某个子节点，动的还是面板自己的子物体"，且没有任何报错。
    ///
    /// 与 SyncMoveTarget 不同的是这里没有需要录制的基准值（子元素的静止姿态是按节点存在
    /// _itemCache 里的），所以进退场都可以安全同步。
    /// </summary>
    private void SyncStaggerOrigin()
    {
        Transform origin = staggerRoot != null ? staggerRoot : transform;
        if (origin == _staggerOriginSource) return;

        _staggerOrigin = origin;
        _staggerOriginSource = origin;
    }

    /// <summary>把静止状态（位置 / 缩放 / 透明度）写回节点。</summary>
    private void ApplyRestState()
    {
        ApplyState(_restPos, _restScale, _restAlpha);
    }

    private void ApplyState(Vector2 pos, Vector3 scale, float alpha)
    {
        if (_moveTarget != null)
        {
            _moveTarget.anchoredPosition = pos;
            _moveTarget.localScale = scale;
        }
        _canvasGroup.alpha = alpha;
    }

    private void FinishExit()
    {
        gameObject.SetActive(false);
    }

    /// <summary>过渡期间关闭 blocksRaycasts，结束/中断后还原成设计值。绝不改 interactable。</summary>
    private void SetTransitionRaycasts(bool inTransition)
    {
        if (!blockInputDuringTransition) return;
        _canvasGroup.blocksRaycasts = inTransition ? false : _restBlocksRaycasts;
    }

    #endregion

    #region 姿态计算

    /// <summary>
    /// 计算"起始姿态"：以静止状态为基准，按动画类型算出偏移后的位置 / 缩放 / 透明度。
    /// 进场是 起始姿态 -> 静止状态，退场是 静止状态 -> 起始姿态，共用这一份计算。
    /// </summary>
    private static Pose ComputeStartPose(PanelAnimType type, bool fadeAlong, float slideDist,
                                         float smallScale, float largeScale,
                                         RectTransform target,
                                         Vector2 restPos, Vector3 restScale, float restAlpha)
    {
        Pose p = new Pose();
        p.pos = restPos;
        p.scale = restScale;
        p.alpha = restAlpha;

        // Fade 本身就是要淡入淡出；位移/缩放类型则由 fadeAlong 决定是否叠加透明度变化
        bool fade = type == PanelAnimType.Fade || (type != PanelAnimType.None && fadeAlong);
        if (fade) p.alpha = 0f;

        switch (type)
        {
            case PanelAnimType.SlideFromLeft:
                p.pos = restPos - new Vector2(SlideX(target, slideDist), 0f);
                break;
            case PanelAnimType.SlideFromRight:
                p.pos = restPos + new Vector2(SlideX(target, slideDist), 0f);
                break;
            case PanelAnimType.SlideFromTop:
                p.pos = restPos + new Vector2(0f, SlideY(target, slideDist));
                break;
            case PanelAnimType.SlideFromBottom:
                p.pos = restPos - new Vector2(0f, SlideY(target, slideDist));
                break;
            case PanelAnimType.ScaleFromSmall:
                p.scale = Vector3.Scale(restScale, new Vector3(smallScale, smallScale, 1f));
                break;
            case PanelAnimType.ScaleFromLarge:
                p.scale = Vector3.Scale(restScale, new Vector3(largeScale, largeScale, 1f));
                break;
        }

        return p;
    }

    /// <summary>水平平移距离：slideDistance <= 0 时取节点自身宽度（全屏拉伸锚点的 sizeDelta 为 0，不能用它）。</summary>
    private static float SlideX(RectTransform target, float dist)
    {
        if (dist > 0f) return dist;
        return target != null ? target.rect.width : 0f;
    }

    /// <summary>垂直平移距离：slideDistance <= 0 时取节点自身高度。</summary>
    private static float SlideY(RectTransform target, float dist)
    {
        if (dist > 0f) return dist;
        return target != null ? target.rect.height : 0f;
    }

    #endregion

    #region 补间构建

    /// <summary>
    /// 把位置 / 缩放 / 透明度三个通道里"确实有变化"的那些并排挂到序列上。
    /// 调用前必须已经用 ApplyState 把 from 值写到节点上了。
    ///
    /// 一律用 Insert(at, ...) 而不是 Join(...)：Join 的语义是"插入到最后一个补间所在的时间点"，
    /// 也就是 Insert(duration - lastTweenInsertTime, t)。所以
    ///     seq.AppendInterval(delay); seq.Join(fade);
    /// 里的 Join 会被重新基准化到 delay - delay = 0，延迟被整个吃掉 ——
    /// 动画照常立刻开始，序列却仍然被拉长 delay 那么久，结果是"动画早就播完了，
    /// 输入还被挡着、退场对象还迟迟不 SetActive(false)"。改成显式插入到 delay 时刻。
    /// </summary>
    private static bool AppendChannelTweens(Sequence seq, CanvasGroup cg, RectTransform target,
                                           Vector2 fromPos, Vector2 toPos,
                                           Vector3 fromScale, Vector3 toScale,
                                           float fromAlpha, float toAlpha,
                                           float duration, Ease ease, float at)
    {
        bool any = false;

        if (cg != null && !Mathf.Approximately(fromAlpha, toAlpha))
        {
            seq.Insert(at, cg.DOFade(toAlpha, duration).SetEase(ease));
            any = true;
        }

        if (target != null)
        {
            if (fromPos != toPos)
            {
                seq.Insert(at, target.DOAnchorPos(toPos, duration).SetEase(ease));
                any = true;
            }

            if (fromScale != toScale)
            {
                seq.Insert(at, target.DOScale(toScale, duration).SetEase(ease));
                any = true;
            }
        }

        return any;
    }

    private Sequence Configure(Sequence seq)
    {
        // 忽略 Time.timeScale：暂停面板是在业务 OnEnter 里把 timeScale 置 0 的，
        // 而 tween 在 OnEnter 之后创建，受缩放的 tween 会一帧都不推进
        seq.SetUpdate(updateType, true);

        // 面板随场景销毁时静默杀掉，避免 safe mode 报错与悬空 tween
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        return seq;
    }

    /// <summary>Ease.Unset 合法（交给 DOTween 全局默认），但 INTERNAL_* 不该被手选。</summary>
    private static Ease Sanitize(Ease e)
    {
        if (e == Ease.INTERNAL_Zero || e == Ease.INTERNAL_Custom) return Ease.OutQuad;
        return e;
    }

    #endregion

    #region 子元素依次入场

    /// <summary>
    /// 构建子元素补间并 Insert 进主序列。
    /// 返回是否真的添加了内容（一个都没添加时调用方要走"立即完成"分支）。
    /// baseDelay 是面板自身动画的延迟（进场的 enterDelay / 退场的 exitDelay）：
    /// 子元素的时间轴以"面板开始动"为原点，否则面板延迟 1 秒进场时，子元素会先自己演完，
    /// 等面板淡进来时它们已经是静止态，staggerDelay / staggerInterval 白设。
    /// </summary>
    private bool BuildStaggerTweens(Sequence seq, bool onExit, float baseDelay)
    {
        CollectStaggerItems(_activeItems);
        if (_activeItems.Count == 0) return false;

        float itemDuration = Mathf.Max(0f, staggerDuration);
        Ease ease = Sanitize(staggerEase);
        PanelAnimType type = staggerType;
        bool animate = type != PanelAnimType.None && itemDuration > 0f;

        // 子元素没有可动的通道（类型为 None 或时长为 0）：直接复位就结束了。
        // 原实现在这里给每一项插一个 InsertCallback 把姿态写回目标值，但那个值就是它当前
        // 所在的姿态（进场前 ResetStaggerItems 已复位过），视觉上零变化；代价却是主序列
        // 被拉长到 延迟 + 间隔×(n-1)，本就被 blockInputDuringTransition 屏蔽的输入要跟着多
        // 等这么久 —— 20 个条目按 0.06 的间隔就是白等 1.1 秒。
        if (!animate)
        {
            ResetStaggerItems();
            return false;
        }

        bool any = false;
        int n = _activeItems.Count;

        for (int i = 0; i < n; i++)
        {
            // 退场时按相反顺序依次消失
            StaggerItem item = _activeItems[onExit ? (n - 1 - i) : i];
            float at = Mathf.Max(0f, baseDelay + staggerDelay + i * staggerInterval);

            Pose start = ComputeStartPose(type, staggerFadeAlong, staggerSlideDistance,
                                          scaleFromSmall, scaleFromLarge,
                                          item.rt, item.restPos, item.restScale, item.restAlpha);

            // 目标姿态：进场去静止状态，退场去起始姿态
            Pose to = onExit
                ? start
                : new Pose { pos = item.restPos, scale = item.restScale, alpha = item.restAlpha };

            // from 值：进场用起始姿态（此刻就写下去，轮到它之前保持不可见）；退场用静止姿态
            Pose from = onExit
                ? new Pose { pos = item.restPos, scale = item.restScale, alpha = item.restAlpha }
                : start;

            if (item.cg != null) item.cg.alpha = from.alpha;
            if (item.rt != null)
            {
                item.rt.anchoredPosition = from.pos;
                item.rt.localScale = from.scale;
            }

            Sequence itemSeq = DOTween.Sequence();
            AppendChannelTweens(itemSeq, item.cg, item.rt,
                                from.pos, to.pos, from.scale, to.scale,
                                from.alpha, to.alpha, itemDuration, ease, 0f);
            seq.Insert(at, itemSeq);
            any = true;
        }

        return any;
    }

    /// <summary>把参与依次入场的子元素收集到 buffer 里。</summary>
    private void CollectStaggerItems(List<StaggerItem> buffer)
    {
        buffer.Clear();
        if (_staggerOrigin == null) return;

        PruneItemCache();

        // 1) 显式标记优先：只动被标记的项，按 order 排序（相同则按层级顺序）
        UIPanelStaggerItem[] marked = _staggerOrigin.GetComponentsInChildren<UIPanelStaggerItem>(false);
        if (marked.Length > 0)
        {
            Array.Sort(marked, (a, b) =>
            {
                int c = a.order.CompareTo(b.order);
                if (c != 0) return c;
                return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            });

            for (int i = 0; i < marked.Length; i++)
                buffer.Add(GetOrCreateItem(marked[i].transform));
            return;
        }

        // 2) 自动收集直接子物体（每次进场重新收集，兼容运行时增删的子物体）
        for (int i = 0; i < _staggerOrigin.childCount; i++)
        {
            Transform child = _staggerOrigin.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            if (staggerExclude != null && staggerExclude.Contains(child)) continue;
            buffer.Add(GetOrCreateItem(child));
        }
    }

    private StaggerItem GetOrCreateItem(Transform t)
    {
        StaggerItem item;
        if (_itemCache.TryGetValue(t, out item) && item.rt != null) return item;

        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = t as RectTransform;
        item = new StaggerItem();
        item.tr = t;
        item.rt = rt;
        item.cg = cg;
        // rest 值只在首次收集时录制；此时子元素还没被本组件动过，取到的就是设计值
        item.restPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        item.restScale = rt != null ? rt.localScale : Vector3.one;
        item.restAlpha = cg.alpha;

        _itemCache[t] = item;
        return item;
    }

    /// <summary>把所有收集过的子元素复位到静止状态。</summary>
    private void ResetStaggerItems()
    {
        foreach (KeyValuePair<Transform, StaggerItem> kv in _itemCache)
        {
            StaggerItem item = kv.Value;
            if (item == null) continue;
            if (item.cg != null) item.cg.alpha = item.restAlpha;
            if (item.rt != null)
            {
                item.rt.anchoredPosition = item.restPos;
                item.rt.localScale = item.restScale;
            }
        }
    }

    /// <summary>清掉已被销毁的子元素缓存，避免字典无限增长。</summary>
    private void PruneItemCache()
    {
        if (_itemCache.Count == 0) return;

        _pruneBuffer.Clear();
        foreach (KeyValuePair<Transform, StaggerItem> kv in _itemCache)
        {
            if (kv.Key == null) _pruneBuffer.Add(kv.Key);
        }

        for (int i = 0; i < _pruneBuffer.Count; i++) _itemCache.Remove(_pruneBuffer[i]);
    }

    #endregion
}
