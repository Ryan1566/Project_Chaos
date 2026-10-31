using System;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive RunSettlement view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
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

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _hint;
    private TextMeshProUGUI _woundLine;
    private RectTransform _listArea;
    private TMP_FontAsset _font;
    private bool _built;

    public static void Open(bool win)
    {
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
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        bg.raycastTarget = true;

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

        //列表区（中部，带遮罩裁剪）
        GameObject listGo = new GameObject("ListArea", typeof(RectTransform));
        _listArea = (RectTransform)listGo.transform;
        _listArea.SetParent(boxRect, false);
        _listArea.anchorMin = new Vector2(0f, 0f);
        _listArea.anchorMax = new Vector2(1f, 1f);
        _listArea.offsetMin = new Vector2(24f, 96f);
        _listArea.offsetMax = new Vector2(-24f, -212f);
        listGo.AddComponent<RectMask2D>();

        //底部按钮（居中并排，纯中文）
        MakeButton(boxRect, "Btn_Confirm", "确认", new Vector2(-100f, 30f), Confirm);
        MakeButton(boxRect, "Btn_Discard", "放弃全部", new Vector2(100f, 30f), DiscardAll);
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void MakeRow(string text, Color color, float y)
    {
        if (_listArea == null) return;
        TextMeshProUGUI label = MakeText(_listArea, "Row", text, 22f, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(8f, y - 34f);
        rect.offsetMax = new Vector2(-150f, y);
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void MakeSelectButton(uint itemId, bool selected, float y)
    {
        if (_listArea == null) return;
        GameObject go = new GameObject("Sel_" + itemId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_listArea, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(-136f, y - 34f);
        rect.offsetMax = new Vector2(-8f, y);
        go.GetComponent<Image>().color = selected ? new Color(0.25f, 0.55f, 0.35f, 1f) : new Color(0.2f, 0.42f, 0.72f, 1f);
        TextMeshProUGUI tmp = MakeText(rect, "Text", selected ? "取消" : "选中", 20f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        uint captured = itemId;
        go.GetComponent<Button>().onClick.AddListener(() => ToggleSelect(captured));
    }

    private void ToggleSelect(uint itemId)
    {
        Send(PanelAction.Toggle, itemId);
    }

    // ══════════════════ 结算推进（与 W3.2e 一致，未改）══════════════════
    public override void HandleBack() { Confirm(); }

    private void Confirm()
    {
        Send(PanelAction.Confirm);
    }

    private void DiscardAll()
    {
        Send(PanelAction.Discard);
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

    private void MakeButton(Transform parent, string name, string label, Vector2 offset, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(180f, 52f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.42f, 0.72f, 1f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void ClearList()
    {
        if (_listArea == null) return;
        for (int i = _listArea.childCount - 1; i >= 0; i--)
        {
            Transform c = _listArea.GetChild(i);
            c.SetParent(null, false);
            Destroy(c.gameObject);
        }
    }

    private TextMeshProUGUI FindChildText(string n)
    {
        Transform c = transform.Find(n);
        return c == null ? null : c.GetComponent<TextMeshProUGUI>();
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(SettlementState state)
    {
        if (_title != null) _title.text = state.Title;
        if (_hint != null) _hint.text = state.Hint;
        if (_woundLine != null) _woundLine.text = state.Wounds;
        ClearList();
        float y = -4f;
        foreach (var row in state.Rows)
        {
            MakeRow(row.Text, row.Tone == TextTone.Success ? new Color(0.35f, 0.88f, 0.45f, 1f) : new Color(0.92f, 0.93f, 0.96f, 1f), y);
            if (state.Win) MakeSelectButton(row.Id, row.Selected, y);
            y -= 42f;
        }
    }
}
