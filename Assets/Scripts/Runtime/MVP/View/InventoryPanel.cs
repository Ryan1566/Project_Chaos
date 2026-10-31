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

/// <summary>Passive Inventory view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class InventoryPanel : BasePanel, IGameplayView<InventoryState>
{
    public event Action<PanelIntent> Intent;
    private InventoryPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new InventoryPresenter();
        _presenter.Attach(this);
    }
    private void Send(PanelAction action, uint id = 0)
    {
        if (!IsShowing) return;
        EnsurePresenter();
        Intent?.Invoke(new PanelIntent(action, id));
    }

    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.76f, 0.82f, 1f);

    /// <summary>类别 → 占位色（1 药品 / 2 材料 / 3 装备 / 4 图纸 / 5 消耗 / 6 其它）。</summary>
    private static Color CategoryColor(uint category)
    {
        switch (category)
        {
            case 1: return new Color(0.85f, 0.35f, 0.40f, 1f);
            case 2: return new Color(0.55f, 0.62f, 0.45f, 1f);
            case 3: return new Color(0.40f, 0.55f, 0.80f, 1f);
            case 4: return new Color(0.70f, 0.55f, 0.85f, 1f);
            case 5: return new Color(0.90f, 0.70f, 0.35f, 1f);
            default: return new Color(0.60f, 0.62f, 0.66f, 1f);
        }
    }

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _resources;
    private TextMeshProUGUI _slots;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;
    private TextMeshProUGUI _closeLabel;
    private LocalizationManager _localization;

    private void Awake()
    {
        BuildUi();
    }

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
    private void OnLanguageChanged(LanguageType language) { Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null) _font = _title.font;

        _resources = CreateText("Resources", 64f, NormalColor, 22f);
        _slots = CreateText("Slots", 100f, NormalColor, 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 128f, WarnColor, 20f);

        CreateCloseButton();
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Transform child = _content.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void AddRow(int index, string firstChar, string name, string countText, Color color)
    {
        if (_content == null) return;

        float y = -8f - index * 46f;

        GameObject block = new GameObject("Block" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform blockRect = (RectTransform)block.transform;
        blockRect.SetParent(_content, false);
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(0f, 1f);
        blockRect.pivot = new Vector2(0f, 1f);
        blockRect.anchoredPosition = new Vector2(24f, y);
        blockRect.sizeDelta = new Vector2(36f, 36f);
        block.GetComponent<Image>().color = color;
        block.GetComponent<Image>().raycastTarget = false;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(72f, y);
        rect.sizeDelta = new Vector2(-96f, 36f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = NormalColor;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        // 格子内容：首字 + 名称 + 数量/上限（如 12/99）
        label.text = firstChar + "　" + name + "　" + countText;
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 28f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 40f);
        rect.offsetMax = new Vector2(0f, -210f);
        return rect;
    }

    /// <summary>右下角「关闭」按钮。</summary>
    private void CreateCloseButton()
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 48f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.21f, 0.26f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = NormalColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        _closeLabel = label;
        label.text = GameplayText.Text("ui_common_close", "关闭");

        go.GetComponent<Button>().onClick.AddListener(() => Send(PanelAction.Close));
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(InventoryState state)
    {
        if (_title != null) _title.text = GameplayText.Text("ui_inventory_panel_title", "库存");
        if (_closeLabel != null) _closeLabel.text = GameplayText.Text("ui_common_close", "关闭");
        if (_resources != null) _resources.text = state.Resources;
        if (_slots != null) { _slots.text = state.Slots; _slots.color = state.Full ? WarnColor : NormalColor; }
        if (_hint != null) _hint.text = state.Hint;
        ClearRows();
        for (int i = 0; i < state.Rows.Count; i++)
        {
            var row = state.Rows[i];
            AddRow(i, row.Name.Length > 0 ? row.Name.Substring(0, 1) : "?", row.Name, row.Count, CategoryColor(row.Category));
        }
    }
}
