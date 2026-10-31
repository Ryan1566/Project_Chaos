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

/// <summary>Passive FamilyTree view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class FamilyTreePanel : BasePanel, IGameplayView<FamilyTreeState>
{
    private void OnDestroy() { OnExit(); }

    public override void OnExit() { _presenter?.Dispose(); _presenter = null; }

    public event Action<PanelIntent> Intent;
    private FamilyTreePresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new FamilyTreePresenter();
        _presenter.Attach(this);
    }
    private void Send(PanelAction action, uint id = 0)
    {
        if (!IsShowing) return;
        EnsurePresenter();
        Intent?.Invoke(new PanelIntent(action, id));
    }

    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.66f, 0.70f, 0.76f, 1f);
    private static readonly Color UnlockedColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color ReadyColor = new Color(1.00f, 0.82f, 0.25f, 1f);

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
        _summary = CreateText("Summary", 64f, NormalColor, 22f);
        _content = CreateRect("Content", 110f);
        _hint = CreateText("Hint", 96f, ReadyColor, 20f);
        CreateCloseButton();
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void AddTextRow(float y, string text, Color color, float size)
    {
        if (_content == null) return;
        GameObject go = new GameObject("Row", typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, y);
        rect.sizeDelta = new Vector2(-260f, 28f);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Left; label.raycastTarget = false;
        label.text = text;
    }

    private void AddLightButton(float y, uint nodeId, bool interactable)
    {
        if (_content == null) return;
        GameObject go = new GameObject("Light_" + nodeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(1f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, y);
        rect.sizeDelta = new Vector2(150f, 30f);
        Image bg = go.GetComponent<Image>();
        bg.color = interactable ? new Color(0.20f, 0.42f, 0.72f, 1f) : new Color(0.22f, 0.24f, 0.28f, 1f);
        Button button = go.GetComponent<Button>();
        button.interactable = interactable;
        if (interactable) button.onClick.AddListener(() => DoUnlock(nodeId));

        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 18f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        tmp.text = "点亮";
    }

    private void DoUnlock(uint nodeId)
    {
        Send(PanelAction.Unlock, nodeId);
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
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 40f); rect.offsetMax = new Vector2(0f, -top);
        return rect;
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        { Transform c = _content.GetChild(i); c.SetParent(null, false); Destroy(c.gameObject); }
    }

    private void CreateCloseButton()
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 48f);
        go.GetComponent<Image>().color = new Color(0.18f, 0.21f, 0.26f, 1f);
        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 22f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        ApplyLocalized(tmp, "ui_common_close", "关闭");
        go.GetComponent<Button>().onClick.AddListener(() => Send(PanelAction.Close));
    }

    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = fallback;
        LocalizedText loc = label.GetComponent<LocalizedText>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedText>();
        loc.localizationKey = key; loc.autoUpdateOnStart = false; loc.listenToLanguageChange = false;
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(FamilyTreeState state)
    {
        if (_title != null) ApplyLocalized(_title, "ui_familytree_title", "家族树");
        if (_summary != null) _summary.text = state.Summary;
        if (_hint != null) _hint.text = "（点亮的节点效果会立刻聚合：带出上限/格位/门客属性/扎营补员）";
        ClearRows();
        float y = -8f;
        uint lastBoard = 0;
        foreach (var row in state.Rows)
        {
            if (row.Group != lastBoard) { lastBoard = row.Group; AddTextRow(y, "【板块 " + row.Group + "】", ReadyColor, 22f); y -= 34f; }
            AddTextRow(y, row.Text, row.Tone == TextTone.Success ? UnlockedColor : row.Tone == TextTone.Warning ? ReadyColor : DimColor, 20f);
            AddLightButton(y, row.Id, row.Enabled);
            y -= 38f;
        }
    }
}
