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
    private static readonly Color CardColor = new Color(.105f, .126f, .127f);
    private static readonly Color CardBorder = new Color(.36f, .32f, .24f);
    private static readonly Color Gold = new Color(.96f, .82f, .48f, 1f);
    private static readonly Color WarmText = new Color(.94f, .90f, .80f, 1f);
    private static readonly Color MutedText = new Color(.72f, .66f, .55f, 1f);
    private static readonly Color LockText = new Color(.96f, .64f, .42f, 1f);
    private static readonly Color ButtonNormal = new Color(.18f, .21f, .21f);
    private static readonly Color ButtonActive = new Color(.77f, .62f, .38f);
    private static readonly Color MarkerGold = new Color(.99f, .76f, .40f);
    private static readonly Color HomeSeal = new Color(.81f, .44f, .32f);
    private static readonly Color[] CategoryColors = {
        new Color(.95f, .79f, .51f), new Color(.93f, .88f, .74f), new Color(.72f, .81f, .64f),
        new Color(.76f, .75f, .83f), new Color(.94f, .64f, .45f)
    };
    private static readonly string[] CategoryNames = { "主城", "贸易", "资源", "见闻", "任务" };

    private const float CapitalSize = 32f;
    private const float NormalSize = 25f;
    public const float SidebarWidth = 380f;
    private const float CardTop = 112f;
    private const float CardBottom = 99f;
    private const float FilterBarWidth = 512f;
    private const float FilterBarHeight = 40f;
    private const float TradeRowStride = 58f;
    /// <summary>标签在 zoom≥1.5 时显示；选中的标记与主城名始终显示。</summary>
    private const float LabelZoomThreshold = 1.65f;

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
    private RectTransform _card, _overview;
    private TextMeshProUGUI _overviewCount;
    private Sprite _badgeSprite;
    private Material _mapLabelMaterial;

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
    private readonly List<Marker> _labelCandidates = new List<Marker>();
    private readonly List<Rect> _labelBounds = new List<Rect>();
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
        if (_badgeSprite != null)
        {
            Destroy(_badgeSprite.texture);
            Destroy(_badgeSprite);
        }
        if (_mapLabelMaterial != null) Destroy(_mapLabelMaterial);
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
        BuildOverview();
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
        _overview.gameObject.SetActive(state.Selected == null);
        _overviewCount.text = $"{state.Locations?.Count ?? 0:00}  处可查阅";
        if (_hint != null) _hint.text = state.Selected?.Kind == WorldLocationKind.PlayerVillage
            ? "滚轮缩放，拖动地图平移。" : state.Hint ?? string.Empty;
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

    internal void RefreshLayout() => ApplySelection();

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
        marker.Background.sprite = BadgeSprite();
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
        glyphRect.offsetMin = new Vector2(5f, 5f);
        glyphRect.offsetMax = new Vector2(-5f, -5f);
        marker.Glyph = glyphRect.gameObject.AddComponent<Image>();
        marker.Glyph.raycastTarget = false;
        marker.Glyph.preserveAspect = true;

        RectTransform initialRect = Stretch(GameplayUi.Rect(marker.Root, "Initial", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        marker.Initial = initialRect.gameObject.AddComponent<TextMeshProUGUI>();
        marker.Initial.font = RunTeamHud.ResolveFont();
        marker.Initial.fontSize = 16;
        marker.Initial.fontStyle = FontStyles.Bold;
        marker.Initial.alignment = TextAlignmentOptions.Center;
        marker.Initial.color = WarmText;
        marker.Initial.raycastTarget = false;

        RectTransform labelRect = GameplayUi.Rect(marker.Root, "Label", new Vector2(.5f, 0f), new Vector2(.5f, 1f),
            new Vector2(0f, -2f), new Vector2(150f, 20f));
        marker.Label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        marker.Label.font = RunTeamHud.ResolveFont();
        marker.Label.fontSize = 16;
        marker.Label.fontStyle = FontStyles.Bold;
        marker.Label.alignment = TextAlignmentOptions.Center;
        marker.Label.color = Ink;
        if (_mapLabelMaterial == null && marker.Label.font != null)
        {
            _mapLabelMaterial = new Material(marker.Label.font.material) { name = "WorldLocationLabel_PaperHalo" };
            _mapLabelMaterial.SetFloat("_OutlineWidth", .20f);
            _mapLabelMaterial.SetColor("_OutlineColor", new Color(.98f, .92f, .77f, .94f));
            _mapLabelMaterial.EnableKeyword("OUTLINE_ON");
        }
        if (_mapLabelMaterial != null) marker.Label.fontSharedMaterial = _mapLabelMaterial;
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
            bool prominent = marker.Data?.Kind == WorldLocationKind.Capital || marker.Data?.Kind == WorldLocationKind.PlayerVillage;
            float size = prominent ? CapitalSize : NormalSize;
            marker.Root.sizeDelta = new Vector2(size, size);
            marker.Point.scale=1f/Mathf.Max(1,_zoom);
            marker.Icon.Bind(marker.Point,null);
        }
    }

    private void ApplySelection()
    {
        _labelCandidates.Clear();
        _labelBounds.Clear();
        foreach (var pair in _markers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.Background == null) continue;
            bool selected = _selectedId != 0u && pair.Key == _selectedId;
            var kind = marker.Data.Kind;
            bool home = kind == WorldLocationKind.PlayerVillage;
            marker.Background.color = selected ? MarkerGold : MarkerTone(kind);
            marker.Initial.color = home && !selected ? WarmText : Ink;
            marker.Icon.SetSelected(selected);
            marker.Label.gameObject.SetActive(false);
            if (marker.Root.gameObject.activeSelf && (selected || home || kind == WorldLocationKind.Capital || _zoom >= LabelZoomThreshold))
                _labelCandidates.Add(marker);
        }
        _labelCandidates.Sort(CompareLabelPriority);
        Vector2 mapSize = _mapContent.sizeDelta * _zoom;
        foreach (var marker in _labelCandidates)
        {
            bool pinned = marker.Id == _selectedId || marker.Data.Kind == WorldLocationKind.PlayerVillage;
            Vector2 point = Vector2.Scale(marker.Data.Position, mapSize);
            float width = Mathf.Max(28f, marker.Label.text.Length * 16f);
            var bounds = new Rect(point.x - width * .5f, point.y - 38f, width, 20f);
            if (!pinned && _labelBounds.Exists(other => other.Overlaps(bounds))) continue;
            _labelBounds.Add(bounds);
            marker.Label.gameObject.SetActive(true);
        }
        // 先让主城与里标浮到同区普通兴趣点上方，当前选中点最后绘制。
        for (int i = _labelCandidates.Count - 1; i >= 0; i--)
        {
            var marker = _labelCandidates[i];
            if (LabelPriority(marker) <= 2) marker.Root.SetAsLastSibling();
        }
    }

    private int CompareLabelPriority(Marker first, Marker second)
    {
        int firstPriority = LabelPriority(first);
        int secondPriority = LabelPriority(second);
        return firstPriority != secondPriority ? firstPriority.CompareTo(secondPriority) : first.Id.CompareTo(second.Id);
    }

    private int LabelPriority(Marker marker)
    {
        if (marker.Id == _selectedId) return 0;
        if (marker.Data.Kind == WorldLocationKind.PlayerVillage) return 1;
        return marker.Data.Kind == WorldLocationKind.Capital ? 2 : 3;
    }

    private static Color MarkerTone(WorldLocationKind kind) => kind == WorldLocationKind.PlayerVillage
        ? HomeSeal : CategoryColors[(int)WorldLocationKindFilter.Of(kind)];

    private Sprite BadgeSprite()
    {
        if (_badgeSprite != null) return _badgeSprite;
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
            name = "WorldLocationBadge", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var offset = new Vector2(x + .5f - size * .5f, y + .5f - size * .5f);
            float edge = size * .5f - offset.magnitude;
            byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(61f, 255f, Mathf.Clamp01(edge - 1.8f)));
            pixels[y * size + x] = new Color32(shade, shade, shade, (byte)Mathf.RoundToInt(Mathf.Clamp01(edge) * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _badgeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect);
        return _badgeSprite;
    }
    // ══════════════════ 筛选 ══════════════════

    private void ApplyFilters()
    {
        foreach (var pair in _markers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.Root == null) continue;
            WorldLocationKind kind = marker.Data != null ? marker.Data.Kind : WorldLocationKind.Task;
            bool visible = IsLocationVisible(kind);
            marker.Root.gameObject.SetActive(visible);
            if (!visible && _selectedId == marker.Id) _selectedId = 0u;
        }
        RefreshFilterButtons();
        ApplySelection();
    }

    // 辖乡聚落是固定参照点，其他地点筛选不隐藏它。
    private bool IsLocationVisible(WorldLocationKind kind) =>
        kind == WorldLocationKind.PlayerVillage || IsFilterEnabled(WorldLocationKindFilter.Of(kind));

    private void ToggleFilter(WorldLocationFilter filter)
    {
        _filters[filter] = !IsFilterEnabled(filter);
        ApplyFilters();
        if (_state?.Selected != null && !IsLocationVisible(_state.Selected.Kind)) CloseDetail();
    }

    private void BuildFilterBar()
    {
        if (_filterBar != null) return;
        _filterBar = GameplayUi.Rect(_overlay, "LocationFilters", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -108f), new Vector2(FilterBarWidth, FilterBarHeight));

        WorldLocationFilter[] order =
        {
            WorldLocationFilter.Capital, WorldLocationFilter.Trade, WorldLocationFilter.Resource,
            WorldLocationFilter.Rumor, WorldLocationFilter.Task
        };
        for (int i = 0; i < order.Length; i++)
        {
            WorldLocationFilter filter = order[i];
            _filters[filter] = true; // 默认全开；可并列开关
            Button button = GameplayUi.Button(_filterBar, "Filter_" + filter, CategoryNames[i],
                new Vector2(i * 99f, 0f), new Vector2(91f, 36f), () => ToggleFilter(filter));
            button.transition = Selectable.Transition.None;
            _filterImages[filter] = button.targetGraphic as Image;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.fontSize = 18;
            _filterLabels[filter] = label;
        }
        RefreshFilterButtons();
    }

    private void RefreshFilterButtons()
    {
        foreach (var pair in _filterImages)
        {
            bool on = IsFilterEnabled(pair.Key);
            if (pair.Value != null) pair.Value.color = on ? new Color(.22f, .25f, .24f) : CardColor;
            TextMeshProUGUI label;
            if (_filterLabels.TryGetValue(pair.Key, out label) && label != null)
                label.color = on ? CategoryColors[(int)pair.Key] : MutedText;
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
            case WorldLocationKind.PlayerVillage: return "里";
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
            case WorldLocationKind.PlayerVillage: return "玩家村庄";
            case WorldLocationKind.Capital: return "主城";
            case WorldLocationKind.Trade: return "贸易城邑";
            case WorldLocationKind.Tribe: return "部落";
            case WorldLocationKind.Resource: return "资源点";
            case WorldLocationKind.Temporary: return "临时见闻";
            default: return "任务";
        }
    }

    private RectTransform _identityContent;

    private RectTransform BuildSidebar(string name, out RectTransform panel)
    {
        panel = GameplayUi.Rect(_overlay, name, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(-(SidebarWidth + 28f), CardBottom);
        panel.offsetMax = new Vector2(-28f, -CardTop);
        var frame = panel.gameObject.AddComponent<Image>();
        frame.color = CardBorder;
        frame.raycastTarget = true;

        RectTransform body = Stretch(GameplayUi.Rect(panel, "Body", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
        body.offsetMin = new Vector2(1f, 1f);
        body.offsetMax = new Vector2(-1f, -1f);
        var bodyImage = body.gameObject.AddComponent<Image>();
        bodyImage.color = CardColor;
        bodyImage.raycastTarget = true; // 挡住地图拖动穿透
        return body;
    }

    private void BuildOverview()
    {
        RectTransform body = BuildSidebar("LocationOverview", out _overview);
        SidebarText(body, "Section", "地点总览", 17, Gold, 22f, 25f);
        SidebarText(body, "OverviewTitle", "诸地一览", 33, WarmText, 55f, 44f).fontStyle = FontStyles.Bold;
        _overviewCount = SidebarText(body, "LocationCount", "", 21, Gold, 106f, 32f);
        SidebarText(body, "Instructions", "点击地图上的地点，查看当地势力、关系和可办理的事务。", 19,
            MutedText, 152f, 68f).enableWordWrapping = true;
        SidebarRule(body, 232f);
        SidebarText(body, "LegendTitle", "地图图例", 18, Gold, 252f, 28f);

        string[] iconKeys = { "capital", "market_town", "forest", "rumor", "task" };
        string[] legend = { "主城     势力锚点", "贸易     城邑与部落", "资源     可经营地点", "见闻     临时发现", "任务     已接取的事务" };
        for (int i = 0; i < legend.Length; i++)
        {
            float top = 294f + i * 46f;
            var badge = GameplayUi.Rect(body, "LegendMark_" + i, Vector2.up, Vector2.up,
                new Vector2(22f, -top), new Vector2(27f, 27f));
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = BadgeSprite();
            badgeImage.color = CategoryColors[i];
            badgeImage.raycastTarget = false;
            var glyph = GameplayUi.Rect(badge, "Glyph", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(17f, 17f))
                .gameObject.AddComponent<Image>();
            glyph.sprite = Resources.Load<Sprite>("UI/WorldLocations/" + iconKeys[i]);
            glyph.enabled = glyph.sprite != null;
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;
            var label = SidebarText(body, "LegendText_" + i, legend[i], 18, WarmText, top - 2f, 30f);
            PlaceTop(label.rectTransform, 63f, top - 2f, 30f, 18f);
        }
        SidebarRule(body, 530f);
        SidebarText(body, "HomeNote", "朱色「里」标出你治理的榆川里。", 17, MutedText, 549f, 50f).enableWordWrapping = true;
        var identity = MakeText(body, "IdentityNote", 17, MutedText, TextAlignmentOptions.BottomLeft);
        identity.text = "官员处理任务与事件\n族长负责贸易与经营";
        identity.rectTransform.anchorMin = Vector2.zero;
        identity.rectTransform.anchorMax = Vector2.right;
        identity.rectTransform.offsetMin = new Vector2(22f, 24f);
        identity.rectTransform.offsetMax = new Vector2(-22f, 74f);
    }

    private static TextMeshProUGUI SidebarText(RectTransform body, string name, string value, float size, Color color, float top, float height)
    {
        var label = MakeText(body, name, size, color, TextAlignmentOptions.TopLeft);
        label.text = value;
        PlaceTop(label.rectTransform, 22f, top, height, 20f);
        return label;
    }

    private static void SidebarRule(RectTransform body, float top)
    {
        var rect = GameplayUi.Rect(body, "Rule", Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
        PlaceTop(rect, 22f, top, 1f, 22f);
        var line = rect.gameObject.AddComponent<Image>();
        line.color = CardBorder;
        line.raycastTarget = false;
    }

    private void BuildDetailCard()
    {
        if (_card != null) return;
        var body = BuildSidebar("LocationDetail", out _card);

        Button close = GameplayUi.Button(body, "Close", "×", new Vector2(SidebarWidth - 62f, -8f), new Vector2(42f, 38f), CloseFromButton);
        close.targetGraphic.color = ButtonNormal;
        TextMeshProUGUI closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        if (closeLabel != null) closeLabel.fontSize = 26;

        _title = MakeText(body, "Title", 30, Gold, TextAlignmentOptions.Left);
        _title.fontStyle = FontStyles.Bold;
        PlaceTop(_title.rectTransform, 16f, 8f, 40f, 62f);

        _meta = MakeText(body, "Meta", 18, MutedText, TextAlignmentOptions.Left);
        PlaceTop(_meta.rectTransform, 10f, 52f, 24f, 16f);

        _relation = MakeText(body, "Relation", 18, WarmText, TextAlignmentOptions.Left);
        PlaceTop(_relation.rectTransform, 10f, 80f, 22f, 16f);
        SidebarRule(body, 104f);

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
        RectTransform content = _identityContent = Stretch(GameplayUi.Rect(body, "IdentityContent", Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero));
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
        foreach (var button in new[] { _officeButton, _buildButton, _harvestButton, _resolveButton })
        {
            button.targetGraphic.color = ButtonActive;
            button.GetComponentInChildren<TextMeshProUGUI>().color = Ink;
        }
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

        bool home = data.Kind == WorldLocationKind.PlayerVillage;
        _identityContent.gameObject.SetActive(!home);
        _title.text = string.IsNullOrEmpty(data.Name) ? "未命名地点" : data.Name;
        _meta.text = KindName(data.Kind) + (home ? " · 所在疆域 " : " · 势力 ") + (string.IsNullOrEmpty(data.FactionName) ? "无主" : data.FactionName)
            + " · 时代 " + state.Era;
        _relation.text = home ? data.RelationName : "关系：" + (string.IsNullOrEmpty(data.RelationName) ? "未建立" : data.RelationName);
        _description.text = string.IsNullOrEmpty(data.Description) ? "尚无可考记述。" : data.Description;
        float descriptionHeight=_description.GetPreferredValues(_description.text,Mathf.Max(100,_description.rectTransform.rect.width),0).y+10;
        _description.rectTransform.sizeDelta=new Vector2(_description.rectTransform.sizeDelta.x,Mathf.Max(112,descriptionHeight));
        if(_descriptionLocation!=data.Id){_descriptionLocation=data.Id;_descriptionScroll.verticalNormalizedPosition=1;}
        _lock.color = home ? MutedText : LockText;
        _lock.text = home ? data.ConditionText : string.IsNullOrEmpty(data.LockReason) ? string.Empty : "限制：" + data.LockReason;

        if (home) return;
        RenderOfficial(data);
        RenderFamily(data, state.Offers);
    }

    private void RenderOfficial(WorldLocationSnapshot data)
    {
        bool canOpen = data.CanOpenOffice;
        // 官员页只保留一个真实入口；不可用时按钮隐藏并解释限制。
        _officeButton.gameObject.SetActive(canOpen);
        _officeLabel.text = data.OfficialActionLabel ?? "任务与事件";
        _officialHint.text = canOpen
            ? data.OfficialActionLabel == null ? "此处设有官署，可查阅辖区任务与事件。" : "供给已备齐，可在此交付。"
            : string.IsNullOrEmpty(data.LockReason) ? "此处不设官署，官员事务不可用。" : "";
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
        if (data.ProductionSeconds > 0d)
            parts.Add("当前周期已工作 " + Mathf.FloorToInt((float)data.ProductionSeconds) + " 秒，暂存满后停产。");
        if (data.CanTrade) parts.Add("本地买价为原价 " + data.BuyPercent + "%，卖价按原价。");
        if (parts.Count == 0 && !data.CanTrade && !data.CanBuild && !data.CanHarvest && string.IsNullOrEmpty(data.LockReason))
            parts.Add("此地暂无家族可经营的业务。");
        _familyHint.text = string.Join("　", parts.ToArray());
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
