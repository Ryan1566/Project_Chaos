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

/// <summary>Passive Dispatch view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class DispatchPanel : BasePanel, IGameplayView<DispatchState>
{
    private void OnDestroy() { OnExit(); }

    public override void OnExit() { _presenter?.Dispose(); _presenter = null; }

    public event Action<PanelIntent> Intent;
    private DispatchPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new DispatchPresenter();
        _presenter.Attach(this);
    }
    private void Send(PanelAction action, uint id = 0)
    {
        if (!IsShowing) return;
        EnsurePresenter();
        Intent?.Invoke(new PanelIntent(action, id));
    }

    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color SelectedColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color WoundedColor = new Color(1.00f, 0.36f, 0.32f, 1f);
    private static readonly Color LowConfColor = new Color(1.00f, 0.82f, 0.25f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _summary;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;
        _title = FindChildText("Title");
        if (_title != null) _font = _title.font;
        if (_font == null) _font = RunTeamHud.ResolveFont();
        if (_title != null) _title.alignment = TextAlignmentOptions.Center;
        _summary = CreateText("Summary", 64f, NormalColor, 22f);
        _content = CreateRect("Content", 140f);
        _hint = CreateText("Hint", 96f, DimColor, 20f);
        CreateBottomButtons();
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void AddRow(int index, string text, Color color, uint id, bool interactable = true)
    {
        if (_content == null) return;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -8f - index * 46f);
        rect.sizeDelta = new Vector2(-48f, 40f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.16f, 0.20f, 1f);

        GameObject labelGo = new GameObject("Text", typeof(RectTransform));
        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f); labelRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = 22f; label.color = color;
        label.alignment = TextAlignmentOptions.Left; label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 22f;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = text;

        if (id != 0u)
        {
            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = interactable;
            button.onClick.AddListener(() => DoToggle(id));
        }
        else
        {
            UnityEngine.Object.Destroy(go.GetComponent<Button>());
        }
    }

    private void DoToggle(uint id)
    {
        Send(PanelAction.Toggle, id);
    }

    private void DoDepart()
    {
        Send(PanelAction.Depart);
    }

    private void DoReturn()
    {
        Send(PanelAction.Return);
    }

    private void CreateBottomButtons()
    {
        CreateButton("Btn_Depart", "出发", new Vector2(-380f, 24f), () => DoDepart());
        CreateButton("Btn_Return", "撤离 / 返回局外", new Vector2(-200f, 24f), () => DoReturn());
        CreateButton("Btn_Close", "关闭", new Vector2(-24f, 24f), Close);
    }

    private void Close() { Send(PanelAction.Close); }

    private void CreateButton(string name, string label, Vector2 offset, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(160f, 48f);
        go.GetComponent<Image>().color = new Color(0.20f, 0.42f, 0.72f, 1f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 22f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        ApplyLocalized(tmp, "ui_dispatch_btn_" + name, label);
    }

    private TextMeshProUGUI FindChildText(string n) { Transform c = transform.Find(n); return c == null ? null : c.GetComponent<TextMeshProUGUI>(); }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 28f);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft; label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name, float top)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 90f); rect.offsetMax = new Vector2(0f, -top);
        return rect;
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        { Transform c = _content.GetChild(i); c.SetParent(null, false); Destroy(c.gameObject); }
    }

    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = GameplayText.Text(key, fallback);
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(DispatchState state)
    {
        if (_title != null) ApplyLocalized(_title, "ui_dispatch_title", "派遣");
        if (_summary != null) _summary.text = state.Summary;
        if (_hint != null) _hint.text = state.Hint;
        ClearRows();
        if (state.Rows.Count == 0) AddRow(0, "暂无门客，请先招募门客。", DimColor, 0u);
        for (int i = 0; i < state.Rows.Count; i++)
        {
            var row = state.Rows[i];
            Color color = row.Tone == TextTone.Warning ? WoundedColor : row.Tone == TextTone.Dim ? LowConfColor
                : row.Tone == TextTone.Success ? SelectedColor : NormalColor;
            AddRow(i, row.Text, color, row.Id, row.Enabled);
        }
    }
}
