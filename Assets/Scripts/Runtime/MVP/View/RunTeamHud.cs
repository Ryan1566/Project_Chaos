using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内**左侧队伍血条**（W3.3）：每人一行「名字 HP/MaxHp 状态（可用/倒地）」。
///
/// 写法**照 `RunDepthHud`**（同一套已验证过的坑）：
/// * 自带**独立 `Canvas`（sortingOrder 99）** —— 局内 `HideAllPanelsForGameplay()` 会把主 UI Canvas 整块隐藏；
/// * `_enabled` 闸门：只有进局期间为 true，撤离后任何 Refresh 都不会重建 Canvas；
/// * **不每帧刷**：`RunTeamHudTicker` 每 0.3s 刷一次；另在 `EnterScene` / 显式调用时刷；
/// * 单实例：`EnsureBuilt()` 按「场景内存活实例数」判断，残留先清后建；`DestroyHud()` 打印 `killed=N`。
/// </summary>
public static class RunTeamHud
{
    public const string CanvasName = "RunTeamHudCanvas";
    /// <summary>刷新间隔（秒）—— 不每帧刷。</summary>
    public const float RefreshInterval = 0.3f;
    /// <summary>行数上限：受控角色 1 + 门客 3。</summary>
    public const int MaxRows = 4;

    private const float RowWidth = 400f;
    private const float RowHeight = 30f;

    /// <summary>根因闸：只有进局期间为 true；撤离后任何 Refresh 都被挡掉（防重建复活）。</summary>
    private static bool _enabled;

    private static GameObject _root;
    private static Canvas _canvas;
    private static TextMeshProUGUI _title;
    /// <summary>中文字体（TMP 默认字体没有中文字形 → 直接用它会出现方框，见 ResolveFont）。</summary>
    private static TMP_FontAsset _font;

    private static readonly List<RowWidgets> Rows = new List<RowWidgets>();
    /// <summary>上次每行文本（只在变化时打日志，避免刷屏）。</summary>
    private static readonly List<string> RowCache = new List<string>();

    private sealed class RowWidgets
    {
        public RectTransform Root;
        public Image Fill;
        public TextMeshProUGUI Label;
    }

    // ────────────────────────── 对外 API ──────────────────────────

    /// <summary>进局：打开闸并建血条（`EnterScene` 成功后调用）。</summary>
    public static void EnableForRun()
    {
        _enabled = true;
        EnsureBuilt();
        RefreshRows();
        ChaosLog.Info(LogChannel.UI, "[HUD] 队伍血条 EnableForRun（左侧，独立 Canvas sortingOrder=99）");
    }

    /// <summary>显示/隐藏（结算面板打开时传 false）。</summary>
    public static void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }

    /// <summary>撤离：关闸并销毁（`Clear()` / `ReturnToMeta` 调用）。关闸后别处再刷也不会重建。</summary>
    public static void DisableAndDestroy()
    {
        _enabled = false;
        DestroyHud();
        ChaosLog.Info(LogChannel.UI, "[HUD] 队伍血条 DisableAndDestroy");
    }

    /// <summary>显式刷新（EnterScene / 切场景 / 扎营后调用；内部会做单实例校验）。</summary>
    public static void Refresh()
    {
        if (!_enabled) return;
        EnsureBuilt();
        RefreshRows();
    }

    /// <summary>由 `RunTeamHudTicker` 每 0.3s 调用（**只刷新行，不做单实例扫描**）。</summary>
    public static void Tick()
    {
        if (!_enabled) return;
        if (_root == null) return;
        RefreshRows();
    }

    /// <summary>销毁**所有**血条对象（撤离 / 重进局）。打印 `killed=N` 便于诊断。</summary>
    public static void DestroyHud()
    {
        int killed = 0;

        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform transform = all[i];
            if (transform == null) continue;
            if (!transform.name.StartsWith(CanvasName)) continue;
            Object.Destroy(transform.gameObject);
            killed++;
        }

        GameObject found = GameObject.Find(CanvasName);
        if (found != null)
        {
            Object.Destroy(found);
            killed++;
        }

        _root = null;
        _canvas = null;
        _title = null;
        Rows.Clear();
        RowCache.Clear();
        ChaosLog.Info(LogChannel.UI, "[HUD] 队伍血条 DestroyHud killed=" + killed);
    }

    // ────────────────────────── 内部 ──────────────────────────

    private static void EnsureBuilt()
    {
        if (!_enabled) return;

        int alive = CountAlive();
        if (_root != null && alive == 1) return;   //正常复用

        if (alive > 0 || _root != null) DestroyHud();   //残留/引用失效 → 全清
        Build();
    }

    private static int CountAlive()
    {
        int count = 0;
        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name.StartsWith(CanvasName)) count++;
        return count;
    }

    private static void Build()
    {
        GameObject canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99;   //低于深度 HUD 的 100

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;

        //小标题
        _title = MakeLabel(canvasGo.transform, "Title", "队伍血量", 20f, new Color(0.85f, 0.9f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(20f, -72f);   //避开左上角 GM 悬浮按钮
        titleRect.sizeDelta = new Vector2(RowWidth, 26f);

        Rows.Clear();
        RowCache.Clear();
        for (int i = 0; i < MaxRows; i++)
        {
            GameObject rowGo = new GameObject("Row_" + i, typeof(RectTransform), typeof(Image));
            RectTransform rowRect = (RectTransform)rowGo.transform;
            rowRect.SetParent(canvasGo.transform, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(20f, -104f - i * 36f);
            rowRect.sizeDelta = new Vector2(RowWidth, RowHeight);

            Image background = rowGo.GetComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.09f, 0.55f);
            background.raycastTarget = false;

            //血条填充（从左侧按比例伸）
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = (RectTransform)fillGo.transform;
            fillRect.SetParent(rowRect, false);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(0f, 0f);
            fillRect.offsetMax = new Vector2(0f, 0f);
            fillRect.sizeDelta = new Vector2(RowWidth, 0f);
            Image fill = fillGo.GetComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 0.4f, 0.55f);
            fill.raycastTarget = false;

            TextMeshProUGUI label = MakeLabel(rowRect, "Label", "", 17f, new Color(0.96f, 0.97f, 1f, 1f));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
            label.enableWordWrapping = false;

            Rows.Add(new RowWidgets { Root = rowRect, Fill = fill, Label = label });
            rowGo.SetActive(false);
        }

        canvasGo.AddComponent<RunTeamHudTicker>();

        _root = canvasGo;
        ChaosLog.Info(LogChannel.UI, "[HUD] 队伍血条已创建（独立 Canvas sortingOrder=99，左上角，最多 " + MaxRows + " 行）");
    }

    /// <summary>刷新所有行：受控角色 1 行 + 未销毁门客依次 1 行。</summary>
    private static void RefreshRows()
    {
        int row = 0;

        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player != null && row < Rows.Count)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            int hp = health == null ? 0 : health.Hp;
            int max = health == null ? 0 : health.maxHp;
            bool downed = health != null && health.IsDowned;
            SetRow(row++, "受控角色", hp, max, downed);
        }

        for (int i = 0; i < AllyController.All.Count && row < Rows.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally == null) continue;
            SetRow(row++, ally.AllyName, ally.Hp, ally.MaxHp, ally.IsDowned);
        }

        for (; row < Rows.Count; row++)
        {
            RowWidgets empty = Rows[row];
            if (empty != null && empty.Root != null) empty.Root.gameObject.SetActive(false);
        }
    }

    private static void SetRow(int index, string name, int hp, int max, bool downed)
    {
        RowWidgets row = Rows[index];
        if (row == null || row.Root == null) return;
        row.Root.gameObject.SetActive(true);

        float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)hp / max);
        if (row.Fill != null)
        {
            row.Fill.rectTransform.sizeDelta = new Vector2(RowWidth * ratio, 0f);
            row.Fill.color = downed
                ? new Color(0.55f, 0.2f, 0.2f, 0.5f)
                : (ratio > 0.5f
                    ? new Color(0.3f, 0.85f, 0.4f, 0.55f)
                    : (ratio > 0.25f ? new Color(0.95f, 0.8f, 0.25f, 0.55f) : new Color(0.9f, 0.35f, 0.25f, 0.55f)));
        }

        string state = downed ? "倒地" : "可用";
        string text = name + "　" + hp + "/" + max + "　" + state;
        if (row.Label != null)
        {
            row.Label.text = text;
            row.Label.color = downed ? new Color(1f, 0.62f, 0.55f, 1f) : new Color(0.96f, 0.97f, 1f, 1f);
        }

        while (RowCache.Count <= index) RowCache.Add("");
        if (RowCache[index] != text)
        {
            RowCache[index] = text;
            ChaosLog.Info(LogChannel.UI, "[HUD] 队伍行 " + index + "：" + text);
        }
    }

    private static TextMeshProUGUI MakeLabel(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = ResolveFont();
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Left;
        return label;
    }

    /// <summary>
    /// 取一个**能显示中文**的 TMP 字体（**公共静态方法**：`RunDepthHud` 也调它，禁止复制第二份逻辑）。
    ///
    /// `TMP_Settings.defaultFontAsset`（LiberationSans SDF）只有拉丁字形 → 中文会变成方框（□），
    /// 所以优先从 `Resources/UIPanels` 的面板 prefab 里取"摆 prefab 时烘进去"的中文字体（MSYH SDF，动态图集）；
    /// 再退到 `Resources` 下可加载的中文字体资产；都拿不到才退回默认字体（并 Warn，明确失败而不是装作正常）。
    /// ⚠ 不能用 `HasCharacter('血')` 判断：动态图集按需加字，未用过时它返回 false 但实际能渲染。
    /// ⚠ 失败结果**不写进 `_font`**：下次 Build 再试一次，避免一次 prefab 加载失败就永久方框。
    /// </summary>
    public static TMP_FontAsset ResolveFont()
    {
        if (_font != null) return _font;

        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;

        //① 面板 prefab（已验证可用）：字体是摆 prefab 时烘进去的
        string[] prefabCandidates = { "UIPanels/RunSettlementPanel", "UIPanels/RunCampPanel", "UIPanels/DispatchPanel" };
        for (int i = 0; i < prefabCandidates.Length; i++)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabCandidates[i]);
            if (prefab == null) continue;
            TextMeshProUGUI text = prefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null || text.font == null) continue;
            if (text.font == fallback) continue;          //默认字体没有中文字形，跳过
            if (text.font.sourceFontFile == null) continue; //没有源字体文件 = 静态图集，缺字补不上

            _font = text.font;
            ChaosLog.Info(LogChannel.UI, "[HUD] 中文字体取自 " + prefabCandidates[i] + "：" + _font.name
                + "（源 " + _font.sourceFontFile.name + "）");
            return _font;
        }

        //② Resources 下的字体资产（当前工程在 Assets/Fonts/cn，不在 Resources 内 → 这两条是给"以后挪进 Resources"留的钩子）
        string[] fontCandidates = { "Fonts/MSYH SDF", "Fonts/cn/MSYH SDF" };
        for (int i = 0; i < fontCandidates.Length; i++)
        {
            TMP_FontAsset asset = Resources.Load<TMP_FontAsset>(fontCandidates[i]);
            if (asset == null || asset == fallback) continue;
            if (asset.sourceFontFile == null) continue;

            _font = asset;
            ChaosLog.Info(LogChannel.UI, "[HUD] 中文字体取自 Resources/" + fontCandidates[i] + "：" + _font.name);
            return _font;
        }

        ChaosLog.Warn(LogChannel.UI, "[HUD] 找不到中文字体（中文会显示成方框），退回 TMP 默认字体 "
            + (fallback == null ? "null" : fallback.name));
        return fallback;
    }
}

/// <summary>队伍血条 0.3s 刷新（挂在血条 Canvas 上，随 Canvas 一起销毁；**不是每帧刷**）。</summary>
public class RunTeamHudTicker : MonoBehaviour
{
    private float _next;

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + RunTeamHud.RefreshInterval;
        RunTeamHud.Tick();
    }
}
