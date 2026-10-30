using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using RunMap;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Map presentation and viewport gestures only. World discovery belongs to the model/service.</summary>
public sealed class RunMapView : MonoBehaviour
{
    public event Action RefreshRequested;
    private RectTransform _viewport, _terrainRect;
    private RawImage _terrain;
    private TextMeshProUGUI _status;
    private GameObject _legend;
    private readonly List<RunMapMarkerView> _pool = new List<RunMapMarkerView>();
    private readonly List<RunMapMarkerState> _markers = new List<RunMapMarkerState>();
    private Vector2 _worldSize, _focus, _pan;
    private float _zoom = 1, _next;
    private Texture _mapIdentity;
    public float Zoom => _zoom;
    public bool LegendVisible => _legend != null && _legend.activeSelf;
    public RectTransform Viewport => _viewport;
    public Vector2 Pan => _pan;

    public void Initialize()
    {
        _status = Label(transform, "MapStatus", "地图正在准备", new Vector2(24, 0), new Vector2(390, 36), 18);
        GameplayUi.Button(transform, "ZoomOut", "−", new Vector2(430, 0), new Vector2(50, 34), () => ZoomAt(_zoom / 1.35f, Vector2.zero));
        GameplayUi.Button(transform, "ZoomIn", "+", new Vector2(488, 0), new Vector2(50, 34), () => ZoomAt(_zoom * 1.35f, Vector2.zero));
        GameplayUi.Button(transform, "FitMap", "全图", new Vector2(550, 0), new Vector2(110, 34), Fit);
        GameplayUi.Button(transform, "CenterTeam", "定位队伍", new Vector2(672, 0), new Vector2(150, 34), Center);
        GameplayUi.Button(transform, "MapLegend", "图例", new Vector2(850, 0), new Vector2(200, 34), () => _legend.SetActive(true));
        _viewport = GameplayUi.Rect(transform, "Viewport", Vector2.up, Vector2.up, new Vector2(24, -44), new Vector2(1052, 410));
        _viewport.gameObject.AddComponent<Image>().color = new Color(.025f, .035f, .05f, 1);
        _viewport.gameObject.AddComponent<RectMask2D>();
        var gestures = _viewport.gameObject.AddComponent<RunMapGestures>(); gestures.Owner = this;
        _terrainRect = GameplayUi.Rect(_viewport, "Terrain", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one);
        _terrain = _terrainRect.gameObject.AddComponent<RawImage>(); _terrain.raycastTarget = false;
        BuildLegend();
    }

    private void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + RunMapExplorationService.RefreshSeconds;
        RefreshRequested?.Invoke();
    }
    private void OnEnable() { _next = 0; }
    private void OnDisable() { CloseSecondary(); }
    private void OnDestroy() { if (_legend != null) Destroy(_legend); }
    public bool CloseSecondary()
    { if (!LegendVisible) return false; _legend.SetActive(false); return true; }

    public void Render(RunMapModel model)
    {
        _status.text = model.Status;
        if (model.Plan == null) { _terrain.texture = null; foreach (var icon in _pool) icon.gameObject.SetActive(false); return; }
        bool changed = _mapIdentity != model.Terrain;
        _mapIdentity = model.Terrain; _terrain.texture = model.Terrain;
        _worldSize = new Vector2(model.Plan.Width, model.Plan.Height); _focus = model.Focus;
        _markers.Clear(); _markers.AddRange(model.Markers);
        if (changed) { CloseSecondary(); _zoom = 1; _pan = Vector2.zero; }
        LayoutMap();
    }

    private Vector2 MapSize => _worldSize.x <= 0 ? Vector2.one : _worldSize *
        (Mathf.Min(_viewport.rect.width / _worldSize.x, _viewport.rect.height / _worldSize.y) * _zoom);
    public Vector2 WorldToViewport(Vector2 world)
    { var size = MapSize; return new Vector2((world.x / _worldSize.x - .5f) * size.x, (world.y / _worldSize.y - .5f) * size.y) + _pan; }
    public void ZoomAt(float zoom, Vector2 point)
    {
        if (LegendVisible || _worldSize.x <= 0) return;
        float next = Mathf.Clamp(zoom, 1, 6), ratio = next / _zoom;
        _pan = point - (point - _pan) * ratio; _zoom = next; LayoutMap();
    }
    public void DragBy(Vector2 delta)
    { if (LegendVisible || _worldSize.x <= 0) return; _pan += delta; LayoutMap(); }
    public void Fit() { if (LegendVisible) return; _zoom = 1; _pan = Vector2.zero; LayoutMap(); }
    public void Center()
    {
        if (LegendVisible || _worldSize.x <= 0) return;
        var size = MapSize;
        _pan = new Vector2((.5f - _focus.x / _worldSize.x) * size.x, (.5f - _focus.y / _worldSize.y) * size.y); LayoutMap();
    }
    private void LayoutMap()
    {
        if (_worldSize.x <= 0) return;
        var size = MapSize;
        var limit = Vector2.Max(Vector2.zero, (size - _viewport.rect.size) * .5f);
        _pan = new Vector2(Mathf.Clamp(_pan.x, -limit.x, limit.x), Mathf.Clamp(_pan.y, -limit.y, limit.y));
        _terrainRect.sizeDelta = size; _terrainRect.anchoredPosition = _pan;
        for (int i = 0; i < _markers.Count; i++)
        {
            if (i >= _pool.Count)
            {
                var node = GameplayUi.Rect(_viewport, "Marker_" + i, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 12);
                var icon = node.gameObject.AddComponent<RunMapMarkerView>(); icon.Initialize(); _pool.Add(icon);
            }
            var marker = _markers[i]; var point = WorldToViewport(marker.Position);
            // Explicit culling also hides the portrait when the actor itself is outside the viewport.
            bool inside = new Rect(-_viewport.rect.size * .5f, _viewport.rect.size).Contains(point);
            _pool[i].gameObject.SetActive(inside);
            _pool[i].Render(marker, point);
        }
        for (int i = _markers.Count; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        // Keep the yellow dot readable when squad members stand at the same position.
        for (int i = 0; i < _markers.Count; i++)
            if (_markers[i].Kind == RunMapMarkerKind.Player) _pool[i].transform.SetAsLastSibling();
    }

    private void BuildLegend()
    {
        var overlay = GameplayUi.Rect(transform.parent, "MapLegendModal", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(1100, 700));
        _legend = overlay.gameObject; overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .88f);
        var box = GameplayUi.Rect(overlay, "LegendBox", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(630, 580));
        box.gameObject.AddComponent<Image>().color = new Color(.08f, .12f, .17f);
        Label(box, "Title", "地图图例", new Vector2(40, -18), new Vector2(500, 44), 28);
        string[] names = { "受控角色 · 黄色光点与菱形头像", "派遣队友 · 蓝色光点（倒地变暗）", "敌人 · 红色光点（仅当前视野）", "可交互人物 · 菱形头像", "宝箱等交互物 · 菱形头像" };
        for (int i = 0; i < names.Length; i++)
        {
            var area = GameplayUi.Rect(box, "LegendMarker" + i, Vector2.up, Vector2.up, new Vector2(56, -100 - i * 57), Vector2.one * 12);
            var icon = area.gameObject.AddComponent<RunMapMarkerView>(); icon.Initialize();
            icon.Render(new RunMapMarkerState { Id = "legend", Kind = (RunMapMarkerKind)i }, Vector2.zero);
            Label(box, "LegendText" + i, names[i], new Vector2(100, -77 - i * 57), new Vector2(500, 44), 20);
        }
        Label(box, "TerrainLegend", "绿：地表　灰：地下　棕：建筑\n黑：未探索　暗：已探索　亮：当前视野\n滚轮 / + − 缩放 · 拖动平移 · M / Esc 先关闭图例", new Vector2(28, -380), new Vector2(574, 105), 19);
        GameplayUi.Button(box, "CloseMapLegend", "关闭图例", new Vector2(180, -510), new Vector2(270, 44), () => CloseSecondary());
        _legend.SetActive(false);
    }
    private static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = GameplayUi.Rect(parent, name + "Area", Vector2.up, Vector2.up, position, size);
        var label = GameplayUi.Text(rect, name, text, fontSize); label.alignment = TextAlignmentOptions.MidlineLeft; return label;
    }
}

public sealed class RunMapGestures : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    public RunMapView Owner;
    private Vector2 _previous;
    private bool Local(PointerEventData data, out Vector2 point)
    {
        var canvas = GetComponentInParent<Canvas>();
        var camera = data.pressEventCamera ?? data.enterEventCamera;
        if (camera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) camera = canvas.worldCamera;
        var rect = (RectTransform)transform;
        bool valid = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, camera, out point);
        point -= rect.rect.center;
        return valid;
    }
    public void OnScroll(PointerEventData data)
    { if (Owner != null && Local(data, out var point)) Owner.ZoomAt(Owner.Zoom * Mathf.Pow(1.2f, data.scrollDelta.y), point); }
    public void OnBeginDrag(PointerEventData data) { Local(data, out _previous); }
    public void OnDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || Owner == null || !Local(data, out var point)) return;
        Owner.DragBy(point - _previous); _previous = point;
    }
}
