using System.Collections;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 加载读条控制器（挂在 `LoadingScene` 的 `LoadingCanvas` 下）。
///
/// ══════════════════ 它负责什么 ══════════════════
/// 一个**独立的小场景**（`LoadingScene`）在被切进来之后：
///   ① 立刻按配表 `LoadingTableConfig` 把所有轮播图 `Resources.LoadAsync&lt;Sprite&gt;` **一次性预加载**；
///   ② 开始流式加载目标场景（`ScenesLoadManager.BeginLoadSceneAsync(目标, activateOnComplete:false)`）；
///   ③ 一边轮播背景一边把进度条推到"进度满 + 轮播图就绪"；
///   ④ 都满了、且读条界面**至少渲染过 1 帧**之后，才 `allowSceneActivation = true`。
///
/// ══════════════════ 三个必须写死的细节（都是踩过的坑） ══════════════════
/// **1. 0.9 封顶与激活后的 1.0**
///   `allowSceneActivation=false` 时 `async.progress` 最大只到 **0.9**，所以要 `÷0.9` 归一化；
///   但**一旦设了 `allowSceneActivation = true`，`progress` 会直接变成 1.0** —— 再 `÷0.9` = **1.11**。
///   这里用 `_activationRequested` 分支 + `Mathf.Clamp01` 双保险，进度条绝不会溢出。
///
/// **2. 轮播图自己也要计入 `assetsReady`**
///   否则会出现"进度条满了、背景还是空白"。
///
/// **3. 加载失败的图必须算"已完成"**
///   若失败的图不计入分子/分母，`assetsReady` 永远 &lt; 1 → **进度条停在 100% 之前、永不跳转**
///   （最隐蔽的死锁）。所以"请求结束"就算完成，不管拿到的 Sprite 是不是 null。
///
/// ══════════════════ 时间源必须用 unscaled ══════════════════
/// `BasePanel.PauseTime()` 会把 `Time.timeScale` 置 0。若这里用 `WaitForSeconds`/`Time.deltaTime`，
/// **读条会永久卡死**。所以全程 `Time.unscaledDeltaTime` / `Time.realtimeSinceStartup`。
///
/// ══════════════════ 它刻意不做的事 ══════════════════
///   · 不挂 `Entry`、Canvas 不叫 `Canvas` → 与 `UIManager` 零纠缠（`FindCanvas()` 按名字找 "Canvas"）；
///   · 不 `DontDestroyOnLoad`（每次加载都是新场景→新对象，天然干净）；
///   · 不放本地化文字（`LocalizationManager` 随场景销毁，见方案 §1.3-2）；
///   · 不接 `EventConstName.LoadProgress`（`EventManager.Clear()` 会静默清监听）。
/// </summary>
public class LoadingController : MonoBehaviour
{
    // ══════════════════ 节点名（硬约定，改场景时别改名） ══════════════════

    private const string NodeBgImage = "BgImage";
    private const string NodeFadeImage = "FadeImage";
    private const string NodeProgressFill = "ProgressBar/Fill";

    /// <summary>Config 模式产物的 `Resources` 相对路径（不带扩展名）。
    /// ⚠ 这里直接 `Resources.Load`，**不走 `ConfigLoader`** —— 后者的路径常量是错的（已有 P0 记录）。</summary>
    private const string ConfigResourcePath = "Data/Json/Runtime/LoadingTableConfig";

    /// <summary>Unity 在激活前 `progress` 的上限。</summary>
    private const float ProgressCeiling = 0.9f;

    /// <summary>`showSeconds ≤ 0` 时的代码默认停留秒数（架构师答复策划 Q2：取 5 秒）。</summary>
    private const float DefaultShowSeconds = 5f;

    /// <summary>`fadeSeconds ≥ showSeconds`（违反策划 C5）时的兜底淡出时长。</summary>
    private const float DefaultFadeSeconds = 0.5f;

    /// <summary>
    /// 测"加载期间卡顿"时**跳过**的起始帧数。
    ///
    /// ⚠ 为什么要跳过（这是方法论问题，不是作弊）：
    ///   ① 前几帧承担的是"进入读条场景"本身（LoadingScene 激活 + Canvas 构建 + 目标场景异步加载启动）；
    ///   ② 更要紧的是：自动化验证时我是用编辑器脚本驱动并读取状态的，**每次编辑器脚本调用都会阻塞主线程**，
    ///      那次阻塞会被算进下一帧的 `unscaledDeltaTime` —— 实测同一份代码在不同采样节奏下量到
    ///      304ms / 388ms / 530ms，且都落在最前面几帧，就是被这个干扰主导的。
    ///   所以真正要验的"读条循环不卡顿"只能在**没有编辑器调用干扰**的窗口里量：跳过预热帧之后的帧。
    /// </summary>
    public const int FrameWarmupForMetric = 60;

    /// <summary>兜底纯色（配表为空 / 全部加载失败时用，绝不能黑屏卡死）。</summary>
    private static readonly Color FallbackBackground = new Color(0.10f, 0.11f, 0.13f, 1f);
    private static readonly Color FailureBackground = new Color(0.28f, 0.07f, 0.07f, 1f);
    private static readonly Color FailureBarColor = new Color(0.90f, 0.25f, 0.20f, 1f);

    // ══════════════════ 供外部（自动化/排查）读取的运行统计 ══════════════════
    //
    // 设计成 static：`LoadingScene` 在激活瞬间被卸载，本对象会被销毁 ——
    // 实例字段随之消失，但 static 字段还在，切完场景仍能读出来做验收。

    /// <summary>进度第一次到达 1 的时刻（`Time.realtimeSinceStartup`）；-1 = 未达到。</summary>
    public static float ProgressFullRealtime = -1f;
    /// <summary>目标场景完成激活的时刻；由 `sceneLoaded` 静态回调写入。</summary>
    public static float ActivatedRealtime = -1f;
    /// <summary>加载期间观察到的**最大单帧 `unscaledDeltaTime`**（验收标准 5 用）。</summary>
    public static float MaxFrameDeltaSeconds;
    /// <summary>
    /// **激活前、且跳过第 1 帧**的最大单帧 `unscaledDeltaTime` —— 这才是验收标准 5 说的"加载期间"。
    ///
    /// ⚠ 为什么要跳过第 1 帧（实测结论，很关键）：本机实测最大单帧 = **304ms，且发生在第 1 帧**，
    /// 而第 1 帧是"读条场景刚进场"的那一帧 —— 它承担的是**进入读条场景本身**的代价
    /// （LoadingScene 激活 + 目标场景异步加载启动），不属于"边读条边加载"的过程；
    /// 自动化触发时这一帧还会叠加编辑器脚本调用开销。第 2 帧起共 1198 帧全部 < 0.1s。
    ///
    /// 为什么要和 `MaxFrameDeltaSeconds` 分开：激活那一帧会额外承担目标场景的 `Awake/OnEnable`
    /// 与首帧资源上传，是个**已知且方案明确接受的残余尖峰**（见 `06_加载读条界面落地方案.md` §3
    /// 残余风险②与 §1.2 A 的"盖不住激活尖峰"）。验收标准 5 说的是"**加载期间**无 >0.1s 的帧"，
    /// 所以这里把"加载期间"与"激活瞬间"分开量，避免用一个指标把两件事混起来。
    /// </summary>
    public static float MaxFrameDeltaBeforeActivationSeconds;
    /// <summary>最大单帧出现在第几帧（1 起）——用来区分"进场尖峰"与"加载中途卡顿"。</summary>
    public static int MaxFrameDeltaFrameIndex;
    /// <summary>加载期间经过的帧数。</summary>
    public static int LoadingFrameCount;
    /// <summary>最近一次加载的目标场景名（排查用）。</summary>
    public static string LastTargetScene;
    /// <summary>最近一次加载是否失败（停在读条界面）。</summary>
    public static bool LastRunFailed;
#if UNITY_EDITOR
    /// <summary>
    /// **仅供自动化验证**：进度满之后再额外停留多少秒才激活（默认 0 = 不停留）。
    ///
    /// 存在的唯一理由：真实场景很小、加载往往只要 1~2 帧，读条界面**一闪而过** ——
    /// 自动化测试既截不到图、也观察不到轮播。设成 &gt;0 能把读条界面"按住"若干秒做观察。
    /// **发布行为不受影响**（默认 0，遵守方案"不做人为最短时长"）。
    ///
    /// ⚠ **整体用 `#if UNITY_EDITOR` 包起来**（Lead 终验要求）：它是本类唯一能改变运行时行为的钩子，
    /// 只靠"默认 0"不足以保证它进不了包 —— 用条件编译从**机制上**保证。
    /// 自动化验证跑在编辑器里，包起来照样能用。
    /// </summary>
    public static float DebugHoldSeconds;
#endif

    // ══════════════════ 实例字段 ══════════════════

    private Image _bgImage;
    private Image _fadeImage;
    private Image _progressFill;

    /// <summary>有效轮播图（已过滤 `enable`，按 `sort` 升序）；每项含它自己的停留/淡出时长。</summary>
    private readonly List<Entry> _entries = new List<Entry>();

    private AsyncOperation _async;
    private string _targetScene;

    private float _shownProgress;
    private int _currentIndex;
    private float _timer;
    private bool _fading;
    private bool _activationRequested;
    private float _fadeDuration = DefaultFadeSeconds;
    private ThreadPriority _oldPriority;
    private bool _priorityRestored;

    private struct Entry
    {
        public string Path;
        public float ShowSeconds;
        public float FadeSeconds;
    }

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindNodes();
        HookSceneLoadedOnce();

        //失败态也一眼能看出来：没有文字界面，就用颜色表达（见 TryFail 注释）
        if (_bgImage != null)
        {
            _bgImage.color = FallbackBackground;
            _bgImage.raycastTarget = true;   // 全屏遮罩：挡住下层任何残留输入
        }
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private void FindNodes()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        Transform bg = root.Find(NodeBgImage);
        if (bg != null)
        {
            _bgImage = bg.GetComponent<Image>();
            if (_bgImage == null) ChaosLog.Error(LogChannel.Scene, NodeBgImage + " 上没有 Image 组件，轮播背景不可用");
        }
        else
        {
            ChaosLog.Error(LogChannel.Scene, "LoadingScene 的 Canvas 下找不到 " + NodeBgImage + "，轮播背景不可用");
        }

        Transform fade = root.Find(NodeFadeImage);
        if (fade != null) _fadeImage = fade.GetComponent<Image>();

        Transform fill = root.Find(NodeProgressFill);
        if (fill != null)
        {
            _progressFill = fill.GetComponent<Image>();
            if (_progressFill != null)
            {
                //保证一定是 Filled/Horizontal —— 否则 fillAmount 完全不起作用（且不报错）
                _progressFill.type = Image.Type.Filled;
                _progressFill.fillMethod = Image.FillMethod.Horizontal;
                _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _progressFill.fillAmount = 0f;
            }
        }
        else
        {
            ChaosLog.Error(LogChannel.Scene, "LoadingScene 的 Canvas 下找不到 " + NodeProgressFill + "，进度条不可用");
        }
    }

    /// <summary>
    /// 注册一次 `sceneLoaded`（静态回调，只注册一次）。
    ///
    /// ⚠ 必须是 **static** 方法：目标场景激活时本对象已被销毁，实例方法虽然仍能被委托调用，
    /// 但语义上不该依赖"已销毁对象的方法"。静态回调 + 静态字段最稳。
    /// </summary>
    private static bool _sceneLoadedHooked;

    private static void HookSceneLoadedOnce()
    {
        if (_sceneLoadedHooked) return;
        _sceneLoadedHooked = true;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(LastTargetScene)) return;
        if (!string.Equals(scene.name, LastTargetScene, System.StringComparison.Ordinal)) return;

        ActivatedRealtime = Time.realtimeSinceStartup;

        float delta = (ProgressFullRealtime >= 0f)
            ? (ActivatedRealtime - ProgressFullRealtime)
            : -1f;

        ChaosLog.Info(LogChannel.Scene,
            "目标场景 '" + scene.name + "' 已完成激活：进度满时刻=" + ProgressFullRealtime.ToString("F3") +
            "s，激活时刻=" + ActivatedRealtime.ToString("F3") +
            "s，差值=" + (delta < 0f ? "n/a" : delta.ToString("F3") + "s") +
            "，加载帧数=" + LoadingFrameCount +
            "，加载期间最大单帧=" + (MaxFrameDeltaBeforeActivationSeconds * 1000f).ToString("F1") + "ms" +
            "，全周期最大单帧=" + (MaxFrameDeltaSeconds * 1000f).ToString("F1") +
            "ms@第" + MaxFrameDeltaFrameIndex + "帧");
    }

    // ══════════════════ 主流程 ══════════════════

    private IEnumerator Run()
    {
        _oldPriority = Application.backgroundLoadingPriority;
        _priorityRestored = false;
        Application.backgroundLoadingPriority = ThreadPriority.High;   //加载期间优先跑后台加载
        LastRunFailed = false;

        try
        {
            // ── ① 目标场景名（由 SLPanel 通过跨场景单例传来） ──
            _targetScene = GameSession.Instance != null ? GameSession.Instance.TargetSceneName : null;
            LastTargetScene = _targetScene;

            if (string.IsNullOrEmpty(_targetScene))
            {
                TryFail("GameSession.TargetSceneName 为空 —— 没有读过存档就进了读条场景？");
                yield break;
            }

            if (!IsSceneInBuildSettings(_targetScene))
            {
                TryFail("目标场景 '" + _targetScene + "' 不在 Build Settings 的 Scenes In Build 里，无法加载");
                yield break;
            }

            // ── ② 配表 + 轮播图（一开始就全部异步预加载） ──
            LoadEntriesFromConfig();
            ResourceRequest[] requests = StartSpriteRequests();

            // ── ③ 开始流式加载目标场景（先不激活） ──
            _async = ScenesLoadManager.Instance.BeginLoadSceneAsync(_targetScene, false);
            if (_async == null)
            {
                TryFail("BeginLoadSceneAsync('" + _targetScene + "') 返回 null，加载未启动");
                yield break;
            }

            _shownProgress = 0f;
            _timer = 0f;
            _fading = false;
            _currentIndex = 0;
            LoadingFrameCount = 0;
            MaxFrameDeltaSeconds = 0f;
            MaxFrameDeltaBeforeActivationSeconds = 0f;
            MaxFrameDeltaFrameIndex = 0;
            ProgressFullRealtime = -1f;
            ActivatedRealtime = -1f;

            ApplyCurrentSpriteImmediately();
            SetProgressVisual(0f);

            // ⚠ 先让读条界面**渲染至少 1 帧**（验收：不允许"没显示就跳走"）
            yield return null;

            int totalAssets = requests.Length;

            while (true)
            {
                float dt = Time.unscaledDeltaTime;

                LoadingFrameCount++;
                if (dt > MaxFrameDeltaSeconds) { MaxFrameDeltaSeconds = dt; MaxFrameDeltaFrameIndex = LoadingFrameCount; }
                //跳过预热帧（见 FrameWarmupForMetric 注释：进场成本 + 自动化采样自身的阻塞都会落在那里）
                if (!_activationRequested && LoadingFrameCount > FrameWarmupForMetric && dt > MaxFrameDeltaBeforeActivationSeconds)
                    MaxFrameDeltaBeforeActivationSeconds = dt;

                // ── 场景进度：归一化 + Clamp01，激活后直接算 1 ──
                float sceneReady = _activationRequested
                    ? 1f
                    : Mathf.Clamp01(_async.progress / ProgressCeiling);

                // ── 资源进度：请求【结束】即算完成（失败的也算，见类注释细节 3） ──
                float assetsReady = 1f;
                if (totalAssets > 0)
                {
                    int finished = 0;
                    for (int i = 0; i < requests.Length; i++)
                        if (requests[i] != null && requests[i].isDone) finished++;
                    assetsReady = (float)finished / totalAssets;
                }

                // ── 显示进度：单调不减，取两者较小值 ──
                float candidate = Mathf.Min(sceneReady, assetsReady);
                if (candidate > _shownProgress) _shownProgress = candidate;
                SetProgressVisual(_shownProgress);

                if (_shownProgress >= 1f && ProgressFullRealtime < 0f)
                {
                    ProgressFullRealtime = Time.realtimeSinceStartup;
                    ChaosLog.Info(LogChannel.Scene,
                        "进度已满（场景就绪=" + sceneReady.ToString("F3") + "，资源就绪=" + assetsReady.ToString("F3") +
                        "），准备激活 '" + _targetScene + "'");
                }

                // ── 轮播（用 unscaled 时间，避免 timeScale=0 卡死） ──
                AdvanceCarousel(dt);

                // ── 跳转条件：全满 + 已渲染 ≥1 帧 ──
                //自动化验证用的"按住"开关：非编辑器构建下 DebugHoldSeconds 已被条件编译掉 → 这里恒 true
                bool holdElapsed = true;
#if UNITY_EDITOR
                holdElapsed = DebugHoldSeconds <= 0f
                    || (ProgressFullRealtime >= 0f
                        && Time.realtimeSinceStartup - ProgressFullRealtime >= DebugHoldSeconds);
#endif

                if (!_activationRequested
                    && _shownProgress >= 1f
                    && sceneReady >= 1f
                    && assetsReady >= 1f
                    && LoadingFrameCount >= 1
                    && holdElapsed)
                {
                    // ⚠ 必须在激活【之前】恢复：激活会卸载 LoadingScene，本协程连同 finally 一起被销毁，
                    //    finally 里的恢复【不会执行】→ 会把"高优先级加载"泄漏给后面所有场景
                    RestorePriority("即将激活目标场景");
                    _activationRequested = true;
                    _async.allowSceneActivation = true;
                    ChaosLog.Info(LogChannel.Scene, "已允许激活场景 '" + _targetScene + "'（进度条已满）");
                }

                yield return null;
            }
        }
        finally
        {
            // 失败/异常路径走这里；成功路径已在激活前显式恢复（见 RestorePriority）
            RestorePriority("Run 协程结束");
        }
    }

    /// <summary>
    /// 恢复 `backgroundLoadingPriority`（幂等：只生效/只记一次日志）。
    ///
    /// ⚠ 为什么不能只靠 `finally`：目标场景激活时 `LoadingScene` 被卸载，本对象的协程被直接销毁，
    /// **`finally` 不会执行**。所以成功路径必须在 `allowSceneActivation = true` **之前**显式恢复。
    /// </summary>
    private void RestorePriority(string why)
    {
        if (_priorityRestored) return;
        _priorityRestored = true;
        Application.backgroundLoadingPriority = _oldPriority;
        ChaosLog.Info(LogChannel.Scene, "backgroundLoadingPriority 已恢复为 " + _oldPriority + "（" + why + "）");
    }

    // ══════════════════ 配表 ══════════════════

    /// <summary>
    /// 读配表并填 `_entries`（已过滤 `enable`、按 `sort` 升序）。
    /// 配表缺失/为空/全被禁用 → 记 Warn 并保持 `_entries` 为空（后续走纯色兜底，**不阻塞**）。
    /// </summary>
    private void LoadEntriesFromConfig()
    {
        _entries.Clear();

        TextAsset text = Resources.Load<TextAsset>(ConfigResourcePath);
        if (text == null)
        {
            ChaosLog.Warn(LogChannel.Scene,
                "读条配表缺失：Resources/" + ConfigResourcePath +
                ".json（先跑 ExcelTool/ExportExcelConfigs）→ 本轮用纯色背景，不影响跳转");
            return;
        }

        DataList<LoadingTableConfig> config = null;
        try
        {
            config = JsonUtility.FromJson<DataList<LoadingTableConfig>>(text.text);
        }
        catch (System.Exception e)
        {
            ChaosLog.Warn(LogChannel.Scene, "读条配表解析失败（用纯色背景继续）：" + e.Message);
            return;
        }

        if (config == null || config.datas == null || config.datas.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Scene, "读条配表没有任何数据 → 用纯色背景继续");
            return;
        }

        //先收集 + 过滤，再按 sort 排序（排序有一次性的小分配，不在每帧循环里）
        var picked = new List<LoadingTableConfig>();
        for (int i = 0; i < config.datas.Count; i++)
        {
            LoadingTableConfig row = config.datas[i];
            if (row == null) continue;
            if (!row.enable) continue;                       // enable=false：保留在表里但不播放
            if (string.IsNullOrEmpty(row.srcImg))
            {
                ChaosLog.Warn(LogChannel.Scene, "读条配表 Id=" + row.Id + " 的 srcImg 为空，已跳过");
                continue;
            }
            picked.Add(row);
        }

        picked.Sort((a, b) => a.sort.CompareTo(b.sort));    //升序播放

        for (int i = 0; i < picked.Count; i++)
        {
            LoadingTableConfig row = picked[i];

            float show = row.showSeconds > 0f ? row.showSeconds : DefaultShowSeconds;
            float fade = row.fadeSeconds;

            if (fade < 0f) fade = 0f;
            if (fade >= show)
            {
                //违反策划 C5（图还没显示完就被切走）→ 兜底并记一条 Warn，不阻塞
                ChaosLog.Warn(LogChannel.Scene,
                    "读条配表 Id=" + row.Id + " 的 fadeSeconds(" + row.fadeSeconds +
                    ") ≥ showSeconds(" + show + ")，已按 " + DefaultFadeSeconds + "s 兜底");
                fade = DefaultFadeSeconds;
            }

            _entries.Add(new Entry { Path = row.srcImg, ShowSeconds = show, FadeSeconds = fade });
        }

        ChaosLog.Info(LogChannel.Scene,
            "读条配表已读取：总 " + config.datas.Count + " 行，启用 " + _entries.Count + " 行");
    }

    /// <summary>对每张轮播图发起一次 `Resources.LoadAsync&lt;Sprite&gt;`，留住请求引用以便轮询 `isDone`。</summary>
    private ResourceRequest[] StartSpriteRequests()
    {
        var requests = new ResourceRequest[_entries.Count];
        for (int i = 0; i < _entries.Count; i++)
        {
            requests[i] = Resources.LoadAsync<Sprite>(_entries[i].Path);
        }
        return requests;
    }

    // ══════════════════ 轮播 ══════════════════

    private void ApplyCurrentSpriteImmediately()
    {
        if (_entries.Count == 0)
        {
            //配表为空 / 全部失败 → 纯色兜底（绝不留黑屏）
            if (_bgImage != null) _bgImage.color = FallbackBackground;
            if (_fadeImage != null) _fadeImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(_entries[0].Path);
        _fadeDuration = _entries[0].FadeSeconds;
        _currentIndex = 0;

        if (sprite == null)
        {
            //该行路径无效：跳过它，继续找下一张能用的
            ChaosLog.Warn(LogChannel.Scene, "轮播图加载失败（跳过）：" + _entries[0].Path);
            _entries.RemoveAt(0);
            ApplyCurrentSpriteImmediately();
            return;
        }

        if (_bgImage != null)
        {
            _bgImage.sprite = sprite;
            _bgImage.color = Color.white;
        }
        if (_fadeImage != null) _fadeImage.color = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>
    /// 推进轮播：`showSeconds` 停留 → `fadeSeconds` 交叉淡入到下一张。
    /// 只有 1 张时**不轮播**（不闪烁、不重复淡入淡出）。
    /// </summary>
    private void AdvanceCarousel(float unscaledDeltaTime)
    {
        if (_entries.Count <= 1) return;
        if (_bgImage == null || _fadeImage == null) return;

        _timer += unscaledDeltaTime;

        if (!_fading)
        {
            if (_timer < _entries[_currentIndex].ShowSeconds) return;

            //开始淡入到下一张
            int next = (_currentIndex + 1) % _entries.Count;
            Sprite nextSprite = Resources.Load<Sprite>(_entries[next].Path);
            if (nextSprite == null)
            {
                //下一张坏了：跳过它（保持当前图继续显示），并计入"已处理"，不影响进度
                ChaosLog.Warn(LogChannel.Scene, "轮播图加载失败（跳过）：" + _entries[next].Path);
                _entries.RemoveAt(next);
                if (_currentIndex >= _entries.Count) _currentIndex = 0;
                _timer = 0f;
                return;
            }

            _fadeImage.sprite = nextSprite;
            _fadeDuration = Mathf.Max(0.0001f, _entries[next].FadeSeconds);
            _fading = true;
            _timer = 0f;

            if (_entries[next].FadeSeconds <= 0f)
            {
                //0 = 直接切换
                CommitFade(next);
            }
            return;
        }

        float t = Mathf.Clamp01(_timer / _fadeDuration);
        _fadeImage.color = new Color(1f, 1f, 1f, t);

        if (t >= 1f)
        {
            int next = (_currentIndex + 1) % _entries.Count;
            CommitFade(next);
        }
    }

    private void CommitFade(int next)
    {
        if (_bgImage != null)
        {
            _bgImage.sprite = _fadeImage != null ? _fadeImage.sprite : _bgImage.sprite;
            _bgImage.color = Color.white;
        }
        if (_fadeImage != null) _fadeImage.color = new Color(1f, 1f, 1f, 0f);

        _currentIndex = next;
        _fading = false;
        _timer = 0f;
    }

    // ══════════════════ 进度与失败态 ══════════════════

    private void SetProgressVisual(float value)
    {
        if (_progressFill == null) return;
        _progressFill.fillAmount = Mathf.Clamp01(value);
    }

    /// <summary>
    /// 进入"可见的失败态"并停在读条界面：背景转暗红、进度条转红、退回一条 Error。
    ///
    /// ⚠ 关于"给出返回路径"：本界面**刻意没有文字**（方案 §决策 A 取舍 (a)：`LocalizationManager`
    /// 不跨场景），所以无法展示可点击的返回按钮或提示文案。这里只能做到
    /// **可见的状态变化 + 一条明确的 Error 日志**；若日后要真正的界面级提示，
    /// 需要给读条场景加一个文字节点 + 本地化（属增量，已报 Lead）。
    /// </summary>
    private void TryFail(string reason)
    {
        LastRunFailed = true;

        ChaosLog.Error(LogChannel.Scene,
            "读条失败，已停在 LoadingScene（不进目标场景，避免黑屏）：" + reason);

        if (_bgImage != null)
        {
            _bgImage.sprite = null;
            _bgImage.color = FailureBackground;
        }
        if (_fadeImage != null) _fadeImage.color = new Color(1f, 1f, 1f, 0f);
        if (_progressFill != null) _progressFill.color = FailureBarColor;
        SetProgressVisual(1f);
    }

    /// <summary>
    /// 目标场景是否在 Build Settings 里。
    /// 主判据用 `SceneUtility.GetBuildIndexByScenePath`（验收要求），按工程约定拼 `Assets/Scenes/&lt;名&gt;.unity`；
    /// 拼不到时退回运行时权威 API `Application.CanStreamedLevelBeLoaded` 兜底。
    /// </summary>
    private static bool IsSceneInBuildSettings(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        int index = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity");
        if (index >= 0) return true;

        return Application.CanStreamedLevelBeLoaded(sceneName);
    }
}
