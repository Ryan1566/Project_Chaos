using System;
using Object = UnityEngine.Object;
using Chaos.MVP.Presenter.Gameplay;
using Chaos.MVP.Model.Gameplay;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive RunCamp view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class RunCampPanel : BasePanel, IGameplayView<CampState>
{
    private bool _returnToGameplay;
    public override bool ReturnsToGameplayOnClose => _returnToGameplay;
    private void OnDestroy() { OnExit(); }

    public override void OnExit()
    {
        if (uiManager == null || !uiManager.panelStack.Contains(this)) _presenter?.ReleaseControl();
        _presenter?.Dispose();
        _presenter = null;
    }

    public event Action<PanelIntent> Intent;
    private CampPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new CampPresenter();
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
    private TextMeshProUGUI _state;
    private TextMeshProUGUI _hint;
    private Button _healButton;
    private Button _refillButton;
    private TMP_FontAsset _font;
    private bool _built;

    public static void Open()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.RestoreAfterGameplay();
        UIManager.Instance.PushPanel(PanelType.RunCampPanel);

        RunCampPanel panel = Object.FindFirstObjectByType<RunCampPanel>(FindObjectsInactive.Include);
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
            _font = oldTitle.GetComponent<TMPro.TextMeshProUGUI>() == null ? null : oldTitle.GetComponent<TMPro.TextMeshProUGUI>().font;
            oldTitle.gameObject.SetActive(false);   //prefab 旧标题会与 GM 按钮重叠
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

        _title = MakeText(boxRect, "Title", "扎营", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform tr = (RectTransform)_title.transform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(0f, -70f); tr.offsetMax = new Vector2(0f, -16f);

        _state = MakeText(boxRect, "State", "", 22f, new Color(0.92f, 0.93f, 0.96f, 1f));
        RectTransform sr = (RectTransform)_state.transform;
        sr.anchorMin = new Vector2(0f, 1f); sr.anchorMax = new Vector2(1f, 1f);
        sr.pivot = new Vector2(0.5f, 1f);
        sr.offsetMin = new Vector2(24f, -190f); sr.offsetMax = new Vector2(-24f, -90f);

        _hint = MakeText(boxRect, "Hint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        RectTransform hr = (RectTransform)_hint.transform;
        hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(0.5f, 0f);
        hr.offsetMin = new Vector2(24f, 108f); hr.offsetMax = new Vector2(-24f, 168f);

        _healButton = MakeButton(boxRect, "Btn_Heal", "完全治疗（1 药）", new Vector2(-170f, 200f));
        _healButton.onClick.AddListener(OnHeal);
        _refillButton = MakeButton(boxRect, "Btn_Refill", "补充减员（2 药）", new Vector2(170f, 200f));
        _refillButton.onClick.AddListener(OnRefill);
        Button close = MakeButton(boxRect, "Btn_Close", "关闭", new Vector2(0f, 30f));
        close.onClick.AddListener(() =>
        {
            Send(PanelAction.Close);
        });
    }

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    private void OnHeal()
    {
        Send(PanelAction.Heal);
    }

    private void OnRefill()
    {
        Send(PanelAction.Refill);
    }

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
        label.enableWordWrapping = true;
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
        rect.sizeDelta = new Vector2(300f, 56f);
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
    public void Render(CampState state)
    {
        _returnToGameplay = state.InRun;
        if (_title != null) _title.text = "扎营";
        if (_state != null) _state.text = state.State;
        if (_hint != null) _hint.text = state.Hint;
        SetButton(_healButton, state.CanHeal);
        SetButton(_refillButton, state.CanRefill);
    }
}
