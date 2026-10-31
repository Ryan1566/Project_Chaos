using System;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive Market view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class MarketPanel : BasePanel, IGameplayView<MarketState>
{
    public event Action<PanelIntent> Intent;
    private MarketPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new MarketPresenter(new MarketModel());
        _presenter.Attach(this);
    }
    private void Send(PanelAction action, uint id = 0)
    {
        if (!IsShowing) return;
        EnsurePresenter();
        Intent?.Invoke(new PanelIntent(action, id));
    }

    private const float PanelWidth = 800f;
    private const float PanelHeight = 600f;
    private const float RowStep = 36f;
    private const float RowHeight = 32f;

    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);
    private static readonly Color OkColor = new Color(0.45f, 0.90f, 0.55f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _money;
    private TextMeshProUGUI _result;
    private RectTransform _listArea;
    private TMP_FontAsset _font;
    private bool _built;
    private TextMeshProUGUI _closeLabel;
    private LocalizationManager _localization;

    /// <summary>GM / 其它面板统一入口：Push 之后**显式刷新**（面板已缓存时 OnEnter 不会重跑）。</summary>
    public static void Open()
    {
        if (UIManager.Instance == null)
        {
            ChaosLog.Error(LogChannel.UI, "[市场] UIManager 还没初始化，打不开市场面板");
            return;
        }

        UIManager.Instance.PushPanel(PanelType.MarketPanel);

        MarketPanel panel = Object.FindFirstObjectByType<MarketPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            panel.RefreshFromOpen();
        }

        ChaosLog.Info(LogChannel.UI, "[市场] 面板已打开");
    }

    public void RefreshFromOpen()
    {
        BuildUi();
        Refresh();
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter()
    {
        BuildUi();
        _localization = LocalizationManager.GetInstance();
        if (_localization != null)
        {
            _localization.OnLanguageChanged.RemoveListener(OnLanguageChanged);
            _localization.OnLanguageChanged.AddListener(OnLanguageChanged);
        }
        Refresh();
    }

    public override void OnExit()
    {
        if (_localization != null) _localization.OnLanguageChanged.RemoveListener(OnLanguageChanged);
        _presenter?.Dispose();
        _presenter = null;
    }

    private void OnDestroy() { OnExit(); }
    private void OnLanguageChanged(LanguageType language)
    {
        if (_result != null) _result.text = "";
        Refresh();
    }

    // ══════════════════════ UI 构建（居中 800×600）══════════════════════

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null)
        {
            _font = _title.font;
            //prefab 自带的 Title 在左上角（会被 GM 悬浮按钮压住）→ 隐藏，改用内容区里的居中标题
            _title.gameObject.SetActive(false);
        }
        if (_font == null) _font = RunTeamHud.ResolveFont();
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        //居中内容区
        GameObject box = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        RectTransform boxRect = (RectTransform)box.transform;
        boxRect.SetParent(transform, false);
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        bg.raycastTarget = true;

        //标题在 Refresh 中按当前语言填充
        _title = MakeText(boxRect, "Title", "市场", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -66f);
        titleRect.offsetMax = new Vector2(0f, -12f);

        //顶部：钱财余额 + 格位
        _money = MakeText(boxRect, "Money", "", 22f, new Color(1f, 0.86f, 0.45f, 1f));
        RectTransform moneyRect = (RectTransform)_money.transform;
        moneyRect.anchorMin = new Vector2(0f, 1f);
        moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.pivot = new Vector2(0.5f, 1f);
        moneyRect.offsetMin = new Vector2(24f, -108f);
        moneyRect.offsetMax = new Vector2(-24f, -70f);

        //结果行（买/卖结果 + 失败原因）
        _result = MakeText(boxRect, "Result", "", 18f, DimColor);
        RectTransform resultRect = (RectTransform)_result.transform;
        resultRect.anchorMin = new Vector2(0f, 1f);
        resultRect.anchorMax = new Vector2(1f, 1f);
        resultRect.pivot = new Vector2(0.5f, 1f);
        resultRect.offsetMin = new Vector2(24f, -146f);
        resultRect.offsetMax = new Vector2(-24f, -110f);
        _result.alignment = TextAlignmentOptions.Left;

        //列表区（中部，带遮罩裁剪）
        GameObject listGo = new GameObject("ListArea", typeof(RectTransform), typeof(RectMask2D));
        _listArea = (RectTransform)listGo.transform;
        _listArea.SetParent(boxRect, false);
        _listArea.anchorMin = new Vector2(0f, 0f);
        _listArea.anchorMax = new Vector2(1f, 1f);
        _listArea.offsetMin = new Vector2(24f, 84f);
        _listArea.offsetMax = new Vector2(-24f, -150f);

        //底部：关闭
        Button closeButton = MakeButton(boxRect, "Btn_Close", "关闭", 22f,
            new Vector2(0f, 16f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(180f, 48f),
            Close);
        _closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ══════════════════════ 刷新 ══════════════════════

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    /// <summary>可交易 = 启用 且（买价 &gt; 0 或 卖价 &gt; 0）；按 `Item.Index` 升序（稳定、可预期）。</summary>

    private void MakeRow(MarketRow def, float y)
    {
        if (_listArea == null || def == null) return;

        GameObject rowGo = new GameObject("Row_" + def.Id, typeof(RectTransform));
        RectTransform row = (RectTransform)rowGo.transform;
        row.SetParent(_listArea, false);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.offsetMin = new Vector2(0f, y - RowHeight);
        row.offsetMax = new Vector2(0f, y);

        //① 名称
        TextMeshProUGUI name = MakeChildText(row, "Name", def.Name, 20f, NormalColor);
        SetLeftRect((RectTransform)name.transform, 4f, 142f, RowHeight);

        //② 买价 / 卖价
        TextMeshProUGUI price = MakeChildText(row, "Price", def.Price, 18f,
            def.HasPrice ? NormalColor : DimColor);
        SetLeftRect((RectTransform)price.transform, 158f, 158f, RowHeight);

        //③ 持有数

        TextMeshProUGUI hold = MakeChildText(row, "Hold", def.Owned, 18f,
            def.HasOwned ? NormalColor : DimColor);
        SetLeftRect((RectTransform)hold.transform, 328f, 76f, RowHeight);

        //④ 买 / 卖（灰显时行尾给原因）
        bool canBuy = def.CanBuy;
        bool canSell = def.CanSell;

        Button buyButton = MakeButton(row, "Btn_Buy", GameplayText.Text("ui_market_buy", "买"), 18f,
            new Vector2(-100f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(84f, RowHeight - 2f), () => DoBuy(def.Id));
        buyButton.interactable = canBuy;

        Button sellButton = MakeButton(row, "Btn_Sell", GameplayText.Text("ui_market_sell", "卖"), 18f,
            new Vector2(-8f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(84f, RowHeight - 2f), () => DoSell(def.Id));
        sellButton.interactable = canSell;

        string why = def.Reason;
        TextMeshProUGUI reason = MakeChildText(row, "Reason", why, 13f, WarnColor);
        SetLeftRect((RectTransform)reason.transform, 416f, 140f, RowHeight);
    }

    // ══════════════════════ 可买 / 可卖判定（与 MarketService 的失败原因对齐）══════════════════════

    /// <summary>买按钮为什么灰：优先给「钱财不足」，再给格位/叠加（与 §5.1 的状态表一致）。</summary>

    // ══════════════════════ 点按 ══════════════════════

    private void DoBuy(uint itemId)
    {
        Send(PanelAction.Buy, itemId);
    }

    private void DoSell(uint itemId)
    {
        Send(PanelAction.Sell, itemId);
    }

    private void SetResult(string text, Color color)
    {
        if (_result == null) return;
        _result.text = text;
        _result.color = color;
        ChaosLog.Info(LogChannel.UI, "[市场] 面板结果：" + text);
    }

    /// <summary>
    /// 关闭：走 `UIManager.PopPanel()` 而不是直接 `Hide()`。
    /// 直接 Hide 会把面板留在 `panelStack` 里且 `IsShowing=false`，之后再 `PushPanel` 会命中
    /// “重复打开栈顶面板”的分支直接 return → 面板永远打不开（库存面板就是这么踩过的）。
    /// </summary>
    private void Close() { Send(PanelAction.Close); }

    // ══════════════════════ 通用小工具（文本由调用处解析本地化）══════════════════════

    private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private TextMeshProUGUI MakeChildText(RectTransform parent, string name, string text, float size, Color color)
    {
        TextMeshProUGUI label = MakeText(parent, name, text, size, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.pivot = new Vector2(0f, 0.5f);
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        // Each column owns its bounds; longer translations shrink before using an ellipsis.
        // The 12px gaps keep the name, prices, count, reason and buttons visually separate.
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(12f, size * 0.8f);
        label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    /// <summary>左侧固定宽度的列（anchorX=0，纵向居中）。</summary>
    private static void SetLeftRect(RectTransform rect, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, height);
    }

    private Button MakeButton(Transform parent, string name, string text, float fontSize,
        Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.20f, 0.42f, 0.72f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
        colors.disabledColor = new Color(0.55f, 0.57f, 0.62f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", text, fontSize, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void ClearList()
    {
        if (_listArea == null) return;
        for (int i = _listArea.childCount - 1; i >= 0; i--)
        {
            Transform child = _listArea.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { SetResult(message, success ? OkColor : WarnColor); }
    public void Render(MarketState state)
    {
        if (_title != null) _title.text = GameplayText.Text("ui_market_title", "市场");
        if (_closeLabel != null) _closeLabel.text = GameplayText.Text("ui_common_close", "关闭");
        if (_money != null) _money.text = state.Money;
        if (_result != null && string.IsNullOrEmpty(_result.text)) { _result.text = state.Hint; _result.color = DimColor; }
        ClearRows();
        for (int i = 0; i < state.Rows.Count; i++) MakeRow(state.Rows[i], -2f - i * RowStep);
    }

    private void ClearRows()
    {
        if (_listArea == null) return;
        for (int i = _listArea.childCount - 1; i >= 0; i--)
        {
            Transform row = _listArea.GetChild(i);
            row.SetParent(null, false);
            Destroy(row.gameObject);
        }
    }
}
