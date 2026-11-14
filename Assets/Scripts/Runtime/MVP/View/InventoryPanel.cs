using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
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

    // 行复用缓存：刷新只更新已有行文本/颜色，不重建整屏条目。
    private sealed class RowWidget
    {
        public GameObject Root;
        public Image Block;
        public TextMeshProUGUI Label;
    }

    private readonly List<RowWidget> _rows = new List<RowWidget>();
    private ScrollRect _scroll;

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
    private static readonly Color PermanentColor = new Color(0.95f, 0.85f, 0.45f, 1f);

    private const float RowHeight = 46f;
    private const float RowTop = 8f;

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
    private Button _upgrade;
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
        _localization = null;
        _presenter?.Dispose();
        _presenter = null;
        ClearRows();
    }

    private void OnDestroy() { OnExit(); }
    private void OnLanguageChanged(LanguageType language) { Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null)
        {
            _font = _title.font;
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(.5f, 1);
            titleRect.offsetMin = new Vector2(180, -56); titleRect.offsetMax = new Vector2(-180, -8);
            _title.alignment = TextAlignmentOptions.Center;
        }

        _resources = CreateText("Resources", 64f, NormalColor, 22f);
        _slots = CreateText("Slots", 100f, NormalColor, 22f);
        CreateScrollArea();
        _hint = CreateText("Hint", 128f, WarnColor, 20f);

        CreateCloseButton();
        _upgrade = GameplayUi.Button(transform, "WarehouseManagement", "仓库升级", Vector2.zero, new Vector2(160f, 48f),
            () => Send(PanelAction.Upgrade));
        var upgradeRect = (RectTransform)_upgrade.transform;
        upgradeRect.anchorMin = upgradeRect.anchorMax = Vector2.zero;
        upgradeRect.pivot = Vector2.zero;
        upgradeRect.anchoredPosition = new Vector2(24, 24);
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    /// <summary>滚动区域：ScrollRect + 视口 Mask + 行容器；关闭按钮不在其中，不受遮挡。</summary>
    private void CreateScrollArea()
    {
        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform viewport = (RectTransform)viewportGo.transform;
        viewport.SetParent(transform, false);
        viewport.anchorMin = new Vector2(0f, 0f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.offsetMin = new Vector2(0f, 156f);
        viewport.offsetMax = new Vector2(0f, -140f);
        Image viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;
        viewportGo.AddComponent<RectMask2D>();

        GameObject contentGo = new GameObject("Rows", typeof(RectTransform));
        _content = (RectTransform)contentGo.transform;
        _content.SetParent(viewport, false);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);

        _scroll = gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport;
        _scroll.content = _content;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 24f;
        _scroll.inertia = false;
    }

    private void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            RowWidget widget = _rows[i];
            if (widget == null || widget.Root == null) continue;
            widget.Root.SetActive(false);
            Destroy(widget.Root);
        }
        _rows.Clear();
        if (_content != null)
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);
                if (child == null) continue;
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
    }

    /// <summary>按需取得行部件（复用已有的，只有不够时才新建）。</summary>
    private RowWidget EnsureRow(int index)
    {
        while (_rows.Count <= index)
        {
            int created = _rows.Count;
            if (_content == null) return null;

            float y = -RowTop - created * RowHeight;

            GameObject block = new GameObject("Block" + created, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform blockRect = (RectTransform)block.transform;
            blockRect.SetParent(_content, false);
            blockRect.anchorMin = new Vector2(0f, 1f);
            blockRect.anchorMax = new Vector2(0f, 1f);
            blockRect.pivot = new Vector2(0f, 1f);
            blockRect.anchoredPosition = new Vector2(24f, y);
            blockRect.sizeDelta = new Vector2(36f, 36f);
            Image image = block.GetComponent<Image>();
            GameplayUiSkin.Apply(image, "UI_Slot_Quality", Color.white);
            image.raycastTarget = false;

            GameObject go = new GameObject("Row" + created, typeof(RectTransform));
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

            _rows.Add(new RowWidget { Root = block, Block = image, Label = label });
        }
        return _rows[index];
    }

    private void RenderRows(InventoryState state)
    {
        if (_content == null) return;
        int shown = 0;
        if (state.Rows != null && state.HasSession)
            for (int i = 0; i < state.Rows.Count; i++)
            {
                InventoryRow row = state.Rows[i];
                if (row == null) continue;
                RowWidget widget = EnsureRow(shown);
                if (widget == null) break;
                shown++;
                widget.Root.SetActive(true);
                widget.Label.gameObject.SetActive(true);
                widget.Block.color = row.Permanent ? PermanentColor : CategoryColor(row.Category);
                widget.Label.color = row.Permanent ? PermanentColor : NormalColor;
                string name = row.Name ?? string.Empty;
                string firstChar = name.Length > 0 ? name.Substring(0, 1) : "?";
                widget.Label.text = firstChar + "　" + name + "　" + (row.Count ?? string.Empty);
            }

        for (int i = shown; i < _rows.Count; i++)
            if (_rows[i]?.Root != null) { _rows[i].Root.SetActive(false); _rows[i].Label.gameObject.SetActive(false); }

        _content.sizeDelta = new Vector2(0f, RowTop * 2f + shown * RowHeight);
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

    /// <summary>右下角「关闭」按钮（位于滚动区域之外，始终可点）。</summary>
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

        var close = go.GetComponent<Button>();
        close.targetGraphic = bg;
        GameplayUiSkin.Button(close, "UI_Btn_Sm");
        close.onClick.AddListener(() => Send(PanelAction.Close));
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(InventoryState state)
    {
        if (state == null) return;
        if (_title != null) _title.text = string.IsNullOrEmpty(state.Title)
            ? GameplayText.Text("ui_inventory_panel_title", "库存") : state.Title;
        if (_closeLabel != null) _closeLabel.text = GameplayText.Text("ui_common_close", "关闭");
        if (_resources != null)
        {
            _resources.text = state.Resources;
            _resources.gameObject.SetActive(state.HasSession);
        }
        if (_slots != null)
        {
            _slots.text = state.Slots;
            _slots.gameObject.SetActive(state.HasSession);
        }
        if (_hint != null)
        {
            _hint.text = state.Hint;
            _hint.gameObject.SetActive(!string.IsNullOrEmpty(state.Hint));
        }
        RenderRows(state);
        if (_upgrade != null) _upgrade.gameObject.SetActive(state.CanManageWarehouse);
    }
}
