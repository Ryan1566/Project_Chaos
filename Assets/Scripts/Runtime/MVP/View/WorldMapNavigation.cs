using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 世界地图同轴缩放与拖动：底图、疆域、标记都在同一个 content 里变换，
/// 裁切交给 viewport 上的 RectMask2D。只用 PointerEventData，不读旧 Input 轴。
/// </summary>
public sealed class WorldMapNavigation : MonoBehaviour,
    IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public const float MinZoom = 1f;
    public const float MaxZoom = 6f;
    public const float DragSensitivity = .65f;
    /// <summary>按下到拖动的位移超过该值即视为拖动，点击随之失效。</summary>
    public const float DragThreshold = 8f;

    private RectTransform _viewport;
    private RectTransform _content;
    private float _zoom = MinZoom;
    private Vector2 _lastViewportSize;
    private bool _dragging;
    private Vector2 _dragStartPointer;
    private bool _dragMoved;

    /// <summary>每次缩放、拖动或复位后报告当前缩放。</summary>
    public event Action<float> ZoomChanged;

    public float Zoom { get { return _zoom; } }

    public void Initialize(RectTransform viewport, RectTransform content)
    {
        _viewport = viewport;
        _content = content;
        if (_content != null) _content.localScale = new Vector3(_zoom, _zoom, 1f);
        _lastViewportSize = _viewport != null ? _viewport.rect.size : Vector2.zero;
        ClampPosition();
        Notify();
    }

    /// <summary>复位到 1 倍并居中。</summary>
    public void ResetView()
    {
        if (_content == null) return;
        _zoom = MinZoom;
        _content.localScale = Vector3.one;
        _content.anchoredPosition = Vector2.zero;
        _lastViewportSize = _viewport != null ? _viewport.rect.size : Vector2.zero;
        Notify();
    }

    /// <summary>设置缩放；给定屏幕坐标时该点下的地图内容保持不动。</summary>
    public void SetZoom(float zoom, Vector2 screenPosition)
    {
        if(float.IsNaN(zoom)||float.IsInfinity(zoom))return;
        if (_content == null || _viewport == null) return;
        zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        Vector2 pivot;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, screenPosition, ResolveCamera(), out pivot))
            pivot = Vector2.zero;
        float old = _zoom;
        if (Mathf.Approximately(old, zoom)) return;
        // content 轴心在视口局部坐标里为 pivot 一侧；保持鼠标下的地图点不动。
        Vector2 position = _content.anchoredPosition;
        _content.anchoredPosition = pivot + (position - pivot) * (zoom / old);
        _zoom = zoom;
        _content.localScale = new Vector3(zoom, zoom, 1f);
        ClampPosition();
        Notify();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!InsideViewport(eventData)) return;
        float step = eventData.scrollDelta.y;
        if (Mathf.Abs(step) < 0.01f) return;
        SetZoom(_zoom * Mathf.Pow(1.12f, step), eventData.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!InsideViewport(eventData)) return;
        _dragging = true;
        _dragMoved = false;
        _dragStartPointer = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _content == null) return;
        if (!_dragMoved &&
            (eventData.position - _dragStartPointer).sqrMagnitude > DragThreshold * DragThreshold)
        {
            _dragMoved = true;
            // 超过阈值后取消本次按下对点位 Button 的点击（Unity 会读该标记）。
            eventData.eligibleForClick = false;
        }
        if (!_dragMoved) return;
        // 相机 Canvas 的像素距离不能用世界缩放直接换算。
        Camera camera = ResolveCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, camera, out Vector2 current)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position - eventData.delta, camera, out Vector2 previous))
            return;
        _content.anchoredPosition += (current - previous) * DragSensitivity;
        ClampPosition();
        Notify();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
    }

    private void LateUpdate()
    {
        if (_content == null || _viewport == null) return;
        Vector2 size = _viewport.rect.size;
        if (size != _lastViewportSize)
        {
            // 尺寸变化：重新居中并复位，避免留下半张空白地图。
            _lastViewportSize = size;
            ResetView();
        }
    }

    private void OnDestroy()
    {
        ZoomChanged = null;
        _viewport = null;
        _content = null;
    }

    private bool InsideViewport(PointerEventData eventData)
    {
        if (_viewport == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(_viewport, eventData.position, ResolveCamera());
    }

    private Camera ResolveCamera()
    {
        Canvas canvas = _viewport != null ? _viewport.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    /// <summary>把 content 约束在视口内：未放大的轴居中，放大后的轴不许拖出空白。</summary>
    private void ClampPosition()
    {
        if (_content == null || _viewport == null) return;
        Vector2 view = _viewport.rect.size;
        Vector2 map = _content.rect.size * _zoom;
        Vector2 position = _content.anchoredPosition;
        position.x = ClampAxis(position.x, view.x, map.x);
        position.y = ClampAxis(position.y, view.y, map.y);
        _content.anchoredPosition = position;
    }

    private static float ClampAxis(float position, float view, float map)
    {
        float limit = Mathf.Max(0f, (map - view) * .5f);
        return limit <= 0f ? 0f : Mathf.Clamp(position, -limit, limit);
    }

    private void Notify()
    {
        var handler = ZoomChanged;
        if (handler != null) handler(_zoom);
    }
}
