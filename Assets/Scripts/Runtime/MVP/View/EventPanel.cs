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

/// <summary>Passive Event view: builds controls, renders snapshots, and sends typed intents to its presenter.</summary>
public class EventPanel : BasePanel, IGameplayView<EventState>
{
    public event Action<PanelIntent> Intent;
    private EventPresenter _presenter;
    private void EnsurePresenter()
    {
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new EventPresenter();
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

    /// <summary>选项按钮高度（3 个 + 2 个间距刚好铺满选项区 180px）。</summary>
    private const float OptionButtonHeight = 52f;
    private const float OptionButtonGap = 12f;

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _queueHint;
    private TextMeshProUGUI _body;
    private TextMeshProUGUI _result;
    private RectTransform _optionArea;
    private TMP_FontAsset _font;
    private bool _built;
    private LocalizationManager _localization;

    /// <summary>是否处于"已选完、等玩家点继续"的状态。</summary>
    private bool _showingResult;

    /// <summary>上一次选项的结算结果（逐条，用「；」分隔）。</summary>
    private string _lastResult = "";

    private readonly List<GameObject> _optionButtons = new List<GameObject>();

    /// <summary>打开面板（UIManager 推栈；面板内容在 OnEnter 里按队列刷新）。</summary>
    public static void Open()
    {
        if (UIManager.Instance == null)
        {
            ChaosLog.Error(LogChannel.UI, "[剧情事件] UIManager 未初始化，打不开事件面板");
            return;
        }
        UIManager.Instance.PushPanel(PanelType.EventPanel);
    }

    private void Awake() { BuildUi(); }

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

    // ══════════════════ UI 构建（居中 800×600）══════════════════

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        TextMeshProUGUI prefabTitle = FindChildText("Title");
        if (prefabTitle != null)
        {
            _font = prefabTitle.font;
            prefabTitle.gameObject.SetActive(false);   //prefab 自带左上角标题会压住 GM 悬浮按钮
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

        //标题（顶部居中）
        _title = MakeText(boxRect, "Title2", "", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(88f, -66f);
        titleRect.offsetMax = new Vector2(-88f, -16f);

        //「还有 N 个事件」（标题旁，右对齐）
        _queueHint = MakeText(boxRect, "QueueHint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        RectTransform hintRect = (RectTransform)_queueHint.transform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.offsetMin = new Vector2(16f, -100f);
        hintRect.offsetMax = new Vector2(-16f, -70f);
        _queueHint.alignment = TextAlignmentOptions.Right;

        //正文（中部，可换行）
        _body = MakeText(boxRect, "Body", "", 22f, new Color(0.90f, 0.92f, 0.96f, 1f));
        RectTransform bodyRect = (RectTransform)_body.transform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(40f, -296f);
        bodyRect.offsetMax = new Vector2(-40f, -108f);
        _body.alignment = TextAlignmentOptions.TopLeft;
        _body.enableWordWrapping = true;

        //结算结果（正文下方，逐条提示）
        _result = MakeText(boxRect, "Result", "", 22f, new Color(0.45f, 0.90f, 0.55f, 1f));
        RectTransform resultRect = (RectTransform)_result.transform;
        resultRect.anchorMin = new Vector2(0f, 1f);
        resultRect.anchorMax = new Vector2(1f, 1f);
        resultRect.pivot = new Vector2(0.5f, 1f);
        resultRect.offsetMin = new Vector2(24f, -340f);
        resultRect.offsetMax = new Vector2(-24f, -302f);
        _result.alignment = TextAlignmentOptions.Center;
        _result.enableWordWrapping = true;

        //选项区（下部 180px = 3×52 + 2×12）
        GameObject areaGo = new GameObject("OptionArea", typeof(RectTransform));
        _optionArea = (RectTransform)areaGo.transform;
        _optionArea.SetParent(boxRect, false);
        _optionArea.anchorMin = new Vector2(0f, 0f);
        _optionArea.anchorMax = new Vector2(1f, 1f);
        _optionArea.offsetMin = new Vector2(120f, 72f);
        _optionArea.offsetMax = new Vector2(-120f, -348f);

        //关闭按钮（右上角）
        MakeCornerButton(boxRect, "Btn_Close", "×", OnCloseClicked);

        ChaosLog.Info(LogChannel.UI, "[剧情事件] 面板 UI 已构建（居中 " + PanelWidth + "×" + PanelHeight + "，支持本地化）");
    }

    // ══════════════════ 刷新（队首事件）══════════════════

    private void Refresh()
    {
        if (_presenter == null || _presenter.IsDisposed) EnsurePresenter();
        else _presenter.Refresh();
    }

    // ══════════════════ 玩家操作 ══════════════════

    /// <summary>选一个选项：立刻结算奖励 → 显示结果 →（继续）处置队首。</summary>
    public void ChooseOption(uint optionIndex)
    {
        Send(PanelAction.ChooseOption, optionIndex);
    }

    /// <summary>「继续」/「关闭」：处置队首，队列非空就刷下一个，空了就关面板。</summary>
    public void ContinueNext()
    {
        Send(PanelAction.Continue);
    }

    private void OnCloseClicked()
    {
        ChaosLog.Info(LogChannel.UI,
            "[剧情事件] 玩家点关闭（关闭也视为已处置，§13 Q4）" + (_showingResult ? "；本次已结算过选项" : "；未选选项、无奖励"));
        ContinueNext();
    }

    private void CloseSelf()
    {
        _showingResult = false;
        _lastResult = "";

        if (uiManager != null && uiManager.panelStack.Count > 0 && uiManager.panelStack.Peek() == (BasePanel)this)
        {
            uiManager.PopPanel();
            return;
        }
        Hide();
    }

    // ══════════════════ 给验收/调试用的只读快照 ══════════════════

    /// <summary>面板当前显示内容的文字快照（GM/自测直接打日志，避免"看截图猜"）。</summary>
    public string DumpUiState()
    {
        EventState ev = _rendered;
        var sb = new System.Text.StringBuilder();
        sb.Append("[剧情事件·面板] 标题《").Append(_title == null ? "" : _title.text).Append("》")
          .Append(" 队列提示《").Append(_queueHint == null ? "" : _queueHint.text).Append("》")
          .Append(" 正文长度=").Append(_body == null ? 0 : _body.text.Length)
          .Append(" 结果《").Append(_result == null ? "" : _result.text).Append("》")
          .Append(" 状态=").Append(_showingResult ? "已选待继续" : "待选")
          .Append("\n  按钮：");
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            if (_optionButtons[i] == null) continue;
            TextMeshProUGUI label = _optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            sb.Append(i == 0 ? "[" : " [").Append(label == null ? "?" : label.text).Append("]");
        }
        sb.Append("\n  队首事件=").Append(ev == null ? "无" : (ev.EventId + "《" + ev.Title + "》"))
          .Append(" 队列剩余=").Append((ev == null ? 0 : ev.PendingCount));
        return sb.ToString();
    }

    // ══════════════════ 控件小工具（文本由调用处解析本地化）══════════════════

    private void ClearOptions()
    {
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            if (_optionButtons[i] == null) continue;
            _optionButtons[i].SetActive(false);
            _optionButtons[i].transform.SetParent(null, false);
            Destroy(_optionButtons[i]);
        }
        _optionButtons.Clear();
    }

    private void MakeOptionButton(string label, int slot, System.Action onClick)
    {
        if (_optionArea == null) return;

        GameObject go = new GameObject("Option_" + slot, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_optionArea, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float y = -slot * (OptionButtonHeight + OptionButtonGap);
        rect.offsetMin = new Vector2(0f, y - OptionButtonHeight);
        rect.offsetMax = new Vector2(0f, y);

        Image image = go.GetComponent<Image>();
        image.color = slot == 0 && _showingResult
            ? new Color(0.25f, 0.55f, 0.35f, 1f)
            : new Color(0.20f, 0.42f, 0.72f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;

        _optionButtons.Add(go);
    }

    private void MakeCornerButton(Transform parent, string name, string label, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(-60f, -60f);
        rect.offsetMax = new Vector2(-8f, -8f);

        go.GetComponent<Image>().color = new Color(0.35f, 0.18f, 0.20f, 1f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 26f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
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

    private TextMeshProUGUI FindChildText(string n)
    {
        Transform c = transform.Find(n);
        return c == null ? null : c.GetComponent<TextMeshProUGUI>();
    }

    private EventState _rendered;
    public void ClosePanel() { CloseSelf(); }
    public void ShowMessage(string message, bool success) { if (_result != null) _result.text = message; }
    public void Render(EventState state)
    {
        _rendered = state;
        if (!state.HasEvent) { CloseSelf(); return; }
        _showingResult = state.Resolved;
        _lastResult = state.Result;
        _title.text = state.Title;
        _body.text = state.Body;
        _result.text = state.Result;
        _queueHint.text = state.QueueHint;
        ClearOptions();
        if (state.Resolved) { MakeOptionButton(GameplayText.Text("ui_common_continue", "继续"), 0, ContinueNext); return; }
        for (int i = 0; i < state.Options.Count; i++)
        {
            var row = state.Options[i];
            MakeOptionButton(row.Text, i, () => ChooseOption(row.Id));
        }
    }
}
