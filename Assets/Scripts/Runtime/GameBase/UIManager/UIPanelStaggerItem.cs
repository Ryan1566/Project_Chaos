using UnityEngine;

/// <summary>
/// 依次入场（Stagger）的可选标记组件。
///
/// UIPanelAnimator 的 Stagger 默认会自动收集 staggerRoot 的直接子物体，
/// 大多数面板不需要挂这个组件。只有需要"只动其中几个子物体"或"自定义先后顺序"时，
/// 才在要参与的子物体上挂它 —— 一旦子树中存在该标记，就只动被标记的项，
/// 此时 UIPanelAnimator.staggerExclude 不再生效。
/// </summary>
public class UIPanelStaggerItem : MonoBehaviour
{
    [Tooltip("越小越先入场；相同则按层级顺序")]
    public int order = 0;
}
