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

/// <summary>Passive Retainer view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class RetainerPanel : BasePanel, IGameplayView<RetainerState>
{
    public event Action<PanelIntent> Intent;
    private RetainerPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new RetainerPresenter(_recruitment);
        _presenter.Attach(this);
    }
    private void Send(PanelAction action, uint id = 0)
    {
        if (!IsShowing) return;
        EnsurePresenter();
        Intent?.Invoke(new PanelIntent(action, id));
    }

    private static readonly Color AvailableColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color WoundedColor = new Color(1.00f, 0.36f, 0.32f, 1f);
    private static readonly Color LowConfidenceColor = new Color(1.00f, 0.82f, 0.25f, 1f);
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.76f, 0.82f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _summary;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;
    private Button _recruitButton;
    private TextMeshProUGUI _closeLabel;
    private readonly RetainerRecruitmentModel _recruitment = new RetainerRecruitmentModel();

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
        if (_title != null)
        {
            _font = _title.font;
            _title.alignment = TextAlignmentOptions.Center;
            ApplyLocalized(_title, "ui_retainer_panel_title", "门客");
        }
        if (_font == null) _font = RunTeamHud.ResolveFont();

        _summary = CreateText("Summary", 64f, NormalColor, 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 100f, DimColor, 20f);

        CreateCloseButton();
        CreateRecruitButton();
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void RecruitOne()
    {
        Send(PanelAction.Recruit);
    }

    /// <summary>状态标签（中文）：重伤 Ns / 信心不足 / 可用 —— 优先级 重伤 &gt; 信心不足 &gt; 可用。</summary>

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

    private void AddRow(int index, string text, Color color)
    {
        if (_content == null) return;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -8f - index * 62f);
        rect.sizeDelta = new Vector2(-48f, 58f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        label.text = text;
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
        rect.sizeDelta = new Vector2(-48f, 30f);

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
        rect.offsetMin = new Vector2(0f, 90f);
        rect.offsetMax = new Vector2(0f, -142f);
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
        ApplyLocalized(label, "ui_common_close", "关闭");

        go.GetComponent<Button>().onClick.AddListener(() => Send(PanelAction.Close));
    }

    private void CreateRecruitButton()
    {
        GameObject go = new GameObject("RecruitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-204f, 24f);
        rect.sizeDelta = new Vector2(180f, 48f);
        Image background = go.GetComponent<Image>();
        background.color = new Color(0.20f, 0.42f, 0.72f, 1f);
        _recruitButton = go.GetComponent<Button>();
        _recruitButton.targetGraphic = background;
        _recruitButton.onClick.AddListener(RecruitOne);

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
        ApplyLocalized(label, "ui_retainer_recruit", "招募门客");
    }

    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback, params object[] args)
    {
        if (label == null) return;
        label.text = GameplayText.Text(key, fallback, args);
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(RetainerState state)
    {
        if (_title != null) ApplyLocalized(_title, "ui_retainer_panel_title", "门客");
        if (_closeLabel != null) ApplyLocalized(_closeLabel, "ui_common_close", "关闭");
        if (_summary != null) _summary.text = state.Summary;
        if (_hint != null) _hint.text = state.Hint;
        if (_recruitButton != null)
        {
            _recruitButton.interactable = state.CanRecruit;
            ApplyLocalized(_recruitButton.GetComponentInChildren<TextMeshProUGUI>(), "ui_retainer_recruit", "招募门客");
        }
        ClearRows();
        for (int i = 0; i < state.Rows.Count; i++)
        {
            var row = state.Rows[i];
            AddRow(i, row.Text, row.Tone == TextTone.Warning ? WoundedColor : row.Tone == TextTone.Dim ? LowConfidenceColor : AvailableColor);
        }
    }
}
