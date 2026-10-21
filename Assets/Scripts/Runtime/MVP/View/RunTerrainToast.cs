using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内**进场地形 / 阵营提示**（W5.2 / M14）：屏幕上方居中一行「地形：地下 · 阵营：秦军」，3 秒淡出。
///
/// 写法**照 `RunDepthHud`**（同一套已验证过的坑，别自创）：
/// * 自带**独立 `Canvas`（sortingOrder 101）** —— 局内 `HideAllPanelsForGameplay()` 会把主 UI Canvas
///   整块隐藏，提示挂在那上面就看不见了；
/// * **`_enabled` 闸门**：只有进局期间为 true；撤离后任何 `Show` 都被挡掉（防"关掉了又自己长出来"）；
/// * **单实例保证**：`EnsureBuilt()` 以"场景内存活实例数"为准，只要不是恰好 1 个且就是 `_root`，
///   就先 `DestroyToast()` 再建；`DestroyToast()` 打印 `killed=N` 便于诊断；
/// * **中文字体必须走 `RunTeamHud.ResolveFont()`（MSYH SDF）** —— `TMP_Settings.defaultFontAsset`
///   是 LiberationSans SDF，没有中文字形，直接用会把「地形 / 阵营 / 秦军」显示成方框（□）。
///
/// 时间口径：**停留 2.6s + 淡出 0.4s = 3.0s**，到 3.0s 时 `activeSelf=false`（纯文本、不打断操作）。
/// 不每帧刷：只由 `RunTerrainToastTicker` 每帧做一次"该不该淡出"的判定。
/// </summary>
public static class RunTerrainToast
{
    public const string CanvasName = "RunTerrainToastCanvas";

    /// <summary>停留时长（秒）。</summary>
    public const float HoldSeconds = 2.6f;

    /// <summary>淡出时长（秒）。</summary>
    public const float FadeSeconds = 0.4f;

    /// <summary>从出现到隐藏的总时长（秒）—— 验收口径「3 秒后 activeSelf=false」。</summary>
    public const float TotalSeconds = HoldSeconds + FadeSeconds;

    /// <summary>根因闸：只有进局期间为 true；撤离后任何 Show 都不建 Canvas。</summary>
    private static bool _enabled;

    private static GameObject _root;
    private static CanvasGroup _group;
    private static TextMeshProUGUI _text;

    /// <summary>计划开始淡出的时间点（`Time.time`）。</summary>
    private static float _hideAt;

    /// <summary>实际开始淡出的时间点（-1 = 还没开始）。</summary>
    private static float _fadeStart = -1f;

    /// <summary>调试 / 验收：提示当前是否显示中。</summary>
    public static bool IsShowing { get { return _root != null && _root.activeSelf; } }

    /// <summary>调试 / 验收：当前提示文字（没建出来时为空串）。</summary>
    public static string CurrentText { get { return _text == null ? "" : _text.text; } }

    // ────────────────────────── 对外 API ──────────────────────────

    /// <summary>进局：打开闸（`EnterScene` 成功后调用一次）。真正的显示交给 <see cref="ShowTerrain"/>。</summary>
    public static void EnableForRun()
    {
        _enabled = true;
        ChaosLog.Info(LogChannel.UI, "[地形] Toast EnableForRun（" + TotalSeconds + "s 后隐藏）");
    }

    /// <summary>显示一行「地形：X · 阵营：Y」。</summary>
    public static void ShowTerrain(string terrainName, string factionName)
    {
        Show("地形：" + terrainName + " · 阵营：" + factionName);
    }

    /// <summary>显示任意一行提示（3 秒淡出）。未打开闸时**什么都不做**（撤离后不会复活）。</summary>
    public static void Show(string line)
    {
        if (!_enabled) return;

        EnsureBuilt();
        if (_root == null || _text == null) return;

        _text.text = line;
        SetAlpha(1f);
        _root.SetActive(true);
        _hideAt = Time.time + HoldSeconds;
        _fadeStart = -1f;

        ChaosLog.Info(LogChannel.UI, "[地形] Toast 显示：「" + line + "」（停留 " + HoldSeconds + "s + 淡出 " + FadeSeconds + "s）");
    }

    /// <summary>撤离：关闸并销毁（`RunEncounter.Clear()` / `ReturnToMeta` 调用）。关闸后别处再调 Show 也不会重建。</summary>
    public static void DisableAndDestroy()
    {
        _enabled = false;
        DestroyToast();
        ChaosLog.Info(LogChannel.UI, "[地形] Toast DisableAndDestroy");
    }

    /// <summary>销毁**所有**提示对象（撤离 / 重进局）。打印 `killed=N` 便于诊断。</summary>
    public static void DestroyToast()
    {
        int killed = 0;

        //① 按 Transform 遍历（含未激活），名字以 CanvasName 开头（覆盖 "(Clone)"）
        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (!t.name.StartsWith(CanvasName)) continue;
            Object.Destroy(t.gameObject);
            killed++;
        }

        //② 兜底：根级同名对象
        GameObject found = GameObject.Find(CanvasName);
        if (found != null)
        {
            Object.Destroy(found);
            killed++;
        }

        _root = null;
        _group = null;
        _text = null;
        _fadeStart = -1f;
        ChaosLog.Info(LogChannel.UI, "[地形] Toast DestroyToast killed=" + killed + "（剩余=" + CountAlive() + "）");
    }

    // ────────────────────────── 内部 ──────────────────────────

    /// <summary>由 `RunTerrainToastTicker` 每帧调用：到点淡出，淡完隐藏（**不是**每帧改文本）。</summary>
    internal static void Tick()
    {
        if (_root == null || !_root.activeSelf) return;

        if (_fadeStart < 0f)
        {
            if (Time.time < _hideAt) return;
            _fadeStart = Time.time;
            ChaosLog.Info(LogChannel.UI, "[地形] Toast 开始淡出（停留到 " + HoldSeconds + "s）");
        }

        float t = (Time.time - _fadeStart) / FadeSeconds;
        SetAlpha(1f - t);
        if (t < 1f) return;

        SetAlpha(0f);
        _root.SetActive(false);
        _fadeStart = -1f;
        ChaosLog.Info(LogChannel.UI, "[地形] Toast 已隐藏（activeSelf=false）");
    }

    /// <summary>单实例保证：只有"恰好 1 个存活实例且就是 _root"才直接复用，否则先清后建。</summary>
    private static void EnsureBuilt()
    {
        if (!_enabled) return;

        int alive = CountAlive();
        if (_root != null && alive == 1 && IsOurInstance(_root)) return;

        if (alive > 0 || _root != null) DestroyToast();   //残留 / 引用失效 → 全清
        Build();
    }

    private static bool IsOurInstance(GameObject go)
    {
        if (go == null) return false;
        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].gameObject == go) return true;
        return false;
    }

    private static int CountAlive()
    {
        int n = 0;
        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name.StartsWith(CanvasName)) n++;
        return n;
    }

    private static void SetAlpha(float alpha)
    {
        float a = Mathf.Clamp01(alpha);
        if (_group != null) _group.alpha = a;
        if (_text != null)
        {
            Color c = _text.color;
            c.a = a;
            _text.color = c;
        }
    }

    private static void Build()
    {
        var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;   //高于深度 HUD(100) / 队伍血条(99)，提示要最上面

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;   //纯提示，不吃射线

        _group = canvasGo.GetComponent<CanvasGroup>();
        _group.alpha = 1f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform rect = (RectTransform)textGo.transform;
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -96f);   //顶部居中：避开右上深度 HUD 与左上门客血条
        rect.sizeDelta = new Vector2(900f, 48f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        //★根因：默认字体是 LiberationSans SDF（无中文字形）→ 必须复用队伍血条那套中文字体解析
        _text.font = RunTeamHud.ResolveFont();
        _text.fontSize = 34f;
        _text.color = new Color(1f, 0.96f, 0.82f, 1f);
        _text.alignment = TextAlignmentOptions.Center;
        _text.raycastTarget = false;
        _text.enableWordWrapping = false;
        _text.text = "";

        canvasGo.AddComponent<RunTerrainToastTicker>();   //跟着 Canvas 一起销毁

        _root = canvasGo;
        ChaosLog.Info(LogChannel.UI, "[地形] Toast 已创建（独立 Canvas sortingOrder=101，顶部居中，字体="
            + (_text.font == null ? "null" : _text.font.name) + "）");
    }
}

/// <summary>地形/阵营提示的淡出驱动（挂在提示 Canvas 上，随 Canvas 一起销毁）。</summary>
public class RunTerrainToastTicker : MonoBehaviour
{
    private void Update()
    {
        RunTerrainToast.Tick();
    }
}
