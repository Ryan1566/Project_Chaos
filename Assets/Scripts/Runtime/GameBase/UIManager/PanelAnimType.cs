// 面板显隐动画类型。
// 约定：除 None 外，每个值都描述"起始状态"，结束状态永远是录制下来的静止状态。
// 因此同一个值既能驱动进场（起始状态 -> 静止状态），也能驱动退场（静止状态 -> 起始状态）。
//
// 注意：数值已显式指定，新增成员只能追加到末尾 —— Prefab 里存的是 int，
// 在中间插入成员会让所有已配置面板的动画类型集体错位。
public enum PanelAnimType
{
    /// <summary>不做动画，立即显示 / 隐藏。</summary>
    None = 0,

    /// <summary>仅淡入淡出。</summary>
    Fade = 1,

    /// <summary>从左侧滑入 / 向左侧滑出。</summary>
    SlideFromLeft = 2,

    /// <summary>从右侧滑入 / 向右侧滑出。</summary>
    SlideFromRight = 3,

    /// <summary>从上方滑入 / 向上方滑出。</summary>
    SlideFromTop = 4,

    /// <summary>从下方滑入 / 向下方滑出。</summary>
    SlideFromBottom = 5,

    /// <summary>从较小尺寸放大到静止尺寸。</summary>
    ScaleFromSmall = 6,

    /// <summary>从较大尺寸缩小到静止尺寸。</summary>
    ScaleFromLarge = 7,
}
