using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剧情事件面板（M4 / W5.1）—— 居中 800×600 内容区：**标题 + 正文 + 1~3 个选项 + 关闭**。
///
/// ══════════════ 文案 ══════════════
/// **纯中文、不挂 `LocalizedText`**（与族人/库存/结算面板一致 —— 挂 key 而本地化表里没有会回填成 key 本身）。
/// 标题/正文直接来自 `EventConfig.TitleText` / `BodyText`（中文原文，见 16_剧情事件.md §7.1）。
///
/// ══════════════ 队列行为（§3.6）══════════════
/// 事件队列由 <see cref="EventService"/> 持有；本面板**只显示队首**：
/// * 选一个选项 → 立刻结算奖励（资源→物品→门客→信心）→ 显示逐条结果 →「继续」；
/// * 「继续」/「关闭」→ `MarkFired`（一次性写 `firedEvents`）→ 队列非空就刷成下一个，空则关面板；
/// * 标题旁「还有 N 个事件」= 队首之后还剩几个。
///
/// ══════════════ 为什么内容运行时构建 ══════════════
/// prefab 只提供根节点（Image + CanvasGroup + UIPanelAnimator + BasePanel 子类），
/// 布局全部在 <see cref="BuildUi"/> 里拼 —— 选项数 1~3 是表驱动的，prefab 里写死三个按钮反而要额外隐藏逻辑。
/// prefab 自带的左上角 `Title` 在运行时隐藏（会被 GM 悬浮按钮压住，与局内结算面板同一处理）。
/// </summary>
public class EventPanel : BasePanel
{
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
        _showingResult = false;
        _lastResult = "";
        Refresh();
    }

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

        //标题（顶部居中，纯中文）
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

        ChaosLog.Info(LogChannel.UI, "[剧情事件] 面板 UI 已构建（居中 " + PanelWidth + "×" + PanelHeight + "，纯中文，无 LocalizedText）");
    }

    // ══════════════════ 刷新（队首事件）══════════════════

    private void Refresh()
    {
        EventConfig ev = EventService.Instance.Current;
        if (ev == null) { CloseSelf(); return; }

        _title.text = ev.TitleText;
        _body.text = ev.BodyText;
        _result.text = _showingResult ? _lastResult : "";

        int remain = EventService.Instance.RemainingAfterCurrent;
        _queueHint.text = remain > 0 ? ("还有 " + remain + " 个事件") : "";

        ClearOptions();

        if (_showingResult)
        {
            MakeOptionButton("继续", 0, ContinueNext);
            return;
        }

        List<EventOptionConfig> options = EventService.Instance.GetOptions(ev.Index);
        for (int i = 0; i < options.Count; i++)
        {
            EventOptionConfig captured = options[i];
            MakeOptionButton(captured.OptionText, i, () => ChooseOption(captured.Index));
        }
        if (options.Count == 0)
        {
            //E1：没有选项行 → 只有一个「关闭」，玩家仍能处置掉该事件
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] 事件 " + ev.Index + " 没有可显示的选项，只显示关闭按钮");
        }

        ChaosLog.Info(LogChannel.UI,
            "[剧情事件] 面板刷新：标题《" + ev.TitleText + "》 选项 " + options.Count + " 个"
            + (remain > 0 ? ("，还有 " + remain + " 个事件") : "，队列已无后续"));
    }

    // ══════════════════ 玩家操作 ══════════════════

    /// <summary>选一个选项：立刻结算奖励 → 显示结果 →（继续）处置队首。</summary>
    public void ChooseOption(uint optionIndex)
    {
        if (_showingResult) return;

        EventConfig ev = EventService.Instance.Current;
        if (ev == null) { CloseSelf(); return; }

        string reason;
        bool ok = EventService.Instance.ApplyOption(optionIndex, out reason);
        _lastResult = string.IsNullOrEmpty(reason) ? "（无结果）" : reason;
        _showingResult = true;

        ChaosLog.Info(LogChannel.UI,
            "[剧情事件] 面板选择：事件 " + ev.Index + " 选项 " + optionIndex + " → " + (ok ? "成功" : "失败") + "：" + _lastResult);

        Refresh();
    }

    /// <summary>「继续」/「关闭」：处置队首，队列非空就刷下一个，空了就关面板。</summary>
    public void ContinueNext()
    {
        EventConfig ev = EventService.Instance.Current;
        if (ev != null) EventService.Instance.MarkFired(ev.Index);

        _showingResult = false;
        _lastResult = "";

        if (EventService.Instance.Current == null)
        {
            ChaosLog.Info(LogChannel.UI, "[剧情事件] 队列已空 → 关闭事件面板");
            CloseSelf();
            return;
        }

        ChaosLog.Info(LogChannel.UI, "[剧情事件] 弹出队列里的下一个（剩余 " + EventService.Instance.PendingCount + " 个）");
        Refresh();
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
        EventConfig ev = EventService.Instance.Current;
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
        sb.Append("\n  队首事件=").Append(ev == null ? "无" : (ev.Index + "《" + ev.TitleText + "》"))
          .Append(" 队列剩余=").Append(EventService.Instance.PendingCount);
        return sb.ToString();
    }

    // ══════════════════ 控件小工具（纯中文，不挂 LocalizedText）══════════════════

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
}
