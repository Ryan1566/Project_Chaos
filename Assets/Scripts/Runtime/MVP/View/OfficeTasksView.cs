using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 办事单位任务界面的纯展示视图。
/// 只把 OfficeTasksDisplayState 里的文案填进控件并按需发事件，
/// 不查询任何业务服务、不读库存、不计时、不发奖、不做导航。
/// </summary>
public sealed class OfficeTasksView : MonoBehaviour
{
    // 卡片尺寸：1120x760，1920x1080 Canvas 下居中显示。
    private const float CardWidth = 1120f;
    private const float CardHeight = 760f;

    // 页面骨架尺寸。
    private const float HeaderHeight = 100f;     // 标题与三指标均预留完整中文行高。
    private const float TabHeight = 52f;         // 四分类页签
    private const float FooterHeight = 56f;      // 底部按钮区
    private const float ListWidth = 380f;        // 左侧任务列表

    // 条目尺寸与间距。
    private const float RowHeight = 92f;
    private const float RowSpacing = 8f;
    private const float OptionHeight = 90f;
    private const float OptionSpacing = 8f;

    private static readonly Color TextColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.75f, 0.80f, 1f);
    private static readonly Color CardColor = new Color(0.055f, 0.075f, 0.10f, 1f);
    private static readonly Color PanelColor = new Color(0.09f, 0.115f, 0.15f, 1f);
    private static readonly Color TabColor = new Color(0.15f, 0.19f, 0.25f, 1f);
    private static readonly Color TabSelectedColor = new Color(0.22f, 0.40f, 0.60f, 1f);
    private static readonly Color RowColor = new Color(0.13f, 0.16f, 0.21f, 1f);
    private static readonly Color RowSelectedColor = new Color(0.22f, 0.36f, 0.52f, 1f);

    public event Action<OfficeTaskCategory> CategorySelected;
    public event Action<uint> TaskSelected;
    /// <summary>参数依次为任务ID、周期、选项ID。</summary>
    public event Action<uint, uint, uint> OptionSelected;
    public event Action BackRequested;
    public event Action LegacyEventsRequested;

    // —— 骨架 ——
    private TMP_FontAsset _font;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _summary;
    private TextMeshProUGUI _detailTitle;
    private TextMeshProUGUI _detailProgress;
    private TextMeshProUGUI _emptyText;
    private TextMeshProUGUI _message;
    private ScrollRect _detailScroll;
    private RectTransform _detailContent;
    private TextMeshProUGUI _detailBody;
    private ScrollRect _listScroll;
    private RectTransform _listContent;
    private ScrollRect _optionScroll;
    private RectTransform _optionContent;
    private Button _backButton;
    private Button _legacyButton;

    // —— 复用条目的索引缓存，渲染只更新文本/颜色/可见性 ——
    private readonly List<TabWidget> _tabs = new List<TabWidget>();
    private readonly List<RowWidget> _rows = new List<RowWidget>();
    private readonly List<OptionWidget> _options = new List<OptionWidget>();

    // —— 内部状态 ——
    private bool _built;                 // 幂等：只建一次 UI
    private uint _rowTaskId;             // 列表当前绑定任务ID，用于判断是否需要复位滚动
    private OfficeTaskCategory _rowCategory;
    private uint _optionTaskId;
    private uint _optionCycle;
    private string _detailBodyText;

    private sealed class TabWidget
    {
        public GameObject Root;
        public Image Background;
        public TextMeshProUGUI LabelTmp;
        public Button Button;
        public OfficeTaskCategory Category;
    }

    private sealed class RowWidget
    {
        public GameObject Root;
        public Image Background;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Status;
        public TextMeshProUGUI Time;
        public Button Button;
        public uint TaskId;
    }

    private sealed class OptionWidget
    {
        public GameObject Root;
        public Image Background;
        public TextMeshProUGUI Label;
        public Button Button;
        public uint TaskId;
        public uint Cycle;
        public uint OptionId;
    }

    private void Awake()
    {
        EnsureBuilt();
    }

    /// <summary>外部渲染入口：Presenter 周期调用，只填数据不查询。</summary>
    public void Render(OfficeTasksDisplayState state)
    {
        if (state == null) return;
        EnsureBuilt();

        if (_title != null) _title.text = state.Title ?? string.Empty;
        if (_summary != null) _summary.text = state.Summary ?? string.Empty;
        if (_message != null)
        {
            string message = state.Message ?? string.Empty;
            _message.text = message;
            SetActive(_message.gameObject, message.Length > 0);
        }

        RenderCategories(state);
        RenderRows(state);
        RenderDetail(state);
        RenderOptions(state);
        RenderFooter(state);
    }

    // ———————————————————— 构建 ————————————————————

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        _font = RunTeamHud.ResolveFont();

        var backdrop = GetComponent<Image>();
        if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.68f);
        backdrop.raycastTarget = true;

        // 使用缩放适配窄视口，避免窄屏下卡片溢出。
        // 画布由上层提供；若卡片的直接父级就是 Canvas 根，则按屏幕尺寸等比缩小。
        RectTransform card = GameplayUi.Rect(transform, "OfficeTasksCard",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(CardWidth, CardHeight));
        var cardBg = card.gameObject.AddComponent<Image>();
        cardBg.color = CardColor;
        cardBg.raycastTarget = true;

        var fitter = card.gameObject.AddComponent<CardFitter>();
        fitter.TargetWidth = CardWidth;
        fitter.TargetHeight = CardHeight;

        BuildHeader(card);
        BuildTabs(card);
        BuildList(card);
        BuildDetail(card);
        BuildFooter(card);
    }

    private void BuildHeader(RectTransform card)
    {
        var titleArea = GameplayUi.Rect(card, "TitleArea", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -4f), new Vector2(700f, 48f));
        _title = GameplayUi.Text(titleArea, "Title", string.Empty, 30f);
        _title.alignment = TextAlignmentOptions.Left;
        _title.color = TextColor;
        _title.enableWordWrapping = false;

        var summaryArea = GameplayUi.Rect(card, "SummaryArea", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -54f), new Vector2(CardWidth - 48f, 40f));
        _summary = GameplayUi.Text(summaryArea, "Summary", string.Empty, 21f);
        _summary.alignment = TextAlignmentOptions.Left;
        _summary.color = DimColor;
        _summary.enableWordWrapping = false;
        _summary.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void BuildTabs(RectTransform card)
    {
        // 四个页签等分一行，位于页眉下方。
        float total = CardWidth - 48f;
        float gap = 8f;
        float width = (total - gap * 3f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            var widget = new TabWidget();
            widget.Category = (OfficeTaskCategory)(i + 1);
            float x = 24f + i * (width + gap);
            var tabRect = GameplayUi.Rect(card, "CategoryTab" + (i + 1), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -HeaderHeight), new Vector2(width, TabHeight));
            widget.Root = tabRect.gameObject;
            widget.Background = widget.Root.AddComponent<Image>();
            widget.Background.color = TabColor;
            widget.Button = widget.Root.AddComponent<Button>();
            widget.Button.targetGraphic = widget.Background;

            var labelArea = GameplayUi.Rect(tabRect, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            labelArea.anchorMin = Vector2.zero;
            labelArea.anchorMax = Vector2.one;
            labelArea.offsetMin = new Vector2(8f, 2f);
            labelArea.offsetMax = new Vector2(-8f, -2f);
            widget.LabelTmp = labelArea.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(widget.LabelTmp, 22f, TextColor, TextAlignmentOptions.Center);
            widget.LabelTmp.text = string.Empty;

            // 捕获分类而非索引，避免闭包共享变量。
            var category = widget.Category;
            widget.Button.onClick.AddListener(() => RaiseCategory(category));

            _tabs.Add(widget);
        }
    }

    private void BuildList(RectTransform card)
    {
        // 列表中栏：位于页签下方、底部按钮上方。
        float top = HeaderHeight + TabHeight + 12f;
        float bottom = FooterHeight + 12f;
        float height = CardHeight - top - bottom;
        var panel = GameplayUi.Rect(card, "ListPanel", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -top), new Vector2(ListWidth, height));
        var panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = PanelColor;
        panelBg.raycastTarget = true;

        // 视口只挂 RectMask2D，背景保持可被射线触达以支持拖拽滚动。
        var viewport = GameplayUi.Rect(panel, "ListViewport", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(6f, -6f), new Vector2(ListWidth - 12f, height - 12f));
        var viewportBg = viewport.gameObject.AddComponent<Image>();
        viewportBg.color = new Color(0f, 0f, 0f, 0f);
        viewportBg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        _listContent = GameplayUi.Rect(viewport, "ListContent", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0f, 1f);

        _listScroll = panel.gameObject.AddComponent<ScrollRect>();
        _listScroll.viewport = viewport;
        _listScroll.content = _listContent;
        _listScroll.horizontal = false;
        _listScroll.vertical = true;
        _listScroll.movementType = ScrollRect.MovementType.Clamped;
        _listScroll.scrollSensitivity = 24f;

        _emptyText = GameplayUi.Text(viewport, "ListEmpty", string.Empty, 22f);
        _emptyText.alignment = TextAlignmentOptions.Center;
        _emptyText.color = DimColor;
        _emptyText.enableWordWrapping = true;
        _emptyText.text = string.Empty;
    }

    private void BuildDetail(RectTransform card)
    {
        float top = HeaderHeight + TabHeight + 12f;
        float bottom = FooterHeight + 12f;
        float height = CardHeight - top - bottom;
        float x = 24f + ListWidth + 12f;
        float width = CardWidth - x - 24f;

        var panel = GameplayUi.Rect(card, "DetailPanel", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, -top), new Vector2(width, height));
        var panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = PanelColor;
        panelBg.raycastTarget = true;

        var titleArea = GameplayUi.Rect(panel, "DetailTitleArea", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -8f), new Vector2(width - 32f, 48f));
        _detailTitle = GameplayUi.Text(titleArea, "DetailTitle", string.Empty, 26f);
        _detailTitle.alignment = TextAlignmentOptions.Left;
        _detailTitle.color = TextColor;
        _detailTitle.enableWordWrapping = false;
        _detailTitle.overflowMode = TextOverflowModes.Ellipsis;

        var progressArea = GameplayUi.Rect(panel, "DetailProgressArea", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -60f), new Vector2(width - 32f, 60f));
        _detailProgress = GameplayUi.Text(progressArea, "DetailProgress", string.Empty, 21f);
        _detailProgress.alignment = TextAlignmentOptions.Left;
        _detailProgress.color = DimColor;
        _detailProgress.enableWordWrapping = true;
        _detailProgress.overflowMode = TextOverflowModes.Ellipsis;

        // 正文区域：上方占剩余高度，长文在内部滚动而非溢出。
        float bodyTop = 128f;
        float bodyHeight = 160f;

        var bodyViewport = GameplayUi.Rect(panel, "DetailBodyViewport", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -bodyTop), new Vector2(width - 32f, bodyHeight));
        var bodyBg = bodyViewport.gameObject.AddComponent<Image>();
        bodyBg.color = new Color(0f, 0f, 0f, 0f);
        bodyBg.raycastTarget = true;
        bodyViewport.gameObject.AddComponent<RectMask2D>();

        _detailContent = GameplayUi.Rect(bodyViewport, "DetailBodyContent", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        _detailContent.anchorMin = new Vector2(0f, 1f);
        _detailContent.anchorMax = new Vector2(1f, 1f);
        _detailContent.pivot = new Vector2(0f, 1f);

        var bodyArea = GameplayUi.Rect(_detailContent, "DetailBodyArea", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(4f, -2f), new Vector2(-8f, 0f));
        bodyArea.anchorMin = new Vector2(0f, 1f);
        bodyArea.anchorMax = new Vector2(1f, 1f);
        bodyArea.pivot = new Vector2(0f, 1f);
        _detailBody = bodyArea.gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(_detailBody, 22f, TextColor, TextAlignmentOptions.TopLeft);
        _detailBody.enableWordWrapping = true;
        _detailBody.text = string.Empty;

        _detailScroll = bodyViewport.gameObject.AddComponent<ScrollRect>();
        _detailScroll.viewport = bodyViewport;
        _detailScroll.content = _detailContent;
        _detailScroll.horizontal = false;
        _detailScroll.vertical = true;
        _detailScroll.movementType = ScrollRect.MovementType.Clamped;
        _detailScroll.scrollSensitivity = 24f;

        // 选项区：固定在正文下方，独立滚动并复用条目。
        float optionTop = bodyTop + bodyHeight + 12f;
        float optionViewportHeight = Mathf.Max(60f, height - optionTop - 16f);
        var optionViewport = GameplayUi.Rect(panel, "OptionViewport", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -optionTop), new Vector2(width - 32f, optionViewportHeight));
        var optionBg = optionViewport.gameObject.AddComponent<Image>();
        optionBg.color = new Color(0f, 0f, 0f, 0f);
        optionBg.raycastTarget = true;
        optionViewport.gameObject.AddComponent<RectMask2D>();

        _optionContent = GameplayUi.Rect(optionViewport, "OptionContent", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        _optionContent.anchorMin = new Vector2(0f, 1f);
        _optionContent.anchorMax = new Vector2(1f, 1f);
        _optionContent.pivot = new Vector2(0f, 1f);

        _optionScroll = optionViewport.gameObject.AddComponent<ScrollRect>();
        _optionScroll.viewport = optionViewport;
        _optionScroll.content = _optionContent;
        _optionScroll.horizontal = false;
        _optionScroll.vertical = true;
        _optionScroll.movementType = ScrollRect.MovementType.Clamped;
        _optionScroll.scrollSensitivity = 24f;
    }

    private void BuildFooter(RectTransform card)
    {
        const float buttonWidth = 220f;
        const float buttonHeight = 48f;
        var backArea = GameplayUi.Rect(card, "BackArea", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 4f), new Vector2(buttonWidth, buttonHeight));
        _backButton = CreateButton(backArea, "BackButton", 21f, () => BackRequested?.Invoke());

        var legacyArea = GameplayUi.Rect(card, "LegacyArea", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-24f, 4f), new Vector2(buttonWidth, buttonHeight));
        _legacyButton = CreateButton(legacyArea, "LegacyButton", 21f, () => LegacyEventsRequested?.Invoke());
        _legacyButton.gameObject.SetActive(false);

        // 提示文案位于底部中间，不使用业务文案，仅回显 state.Message。
        var messageArea = GameplayUi.Rect(card, "MessageArea", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f + buttonWidth + 12f, 4f),
            new Vector2(CardWidth - 48f - (buttonWidth + 12f) * 2f, buttonHeight));
        _message = GameplayUi.Text(messageArea, "Message", string.Empty, 21f);
        _message.alignment = TextAlignmentOptions.Center;
        _message.color = DimColor;
        _message.enableWordWrapping = false;
        _message.overflowMode = TextOverflowModes.Ellipsis;
        _message.text = string.Empty;
        _message.gameObject.SetActive(false);
    }

    private Button CreateButton(RectTransform area, string name, float fontSize, UnityEngine.Events.UnityAction click)
    {
        var root = GameplayUi.Rect(area, name, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.32f, 0.48f, 1f);
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(click);

        var labelArea = GameplayUi.Rect(root, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        labelArea.anchorMin = Vector2.zero;
        labelArea.anchorMax = Vector2.one;
        labelArea.offsetMin = new Vector2(6f, 2f);
        labelArea.offsetMax = new Vector2(-6f, -2f);
        var label = labelArea.gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(label, fontSize, TextColor, TextAlignmentOptions.Center);
        label.text = string.Empty;
        return button;
    }

    private void ConfigureText(TextMeshProUGUI label, float size, Color color, TextAlignmentOptions alignment)
    {
        label.font = _font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
    }

    // ———————————————————— 渲染 ————————————————————

    private void RenderCategories(OfficeTasksDisplayState state)
    {
        string[] labels = state.CategoryLabels;
        for (int i = 0; i < _tabs.Count; i++)
        {
            TabWidget tab = _tabs[i];
            if (tab?.Root == null) continue;
            int index = (int)tab.Category - 1;
            bool has = labels != null && index >= 0 && index < labels.Length;
            string label = has ? labels[index] ?? string.Empty : string.Empty;
            // 顺序对应 1..4，缺省时用空页签保持布局稳定。
            tab.LabelTmp.text = label;
            bool selected = state.SelectedCategory == tab.Category;
            tab.Background.color = selected ? TabSelectedColor : TabColor;
            tab.LabelTmp.color = selected ? TextColor : DimColor;
            // 无文案时禁用点击，避免发出无意义的分类事件。
            tab.Button.interactable = has;
        }
    }

    private void RenderRows(OfficeTasksDisplayState state)
    {
        OfficeTaskDisplayRow[] rows = state.Rows;
        int count = rows == null ? 0 : rows.Length;
        int shown = 0;
        bool categoryChanged = _rowCategory != state.SelectedCategory;
        _rowCategory = state.SelectedCategory;
        for (int i = 0; i < count; i++)
        {
            OfficeTaskDisplayRow row = rows[i];
            if (row == null) continue;
            RowWidget widget = EnsureRow(shown);
            if (widget == null) break;
            shown++;

            widget.Root.SetActive(true);
            widget.TaskId = row.TaskId;
            widget.Title.text = row.Title ?? string.Empty;
            widget.Status.text = row.Status ?? string.Empty;
            widget.Time.text = row.Time ?? string.Empty;
            widget.Time.gameObject.SetActive(!string.IsNullOrEmpty(widget.Time.text));
            widget.Background.color = row.Selected ? RowSelectedColor : RowColor;
        }

        for (int i = shown; i < _rows.Count; i++)
        {
            RowWidget widget = _rows[i];
            if (widget?.Root != null) widget.Root.SetActive(false);
        }

        if (_listContent != null)
        {
            float height = shown * (RowHeight + RowSpacing);
            _listContent.sizeDelta = new Vector2(0f, height);
        }

        bool empty = shown == 0;
        if (_emptyText != null)
        {
            _emptyText.text = empty ? state.EmptyText ?? string.Empty : string.Empty;
            SetActive(_emptyText.gameObject, empty);
        }

        // 切换分类或所选任务变化时复位滚动，其余更新保持滚动位置不动。
        if (_listScroll != null && (categoryChanged || _rowTaskId != state.SelectedTaskId))
        {
            ResetListScroll();
        }
        _rowTaskId = state.SelectedTaskId;
    }

    /// <summary>只在结构变化时复位列表滚动，渲染倒计时不触发。</summary>
    private void ResetListScroll()
    {
        _listScroll.verticalNormalizedPosition = 1f;
    }

    private RowWidget EnsureRow(int index)
    {
        while (_rows.Count <= index)
        {
            int created = _rows.Count;
            float y = -created * (RowHeight + RowSpacing);
            var rect = GameplayUi.Rect(_listContent, "Row" + created, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, y), new Vector2(0f, RowHeight));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var widget = new RowWidget();
            widget.Root = rect.gameObject;
            widget.Background = rect.gameObject.AddComponent<Image>();
            widget.Background.color = RowColor;
            widget.Button = rect.gameObject.AddComponent<Button>();
            widget.Button.targetGraphic = widget.Background;

            var titleArea = GameplayUi.Rect(rect, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -8f), new Vector2(0f, 34f));
            titleArea.anchorMin = new Vector2(0f, 1f);
            titleArea.anchorMax = new Vector2(1f, 1f);
            titleArea.offsetMin = new Vector2(12f, -44f);
            titleArea.offsetMax = new Vector2(-12f, -8f);
            widget.Title = titleArea.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(widget.Title, 22f, TextColor, TextAlignmentOptions.Left);
            widget.Title.enableWordWrapping = false;
            widget.Title.overflowMode = TextOverflowModes.Ellipsis;

            var statusArea = GameplayUi.Rect(rect, "Status", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -48f), new Vector2(160f, 26f));
            widget.Status = statusArea.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(widget.Status, 19f, DimColor, TextAlignmentOptions.Left);
            widget.Status.enableWordWrapping = false;
            widget.Status.overflowMode = TextOverflowModes.Ellipsis;

            var timeArea = GameplayUi.Rect(rect, "Time", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -48f), new Vector2(160f, 26f));
            timeArea.anchorMin = new Vector2(1f, 1f);
            timeArea.anchorMax = new Vector2(1f, 1f);
            timeArea.pivot = new Vector2(1f, 1f);
            widget.Time = timeArea.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(widget.Time, 19f, DimColor, TextAlignmentOptions.Right);
            widget.Time.enableWordWrapping = false;
            widget.Time.overflowMode = TextOverflowModes.Ellipsis;

            widget.Button.onClick.AddListener(() => RaiseTask(widget.TaskId));

            _rows.Add(widget);
        }
        return _rows[index];
    }

    private void RenderDetail(OfficeTasksDisplayState state)
    {
        if (_detailTitle != null) _detailTitle.text = state.DetailTitle ?? string.Empty;
        if (_detailProgress != null)
        {
            string progress = state.DetailProgress ?? string.Empty;
            _detailProgress.text = progress;
            SetActive(_detailProgress.gameObject, progress.Length > 0);
        }
        if (_detailBody != null)
        {
            string body = state.DetailBody ?? string.Empty;
            // 文本未变化时不重复赋值，避免 TMP 反复重排。
            if (!string.Equals(_detailBodyText, body, StringComparison.Ordinal))
            {
                _detailBodyText = body;
                _detailBody.text = body;
                _detailBody.ForceMeshUpdate();
                if (_detailContent != null)
                {
                    float textHeight = Mathf.Max(_detailBody.preferredHeight + 8f, 0f);
                    _detailContent.sizeDelta = new Vector2(0f, textHeight);
                    _detailBody.rectTransform.sizeDelta = new Vector2(-8f, textHeight);
                    if (_detailScroll != null) _detailScroll.verticalNormalizedPosition = 1f;
                }
            }
        }
    }

    private void RenderOptions(OfficeTasksDisplayState state)
    {
        OfficeTaskDisplayOption[] options = state.Options;
        int count = options == null ? 0 : options.Length;
        int shown = 0;
        bool contextChanged = _optionTaskId != state.SelectedTaskId
            || (_optionCycle != 0 && count > 0 && options[0] != null && _optionCycle != options[0].Cycle);

        for (int i = 0; i < count; i++)
        {
            OfficeTaskDisplayOption option = options[i];
            if (option == null) continue;
            OptionWidget widget = EnsureOption(shown);
            if (widget == null) break;
            shown++;

            widget.Root.SetActive(true);
            widget.TaskId = option.TaskId;
            widget.Cycle = option.Cycle;
            widget.OptionId = option.OptionId;
            widget.Label.text = option.Text ?? string.Empty;
            widget.Button.interactable = option.Enabled;
            widget.Background.color = option.Enabled
                ? new Color(0.18f, 0.32f, 0.48f, 1f)
                : new Color(0.16f, 0.18f, 0.21f, 1f);
            widget.Label.color = option.Enabled ? TextColor : DimColor;
        }

        for (int i = shown; i < _options.Count; i++)
        {
            OptionWidget widget = _options[i];
            if (widget?.Root != null) widget.Root.SetActive(false);
        }

        if (_optionContent != null)
        {
            float height = shown * (OptionHeight + OptionSpacing);
            _optionContent.sizeDelta = new Vector2(0f, height);
        }

        if (_optionScroll != null && contextChanged) _optionScroll.verticalNormalizedPosition = 1f;

        // 记录当前选项上下文，供下次判断是否复位。
        if (count > 0 && options[0] != null)
        {
            _optionTaskId = options[0].TaskId;
            _optionCycle = options[0].Cycle;
        }
        else if (count == 0)
        {
            _optionTaskId = state.SelectedTaskId;
            _optionCycle = 0;
        }
    }

    private OptionWidget EnsureOption(int index)
    {
        while (_options.Count <= index)
        {
            int created = _options.Count;
            float y = -created * (OptionHeight + OptionSpacing);
            var rect = GameplayUi.Rect(_optionContent, "Option" + created, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, y), new Vector2(0f, OptionHeight));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var widget = new OptionWidget();
            widget.Root = rect.gameObject;
            widget.Background = rect.gameObject.AddComponent<Image>();
            widget.Background.color = new Color(0.18f, 0.32f, 0.48f, 1f);
            widget.Button = rect.gameObject.AddComponent<Button>();
            widget.Button.targetGraphic = widget.Background;

            var labelArea = GameplayUi.Rect(rect, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            labelArea.anchorMin = Vector2.zero;
            labelArea.anchorMax = Vector2.one;
            labelArea.offsetMin = new Vector2(12f, 2f);
            labelArea.offsetMax = new Vector2(-12f, -2f);
            widget.Label = labelArea.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(widget.Label, 21f, TextColor, TextAlignmentOptions.Center);
            widget.Label.enableWordWrapping = true;
            widget.Label.enableAutoSizing = true;
            widget.Label.fontSizeMin = 17f;
            widget.Label.fontSizeMax = 21f;
            widget.Label.overflowMode = TextOverflowModes.Ellipsis;

            widget.Button.onClick.AddListener(() => RaiseOption(widget.TaskId, widget.Cycle, widget.OptionId));

            _options.Add(widget);
        }
        return _options[index];
    }

    private void RenderFooter(OfficeTasksDisplayState state)
    {
        if (_backButton != null)
        {
            SetButtonText(_backButton, state.BackLabel);
            _backButton.gameObject.SetActive(true);
        }
        if (_legacyButton != null)
        {
            SetButtonText(_legacyButton, state.LegacyLabel);
            _legacyButton.gameObject.SetActive(state.ShowLegacyButton);
        }
    }

    // ———————————————————— 事件发送 ————————————————————

    private void RaiseCategory(OfficeTaskCategory category)
    {
        CategorySelected?.Invoke(category);
    }

    private void RaiseTask(uint taskId)
    {
        TaskSelected?.Invoke(taskId);
    }

    private void RaiseOption(uint taskId, uint cycle, uint optionId)
    {
        OptionSelected?.Invoke(taskId, cycle, optionId);
    }

    // ———————————————————— 工具 ————————————————————

    private static void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = text ?? string.Empty;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    /// <summary>窄视口适配：按卡片尺寸等比缩小，宽屏保持 1:1。</summary>
    private sealed class CardFitter : MonoBehaviour
    {
        public float TargetWidth = 1120f;
        public float TargetHeight = 760f;
        private RectTransform _parent;

        private void Start()
        {
            if (transform is RectTransform rect) _parent = rect.parent as RectTransform;
            Fit();
        }

        private void OnRectTransformDimensionsChange()
        {
            // 父级尺寸变化时才重新计算缩放。
            Fit();
        }

        private void Fit()
        {
            var rect = transform as RectTransform;
            if (rect == null || _parent == null) return;
            float availableWidth = _parent.rect.width;
            float availableHeight = _parent.rect.height;
            float scale = 1f;
            if (availableWidth > 0f && availableHeight > 0f)
                scale = Mathf.Min(1f, Mathf.Min(availableWidth / TargetWidth, availableHeight / TargetHeight));
            scale = Mathf.Clamp(scale, 0.5f, 1f);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
