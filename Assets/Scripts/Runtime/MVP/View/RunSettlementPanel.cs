using System;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>结算只读视图：构建三页控件、渲染快照，并把玩家意图交给呈现器。</summary>
public class RunSettlementPanel : BasePanel, IGameplayView<SettlementState>
{
    private void OnDestroy() { OnExit(); }

    public override void OnExit() { _presenter?.Dispose(); _presenter = null; }

    public event Action<PanelIntent> Intent;
    private SettlementPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = SettlementPresenter.ForCurrentRun();
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
    private const float RowHeight = 42f;

    private TextMeshProUGUI _title;
    private Image _resultBackground;
    private TextMeshProUGUI _hint;
    private TextMeshProUGUI _woundLine;
    private TextMeshProUGUI _pageLabel;
    private RectTransform _listArea;
    private RectTransform _listContent;
    private ScrollRect _scroll;
    private Button _prevButton, _nextButton, _confirmButton;
    private TextMeshProUGUI _prevText, _nextText, _confirmText;
    private TMP_FontAsset _font;
    private bool _built;
    private bool _hasRendered;
    private int _renderedPage = -1;

    public static void Open(bool win)
    {
        // 结算会冻结时间，先回收最后一击的飘字，避免数字长期覆盖结算内容。
        DamageFloaters.Clear();
        SettlementPresenter.Begin(win);
        if (UIManager.Instance == null) return;

        //缺陷B 修复：局内 Canvas 被隐藏 → 先恢复再加面板
        UIManager.Instance.RestoreAfterGameplay();
        RunDepthHud.SetVisible(false);   //W4e：结算时隐藏 HUD
        RunTeamHud.SetVisible(false);    //W3.3：结算隐藏队伍血条
        RunTerrainToast.DisableAndDestroy();
        UIManager.Instance.PushPanel(PanelType.RunSettlementPanel);

        RunSettlementPanel panel = Object.FindFirstObjectByType<RunSettlementPanel>(FindObjectsInactive.Include);
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
    }

    /// <summary>缺陷A 修复：PushPanel 后显式刷新（面板已缓存时 OnEnter 不重跑）。</summary>
    public void RefreshFromOpen()
    {
        BuildUi();
        Refresh();
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    // ══════════════════ UI 构建（居中 800×600）══════════════════
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
        _resultBackground = box.GetComponent<Image>();
        _resultBackground.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        _resultBackground.raycastTarget = true;

        //标题（顶部居中，纯中文，不挂 LocalizedText）
        _title = MakeText(boxRect, "Title", "局内结算", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -70f);
        titleRect.offsetMax = new Vector2(0f, -16f);

        //提示（标题下方）
        _hint = MakeText(boxRect, "Hint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        _hint.enableWordWrapping = true;
        _hint.enableAutoSizing = true;
        _hint.fontSizeMin = 16f;
        _hint.fontSizeMax = 20f;
        _hint.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform hintRect = (RectTransform)_hint.transform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(24f, -160f);
        hintRect.offsetMax = new Vector2(-24f, -80f);

        //W3.3：重伤行（"N 名门客重伤（30s）"）
        _woundLine = MakeText(boxRect, "WoundLine", "", 20f, new Color(1f, 0.55f, 0.45f, 1f));
        RectTransform woundRect = (RectTransform)_woundLine.transform;
        woundRect.anchorMin = new Vector2(0f, 1f);
        woundRect.anchorMax = new Vector2(1f, 1f);
        woundRect.pivot = new Vector2(0.5f, 1f);
        woundRect.offsetMin = new Vector2(24f, -204f);
        woundRect.offsetMax = new Vector2(-24f, -164f);

        //列表区（中部，带遮罩与滚动；内容超过一屏时可拖动或滚轮查看）
        GameObject listGo = new GameObject("ListArea", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        _listArea = (RectTransform)listGo.transform;
        _listArea.SetParent(boxRect, false);
        _listArea.anchorMin = new Vector2(0f, 0f);
        _listArea.anchorMax = new Vector2(1f, 1f);
        _listArea.offsetMin = new Vector2(24f, 118f);
        _listArea.offsetMax = new Vector2(-24f, -212f);
        Image listBg = listGo.GetComponent<Image>();
        listBg.color = new Color(0.06f, 0.07f, 0.09f, 0.72f);
        listBg.raycastTarget = true;

        _scroll = listGo.GetComponent<ScrollRect>();
        _scroll.viewport = _listArea;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 28f;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.135f;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        _listContent = (RectTransform)contentGo.transform;
        _listContent.SetParent(_listArea, false);
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.anchoredPosition = Vector2.zero;
        _listContent.sizeDelta = Vector2.zero;
        _scroll.content = _listContent;

        //底部页码与翻页按钮；确认按钮只在第三页出现
        _pageLabel = MakeText(boxRect, "PageLabel", "", 18f, new Color(0.74f, 0.80f, 0.90f, 1f));
        RectTransform pageRect = (RectTransform)_pageLabel.transform;
        pageRect.anchorMin = new Vector2(0.5f, 0f);
        pageRect.anchorMax = new Vector2(0.5f, 0f);
        pageRect.pivot = new Vector2(0.5f, 0f);
        pageRect.anchoredPosition = new Vector2(0f, 82f);
        pageRect.sizeDelta = new Vector2(680f, 28f);

        _prevButton = MakeButton(boxRect, "Btn_Prev", "上一页", new Vector2(-250f, 22f), PrevPage, 160f);
        _prevText = _prevButton.GetComponentInChildren<TextMeshProUGUI>();
        _nextButton = MakeButton(boxRect, "Btn_Next", "下一页", new Vector2(-70f, 22f), NextPage, 160f);
        _nextText = _nextButton.GetComponentInChildren<TextMeshProUGUI>();
        _confirmButton = MakeButton(boxRect, "Btn_Confirm", "确认结算并返回", new Vector2(175f, 22f), Confirm, 210f);
        _confirmText = _confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        _confirmButton.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (!_built) BuildUi();
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void MakeRow(string text, Color color, float y, bool reserveAction)
    {
        if (_listContent == null) return;
        TextMeshProUGUI label = MakeText(_listContent, "Row", text, 21f, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(8f, y - (RowHeight - 6f));
        rect.offsetMax = new Vector2(reserveAction ? -154f : -8f, y);
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 15f;
        label.fontSizeMax = 21f;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void MakeSelectButton(TextRow row, float y)
    {
        if (_listContent == null) return;
        GameObject go = new GameObject("Sel_" + row.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_listContent, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(-136f, y - (RowHeight - 6f));
        rect.offsetMax = new Vector2(-8f, y);
        Image image = go.GetComponent<Image>();
        image.color = !row.Enabled
            ? new Color(0.28f, 0.30f, 0.35f, 1f)
            : row.Selected ? new Color(0.25f, 0.55f, 0.35f, 1f) : new Color(0.2f, 0.42f, 0.72f, 1f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = row.Enabled;
        if (row.Enabled && row.Action == PanelAction.Toggle)
        {
            uint captured = row.Id;
            button.onClick.AddListener(() => ToggleSelect(captured));
        }

        TextMeshProUGUI tmp = MakeText(rect, "Text", row.ActionLabel ?? "", 19f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 19f;
    }

    private void ToggleSelect(uint itemId)
    {
        Send(PanelAction.Toggle, itemId);
    }

    // Esc 不关闭也不提交结算：只做相邻页回退，避免绕过最终页确认。
    public override void HandleBack() { Send(PanelAction.Return); }

    private void PrevPage() { Send(PanelAction.Return); }

    private void NextPage() { Send(PanelAction.Advance); }

    private void Confirm()
    {
        Send(PanelAction.Confirm);
    }

    // ── 通用小工具（面板内文案一律纯中文，不挂 LocalizedText）──
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

    private Button MakeButton(Transform parent, string name, string label, Vector2 offset, System.Action onClick, float width)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(width, 48f);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.42f, 0.72f, 1f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void ClearList()
    {
        if (_listContent == null) return;
        for (int i = _listContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = _listContent.GetChild(i).gameObject;
            // 先移出滚动内容，避免同一帧重复刷新时新旧行重叠响应。
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Destroy(child);
        }
    }

    private TextMeshProUGUI FindChildText(string n)
    {
        Transform c = transform.Find(n);
        return c == null ? null : c.GetComponent<TextMeshProUGUI>();
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message ?? ""; }

    public void Render(SettlementState state)
    {
        BuildUi();
        if (state == null) return;

        if (_title != null)
        {
            _title.text = state.Title;
            _title.color = state.Win ? new Color(.72f, .95f, .76f) : new Color(1f, .65f, .59f);
        }
        if (_resultBackground != null)
            _resultBackground.color = state.Win ? new Color(.075f, .14f, .12f, .98f) : new Color(.17f, .10f, .11f, .98f);
        if (_hint != null) _hint.text = string.IsNullOrEmpty(state.Notice) ? state.Hint : state.Notice;
        if (_woundLine != null)
        {
            _woundLine.text = state.Wounds ?? "";
            _woundLine.gameObject.SetActive(!string.IsNullOrEmpty(_woundLine.text));
        }
        if (_pageLabel != null) _pageLabel.text = state.PageLabel;

        _prevButton.interactable = state.CanPrev;
        _nextButton.interactable = state.CanNext;
        if (_prevText != null) _prevText.text = state.PrevLabel ?? "上一页";
        if (_nextText != null) _nextText.text = state.NextLabel ?? "下一页";
        bool showConfirm = state.CanConfirm && !string.IsNullOrEmpty(state.ConfirmLabel);
        _confirmButton.gameObject.SetActive(showConfirm);
        if (_confirmText != null) _confirmText.text = state.ConfirmLabel ?? "";

        float previousScroll = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
        bool resetScroll = !_hasRendered || _renderedPage != state.Page;

        ClearList();
        float y = -4f;
        IReadOnlyList<TextRow> rows = state.Rows;
        if (rows == null || rows.Count == 0)
        {
            MakeRow("本页暂无可显示内容。", new Color(0.65f, 0.68f, 0.74f, 1f), y, false);
            y -= RowHeight;
        }
        else
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                bool reserveAction = !string.IsNullOrEmpty(row.ActionLabel);
                MakeRow(row.Text ?? "", ToneColor(row.Tone), y, reserveAction);
                if (reserveAction) MakeSelectButton(row, y);
                y -= RowHeight;
            }
        }

        if (_listContent != null)
            _listContent.sizeDelta = new Vector2(0f, Mathf.Max(0f, -y + 4f));
        Canvas.ForceUpdateCanvases();
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = resetScroll ? 1f : Mathf.Clamp01(previousScroll);

        _renderedPage = state.Page;
        _hasRendered = true;
    }

    private static Color ToneColor(TextTone tone)
    {
        switch (tone)
        {
            case TextTone.Success: return new Color(0.35f, 0.88f, 0.45f, 1f);
            case TextTone.Warning: return new Color(1f, 0.62f, 0.34f, 1f);
            case TextTone.Dim: return new Color(0.65f, 0.68f, 0.74f, 1f);
            default: return new Color(0.92f, 0.93f, 0.96f, 1f);
        }
    }
}
