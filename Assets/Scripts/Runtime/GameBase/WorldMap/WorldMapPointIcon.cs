using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 单个点位图标的交互壳：悬停放大、点击回调、选中放大。
///
/// 之所以单独一个组件（而不是在面板里挂一堆匿名委托）：Unity 的射线事件
/// （`IPointerClickHandler` 等）必须由**组件**实现，面板自己没法收到"哪个图标被点了"。
/// 图标对象是运行时生成的，所以这个组件也只在运行时 `AddComponent`。
/// </summary>
public class WorldMapPointIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float HoverMultiplier = 1.35f;
    private const float SelectedMultiplier = 1.6f;

    private RectTransform _rect;
    private WorldMapPoint _point;
    private Action<WorldMapPoint> _onClick;

    private float _pointScale = 1f;
    private bool _hovered;
    private bool _selected;

    /// <summary>本图标对应的点位（点位是纯 C# 对象，引用相等即可判定"同一个点"）。</summary>
    public WorldMapPoint Point
    {
        get { return _point; }
    }

    /// <summary>绑定点位与点击回调。重复绑定是安全的（覆盖旧值）。</summary>
    public void Bind(WorldMapPoint point, Action<WorldMapPoint> onClick)
    {
        _point = point;
        _onClick = onClick;
        _rect = transform as RectTransform;
        _pointScale = (point != null && point.scale > 0f) ? point.scale : 1f;
        ApplyScale();
    }

    /// <summary>设置选中态（会与悬停叠加）。</summary>
    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyScale();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_onClick != null) _onClick(_point);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        ApplyScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (_rect == null) return;

        float multiplier = 1f;
        if (_selected) multiplier *= SelectedMultiplier;
        else if (_hovered) multiplier *= HoverMultiplier;

        _rect.localScale = Vector3.one * (_pointScale * multiplier);
    }
}
