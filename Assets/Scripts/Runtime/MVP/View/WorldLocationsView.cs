using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>左上筛选类别，顺序与按钮一致。</summary>
public enum WorldLocationFilter
{
    Capital = 0, Trade = 1, Resource = 2, Rumor = 3, Task = 4
}

/// <summary>筛选口径下的单一判断入口：类别 → 筛选按钮。</summary>
public static class WorldLocationKindFilter
{
    public static WorldLocationFilter Of(WorldLocationKind kind)
    {
        switch (kind)
        {
            case WorldLocationKind.Capital: return WorldLocationFilter.Capital;
            case WorldLocationKind.Trade: return WorldLocationFilter.Trade;
            case WorldLocationKind.Resource: return WorldLocationFilter.Resource;
            case WorldLocationKind.Tribe: return WorldLocationFilter.Trade;
            case WorldLocationKind.Temporary: return WorldLocationFilter.Rumor;
            default: return WorldLocationFilter.Task;
        }
    }
}

/// <summary>
/// 被动的地点层：地图标记、左上筛选、右侧双身份详情卡。
/// 只接收 <see cref="Render"/> 的快照并向 <see cref="Intent"/> 转发意图，不查询任何领域服务。
/// </summary>
public sealed class WorldLocationsView : MonoBehaviour
{
    public const string BuildVersion="WL-20261103-C";
    // ── 配色：深墨 + 暖金，贴合羊皮地图 ──
    private static readonly Color Ink = new Color(.145f, .12f, .09f, 1f);
    private static readonly Color CardColor = new Color(.10f, .085f, .065f, .965f);
    private static readonly Color CardBorder = new Color(.62f, .50f, .30f, .85f);
    private static readonly Color Gold = new Color(.96f, .82f, .48f, 1f);
    private static readonly Color WarmText = new Color(.94f, .90f, .80f, 1f);
    private static readonly Color MutedText = new Color(.72f, .66f, .55f, 1f);
    private static readonly Color LockText = new Color(.96f, .64f, .42f, 1f);
    private static readonly Color ButtonNormal = new Color(.22f, .18f, .125f, 1f);
    private static readonly Color ButtonActive = new Color(.70f, .55f, .28f, 1f);
    private static readonly Color MarkerInk = new Color(.16f, .13f, .10f, .86f);
    private static readonly Color MarkerCapital = new Color(.24f, .18f, .11f, .92f);
    private static readonly Color MarkerGold = new Color(.86f, .68f, .30f, .96f);

    private const float CapitalSize = 38f;
    private const float NormalSize = 30f;
    private const float CardWidth = 380f;
    private const float CardTop = 96f;
    private const float CardBottom = 88f;
    private const float FilterBarWidth = 516f;
    private const float FilterBarHeight = 52f;
    private const float TradeRowStride = 58f;
    /// <summary>标签在 zoom≥1.5 时显示；选中的标记与主城名始终显示。</summary>
    private const float LabelZoomThreshold = 1.5f;

    /// <summary>单个地图标记的复用壳（按地点 ID 建一次）。</summary>
    private sealed class Marker
    {
        public uint Id;
        public WorldLocationSnapshot Data;
        public RectTransform Root;
        public Image Background;
        public Image Glyph;
        public TextMeshProUGUI Initial;
        public TextMeshProUGUI Label;
        public WorldMapPointIcon Icon;
        public WorldMapPoint Point;
    }

    /// <summary>交易行的固定结构引用，避免每帧在子物体里按名字找。</summary>
    private sealed class TradeRowView
    {
        public RectTransform Root;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Price;
        public TextMeshProUGUI BuyLabel;
        public TextMeshProUGUI SellLabel;
        public Button Buy;
        public Button Sell;
    }

    // ── 运行时引用 ──
    private RectTransform _mapContent;
    private RectTransform _overlay;
    private RectTransform _markerLayer;
    private RectTransform _filterBar;
    private RectTransform _card;

    private TextMeshProUGUI _title, _meta, _relation, _description, _lock, _hint, _identityName;
    private TextMeshProUGUI _officialTabLabel, _familyTabLabel, _officialHint, _familyHint;
    private RectTransform _officialPage, _familyPage;
    private RectTransform _tradeList, _tradeViewport;
    private ScrollRect _tradeScroll;
    private TextMeshProUGUI _tradeEmpty;
    private Button _officeButton, _buildButton, _harvestButton,_resolveButton;
    private TextMeshProUGUI _tradeHeader;
    private ScrollRect _descriptionScroll;
    private uint _descriptionLocation;
    private TextMeshProUGUI _officeLabel, _buildLabel, _harvestLabel;

    // ── 筛选与缓存 ──
    private readonly Dictionary<WorldLocationFilter, bool> _filters = new Dictionary<WorldLocationFilter, bool>();
    private readonly Dictionary<WorldLocationFilter, Image> _filterImages = new Dictionary<WorldLocationFilter, Image>();
    private readonly Dictionary<WorldLocationFilter, TextMeshProUGUI> _filterLabels = new Dictionary<WorldLocationFilter, TextMeshProUGUI>();
    private readonly Dictionary<uint, Marker> _markers = new Dictionary<uint, Marker>();
    private readonly List<uint> _staleIds = new List<uint>();
    private readonly List<TradeRowView> _tradeRows = new List<TradeRowView>();
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    private WorldLocationsDisplayState _state;
    private uint _selectedId;
    private float _zoom = 1f;
    private bool _initialized;
    private WorldLocationIdentity _activeIdentity = WorldLocationIdentity.Official;

    /// <summary>点击 → 转发意图；供父面板订阅。</summary>
    public event Action<WorldLocationUiIntent> Intent;

    /// <summary>当前缓存的标记数量（验收复用：同一状态重复 Render 不应增长）。</summary>
    public int MarkerCount { get { return _markers.Count; } }

    /// <summary>详情卡是否打开（父面板处理 Escape 前的查询）。</summary>
    public bool DetailOpen { get { return _card != null && _card.gameObject.activeSelf; } }

    /// <summary>当前展示的筛选口径，true=显示。</summary>
    public bool IsFilterEnabled(WorldLocationFilter filter)
    {
        bool on;
        return !_filters.TryGetValue(filter, out on) || on;
    }

    private void OnDestroy()
    {
        Intent = null;
    }

    // ══════════════════ 构建 ══════════════════

    /// <summary>幂等构建：重复调用不会重复搭界面，也不会重复建标记层。</summary>
    public void Initialize(RectTransform mapContent, RectTransform overlayRoot)
    {
        if (mapContent == null || overlayRoot == null)
        {
            Debug.LogWarning("[地点视图] Initialize 缺少 mapContent 或 overlayRoot，界面未构建。");
            return;
        }
        if (_initialized && mapContent == _mapContent && overlayRoot == _overlay) return;
        _mapContent = mapContent;
        _overlay = overlayRoot;

        // 标记层铺满 mapContent：标记用归一化坐标作 anchor，映射由该层矩形决定，随地图同轴缩放。
        Transform existing = _mapContent.Find(MarkerLayerName);
        _markerLayer = existing as RectTransform;
        if (_markerLayer == null)
        {
            _markerLayer = GameplayUi.Rect(_mapContent, MarkerLayerName, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            var layerImage = _markerLayer.gameObject.AddComponent<Image>();
            layerImage.color = new Color(0f, 0f, 0f, 0f);
            layerImage.raycastTarget = false;
        }
        _markerLayer.anchorMin = Vector2.zero;
        _markerLayer.anchorMax = Vector2.one;
        _markerLayer.offsetMin = _markerLayer.offsetMax = Vector2.zero;
        _markerLayer.SetAsLastSibling();

        BuildFilterBar();
        BuildDetailCard();
        _initialized = true;
        ApplyIdentityTabs();
        ApplyFilters();
        if (_state != null) Render(_state);
    }

    private const string MarkerLayerName = "LocationMarkers";

    // ══════════════════ 对外接口 ══════════════════

    /// <summary>只做显示：同步标记缓存、筛选、选中高亮与详情卡内容。</summary>
    public void Render(WorldLocationsDisplayState state)
    {
        if (state == null) return;
        _state = state;
        if (!_initialized) return;

        SyncMarkers(state);
        _selectedId = state.Selected != null ? state.Selected.Id
            : (state.SelectedId != 0u ? state.SelectedId : 0u);
        ApplyFilters();
        ApplySelection();
        RenderDetail(state);
        if (_hint != null) _hint.text = state.Hint ?? string.Empty;
    }

    /// <summary>父面板处理 Escape 时先调用：详情打开则关闭、清选中并发出 CloseDetail，返回 true。</summary>
    public bool CloseDetail()
    {
        if (_card == null || !_card.gameObject.activeSelf) return false;
        _selectedId = 0u;
        ApplySelection();
        _card.gameObject.SetActive(false);
        Emit(WorldLocationUiAction.CloseDetail, 0u);
        return true;
    }

    /// <summary>只调整标记/标签的屏幕尺寸；地图本体缩放由 WorldMapNavigation 负责。</summary>
    public void SetZoom(float zoom)
    {
        float clamped = Mathf.Clamp(zoom, WorldMapNavigation.MinZoom, WorldMapNavigation.MaxZoom);
        if (Mathf.Approximately(clamped, _zoom)) return;
        _zoom = clamped;
        ApplyMarkerSizes();
        ApplySelection();
    }

    // ══════════════════ 意图 ══════════════════

    private void Emit(WorldLocationUiAction action, uint locationId, uint itemId = 0u)
    {
        var handler = Intent;
        if (handler != null) handler(new WorldLocationUiIntent(action, locationId, itemId));
    }

    private uint CurrentLocationId
    {
        get
        {
            if (_state == null) return 0u;
            if (_state.Selected != null) return _state.Selected.Id;
            return _state.SelectedId != 0u ? _state.SelectedId : _selectedId;
        }
    }

    private void OnMarkerClicked(uint id)
    {
        if (id == 0u) return;
        // 先本地高亮/打开详情，业务侧随后回填 Selected；拖动超过阈值时 Button 不会触发。
        _selectedId = id;
        ApplySelection();
        if (_card != null) _card.gameObject.SetActive(true);
        Emit(WorldLocationUiAction.Select, id);
    }

    // ══════════════════ 标记层（稳定 ID 复用） ══════════════════

    private void SyncMarkers(WorldLocationsDisplayState state)
    {
        if (_markerLayer == null) return;
        _staleIds.Clear();
        foreach (var pair in _markers) _staleIds.Add(pair.Key);

        List<WorldLocationSnapshot> locations = state.Locations;
        if (locations != null)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                WorldLocationSnapshot data = locations[i];
                if (data == null || data.Id == 0u) continue;
                Marker marker;
                if (!_markers.TryGetValue(data.Id, out marker))
                {
                    marker = CreateMarker(data.Id);
                    _markers[data.Id] = marker;
                }
                _staleIds.Remove(data.Id);
                ApplyMarker(marker, data);
            }
        }

        // 失效项：先失活再销毁（Destroy 帧末生效，同帧仍会渲染/吃射线）。
        for (int i = 0; i < _staleIds.Count; i++)
        {
            Marker stale;
            if (!_markers.TryGetValue(_staleIds[i], out stale)) continue;
            _markers.Remove(_staleIds[i]);
            if (stale.Root == null) continue;
            stale.Root.gameObject.SetActive(false);
            Destroy(stale.Root.gameObject);
        }
        _staleIds.Clear();
        ApplyMarkerSizes();
    }

    private Marker CreateMarker(uint id)
    {
        var marker = new Marker { Id = id,Point=new WorldMapPoint {index=id} };
        marker.Root = GameplayUi.Rect(_markerLayer, "Location_" + id, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(NormalSize, NormalSize));
        marker.Background = marker.Root.gameObject.AddComponent<Image>();
        marker.Background.color = MarkerInk;
        marker.Background.raycastTarget = true;
        // 点位上挂 Button：拖拽超过阈值时导航会清掉 eligibleForClick，避免误点。
        var button = marker.Root.gameObject.AddComponent<Button>();
        button.targetGraphic = marker.Background;
        button.transition = Selectable.Transition.None;
        uint captured = id;
        button.onClick.AddListener(() => OnMarkerClicked(captured));

        // 现有点位壳只提供悬停/选中的中心缩放（Bind(null,null) 保持自身尺寸不变）。
        marker.Icon = marker.Root.gameObject.AddComponent<WorldMapPointIcon>();

        RectTransform glyphRect = Stretch(GameplayUi.Rect(marker.Root, "Glyph", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        glyphRect.offsetMin = new Vector2(4f, 4f);
        glyphRect.offsetMax = new Vector2(-4f, -4f);
        marker.Glyph = glyphRect.gameObject.AddComponent<Image>();
        marker.Glyph.raycastTarget = false;
        marker.Glyph.preserveAspect = true;

        RectTransform initialRect = Stretch(GameplayUi.Rect(marker.Root, "Initial", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        marker.Initial = initialRect.gameObject.AddComponent<TextMeshProUGUI>();
        marker.Initial.font = RunTeamHud.ResolveFont();
        marker.Initial.fontSize = 17;
        marker.Initial.alignment = TextAlignmentOptions.Center;
        marker.Initial.color = WarmText;
        marker.Initial.raycastTarget = false;

        RectTransform labelRect = GameplayUi.Rect(marker.Root, "Label", new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(0f, -6f), new Vector2(160f, 22f));
        marker.Label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        marker.Label.font = RunTeamHud.ResolveFont();
        marker.Label.fontSize = 15;
        marker.Label.alignment = TextAlignmentOptions.Center;
        marker.Label.color = Gold;
        marker.Label.raycastTarget = false;
        return marker;
    }

    private void ApplyMarker(Marker marker, WorldLocationSnapshot data)
    {
        if (marker.Root == null) return;
        marker.Data = data;
        marker.Root.gameObject.SetActive(true);
        marker.Root.anchorMin = marker.Root.anchorMax = new Vector2(
            Mathf.Clamp01(data.Position.x), Mathf.Clamp01(data.Position.y));
        // 中心点始终落在归一化地理锚点上。
        marker.Root.pivot = new Vector2(.5f, .5f);
        marker.Root.anchoredPosition = Vector2.zero;
        marker.Root.localRotation = Quaternion.identity;
        marker.Root.localScale = Vector3.one;

        Sprite sprite = ResolveSprite(data);
        bool hasSprite = sprite != null;
        marker.Glyph.gameObject.SetActive(hasSprite);
        marker.Glyph.sprite = sprite;
        marker.Glyph.color = Color.white;
        marker.Initial.gameObject.SetActive(!hasSprite);
        marker.Initial.text = PlaceholderGlyph(data);
        marker.Label.text = data.Name ?? string.Empty;
        marker.Icon.Bind(marker.Point, null);
    }

    /// <summary>反向抵消地图缩放，保持标记与文字的屏幕尺寸稳定。</summary>
    private void ApplyMarkerSizes()
    {
        foreach (var pair in _markers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.Root == null) continue;
            bool capital = marker.Data != null && marker.Data.Kind == WorldLocationKind.Capital;
            float size = capital ? CapitalSize : NormalSize;
            marker.Root.sizeDelta = new Vector2(size, size);
            marker.Point.scale=1f/Mathf.Max(1,_zoom);
            marker.Icon.Bind(marker.Point,null);
        }
    }

    private void ApplySelection()
    {
        foreach (var pair in _markers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.Background == null) continue;
            bool selected = _selectedId != 0u && pair.Key == _selectedId;
            bool capital = marker.Data != null && marker.Data.Kind == WorldLocationKind.Capital;
            marker.Background.color = selected ? MarkerGold : (capital ? MarkerCapital : MarkerInk);
            marker.Initial.color = selected ? Ink : WarmText;
            marker.Icon.SetSelected(selected);
            marker.Label.color = selected ? Gold : WarmText;
            marker.Label.gameObject.SetActive(selected || capital || _zoom >= LabelZoomThreshold);
        }
    }

    // ══════════════════ 筛选 ══════════════════

    private void ApplyFilters()
    {
        foreach (var pair in _markers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.Root == null) continue;
            WorldLocationKind kind = marker.Data != null ? marker.Data.Kind : WorldLocationKind.Task;
            bool visible = IsFilterEnabled(WorldLocationKindFilter.Of(kind));
            marker.Root.gameObject.SetActive(visible);
            if (!visible && _selectedId == marker.Id) _selectedId = 0u;
        }
        RefreshFilterButtons();
        ApplySelection();
    }

    private void ToggleFilter(WorldLocationFilter filter)
    {
        _filters[filter] = !IsFilterEnabled(filter);
        ApplyFilters();
        if(_state?.Selected!=null&&!IsFilterEnabled(WorldLocationKindFilter.Of(_state.Selected.Kind)))CloseDetail();
    }

    private void BuildFilterBar()
    {
        if (_filterBar != null) return;
        _filterBar = GameplayUi.Rect(_overlay, "LocationFilters", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -88f), new Vector2(FilterBarWidth, FilterBarHeight));
        var background = _filterBar.gameObject.AddComponent<Image>();
        background.color = new Color(.10f, .085f, .065f, .94f);
        background.raycastTarget = true;

        WorldLocationFilter[] order =
        {
            WorldLocationFilter.Capital, WorldLocationFilter.Trade, WorldLocationFilter.Resource,
            WorldLocationFilter.Rumor, WorldLocationFilter.Task
        };
        string[] names = { "主城", "贸易", "资源", "见闻", "任务" };
        for (int i = 0; i < order.Length; i++)
        {
            WorldLocationFilter filter = order[i];
            _filters[filter] = true; // 默认全开；可并列开关
            Button button = GameplayUi.Button(_filterBar, "Filter_" + filter, names[i],
                new Vector2(10f + i * 100f, -8f), new Vector2(92f, 36f), () => ToggleFilter(filter));
            button.transition = Selectable.Transition.None;
            _filterImages[filter] = button.targetGraphic as Image;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.fontSize = 20;
            _filterLabels[filter] = label;
        }
        RefreshFilterButtons();
    }

    private void RefreshFilterButtons()
    {
        foreach (var pair in _filterImages)
        {
            bool on = IsFilterEnabled(pair.Key);
            if (pair.Value != null) pair.Value.color = on ? ButtonActive : ButtonNormal;
            TextMeshProUGUI label;
            if (_filterLabels.TryGetValue(pair.Key, out label) && label != null)
                label.color = on ? Ink : MutedText;
        }
    }

    // ══════════════════ 图标 ══════════════════

    /// <summary>优先 Resources.Load&lt;Sprite&gt;("UI/WorldLocations/"+IconKey)；缺失回退类别汉字占位。</summary>
    private Sprite ResolveSprite(WorldLocationSnapshot data)
    {
        string path = string.IsNullOrEmpty(data.IconKey) ? string.Empty : "UI/WorldLocations/" + data.IconKey;
        if (path.Length == 0) return null;
        Sprite cached;
        if (_spriteCache.TryGetValue(path, out cached)) return cached;
        Sprite sprite = Resources.Load<Sprite>(path);
        _spriteCache[path] = sprite;
        return sprite;
    }

    private static string PlaceholderGlyph(WorldLocationSnapshot data)
    {
        if (!string.IsNullOrEmpty(data.IconKey))
        {
            switch (data.IconKey)
            {
                case "capital": return "城";
                case "market_town": return "市";
                case "tribe": return "营";
                case "forest": return "林";
                case "iron_mine": return "矿";
                case "quarry": return "石";
                case "saltworks": return "盐";
                case "cave": return "洞";
                case "ruins": return "迹";
                case "easter_egg": return "见";
                case "rumor": return "闻";
                case "task": return "事";
            }
        }
        switch (data.Kind)
        {
            case WorldLocationKind.Capital: return "城";
            case WorldLocationKind.Trade: return "市";
            case WorldLocationKind.Tribe: return "营";
            case WorldLocationKind.Resource: return "林";
            case WorldLocationKind.Temporary: return "闻";
            default: return "事";
        }
    }

    private static string KindName(WorldLocationKind kind)
    {
        switch (kind)
        {
            case WorldLocationKind.Capital: return "主城";
            case WorldLocationKind.Trade: return "贸易城邑";
            case WorldLocationKind.Tribe: return "部落";
            case WorldLocationKind.Resource: return "资源点";
            case WorldLocationKind.Temporary: return "临时见闻";
            default: return "任务";
        }
    }

    private void BuildDetailCard()
    {
        if (_card != null) return;
        // 右侧 380 宽；顶部距 96、底部距 88，高度随画布伸缩。
        _card = GameplayUi.Rect(_overlay, "LocationDetail", new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        _card.anchorMax=Vector2.one;
        _card.offsetMin = new Vector2(-(CardWidth + 20f), CardBottom);
        _card.offsetMax = new Vector2(-20f, -CardTop);
        var frame = _card.gameObject.AddComponent<Image>();
        frame.color = CardBorder;
        frame.raycastTarget = true;

        RectTransform body = Stretch(GameplayUi.Rect(_card, "Body", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        body.offsetMin = new Vector2(2f, 2f);
        body.offsetMax = new Vector2(-2f, -2f);
        var bodyImage = body.gameObject.AddComponent<Image>();
        bodyImage.color = CardColor;
        bodyImage.raycastTarget = true; // 挡住地图拖动穿透

        Button close = GameplayUi.Button(body, "Close", "×", new Vector2(CardWidth - 62f, -8f), new Vector2(42f, 38f), CloseFromButton);
        TextMeshProUGUI closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        if (closeLabel != null) closeLabel.fontSize = 26;

        _title = MakeText(body, "Title", 27, Gold, TextAlignmentOptions.Left);
        PlaceTop(_title.rectTransform, 10f, 8f, 40f, 62f);

        _meta = MakeText(body, "Meta", 18, MutedText, TextAlignmentOptions.Left);
        PlaceTop(_meta.rectTransform, 10f, 52f, 24f, 16f);

        _relation = MakeText(body, "Relation", 18, WarmText, TextAlignmentOptions.Left);
        PlaceTop(_relation.rectTransform, 10f, 80f, 22f, 16f);

        var descriptionViewport=GameplayUi.Rect(body,"DescriptionViewport",Vector2.up,Vector2.up,Vector2.zero,Vector2.zero);
        PlaceTop(descriptionViewport,10f,110f,112f,16f);
        descriptionViewport.gameObject.AddComponent<Image>().color=Color.clear;
        descriptionViewport.gameObject.AddComponent<RectMask2D>();
        _description = MakeText(descriptionViewport, "Description", 18, WarmText, TextAlignmentOptions.TopLeft);
        _description.enableWordWrapping = true;
        PlaceTop(_description.rectTransform,0,0,112f,0);
        _description.rectTransform.pivot=new Vector2(.5f,1);
        _descriptionScroll=descriptionViewport.gameObject.AddComponent<ScrollRect>();
        _descriptionScroll.viewport=descriptionViewport;_descriptionScroll.content=_description.rectTransform;
        _descriptionScroll.horizontal=false;_descriptionScroll.vertical=true;
        _descriptionScroll.movementType=ScrollRect.MovementType.Clamped;

        _lock = MakeText(body, "LockReason", 18, LockText, TextAlignmentOptions.TopLeft);
        _lock.enableWordWrapping = true;
        PlaceTop(_lock.rectTransform, 10f, 228f, 42f, 16f);

        // 身份区：顶部 264 以下到提示条（底部 58）之上，可随卡片高度伸缩。
        RectTransform content = Stretch(GameplayUi.Rect(body, "IdentityContent", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        content.offsetMin = new Vector2(12f, 58f);
        content.offsetMax = new Vector2(-12f, -278f);

        _officialTabLabel = TabButton(content, "Tab_Official", "官员事务", 0f, false);
        _familyTabLabel = TabButton(content, "Tab_Family", "家族经营", 176f, true);

        _identityName = MakeText(content, "IdentityName", 16, MutedText, TextAlignmentOptions.Left);
        PlaceTop(_identityName.rectTransform, 2f, 48f, 22f, 0f);

        _officialPage = PageRoot(content, "OfficialPage");
        _officeButton = GameplayUi.Button(_officialPage, "OpenOffice", "任务与事件", new Vector2(0f, -6f), new Vector2(200f, 44f),
            () => Emit(WorldLocationUiAction.OpenOffice, CurrentLocationId));
        _officeLabel = _officeButton.GetComponentInChildren<TextMeshProUGUI>();
        _officialHint = MakeText(_officialPage, "OfficialHint", 17, LockText, TextAlignmentOptions.TopLeft);
        _officialHint.enableWordWrapping = true;
        PlaceTop(_officialHint.rectTransform, 0f, 60f, 96f, 0f);

        _familyPage = PageRoot(content, "FamilyPage");
        _buildButton = GameplayUi.Button(_familyPage, "Build", "兴建", new Vector2(0f, -6f), new Vector2(158f, 42f),
            () => Emit(WorldLocationUiAction.Build, CurrentLocationId));
        _buildLabel = _buildButton.GetComponentInChildren<TextMeshProUGUI>();
        _harvestButton = GameplayUi.Button(_familyPage, "Harvest", "领取产出", new Vector2(168f, -6f), new Vector2(170f, 42f),
            () => Emit(WorldLocationUiAction.Harvest, CurrentLocationId));
        _harvestLabel = _harvestButton.GetComponentInChildren<TextMeshProUGUI>();
        _resolveButton=GameplayUi.Button(_familyPage,"Resolve","阅读并领取",new Vector2(0f,-6f),new Vector2(210f,42f),
            ()=>Emit(WorldLocationUiAction.Resolve,CurrentLocationId));
        _familyHint = MakeText(_familyPage, "FamilyHint", 16, MutedText, TextAlignmentOptions.TopLeft);
        _familyHint.enableWordWrapping = true;
        PlaceTop(_familyHint.rectTransform, 0f, 56f, 48f, 0f);

        BuildTradeList(_familyPage);

        _hint = MakeText(body, "Hint", 16, MutedText, TextAlignmentOptions.TopLeft);
        _hint.enableWordWrapping = true;
        _hint.rectTransform.anchorMin = new Vector2(0f, 0f);
        _hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        _hint.rectTransform.pivot = new Vector2(.5f, 0f);
        _hint.rectTransform.offsetMin = new Vector2(14f, 8f);
        _hint.rectTransform.offsetMax = new Vector2(-14f, 54f);

        _card.gameObject.SetActive(false);
    }

    private void CloseFromButton()
    {
        CloseDetail();
    }

    /// <summary>页面容器：铺满身份区，顶部留 76 给页签。</summary>
    private RectTransform PageRoot(RectTransform parent, string name)
    {
        RectTransform page = Stretch(GameplayUi.Rect(parent, name, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        page.offsetMin = Vector2.zero;
        page.offsetMax = new Vector2(0f, -76f);
        return page;
    }

    /// <summary>交易区：标题 + 自滚动列表（ScrollRect + RectMask2D），不与说明文字重叠。</summary>
    private void BuildTradeList(RectTransform parent)
    {
        TextMeshProUGUI header = MakeText(parent, "TradeHeader", 15, MutedText, TextAlignmentOptions.Left);
        _tradeHeader=header;
        PlaceTop(header.rectTransform, 0f, 110f, 20f, 0f);
        header.text = "交易 · 每行可买 1 / 卖 1";

        _tradeViewport = Stretch(GameplayUi.Rect(parent, "TradeViewport", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        _tradeViewport.offsetMin = Vector2.zero;
        _tradeViewport.offsetMax = new Vector2(0f, -136f);
        var viewportImage = _tradeViewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(.075f, .065f, .05f, .92f);
        viewportImage.raycastTarget = true; // 交易区独立接收滚轮，不传给地图
        _tradeViewport.gameObject.AddComponent<RectMask2D>();

        _tradeList = GameplayUi.Rect(_tradeViewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        _tradeList.anchorMax=Vector2.one;
        _tradeList.pivot = new Vector2(.5f, 1f);
        _tradeList.offsetMin = Vector2.zero;
        _tradeList.offsetMax = Vector2.zero;

        _tradeScroll = _tradeViewport.gameObject.AddComponent<ScrollRect>();
        _tradeScroll.content = _tradeList;
        _tradeScroll.viewport = _tradeViewport;
        _tradeScroll.horizontal = false;
        _tradeScroll.vertical = true;
        _tradeScroll.movementType = ScrollRect.MovementType.Clamped;
        _tradeScroll.scrollSensitivity = 18f;

        _tradeEmpty = MakeText(_tradeViewport, "Empty", 16, MutedText, TextAlignmentOptions.Center);
        RectTransform emptyRect = _tradeEmpty.rectTransform;
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = new Vector2(8f, 8f);
        emptyRect.offsetMax = new Vector2(-8f, -8f);
        _tradeEmpty.text = "无可交易商品";
    }

    private TextMeshProUGUI TabButton(RectTransform parent, string name, string text, float x, bool family)
    {
        Button button = GameplayUi.Button(parent, name, text, new Vector2(x, -6f), new Vector2(168f, 36f),
            () => SwitchIdentity(family ? WorldLocationIdentity.Family : WorldLocationIdentity.Official));
        button.transition = Selectable.Transition.None;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.fontSize = 19;
        return label;
    }

    private static TextMeshProUGUI MakeText(RectTransform parent, string name, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rect = GameplayUi.Rect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = RunTeamHud.ResolveFont();
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.raycastTarget = false;
        label.text = string.Empty;
        return label;
    }

    /// <summary>顶部锚定条带：从卡片顶往下 top 像素、高 height；左右 inset 像素。</summary>
    private static void PlaceTop(RectTransform rect, float left, float top, float height, float right)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>铺满父节点（用 GameplayUi.Rect 产出后再统一拉伸）。</summary>
    private static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // ══════════════════ 详情渲染 ══════════════════

    private void SwitchIdentity(WorldLocationIdentity identity)
    {
        if (_activeIdentity == identity) return;
        _activeIdentity = identity;
        ApplyIdentityTabs();
        if (_card != null) _card.gameObject.SetActive(true);
        Emit(identity == WorldLocationIdentity.Official
            ? WorldLocationUiAction.Official
            : WorldLocationUiAction.Family, CurrentLocationId);
    }

    private void ApplyIdentityTabs()
    {
        bool official = _activeIdentity == WorldLocationIdentity.Official;
        if (_officialPage != null) _officialPage.gameObject.SetActive(official);
        if (_familyPage != null) _familyPage.gameObject.SetActive(!official);
        TintTab(_officialTabLabel, official);
        TintTab(_familyTabLabel, !official);
        if (_identityName != null)
            _identityName.text = official ? "以官员身份查阅：任务与事件" : "以家族身份查阅：交易与经营";
    }

    private static void TintTab(TextMeshProUGUI label, bool active)
    {
        if (label == null) return;
        label.color = active ? Ink : WarmText;
        var image = label.transform.parent != null ? label.transform.parent.GetComponent<Image>() : null;
        if (image != null) image.color = active ? ButtonActive : ButtonNormal;
    }

    private void RenderDetail(WorldLocationsDisplayState state)
    {
        WorldLocationSnapshot data = state.Selected;
        if (data == null)
        {
            if (_card != null) _card.gameObject.SetActive(false);
            return;
        }

        if (state.Identity != _activeIdentity)
        {
            _activeIdentity = state.Identity;
            ApplyIdentityTabs();
        }
        _card.gameObject.SetActive(true);

        _title.text = string.IsNullOrEmpty(data.Name) ? "未命名地点" : data.Name;
        _meta.text = KindName(data.Kind) + " · 势力 " + (string.IsNullOrEmpty(data.FactionName) ? "无主" : data.FactionName)
            + " · 时代 " + state.Era;
        _relation.text = "关系：" + (string.IsNullOrEmpty(data.RelationName) ? "未建立" : data.RelationName);
        _description.text = string.IsNullOrEmpty(data.Description) ? "尚无可考记述。" : data.Description;
        float descriptionHeight=_description.GetPreferredValues(_description.text,Mathf.Max(100,_description.rectTransform.rect.width),0).y+10;
        _description.rectTransform.sizeDelta=new Vector2(_description.rectTransform.sizeDelta.x,Mathf.Max(112,descriptionHeight));
        if(_descriptionLocation!=data.Id){_descriptionLocation=data.Id;_descriptionScroll.verticalNormalizedPosition=1;}
        _lock.text = string.IsNullOrEmpty(data.LockReason) ? string.Empty : "限制：" + data.LockReason;

        RenderOfficial(data);
        RenderFamily(data, state.Offers);
    }

    private void RenderOfficial(WorldLocationSnapshot data)
    {
        bool canOpen = data.CanOpenOffice;
        // 官员页只保留一个真实入口；不可用时按钮隐藏并解释限制。
        _officeButton.gameObject.SetActive(canOpen);
        _officialHint.text = canOpen
            ? "此处设有官署，可查阅辖区任务与事件。"
            : LockTextOf(data, "此处不设官署，官员事务不可用。");
    }

    private void RenderFamily(WorldLocationSnapshot data, List<WorldLocationTradeRow> offers)
    {
        // 不存在的业务不建按钮：无交易时整段列表隐藏。
        if (_tradeScroll != null) _tradeScroll.gameObject.SetActive(data.CanTrade);
        if (_tradeHeader != null)_tradeHeader.gameObject.SetActive(data.CanTrade);
        if (data.CanTrade) RenderTradeRows(offers);

        _buildButton.gameObject.SetActive(data.CanBuild);
        _resolveButton.gameObject.SetActive(data.CanResolve);
        if (data.CanBuild) _buildLabel.text = data.Built ? "扩建" : "兴建";

        _harvestButton.gameObject.SetActive(data.CanHarvest);
        if (data.CanHarvest)
        {
            _harvestLabel.text = data.PendingOutput > 0u
                ? ("领取产出 ×" + data.PendingOutput)
                : "暂无可领";
        }

        var parts = new List<string>(4);
        if (data.CanResolve) parts.Add("阅读上方记述后，可确认领取本次收获。");
        if (data.CanBuild && !data.Built && !string.IsNullOrEmpty(data.LockReason)) parts.Add(data.LockReason);
        if (data.ProductionSeconds > 0d)
            parts.Add("当前周期已工作 " + Mathf.FloorToInt((float)data.ProductionSeconds) + " 秒，暂存满后停产。");
        if (data.CanTrade) parts.Add("本地买价为原价 " + data.BuyPercent + "%，卖价按原价。");
        if (parts.Count == 0 && !data.CanTrade && !data.CanBuild && !data.CanHarvest)
            parts.Add(LockTextOf(data, "此地暂无家族可经营的业务。"));
        _familyHint.text = string.Join("　", parts.ToArray());
    }

    private static string LockTextOf(WorldLocationSnapshot data, string fallback)
    {
        return string.IsNullOrEmpty(data.LockReason) ? fallback : data.LockReason;
    }

    /// <summary>交易行按需复用：行数收敛不重建，买/卖各一按钮，每行只买卖 1。</summary>
    private void RenderTradeRows(List<WorldLocationTradeRow> offers)
    {
        int want = offers != null ? offers.Count : 0;
        while (_tradeRows.Count < want) _tradeRows.Add(CreateTradeRow(_tradeRows.Count));
        for (int i = 0; i < _tradeRows.Count; i++)
            _tradeRows[i].Root.gameObject.SetActive(i < want);
        _tradeEmpty.gameObject.SetActive(want == 0);

        if (want > 0)
        {
            uint locationId = CurrentLocationId;
            for (int i = 0; i < want; i++)
            {
                WorldLocationTradeRow row = offers[i];
                TradeRowView view = _tradeRows[i];
                view.Name.text = string.IsNullOrEmpty(row.Name) ? ("物资 #" + row.ItemId) : row.Name;
                view.Price.text = "买 " + row.BuyPrice + " / 卖 " + row.SellPrice + " · 持有 " + row.Owned;
                view.Buy.interactable = row.CanBuy;
                view.Sell.interactable = row.CanSell;
                uint itemId = row.ItemId;
                BindRowButton(view.Buy, () => Emit(WorldLocationUiAction.Buy, locationId, itemId));
                BindRowButton(view.Sell, () => Emit(WorldLocationUiAction.Sell, locationId, itemId));
            }
        }
        _tradeList.sizeDelta = new Vector2(0f, want * TradeRowStride + 8f);
    }

    private TradeRowView CreateTradeRow(int index)
    {
        var view = new TradeRowView();
        RectTransform root = GameplayUi.Rect(_tradeList, "Row_" + index, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -index * TradeRowStride), new Vector2(0f, 54f));
        root.anchorMax=Vector2.one;
        root.offsetMin = new Vector2(0f, root.offsetMin.y);
        root.offsetMax = new Vector2(0f, root.offsetMax.y);
        var image = root.gameObject.AddComponent<Image>();
        image.color = new Color(.145f, .12f, .09f, .95f);
        view.Root = root;

        view.Name = MakeText(root, "Name", 16, WarmText, TextAlignmentOptions.Left);
        PlaceTop(view.Name.rectTransform, 10f, 3f, 22f, 118f);

        view.Price = MakeText(root, "Price", 14, MutedText, TextAlignmentOptions.Left);
        PlaceTop(view.Price.rectTransform, 10f, 27f, 20f, 118f);

        view.Buy = GameplayUi.Button(root, "Buy", "买1", new Vector2(196f, -6f), new Vector2(50f, 40f), null);
        view.Sell = GameplayUi.Button(root, "Sell", "卖1", new Vector2(252f, -6f), new Vector2(50f, 40f), null);
        view.BuyLabel = view.Buy.GetComponentInChildren<TextMeshProUGUI>();
        view.SellLabel = view.Sell.GetComponentInChildren<TextMeshProUGUI>();
        if (view.BuyLabel != null) view.BuyLabel.fontSize = 16;
        if (view.SellLabel != null) view.SellLabel.fontSize = 16;
        StyleSmallButton(view.Buy);
        StyleSmallButton(view.Sell);
        return view;
    }

    private static void StyleSmallButton(Button button)
    {
        var target = button.targetGraphic as Image;
        if (target != null) target.color = ButtonActive;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.05f, .9f, 1f);
        colors.disabledColor = new Color(.5f, .5f, .5f, .45f);
        button.colors = colors;
    }

    /// <summary>行复用后监听必须重绑：先清空再挂当前 itemId 的闭包。</summary>
    private static void BindRowButton(Button button, UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
