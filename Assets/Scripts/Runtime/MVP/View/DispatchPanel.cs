using System;
using System.Collections.Generic;
using System.Text;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 派遣面板被动视图：只负责布局、显示和发送意图。
/// 卡片、编队槽和 Spine 预览均复用对象，不在每次渲染时销毁重建。
/// </summary>
public class DispatchPanel : BasePanel, IDispatchView
{
    private static readonly Color Dark = new Color(0.075f, 0.060f, 0.045f, 1f);
    private static readonly Color Panel = new Color(0.13f, 0.105f, 0.075f, 0.98f);
    private static readonly Color Card = new Color(0.18f, 0.14f, 0.095f, 1f);
    private static readonly Color CardSelected = new Color(0.31f, 0.235f, 0.125f, 1f);
    private static readonly Color Disabled = new Color(0.22f, 0.19f, 0.15f, 1f);
    private static readonly Color Gold = new Color(0.88f, 0.73f, 0.40f, 1f);
    private static readonly Color GoldDim = new Color(0.56f, 0.45f, 0.26f, 1f);
    private static readonly Color Paper = new Color(0.95f, 0.91f, 0.81f, 1f);
    private static readonly Color Muted = new Color(0.67f, 0.62f, 0.53f, 1f);
    private static readonly Color PlayerGold = new Color(0.96f, 0.79f, 0.31f, 1f);
    private static readonly Color AiBlue = new Color(0.42f, 0.63f, 0.84f, 1f);
    private static readonly Color Good = new Color(0.58f, 0.85f, 0.58f, 1f);
    private static readonly Color Warning = new Color(0.96f, 0.58f, 0.36f, 1f);
    private static readonly Color LowConfidence = new Color(0.93f, 0.80f, 0.36f, 1f);

    private sealed class CardWidget
    {
        public RectTransform Root;
        public Image Image;
        public Outline Outline;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Quality;
        public TextMeshProUGUI Status;
        public TextMeshProUGUI TeamTag;
    }

    private sealed class SlotWidget
    {
        public Button Button;
        public Image Image;
        public Outline Outline;
        public Image Badge;
        public TextMeshProUGUI BadgeText;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Meta;
        public Button Remove;
        public TextMeshProUGUI RemoveText;
    }

    public event Action<DispatchIntent> Intent;

    private DispatchPresenter _presenter;
    private DispatchVisuals _visuals;
    private TMP_FontAsset _font;
    private DispatchState _state;
    private bool _built;
    private bool _syncingSearch;
    private bool _flowCommandIssued;
    private float _messageUntil;
    private float _nextAutoRefresh;
    private float? _pendingScroll;
    private string _listSignature = string.Empty;

    private RectTransform _layout;
    private RectTransform _left;
    private RectTransform _center;
    private RectTransform _right;
    private RectTransform _listViewport;
    private RectTransform _listContent;
    private RectTransform _previewFrame;
    private RectTransform _previewHost;
    private ScrollRect _scroll;
    private TMP_InputField _search;
    private RunTeamSpinePreview _preview;
    private Image _previewPortrait;
    private TextMeshProUGUI _previewFallback;
    private TextMeshProUGUI _previewNote;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _meta;
    private TextMeshProUGUI _message;
    private TextMeshProUGUI _leftHeader;
    private TextMeshProUGUI _teamHeader;
    private TextMeshProUGUI _emptyText;
    private TextMeshProUGUI _detailName;
    private TextMeshProUGUI _detailMeta;
    private TextMeshProUGUI _detailProfession;
    private TextMeshProUGUI _detailStatus;
    private TextMeshProUGUI _detailStats;
    private TextMeshProUGUI _detailHint;
    private TextMeshProUGUI _reason;
    private TextMeshProUGUI _footerHint;
    private TextMeshProUGUI _searchPlaceholder;
    private TextMeshProUGUI _filterAllText;
    private TextMeshProUGUI _filterAvailableText;
    private TextMeshProUGUI _filterTeamText;
    private TextMeshProUGUI _joinText;
    private TextMeshProUGUI _removeText;
    private TextMeshProUGUI _controlText;
    private TextMeshProUGUI _departText;
    private TextMeshProUGUI _returnText;

    private Button _filterAll;
    private Button _filterAvailable;
    private Button _filterTeam;
    private Button _join;
    private Button _remove;
    private Button _control;
    private Button _depart;
    private Button _return;
    private Button _close;
    private Button _retainers;
    private Image _filterAllImage;
    private Image _filterAvailableImage;
    private Image _filterTeamImage;
    private Image _joinImage;
    private Image _removeImage;
    private Image _controlImage;
    private Image _departImage;
    private Image _returnImage;
    private Image _closeImage;

    private readonly Dictionary<uint, CardWidget> _cards = new Dictionary<uint, CardWidget>();
    private readonly SlotWidget[] _slots = new SlotWidget[4];

    private void Awake()
    {
        BuildUi();
    }

    private void OnEnable()
    {
        if (_built && IsShowing)
        {
            _flowCommandIssued = false;
            EnsurePresenter();
            _presenter?.Refresh();
        }
    }

    private void OnDisable()
    {
        DisposePresenter();
    }

    private void OnDestroy()
    {
        OnExit();
    }

    public override void OnEnter()
    {
        BuildUi();
        _flowCommandIssued = false;
        _messageUntil = 0f;
        if (_message != null) _message.gameObject.SetActive(false);
        _nextAutoRefresh = Time.unscaledTime + 1f;
        EnsurePresenter();
    }

    public override void OnExit()
    {
        DisposePresenter();
        _flowCommandIssued = false;
        _messageUntil = 0f;
        if (_message != null) _message.gameObject.SetActive(false);
    }

    private void OnSearchChanged(string value)
    {
        if (_syncingSearch) return;
        Send(DispatchAction.SetSearch, 0, DispatchFilter.All, value);
    }

    private void DoDepart()
    {
        Send(DispatchAction.Depart);
    }

    private void DoReturn()
    {
        Send(DispatchAction.Return);
    }

    private void DoClose()
    {
        Send(DispatchAction.Close);
    }

    private void Send(DispatchAction action, uint id = 0,
        DispatchFilter filter = DispatchFilter.All, string text = null)
    {
        if (!IsShowing) return;
        if (action == DispatchAction.Depart || action == DispatchAction.Return)
            _flowCommandIssued = true;
        EnsurePresenter();
        Intent?.Invoke(new DispatchIntent(action, id, filter, text));
    }

    private void EnsurePresenter()
    {
        if (IsShowing && _flowCommandIssued) return;
        if (_presenter != null && !_presenter.IsDisposed) return;
        _presenter = new DispatchPresenter();
        _presenter.Attach(this);
    }

    private void DisposePresenter()
    {
        if (_presenter == null) return;
        _presenter.Dispose();
        _presenter = null;
    }

    public void Render(DispatchState state)
    {
        BuildUi();
        if (state == null || state.Snapshot == null) return;
        _state = state;
        _flowCommandIssued = false;

        DispatchSnapshot snapshot = state.Snapshot;
        if (_title != null)
        {
            _title.font = _font;
            _title.fontSize = 34f;
            _title.color = Gold;
            _title.alignment = TextAlignmentOptions.MidlineLeft;
            _title.text = L("ui_dispatch_title", "派遣");
        }
        if (_meta != null)
        {
            _meta.text = L("ui_dispatch_meta",
                "时代 {0} · 深度 {1}/{2}    门客 {3} · 编队 {4}/{5}",
                snapshot.Era, snapshot.Depth, snapshot.DepthCap,
                snapshot.TotalRetainers, snapshot.TeamCount, 4);
        }

        BindStaticText();
        UpdateFilterButtons(snapshot);
        UpdateSearch(snapshot.Search);
        UpdateCards(snapshot);
        UpdateTeam(snapshot);
        UpdateDetail(snapshot);
        UpdateFooter(snapshot);
    }

    public void ShowMessage(string message, bool success)
    {
        if (_message == null) return;
        if (string.IsNullOrEmpty(message))
        {
            _messageUntil = 0f;
            _message.gameObject.SetActive(false);
            return;
        }
        _message.gameObject.SetActive(true);
        _message.text = message;
        _message.color = success ? Good : Warning;
        _messageUntil = Time.unscaledTime + 8f;
    }

    public override void ClosePanel()
    {
        if (UIManager.Instance != null) UIManager.Instance.PopPanel();
        else Hide();
    }

    private void Update()
    {
        if (!IsShowing) return;

        if (_messageUntil > 0f && Time.unscaledTime >= _messageUntil)
        {
            _messageUntil = 0f;
            if (_message != null) _message.gameObject.SetActive(false);
        }

        if (_flowCommandIssued) return;
        if (_presenter == null || _presenter.IsDisposed) return;
        if (Time.unscaledTime < _nextAutoRefresh) return;
        _nextAutoRefresh = Time.unscaledTime + 1f;
        _presenter.Refresh();
    }

    private void LateUpdate()
    {
        if (!_pendingScroll.HasValue || _scroll == null) return;
        float value = Mathf.Clamp01(_pendingScroll.Value);
        _pendingScroll = null;
        _scroll.verticalNormalizedPosition = value;
    }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        var self = transform as RectTransform;
        if (self != null)
        {
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
        }

        Image background = GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();
        background.sprite = null;
        background.type = Image.Type.Simple;
        background.color = Dark;
        background.raycastTarget = true;

        _visuals = Resources.Load<DispatchVisuals>("UI/DispatchVisuals");
        Transform titleTransform = transform.Find("Title");
        if (titleTransform != null) _title = titleTransform.GetComponent<TextMeshProUGUI>();
        _font = _title != null ? _title.font : null;
        if (_font == null) _font = RunTeamHud.ResolveFont();
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        if (_title == null)
        {
            RectTransform titleRect = Area("Title", transform, 0.14f, 0.925f, 0.52f, 0.995f);
            _title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        }
        RectTransform titleArea = (RectTransform)_title.transform;
        titleArea.SetParent(transform, false);
        titleArea.anchorMin = new Vector2(0.14f, 0.925f);
        titleArea.anchorMax = new Vector2(0.52f, 0.995f);
        titleArea.offsetMin = Vector2.zero;
        titleArea.offsetMax = Vector2.zero;
        _title.font = _font;
        _title.fontSize = 34f;
        _title.color = Gold;
        _title.alignment = TextAlignmentOptions.MidlineLeft;
        _title.enableWordWrapping = false;
        _title.overflowMode = TextOverflowModes.Ellipsis;
        _title.raycastTarget = false;

        HideLegacyChild("Summary");
        HideLegacyChild("Content");
        HideLegacyChild("Hint");

        _layout = Area("DispatchLayout", transform, 0f, 0f, 1f, 1f);
        _layout.offsetMin = new Vector2(24f, 18f);
        _layout.offsetMax = new Vector2(-24f, -18f);

        BuildHeader();
        RectTransform body = Area("Body", _layout, 0f, 0.17f, 1f, 0.85f);
        BuildLeftPanel(body);
        BuildCenterPanel(body);
        BuildDetailPanel(body);
        BuildFooter();
    }

    private void BuildHeader()
    {
        _meta = Label("Meta", _layout, 0.55f, 0.932f, 0.99f, 0.988f, string.Empty, 20f, Gold,
            TextAlignmentOptions.MidlineRight, false);
        _message = Label("OperationMessage", _layout, 0f, 0.864f, 1f, 0.914f, string.Empty, 18f, Good,
            TextAlignmentOptions.MidlineLeft, false);
        _message.gameObject.SetActive(false);

        RectTransform rule = Area("HeaderRule", _layout, 0f, 0.855f, 1f, 0.858f);
        Image ruleImage = rule.gameObject.AddComponent<Image>();
        ruleImage.color = GoldDim;
        ruleImage.raycastTarget = false;
    }

    private void BuildLeftPanel(RectTransform body)
    {
        _left = PanelArea("RosterPanel", body, 0f, 0f, 0.315f, 1f);

        _leftHeader = Label("RosterHeader", _left, 0.04f, 0.94f, 0.57f, 0.995f,
            L("ui_dispatch_roster", "门客列表"), 22f, Gold);
        _retainers = MakeButton("Btn_Retainers", _left, .60f, .935f, .96f, .995f,
            L("ui_retainer_recruit", "招募门客"), () => Send(DispatchAction.OpenRetainers), 16f);
        BuildSearchField();
        BuildFilterButtons();
        BuildRosterScroll();
    }

    private void BuildSearchField()
    {
        RectTransform box = Area("SearchBox", _left, 0.04f, 0.835f, 0.96f, 0.915f);
        Image boxImage = box.gameObject.AddComponent<Image>();
        boxImage.color = Card;
        var boxOutline = box.gameObject.AddComponent<Outline>();
        boxOutline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.55f);
        boxOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform viewport = Area("TextArea", box, 0.05f, 0.10f, 0.95f, 0.90f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform textRect = Area("Text", viewport, 0f, 0f, 1f, 1f);
        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.fontSize = 18f;
        text.color = Paper;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        RectTransform placeholderRect = Area("Placeholder", viewport, 0f, 0f, 1f, 1f);
        _searchPlaceholder = placeholderRect.gameObject.AddComponent<TextMeshProUGUI>();
        _searchPlaceholder.font = _font;
        _searchPlaceholder.fontSize = 18f;
        _searchPlaceholder.color = Muted;
        _searchPlaceholder.alignment = TextAlignmentOptions.MidlineLeft;
        _searchPlaceholder.enableWordWrapping = false;
        _searchPlaceholder.text = L("ui_dispatch_search", "搜索姓名");
        _searchPlaceholder.raycastTarget = false;

        _search = box.gameObject.AddComponent<TMP_InputField>();
        _search.textViewport = viewport;
        _search.textComponent = text;
        _search.placeholder = _searchPlaceholder;
        _search.targetGraphic = boxImage;
        _search.lineType = TMP_InputField.LineType.SingleLine;
        _search.characterLimit = 16;
        _search.text = string.Empty;
        _search.onValueChanged.AddListener(OnSearchChanged);
    }

    private void BuildFilterButtons()
    {
        _filterAll = MakeButton("Filter_All", _left, 0.04f, 0.745f, 0.335f, 0.82f,
            L("ui_dispatch_filter_all", "全部"), () => Send(DispatchAction.SetFilter, 0, DispatchFilter.All), 17f);
        _filterAvailable = MakeButton("Filter_Available", _left, 0.345f, 0.745f, 0.64f, 0.82f,
            L("ui_dispatch_filter_available", "可出战"), () => Send(DispatchAction.SetFilter, 0, DispatchFilter.Available), 17f);
        _filterTeam = MakeButton("Filter_Team", _left, 0.65f, 0.745f, 0.96f, 0.82f,
            L("ui_dispatch_filter_team", "已编队"), () => Send(DispatchAction.SetFilter, 0, DispatchFilter.Team), 17f);

        _filterAllText = ButtonLabel(_filterAll);
        _filterAvailableText = ButtonLabel(_filterAvailable);
        _filterTeamText = ButtonLabel(_filterTeam);
        _filterAllImage = _filterAll != null ? _filterAll.GetComponent<Image>() : null;
        _filterAvailableImage = _filterAvailable != null ? _filterAvailable.GetComponent<Image>() : null;
        _filterTeamImage = _filterTeam != null ? _filterTeam.GetComponent<Image>() : null;
    }

    private void BuildRosterScroll()
    {
        _listViewport = Area("RosterViewport", _left, 0.02f, 0.012f, 0.935f, 0.725f);
        Image viewportImage = _listViewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.02f);
        viewportImage.raycastTarget = true;
        _listViewport.gameObject.AddComponent<RectMask2D>();

        _scroll = _listViewport.gameObject.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.135f;
        _scroll.scrollSensitivity = 28f;
        _scroll.viewport = _listViewport;

        _listContent = Area("RosterContent", _listViewport, 0f, 1f, 1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.anchoredPosition = Vector2.zero;
        _listContent.sizeDelta = new Vector2(0f, 0f);
        _scroll.content = _listContent;

        RectTransform bar = Area("RosterScrollbar", _left, 0.95f, 0.012f, 0.975f, 0.725f);
        Image barImage = bar.gameObject.AddComponent<Image>();
        barImage.color = new Color(0.06f, 0.05f, 0.035f, 0.8f);

        RectTransform sliding = Area("SlidingArea", bar, 0f, 0f, 1f, 1f);
        RectTransform handle = Area("Handle", sliding, 0f, 0f, 1f, 1f);
        handle.offsetMin = new Vector2(1f, 1f);
        handle.offsetMax = new Vector2(-1f, -1f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = GoldDim;

        Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        _scroll.verticalScrollbar = scrollbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        _emptyText = Label("EmptyRoster", _listViewport, 0.05f, 0.35f, 0.95f, 0.68f,
            string.Empty, 18f, Muted, TextAlignmentOptions.Center, true);
        _emptyText.gameObject.SetActive(false);
    }

    private void BuildCenterPanel(RectTransform body)
    {
        _center = PanelArea("TeamPanel", body, 0.327f, 0f, 0.635f, 1f);
        _teamHeader = Label("TeamHeader", _center, 0.03f, 0.94f, 0.97f, 0.995f,
            L("ui_dispatch_team", "编队"), 21f, Gold);

        for (int i = 0; i < 4; i++)
        {
            float top = 0.91f - i * 0.225f;
            _slots[i] = CreateSlot(i, _center, top - 0.20f, top);
        }
    }

    private SlotWidget CreateSlot(int index, RectTransform parent, float bottom, float top)
    {
        RectTransform root = Area("Slot_" + (index + 1), parent, 0.01f, bottom, 0.99f, top);
        Image image = root.gameObject.AddComponent<Image>();
        image.color = Card;
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        int captured = index;
        button.onClick.AddListener(() =>
        {
            if (_state == null || _state.Snapshot == null || _state.Snapshot.Slots == null) return;
            if (captured < 0 || captured >= _state.Snapshot.Slots.Length) return;
            DispatchSlotData slot = _state.Snapshot.Slots[captured];
            if (slot != null && slot.Occupied) Send(DispatchAction.Select, slot.Id);
        });

        RectTransform badge = Area("Badge", root, 0.025f, 0.62f, 0.23f, 0.92f);
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.color = index == 0 ? PlayerGold : AiBlue;
        badgeImage.raycastTarget = false;
        TextMeshProUGUI badgeText = Label("BadgeText", badge, 0f, 0f, 1f, 1f,
            index == 0 ? L("ui_dispatch_badge_player", "玩家操控") : "AI",
            14f, index == 0 ? Dark : Paper, TextAlignmentOptions.Center, false);

        TextMeshProUGUI name = Label("Name", root, 0.25f, 0.54f, 0.77f, 0.94f,
            string.Empty, 20f, Paper, TextAlignmentOptions.MidlineLeft, false);
        name.enableAutoSizing = true;
        name.fontSizeMin = 15f;
        name.fontSizeMax = 20f;
        name.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI meta = Label("Meta", root, 0.025f, 0.06f, 0.98f, 0.52f,
            string.Empty, 15f, Muted, TextAlignmentOptions.TopLeft, true);

        Button remove = MakeButton("SlotRemove", root, 0.78f, 0.57f, 0.98f, 0.90f,
            L("ui_dispatch_remove_short", "移除"), () =>
            {
                if (_state == null || _state.Snapshot == null || _state.Snapshot.Slots == null) return;
                if (captured < 0 || captured >= _state.Snapshot.Slots.Length) return;
                DispatchSlotData slot = _state.Snapshot.Slots[captured];
                if (slot != null && slot.Occupied) Send(DispatchAction.Remove, slot.Id);
            }, 15f);
        TextMeshProUGUI removeText = ButtonLabel(remove);

        var widget = new SlotWidget
        {
            Button = button,
            Image = image,
            Outline = outline,
            Badge = badgeImage,
            BadgeText = badgeText,
            Name = name,
            Meta = meta,
            Remove = remove,
            RemoveText = removeText
        };
        return widget;
    }

    private void BuildDetailPanel(RectTransform body)
    {
        _right = PanelArea("DetailPanel", body, 0.647f, 0f, 1f, 1f);
        Image panelImage = _right.GetComponent<Image>();
        if (_visuals != null && _visuals.DetailBackground != null)
        {
            panelImage.sprite = _visuals.DetailBackground;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }

        _previewFrame = Area("PreviewFrame", _right, 0.035f, 0.56f, 0.965f, 0.98f);
        Image frameImage = _previewFrame.gameObject.AddComponent<Image>();
        Sprite frameSprite = _visuals != null ? _visuals.PortraitFrame : null;
        if (frameSprite != null)
        {
            frameImage.sprite = frameSprite;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = Color.white;
        }
        else
        {
            frameImage.color = new Color(0.045f, 0.035f, 0.025f, 0.95f);
        }
        frameImage.raycastTarget = false;

        RectTransform portraitRect = Area("Portrait", _previewFrame, 0.055f, 0.055f, 0.945f, 0.945f);
        _previewPortrait = portraitRect.gameObject.AddComponent<Image>();
        _previewPortrait.preserveAspect = true;
        _previewPortrait.raycastTarget = false;
        _previewPortrait.enabled = false;

        _previewHost = Area("SpinePreview", _previewFrame, 0.035f, 0.06f, 0.965f, 0.945f);
        _preview = _previewHost.gameObject.AddComponent<RunTeamSpinePreview>();

        _previewFallback = Label("PreviewFallback", _previewFrame, 0.06f, 0.16f, 0.94f, 0.88f,
            "—", 46f, GoldDim, TextAlignmentOptions.Center, false);
        _previewFallback.richText = true;

        _previewNote = Label("PreviewNote", _previewFrame, 0.04f, 0.015f, 0.96f, 0.11f,
            string.Empty, 14f, Muted, TextAlignmentOptions.Center, false);

        _detailName = Label("DetailName", _right, 0.04f, 0.47f, 0.96f, 0.55f,
            string.Empty, 28f, Gold, TextAlignmentOptions.MidlineLeft, false);
        _detailName.enableAutoSizing = true;
        _detailName.fontSizeMin = 20f;
        _detailName.fontSizeMax = 28f;
        _detailName.overflowMode = TextOverflowModes.Ellipsis;

        _detailMeta = Label("DetailMeta", _right, 0.04f, 0.427f, 0.96f, 0.468f,
            string.Empty, 17f, Paper, TextAlignmentOptions.MidlineLeft, false);
        _detailProfession = Label("DetailProfession", _right, 0.04f, 0.387f, 0.96f, 0.425f,
            string.Empty, 16f, GoldDim, TextAlignmentOptions.MidlineLeft, false);
        _detailStatus = Label("DetailStatus", _right, 0.04f, 0.347f, 0.96f, 0.385f,
            string.Empty, 17f, Paper, TextAlignmentOptions.MidlineLeft, false);

        _detailStats = Label("DetailStats", _right, 0.04f, 0.215f, 0.96f, 0.34f,
            string.Empty, 18f, Paper, TextAlignmentOptions.TopLeft, true);
        _detailStats.richText = true;

        _detailHint = Label("DetailHint", _right, 0.04f, 0.135f, 0.96f, 0.205f,
            string.Empty, 14f, Muted, TextAlignmentOptions.TopLeft, true);

        _join = MakeButton("Btn_Join", _right, 0.02f, 0.015f, 0.33f, 0.125f,
            L("ui_dispatch_join", "加入队伍"), () => Send(DispatchAction.Add, ViewId()), 17f);
        _remove = MakeButton("Btn_Remove", _right, 0.34f, 0.015f, 0.58f, 0.125f,
            L("ui_dispatch_remove", "移出队伍"), () => Send(DispatchAction.Remove, ViewId()), 17f);
        _control = MakeButton("Btn_SetControlled", _right, 0.60f, 0.015f, 0.98f, 0.125f,
            L("ui_dispatch_control", "设为操控"), () => Send(DispatchAction.SetControlled, ViewId()), 17f);

        _joinText = ButtonLabel(_join);
        _removeText = ButtonLabel(_remove);
        _controlText = ButtonLabel(_control);
        _joinImage = _join != null ? _join.GetComponent<Image>() : null;
        _removeImage = _remove != null ? _remove.GetComponent<Image>() : null;
        _controlImage = _control != null ? _control.GetComponent<Image>() : null;
    }

    private void BuildFooter()
    {
        _reason = Label("DepartReason", _layout, 0f, 0.07f, 0.6f, 0.16f,
            string.Empty, 18f, Muted, TextAlignmentOptions.TopLeft, true);
        _footerHint = Label("FooterHint", _layout, 0f, 0.012f, 0.6f, 0.06f,
            string.Empty, 14f, GoldDim, TextAlignmentOptions.MidlineLeft, true);

        _depart = EnsureFooterButton("Btn_Depart", 0.61f, 0.72f, DoDepart);
        _return = EnsureFooterButton("Btn_Return", 0.73f, 0.84f, DoReturn);
        _close = EnsureFooterButton("Btn_Close", 0.85f, 0.98f, DoClose);

        _departText = ButtonLabel(_depart);
        _returnText = ButtonLabel(_return);
        _departImage = _depart != null ? _depart.GetComponent<Image>() : null;
        _returnImage = _return != null ? _return.GetComponent<Image>() : null;
        _closeImage = _close != null ? _close.GetComponent<Image>() : null;
    }

    private Button EnsureFooterButton(string name, float x0, float x1, Action click)
    {
        Transform existing = transform.Find(name);
        RectTransform rect;
        Button button;
        Image image;
        if (existing != null)
        {
            rect = existing as RectTransform;
            button = existing.GetComponent<Button>();
            image = existing.GetComponent<Image>();
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            button = go.GetComponent<Button>();
            image = go.GetComponent<Image>();
        }

        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(x0, 0.075f);
        rect.anchorMax = new Vector2(x1, 0.155f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        if (button == null) button = rect.gameObject.AddComponent<Button>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        if (click != null) button.onClick.AddListener(() => click());

        if (rect.GetComponent<Outline>() == null)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        TextMeshProUGUI label = ButtonLabel(button);
        if (label == null)
            label = Label("Label", rect, 0.04f, 0.04f, 0.96f, 0.96f, string.Empty, 19f, Paper,
                TextAlignmentOptions.Center, false);
        label.font = _font;
        label.fontSize = 19f;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return button;
    }

    private void UpdateFilterButtons(DispatchSnapshot snapshot)
    {
        if (_filterAllText != null)
            _filterAllText.text = L("ui_dispatch_filter_all_count", "全部 {0}", snapshot.TotalRetainers);
        if (_filterAvailableText != null)
            _filterAvailableText.text = L("ui_dispatch_filter_available_count", "可出战 {0}", snapshot.AvailableCount);
        if (_filterTeamText != null)
            _filterTeamText.text = L("ui_dispatch_filter_team_count", "已编队 {0}", snapshot.TeamCount);

        ApplyFilterVisual(_filterAllImage, _filterAllText, snapshot.Filter == DispatchFilter.All);
        ApplyFilterVisual(_filterAvailableImage, _filterAvailableText, snapshot.Filter == DispatchFilter.Available);
        ApplyFilterVisual(_filterTeamImage, _filterTeamText, snapshot.Filter == DispatchFilter.Team);
    }

    private void ApplyFilterVisual(Image image, TextMeshProUGUI label, bool active)
    {
        Sprite sprite = _visuals == null ? null : active ? _visuals.CardSelected : _visuals.CardNormal;
        SetSurface(image, sprite, active ? CardSelected : Card);
        if (label != null) label.color = active ? Paper : Muted;
    }

    private void UpdateSearch(string search)
    {
        if (_search == null) return;
        if (_searchPlaceholder != null)
            _searchPlaceholder.text = L("ui_dispatch_search", "搜索姓名");
        if (!_search.isFocused && _search.text != (search ?? string.Empty))
        {
            _syncingSearch = true;
            _search.text = search ?? string.Empty;
            _syncingSearch = false;
        }
    }

    private void UpdateCards(DispatchSnapshot snapshot)
    {
        if (_listContent == null) return;

        float scrollValue = 1f;
        if (_scroll != null)
        {
            scrollValue = _scroll.verticalNormalizedPosition;
            if (float.IsNaN(scrollValue)) scrollValue = 1f;
        }

        var signature = new StringBuilder();
        signature.Append((int)snapshot.Filter);
        for (int i = 0; i < snapshot.Cards.Length; i++) signature.Append('|').Append(snapshot.Cards[i].Id);
        string nextSignature = signature.ToString();
        bool listChanged = nextSignature != _listSignature;
        _listSignature = nextSignature;
        _pendingScroll = listChanged ? 1f : scrollValue;

        var visible = new HashSet<uint>();
        for (int i = 0; i < snapshot.Cards.Length; i++)
        {
            DispatchCardData card = snapshot.Cards[i];
            visible.Add(card.Id);
            CardWidget widget;
            if (!_cards.TryGetValue(card.Id, out widget))
            {
                widget = CreateCard(card.Id);
                _cards.Add(card.Id, widget);
            }
            widget.Root.gameObject.SetActive(true);
            widget.Root.SetSiblingIndex(i);
            widget.Root.anchoredPosition = new Vector2(0f, -8f - i * 120f);

            bool selected = snapshot.ResolvedViewId == card.Id;
            Sprite sprite = _visuals == null ? null
                : !card.Available ? _visuals.CardDisabled
                : selected ? _visuals.CardSelected : _visuals.CardNormal;
            Color fallback = !card.Available ? Disabled : selected ? CardSelected : Card;
            SetSurface(widget.Image, sprite, fallback);
            widget.Outline.enabled = selected || card.InTeam;
            widget.Outline.effectColor = selected ? Gold : new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.5f);

            widget.Name.text = card.Name;
            widget.Name.color = card.Available ? Paper : Muted;
            widget.Quality.text = card.QualityText + " · " +
                L("ui_dispatch_confidence_short", "信心 {0}", card.Confidence);
            widget.Quality.color = QualityColor(card.Quality);
            widget.Status.text = card.Status;
            widget.Status.color = card.Available ? Good : card.Wounded ? Warning : LowConfidence;
            widget.TeamTag.text = card.Controlled
                ? L("ui_dispatch_tag_controlled", "操控")
                : card.InTeam ? L("ui_dispatch_tag_team", "已编队") : string.Empty;
            widget.TeamTag.color = card.Controlled ? PlayerGold : Gold;
        }

        foreach (KeyValuePair<uint, CardWidget> pair in _cards)
        {
            if (!visible.Contains(pair.Key)) pair.Value.Root.gameObject.SetActive(false);
        }

        float contentHeight = 16f + snapshot.Cards.Length * 120f;
        if (_listViewport != null) contentHeight = Mathf.Max(contentHeight, _listViewport.rect.height);
        _listContent.sizeDelta = new Vector2(0f, contentHeight);

        if (_emptyText != null)
        {
            _emptyText.gameObject.SetActive(snapshot.Cards.Length == 0);
            if (snapshot.TotalRetainers == 0)
            {
                _emptyText.text = L("ui_dispatch_empty_no_retainers",
                    "暂无门客。\n请先通过村庄招募获得门客，再进行派遣。");
            }
            else
            {
                _emptyText.text = L("ui_dispatch_empty_filter",
                    "没有符合筛选或搜索条件的门客。\n可切换「全部」或清空搜索。");
            }
        }

        if (_leftHeader != null)
            _leftHeader.text = L("ui_dispatch_roster", "门客列表") + " · " + snapshot.TotalRetainers;
    }

    private CardWidget CreateCard(uint id)
    {
        RectTransform root = Area("Card_" + id, _listContent, 0f, 1f, 1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(-10f, 112f);
        root.anchoredPosition = Vector2.zero;

        Image image = root.gameObject.AddComponent<Image>();
        image.color = Card;
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.5f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => Send(DispatchAction.Select, id));

        TextMeshProUGUI name = Label("Name", root, 0.035f, 0.49f, 0.63f, 0.93f,
            string.Empty, 21f, Paper, TextAlignmentOptions.MidlineLeft, false);
        name.enableAutoSizing = true;
        name.fontSizeMin = 15f;
        name.fontSizeMax = 21f;
        name.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI quality = Label("Quality", root, 0.035f, 0.27f, 0.88f, 0.52f,
            string.Empty, 16f, Gold, TextAlignmentOptions.MidlineLeft, false);
        TextMeshProUGUI status = Label("Status", root, 0.035f, 0.025f, 0.88f, 0.29f,
            string.Empty, 16f, Muted, TextAlignmentOptions.MidlineLeft, false);
        TextMeshProUGUI teamTag = Label("TeamTag", root, 0.64f, 0.58f, 0.97f, 0.91f,
            string.Empty, 14f, Gold, TextAlignmentOptions.MidlineRight, false);

        quality.enableAutoSizing = status.enableAutoSizing = true;
        quality.fontSizeMin = status.fontSizeMin = 12f;
        quality.fontSizeMax = status.fontSizeMax = 16f;
        return new CardWidget
        {
            Root = root,
            Image = image,
            Outline = outline,
            Name = name,
            Quality = quality,
            Status = status,
            TeamTag = teamTag
        };
    }

    private void UpdateTeam(DispatchSnapshot snapshot)
    {
        if (_teamHeader != null)
        {
            _teamHeader.text = snapshot.IsInRun
                ? L("ui_dispatch_team_inrun", "探索队伍（只读）")
                : L("ui_dispatch_team_header", "编队 {0}/{1} · 首位玩家操控，其余 AI",
                    snapshot.TeamCount, 4);
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            SlotWidget widget = _slots[i];
            if (widget == null) continue;
            DispatchSlotData slot = snapshot.Slots != null && i < snapshot.Slots.Length
                ? snapshot.Slots[i] : null;
            bool occupied = slot != null && slot.Occupied;
            bool selected = occupied && slot.Id == snapshot.ResolvedViewId;

            Sprite sprite = _visuals == null ? null
                : !occupied ? _visuals.CardNormal
                : !slot.Available ? _visuals.CardDisabled
                : selected ? _visuals.CardSelected : _visuals.CardNormal;
            Color fallback = occupied ? (selected ? CardSelected : Card) : new Color(0.11f, 0.09f, 0.065f, 1f);
            SetSurface(widget.Image, sprite, fallback);
            widget.Outline.enabled = selected;
            widget.Outline.effectColor = Gold;
            widget.Button.interactable = occupied;
            widget.Badge.color = i == 0 ? PlayerGold : AiBlue;
            widget.BadgeText.text = i == 0
                ? L("ui_dispatch_badge_player", "玩家操控")
                : L("ui_dispatch_badge_ai", "AI");
            widget.BadgeText.color = i == 0 ? Dark : Paper;
            widget.Name.text = occupied
                ? (i == 0 ? "操控 · " : "AI · ") + slot.Name
                : (i == 0 ? "操控 · " : "AI · ") + L("ui_dispatch_slot_empty_title", "空槽 {0}", i + 1);
            widget.Name.color = occupied ? Paper : Muted;
            widget.Meta.text = occupied
                ? slot.QualityText + " · " + slot.Status
                : L("ui_dispatch_slot_empty_hint", "从左侧查看门客，在右侧详情中「加入队伍」。");
            widget.Meta.color = occupied ? (slot.Available ? Muted : Warning) : Muted;

            bool canRemove = occupied && !snapshot.IsInRun && snapshot.CanEdit;
            if (widget.Remove != null)
            {
                widget.Remove.gameObject.SetActive(occupied && !snapshot.IsInRun);
                widget.Remove.interactable = canRemove;
            }
            if (widget.RemoveText != null)
                widget.RemoveText.text = L("ui_dispatch_remove_short", "移除");
        }
    }

    private void UpdateDetail(DispatchSnapshot snapshot)
    {
        DispatchRoleData role = snapshot.Detail;
        if (role == null || !role.HasRole)
        {
            if (_detailName != null)
            {
                _detailName.text = role != null && role.Id != 0
                    ? "门客 #" + role.Id
                    : L("ui_dispatch_no_selection", "未选择门客");
            }
            if (_detailMeta != null) _detailMeta.text = string.Empty;
            if (_detailProfession != null)
                _detailProfession.text = L("ui_dispatch_profession_placeholder", "职业待配置");
            if (_detailStatus != null)
            {
                _detailStatus.text = snapshot.TotalRetainers == 0
                    ? L("ui_dispatch_detail_empty", "暂无门客可查看")
                    : L("ui_dispatch_detail_pick", "点击左侧卡片或编队槽位查看详情");
                _detailStatus.color = Muted;
            }
            if (_detailStats != null) _detailStats.text = string.Empty;
            if (_detailHint != null)
            {
                _detailHint.text = snapshot.TotalRetainers == 0
                    ? L("ui_dispatch_detail_empty_hint", "请先通过村庄招募获得门客；本界面不会自动赠送或招募。")
                    : L("ui_dispatch_detail_pick_hint", "查看详情不会自动加入编队，请使用下方「加入队伍」。");
            }

            SetActionButtons(null);
            ShowPreview(role, snapshot);
            return;
        }

        if (_detailName != null) _detailName.text = role.Name;
        if (_detailMeta != null)
        {
            _detailMeta.text = role.QualityText + " · " +
                L("ui_dispatch_confidence", "信心 {0}/100", role.Confidence);
        }
        if (_detailProfession != null) _detailProfession.text = role.Profession;
        if (_detailStatus != null)
        {
            string position = role.Controlled
                ? L("ui_dispatch_badge_player", "玩家操控")
                : role.InTeam
                    ? L("ui_dispatch_badge_ai", "AI") + " · " + L("ui_dispatch_team_index", "第 {0} 位", role.TeamIndex)
                    : L("ui_dispatch_not_in_team", "未编队");
            _detailStatus.text = role.Wounded
                ? position + " · " + L("ui_dispatch_wound_remaining", "重伤剩余 {0}", role.WoundTime)
                : position + " · " + role.Status;
            _detailStatus.color = role.Wounded ? Warning : role.Available ? Good : LowConfidence;
        }
        if (_detailStats != null)
        {
            _detailStats.text =
                L("ui_dispatch_stat_hp", "预计派遣生命") + "  <color=#F3DFA4>" + role.Hp + "</color>    " +
                L("ui_dispatch_stat_attack", "普通攻击") + "  <color=#F3DFA4>" + role.Attack + "</color>\n" +
                L("ui_dispatch_stat_speed", "水平速度") + "  <color=#F3DFA4>" +
                role.Speed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "</color>    " +
                L("ui_dispatch_stat_defense", "防御减伤") + "  <color=#F3DFA4>" + role.DefenseReduction + "</color>";
        }
        if (_detailHint != null)
        {
            _detailHint.text = role.JoinReason + "\n" +
                L("ui_dispatch_stat_note", "属性按基础值与家族加成估算；局内成长从 0 开始。");
        }

        SetActionButtons(role);
        ShowPreview(role, snapshot);
    }

    private void ShowPreview(DispatchRoleData role, DispatchSnapshot snapshot)
    {
        bool hasRole = role != null && role.HasRole;
        bool hasSpine = hasRole && role.HasSpine;
        if (hasSpine && _preview != null)
        {
            _previewHost.gameObject.SetActive(true);
            _preview.Show(role.SpineAsset, role.PreviewSkin, role.SpineMaterial, false);
        }
        else if (_preview != null)
        {
            _preview.Hide();
            if (_previewHost != null) _previewHost.gameObject.SetActive(false);
        }

        bool spineVisible = hasSpine && _preview != null && _preview.IsShowing;
        bool hasPortrait = hasRole && role.Portrait != null;
        if (_previewPortrait != null)
        {
            _previewPortrait.sprite = hasPortrait ? role.Portrait : null;
            _previewPortrait.enabled = hasPortrait && !spineVisible;
        }

        if (_previewFallback != null)
        {
            bool showFallback = !spineVisible && !hasPortrait;
            _previewFallback.gameObject.SetActive(showFallback);
            if (showFallback)
            {
                string initial = hasRole && !string.IsNullOrEmpty(role.Name) ? role.Name.Substring(0, 1) : "—";
                _previewFallback.text = initial + "\n<size=15>" +
                    L("ui_dispatch_preview_fallback", "暂无立绘预览，显示文字占位") + "</size>";
            }
        }

        if (_previewNote != null)
        {
            _previewNote.text = !hasRole
                ? L("ui_dispatch_preview_none", "暂无查看对象")
                : spineVisible
                    ? role.Controlled
                        ? L("ui_dispatch_preview_spine_player", "待命展示 · 玩家操控")
                        : L("ui_dispatch_preview_spine_shared", "待命展示 · 临时形象")
                    : hasPortrait
                        ? L("ui_dispatch_preview_portrait", "立绘预览")
                        : L("ui_dispatch_preview_none_asset", "无立绘素材 · 文字占位");
        }
    }

    private void SetActionButtons(DispatchRoleData role)
    {
        bool canAdd = role != null && role.CanAdd;
        bool canRemove = role != null && role.CanRemove;
        bool canControl = role != null && role.CanSetControlled;
        if (_join != null) _join.interactable = canAdd;
        if (_remove != null) _remove.interactable = canRemove;
        if (_control != null) _control.interactable = canControl;
        SetButtonColors(_join, _joinImage, canAdd, Gold, Dark);
        SetButtonColors(_remove, _removeImage, canRemove, new Color(0.34f, 0.27f, 0.16f), Paper);
        SetButtonColors(_control, _controlImage, canControl, new Color(0.24f, 0.33f, 0.44f), Paper);
    }

    private void UpdateFooter(DispatchSnapshot snapshot)
    {
        BindStaticText();
        bool canDepart = snapshot.CanDepart && !snapshot.IsInRun && !snapshot.IsTransitioning;
        bool canReturn = snapshot.IsInRun && !snapshot.IsTransitioning;
        if (_depart != null) _depart.gameObject.SetActive(!snapshot.IsInRun);
        if (_return != null) _return.gameObject.SetActive(snapshot.IsInRun);
        if (_retainers != null)
        {
            _retainers.gameObject.SetActive(!snapshot.IsInRun);
            _retainers.interactable = !snapshot.IsTransitioning;
        }
        foreach (var primary in new[] { _depart, _return })
        {
            if (primary == null) continue;
            var rect = (RectTransform)primary.transform;
            rect.anchorMin = new Vector2(.61f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(.83f, rect.anchorMax.y);
        }
        var closeLabel = ButtonLabel(_close);
        if (closeLabel != null) closeLabel.text = L("ui_common_close", "返回");
        if (_depart != null) _depart.interactable = canDepart;
        if (_return != null) _return.interactable = canReturn;
        if (_close != null) _close.interactable = true;

        SetButtonColors(_depart, _departImage, canDepart, Gold, Dark);
        SetButtonColors(_return, _returnImage, canReturn, new Color(0.26f, 0.35f, 0.45f), Paper);
        SetButtonColors(_close, _closeImage, true, new Color(0.33f, 0.27f, 0.17f), Gold);

        if (_reason != null)
        {
            _reason.text = snapshot.DepartReason;
            _reason.color = snapshot.IsInRun ? Gold : canDepart ? Good : Warning;
        }
    }

    private void BindStaticText()
    {
        if (_footerHint != null)
            _footerHint.text = L("ui_dispatch_footer_hint",
                "点击卡片或槽位仅查看；加入、移出与设为操控使用右侧按钮。");
        if (_joinText != null) _joinText.text = L("ui_dispatch_join", "加入队伍");
        if (_removeText != null) _removeText.text = L("ui_dispatch_remove", "移出队伍");
        if (_controlText != null) _controlText.text = L("ui_dispatch_control", "设为操控");
        if (_departText != null) _departText.text = L("ui_dispatch_depart", "出发");
        if (_returnText != null) _returnText.text = L("ui_dispatch_return", "撤离");
        if (_searchPlaceholder != null) _searchPlaceholder.text = L("ui_dispatch_search", "搜索姓名");
    }

    private void SetButtonColors(Button button, Image image, bool enabled, Color enabledColor, Color labelColor)
    {
        if (image != null) image.color = enabled ? enabledColor : Disabled;
        TextMeshProUGUI label = ButtonLabel(button);
        if (label != null) label.color = enabled ? labelColor : Muted;
    }

    private uint ViewId()
    {
        return _state != null && _state.Snapshot != null && _state.Snapshot.Detail != null
            ? _state.Snapshot.Detail.Id : 0u;
    }

    private static TextMeshProUGUI ButtonLabel(Button button)
    {
        return button == null ? null : button.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static string L(string key, string fallback, params object[] args)
    {
        return GameplayText.Text(key, fallback, args);
    }

    private static Color QualityColor(uint quality)
    {
        switch (quality)
        {
            case 1: return new Color(0.72f, 0.70f, 0.65f);
            case 2: return new Color(0.55f, 0.84f, 0.56f);
            case 3: return new Color(0.44f, 0.68f, 0.90f);
            case 4: return new Color(0.72f, 0.55f, 0.92f);
            case 5: return new Color(0.98f, 0.66f, 0.28f);
            default: return GoldDim;
        }
    }

    private static void SetSurface(Image image, Sprite sprite, Color fallback)
    {
        if (image == null) return;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = fallback;
        }
    }

    private RectTransform PanelArea(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        RectTransform rect = Area(name, parent, x0, y0, x1, y1);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Panel;
        image.raycastTarget = true;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private Button MakeButton(string name, Transform parent, float x0, float y0, float x1, float y1,
        string label, Action click, float fontSize)
    {
        RectTransform rect = Area(name, parent, x0, y0, x1, y1);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Card;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(GoldDim.r, GoldDim.g, GoldDim.b, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        if (click != null) button.onClick.AddListener(() => click());
        Label("Label", rect, 0.03f, 0.04f, 0.97f, 0.96f, label, fontSize, Paper,
            TextAlignmentOptions.Center, false);
        return button;
    }

    private TextMeshProUGUI Label(string name, Transform parent, float x0, float y0, float x1, float y1,
        string text, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
        bool wrap = true)
    {
        RectTransform rect = Area(name, parent, x0, y0, x1, y1);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = color;
        label.text = text ?? string.Empty;
        label.alignment = alignment;
        label.enableWordWrapping = wrap;
        label.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Area(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void HideLegacyChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null) child.gameObject.SetActive(false);
    }
}
