using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内深度 HUD（W4e 整体重写）：**右上角单行** `深度 d/上限 · 场景 n/3 · 可带出 N 件`。
///
/// 设计要点：
/// * 自带**独立 `Canvas`（sortingOrder 100）** —— 局内 `HideAllPanelsForGameplay()` 会把主 UI Canvas
///   整块隐藏，HUD 挂在那上面就看不见了；
/// * **不每帧刷新**（无 `Update()`）：只在 `EnterScene` 成功、切场景、显式调用时刷一次；
/// * **单实例保证**：`EnsureBuilt()` 以"场景内存活实例数"为准（不是只看 `_root`），
///   只要不是"恰好 1 个且就是 `_root`"，就先 `DestroyHud()` 再建；
/// * `DestroyHud()` 遍历**所有**名字以 `RunDepthHudCanvas` 开头的对象销毁，并打印 `killed=N`。
/// </summary>
public static class RunDepthHud
{
    public const int TotalScenes = 3;

    private const string CanvasName = "RunDepthHudCanvas";

    /// <summary>根因闸：只有进局期间为 true；撤离后任何 Refresh/EnsureBuilt 都会被挡掉（防重建复活）。</summary>
    private static bool _enabled;

    private static GameObject _root;
    private static Canvas _canvas;
    private static TextMeshProUGUI _text;

    // ────────────────────────── 对外 API ──────────────────────────

    /// <summary>确保存在（单实例）并刷新文本。`EnterScene` 成功、切场景时调用。</summary>
    public static void Refresh()
    {
        if (!_enabled) return;   //W4e：未进局时任何刷新都不建 canvas
        EnsureBuilt();
        SetVisible(true);
        if (_text == null) return;

        MetaProgressService meta = MetaProgressService.Instance;
        uint depth = meta == null ? 1u : meta.Depth;
        uint cap = meta == null ? 1u : meta.DepthCap;
        int deploy = meta == null ? 1 : meta.DeployCap;
        int scene = RunEncounter.CurrentSceneIndex;

        _text.text = "深度 " + depth + "/" + cap + " · 场景 " + scene + "/" + TotalScenes + " · 可带出 " + deploy + " 件";
        ChaosLog.Info(LogChannel.UI, "[HUD] " + _text.text);
    }

    /// <summary>显示/隐藏（结算面板打开时传 false）。</summary>
    public static void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }

    /// <summary>进局：打开闸并建 HUD（`EnterScene` 成功后调用）。</summary>
    public static void EnableForRun()
    {
        _enabled = true;
        Refresh();
        ChaosLog.Info(LogChannel.UI, "[HUD] EnableForRun");
    }

    /// <summary>撤离：关闸并销毁（`Clear()` / `ReturnToMeta` 调用）。关闸后别处再调 Refresh 也不会重建。</summary>
    public static void DisableAndDestroy()
    {
        _enabled = false;
        DestroyHud();
        ChaosLog.Info(LogChannel.UI, "[HUD] DisableAndDestroy");
    }

    /// <summary>兼容旧调用名。</summary>
    public static void Hide() { SetVisible(false); }

    /// <summary>销毁**所有** HUD 对象（撤离 / 重进局）。打印 `killed=N` 便于诊断。</summary>
    public static void DestroyHud()
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

        //② 兜底：根级同名对象（FindObjectsOfType 在极端情况下漏掉根对象时补一刀）
        GameObject found = GameObject.Find(CanvasName);
        if (found != null)
        {
            Object.Destroy(found);
            killed++;
        }

        _root = null;
        _canvas = null;
        _text = null;
        ChaosLog.Info(LogChannel.UI, "[HUD] DestroyHud killed=" + killed);
    }

    // ────────────────────────── 内部 ──────────────────────────

    /// <summary>单实例保证：只有"恰好 1 个存活实例且就是 _root"才直接复用，否则先清后建。</summary>
    private static void EnsureBuilt()
    {
        if (!_enabled) return;   //W4e：同上

        int alive = CountAlive();

        if (_root != null && alive == 1 && IsOurInstance(_root))
            return;   //正常复用

        if (alive > 0 || _root != null)
            DestroyHud();   //残留/引用失效 → 全清

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

    private static void Build()
    {
        GameObject canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform rect = (RectTransform)textGo.transform;
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);   //右上角，留 24px
        rect.sizeDelta = new Vector2(520f, 40f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.font = TMP_Settings.defaultFontAsset;
        _text.fontSize = 26f;
        _text.color = new Color(0.96f, 0.97f, 1f, 1f);
        _text.alignment = TextAlignmentOptions.TopRight;
        _text.raycastTarget = false;
        _text.enableWordWrapping = false;

        _root = canvasGo;
        ChaosLog.Info(LogChannel.UI, "[HUD] 已创建（独立 Canvas sortingOrder=100，右上角）");
    }
}
