using System;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive EraProgress view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class EraProgressPanel : BasePanel, IGameplayView<EraProgressState>
{
    private void OnDestroy() { OnExit(); }

    public override void OnExit() { _presenter?.Dispose(); _presenter = null; }

    public event Action<PanelIntent> Intent;
    private EraProgressPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new EraProgressPresenter();
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
    private const int MaxEra = 5;

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _lines;      //8 行合并成一个多行文本（左对齐）
    private TextMeshProUGUI _hint;
    private Button _advanceButton;
    private Button _cancelButton;
    private TMP_FontAsset _font;
    private bool _built;

    public static void Open()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.RestoreAfterGameplay();
        UIManager.Instance.PushPanel(PanelType.EraProgressPanel);

        EraProgressPanel panel = Object.FindFirstObjectByType<EraProgressPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            panel.BuildUi();

            panel.Refresh();
        }
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        Transform oldTitle = transform.Find("Title");
        if (oldTitle != null)
        {
            var oldTmp = oldTitle.GetComponent<TMPro.TextMeshProUGUI>();
            if (oldTmp != null) _font = oldTmp.font;
            oldTitle.gameObject.SetActive(false);
        }
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

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

        _title = MakeText(boxRect, "Title", "时代进度", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform tr = (RectTransform)_title.transform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(0f, -70f); tr.offsetMax = new Vector2(0f, -16f);

        //8 行信息（一个多行文本，左对齐）
        _lines = MakeText(boxRect, "Lines", "", 22f, new Color(0.92f, 0.93f, 0.96f, 1f));
        RectTransform lr = (RectTransform)_lines.transform;
        lr.anchorMin = new Vector2(0f, 1f); lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.offsetMin = new Vector2(60f, -420f); lr.offsetMax = new Vector2(-60f, -90f);
        _lines.alignment = TextAlignmentOptions.TopLeft;
        _lines.enableWordWrapping = true;

        _hint = MakeText(boxRect, "Hint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        RectTransform hr = (RectTransform)_hint.transform;
        hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(0.5f, 0f);
        hr.offsetMin = new Vector2(40f, 120f); hr.offsetMax = new Vector2(-40f, 190f);

        _advanceButton = MakeButton(boxRect, "Btn_Advance", "推进时代", new Vector2(-150f, 36f));
        _advanceButton.onClick.AddListener(OnAdvance);
        _cancelButton = MakeButton(boxRect, "Btn_Cancel", "关闭", new Vector2(150f, 36f));
        _cancelButton.onClick.AddListener(() =>
        {
            Send(PanelAction.Cancel);
        });
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void OnAdvance()
    {
        Send(PanelAction.Advance);
    }

    /// <summary>门槛①：本次通关是否已打 boss（W4c：改成正名访问，不再反射）。</summary>

    private static void SetButton(Button button, bool interactable)
    {
        if (button == null) return;
        button.interactable = interactable;
        button.GetComponent<Image>().color = interactable
            ? new Color(0.2f, 0.42f, 0.72f, 1f)
            : new Color(0.22f, 0.24f, 0.28f, 1f);
    }

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

    private Button MakeButton(Transform parent, string name, string label, Vector2 offset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(300f, 54f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.42f, 0.72f, 1f);

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return go.GetComponent<Button>();
    }

    public override void ClosePanel() { if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide(); }
    public void ShowMessage(string message, bool success) { if (_hint != null) _hint.text = message; }
    public void Render(EraProgressState state)
    {
        if (_lines != null) _lines.text = state.Lines;
        if (_hint != null) _hint.text = state.Hint;
        SetButton(_advanceButton, state.CanAdvance);
        if (_advanceButton != null) _advanceButton.GetComponentInChildren<TextMeshProUGUI>().text = state.AdvanceLabel;
        if (_cancelButton != null) _cancelButton.GetComponentInChildren<TextMeshProUGUI>().text = state.CancelLabel;
    }
}
