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
        // 面板退场时轮盘一并收起，避免下次打开残留预览状态。
        _wheelOpen = false;
        if (_timeOverlay != null) _timeOverlay.gameObject.SetActive(false);
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

    // 面板按 1920×1080 参考分辨率设计；1366×768 由 CanvasScaler 等比缩放，轮盘保持同一比例。
    private const float PanelWidth = 800f;
    private const float PanelHeight = 600f;
    private const float WheelInnerRadius = 126f;
    private const float WheelOuterRadius = 205f;
    private const float WheelButtonRadius = 166f;
    private const float WheelLabelRadius = 214f;
    private const float WheelCellSweep = 13f;   // 每格 15°，留 2° 间隙让「每时辰两格」可辨认

    /// <summary>子时在顶部，顺时针依次为丑时……亥时；每个时辰的两个整点格也按顺时针排列。</summary>
    private static readonly string[] ShichenNames = { "子时", "丑时", "寅时", "卯时", "辰时", "巳时", "午时", "未时", "申时", "酉时", "戌时", "亥时" };
    private static readonly int[] HourOrder = { 23, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _state;
    private TextMeshProUGUI _hint;
    private Button _healButton;
    private Button _refillButton;
    private Button _openTimeButton;
    private TMP_FontAsset _font;
    private bool _built;

    // 时间轮盘：12 个时辰扇区、24 个整点按钮、当前/预览文字与确认/返回按钮。
    private RectTransform _timeOverlay;
    private TextMeshProUGUI _timeCurrent;
    private TextMeshProUGUI _timePreview;
    private TextMeshProUGUI _timeReason;
    private Button _timeConfirm;
    private Button _timeCancel;
    private readonly RadialWedgeGraphic[] _hourWedges = new RadialWedgeGraphic[24];
    private readonly TextMeshProUGUI[] _hourLabels = new TextMeshProUGUI[24];
    private readonly TextMeshProUGUI[] _shichenLabels = new TextMeshProUGUI[12];
    private bool _wheelOpen;

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

    /// <summary>Esc / 返回：轮盘打开时先只收起轮盘并回到扎营菜单，再按一次才关闭面板。</summary>
    public override void HandleBack()
    {
        if (_wheelOpen) { Send(PanelAction.Cancel); return; }
        base.HandleBack();
    }

    /// <summary>任何关闭请求都先让轮盘回到扎营菜单，避免绕过预览直接关掉时间面板。</summary>
    public override void ClosePanel()
    {
        if (_wheelOpen) { Send(PanelAction.Cancel); return; }
        if (UIManager.Instance != null) UIManager.Instance.PopPanel(); else Hide();
    }

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
        GameplayUiSkin.Apply(bg, "UI_Panel_Dialog", GameplayUiSkin.DarkPaper);
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

        _healButton = MakeButton(boxRect, "Btn_Heal", "完全治疗（1 药）", new Vector2(-170f, 200f), new Vector2(300f, 56f));
        _healButton.onClick.AddListener(OnHeal);
        _refillButton = MakeButton(boxRect, "Btn_Refill", "补充减员（2 药）", new Vector2(170f, 200f), new Vector2(300f, 56f));
        _refillButton.onClick.AddListener(OnRefill);
        _openTimeButton = MakeButton(boxRect, "Btn_OpenTimeWheel", "调整时辰（预览后确认）", new Vector2(0f, 292f), new Vector2(340f, 48f), 20f);
        _openTimeButton.onClick.AddListener(() => Send(PanelAction.Toggle));
        Button close = MakeButton(boxRect, "Btn_Close", "关闭", new Vector2(0f, 30f), new Vector2(300f, 56f));
        close.onClick.AddListener(() =>
        {
            Send(PanelAction.Close);
        });

        BuildTimeWheel(boxRect);
    }

    /// <summary>构建覆盖在扎营菜单之上的时间轮盘；真正的切换时间仍由 Presenter 调用服务完成。</summary>
    private void BuildTimeWheel(Transform parent)
    {
        GameObject overlayGo = new GameObject("TimeWheelOverlay", typeof(RectTransform), typeof(Image));
        _timeOverlay = (RectTransform)overlayGo.transform;
        _timeOverlay.SetParent(parent, false);
        _timeOverlay.anchorMin = Vector2.zero;
        _timeOverlay.anchorMax = Vector2.one;
        _timeOverlay.offsetMin = Vector2.zero;
        _timeOverlay.offsetMax = Vector2.zero;
        Image overlayBg = overlayGo.GetComponent<Image>();
        GameplayUiSkin.Apply(overlayBg, "UI_Panel_Sub", GameplayUiSkin.DarkPaper);
        overlayBg.raycastTarget = true;

        TextMeshProUGUI header = MakeText(_timeOverlay, "TimeTitle", "调整时辰", 30f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform headerRect = (RectTransform)header.transform;
        headerRect.anchorMin = new Vector2(0.5f, 1f); headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -10f);
        headerRect.sizeDelta = new Vector2(700f, 38f);

        TextMeshProUGUI tip = MakeText(_timeOverlay, "TimeTip", "选择整点预览，确认后才切换时间 · Esc 返回扎营", 16f, new Color(0.68f, 0.72f, 0.80f, 1f));
        RectTransform tipRect = (RectTransform)tip.transform;
        tipRect.anchorMin = new Vector2(0.5f, 1f); tipRect.anchorMax = new Vector2(0.5f, 1f);
        tipRect.pivot = new Vector2(0.5f, 1f);
        tipRect.anchoredPosition = new Vector2(0f, -48f);
        tipRect.sizeDelta = new Vector2(700f, 26f);

        RectTransform wheel = new GameObject("TimeWheel", typeof(RectTransform)).GetComponent<RectTransform>();
        wheel.SetParent(_timeOverlay, false);
        wheel.anchorMin = new Vector2(0.5f, 0.5f);
        wheel.anchorMax = new Vector2(0.5f, 0.5f);
        wheel.pivot = new Vector2(0.5f, 0.5f);
        wheel.anchoredPosition = new Vector2(0f, -12f);
        wheel.sizeDelta = new Vector2(440f, 440f);

        // 24 个 15° 扇格：相邻两格组成一个时辰，子时固定占据 23 点与 0 点两格。
        for (int i = 0; i < 24; i++)
        {
            int hour = HourOrder[i];
            float angle = -7.5f + i * 15f;
            GameObject cell = new GameObject("Btn_TimeHour_" + hour.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform cellRect = (RectTransform)cell.transform;
            cellRect.SetParent(wheel, false);
            cellRect.anchorMin = Vector2.zero;
            cellRect.anchorMax = Vector2.one;
            cellRect.offsetMin = Vector2.zero;
            cellRect.offsetMax = Vector2.zero;
            RadialWedgeGraphic wedge = cell.AddComponent<RadialWedgeGraphic>();
            wedge.Configure(WheelInnerRadius, WheelOuterRadius, angle, WheelCellSweep, new Color(0.10f, 0.18f, 0.28f, 0.96f), true);
            wedge.SetTexture(GameplayUiSkin.Get("UI_HourWedge"), 440f);
            _hourWedges[hour] = wedge;
            var button = cell.AddComponent<Button>();
            button.targetGraphic = wedge;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Send(PanelAction.Toggle, (uint)hour));

            Vector2 position = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * WheelButtonRadius;
            _hourLabels[hour] = MakeCenteredText(cellRect, "Hour", hour.ToString("00"), position, new Vector2(34f, 30f), 17f, Color.white);
        }

        // 时辰名放在扇区外圈，12 个时辰与 24 个整点格一一对应。
        for (int i = 0; i < ShichenNames.Length; i++)
        {
            float angle = i * 30f;
            TextMeshProUGUI label = MakeText(wheel, "TimeShichen_" + i, ShichenNames[i], 20f, Color.white);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * WheelLabelRadius;
            labelRect.sizeDelta = new Vector2(64f, 26f);
            _shichenLabels[i] = label;
        }

        // 圆心只显示当前时间、预览时间与服务给出的不可用原因，避免文字压住扇格。
        _timeCurrent = MakeCenteredText(wheel, "TimeCurrent", "", new Vector2(0f, 46f), new Vector2(236f, 26f), 15f, new Color(0.72f, 0.76f, 0.84f, 1f));
        _timePreview = MakeCenteredText(wheel, "TimePreview", "", new Vector2(0f, 14f), new Vector2(236f, 32f), 20f, Color.white);
        _timeReason = MakeCenteredText(wheel, "TimeReason", "", new Vector2(0f, -38f), new Vector2(210f, 48f), 14f, new Color(1f, 0.85f, 0.3f, 1f));

        _timeConfirm = MakeButton(_timeOverlay, "Btn_TimeConfirm", "确认切换", new Vector2(-118f, 18f), new Vector2(210f, 44f), 20f);
        _timeConfirm.onClick.AddListener(() => Send(PanelAction.Confirm));
        _timeCancel = MakeButton(_timeOverlay, "Btn_TimeCancel", "返回扎营", new Vector2(118f, 18f), new Vector2(210f, 44f), 20f);
        _timeCancel.onClick.AddListener(() => Send(PanelAction.Cancel));

        _timeOverlay.gameObject.SetActive(false);
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

    private TextMeshProUGUI MakeCenteredText(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        TextMeshProUGUI label = MakeText(parent, name, text, fontSize, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return label;
    }

    private Button MakeButton(Transform parent, string name, string label, Vector2 offset, Vector2 size, float fontSize = 22f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.42f, 0.72f, 1f);

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, fontSize, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        GameplayUiSkin.Button(button);
        return button;
    }

    public void ShowMessage(string message, bool success)
    {
        if (_hint != null)
        {
            _hint.text = message;
            _hint.color = success ? new Color(0.55f, 0.9f, 0.6f, 1f) : new Color(1f, 0.85f, 0.3f, 1f);
        }
        // 确认失败时轮盘仍然打开，原因显示在圆心，不能只写到底层菜单的提示行。
        if (_wheelOpen && _timeReason != null)
        {
            _timeReason.text = message;
            _timeReason.color = success ? new Color(0.55f, 0.9f, 0.6f, 1f) : new Color(1f, 0.85f, 0.3f, 1f);
        }
    }

    public void Render(CampState state)
    {
        _returnToGameplay = state.InRun;
        _wheelOpen = state.WheelOpen;
        if (_title != null) _title.text = "扎营";
        if (_state != null) _state.text = state.State + (string.IsNullOrEmpty(state.TimeSummary) ? "" : "\n" + state.TimeSummary);
        if (_hint != null) _hint.text = state.Hint;
        SetButton(_healButton, state.CanHeal);
        SetButton(_refillButton, state.CanRefill);

        if (_timeOverlay != null)
        {
            _timeOverlay.gameObject.SetActive(state.WheelOpen);
            if (state.WheelOpen) RenderTimeWheel(state);
        }
    }

    /// <summary>轮盘渲染：扇格颜色表示当前/预览，圆心文本给出预览时辰与不可调整原因。</summary>
    private void RenderTimeWheel(CampState state)
    {
        Color idleWedgeA = new Color(0.10f, 0.18f, 0.28f, 0.96f);
        Color idleWedgeB = new Color(0.08f, 0.15f, 0.24f, 0.96f);
        Color currentWedge = new Color(0.18f, 0.46f, 0.68f, 1f);
        Color selectedWedge = new Color(0.82f, 0.62f, 0.18f, 1f);

        for (int i = 0; i < 24; i++)
        {
            int hour = HourOrder[i];
            float angle = -7.5f + i * 15f;
            bool selected = hour == state.PreviewHour;
            bool current = hour == state.CurrentHour;
            Color wedgeColor = selected ? selectedWedge : current ? currentWedge : ((i / 2) % 2 == 0 ? idleWedgeA : idleWedgeB);
            RadialWedgeGraphic wedge = _hourWedges[hour];
            if (wedge != null) wedge.Configure(WheelInnerRadius, WheelOuterRadius, angle, WheelCellSweep, wedgeColor, true);
            if (_hourLabels[hour] != null)
                _hourLabels[hour].color = selected ? new Color(0.10f, 0.08f, 0.02f, 1f) : Color.white;
        }

        int selectedShichen = ShichenIndex(state.PreviewHour);
        int currentShichen = ShichenIndex(state.CurrentHour);
        for (int i = 0; i < _shichenLabels.Length; i++)
        {
            if (_shichenLabels[i] == null) continue;
            _shichenLabels[i].color = i == selectedShichen ? new Color(1f, 0.82f, 0.34f, 1f)
                : i == currentShichen ? new Color(0.62f, 0.86f, 1f, 1f)
                : new Color(0.86f, 0.88f, 0.92f, 1f);
        }

        if (_timeCurrent != null)
            _timeCurrent.text = "当前 " + ShichenNames[ShichenIndex(state.CurrentHour)] + " "
                + (state.CurrentTimeText ?? "") + " " + (state.CurrentPhaseText ?? "");
        if (_timePreview != null)
            _timePreview.text = "预览 " + ShichenNames[ShichenIndex(state.PreviewHour)] + " "
                + (state.PreviewTimeText ?? "") + " " + (state.PreviewPhaseText ?? "");
        if (_timeReason != null)
        {
            if (state.CanSetTime)
            {
                _timeReason.text = "确认后才切换时间";
                _timeReason.color = new Color(0.68f, 0.72f, 0.80f, 1f);
            }
            else
            {
                _timeReason.text = state.TimeReason ?? "当前无法调整时间";
                _timeReason.color = new Color(1f, 0.85f, 0.3f, 1f);
            }
        }

        SetButton(_timeConfirm, state.CanSetTime);
        SetButton(_timeCancel, true);
    }

    /// <summary>23/0 点属于子时，1/2 点属于丑时，依此类推。</summary>
    private static int ShichenIndex(int hour)
    {
        hour %= 24;
        if (hour < 0) hour += 24;
        return ((hour + 1) % 24) / 2;
    }
}
