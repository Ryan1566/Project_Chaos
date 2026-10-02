using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个"要么显示文字、要么显示图标"的显示格。挂在 KeyText / MouseText（以及页签的 Label）上。
///
/// ══════════════════ 它解决什么 ══════════════════
/// 键名原本一律是 TMP 文字，而文字长度随语言剧烈变化（"空格" ↔ "Mouse Left Button" ↔ 韩文长句），
/// 键格宽度是摆 prefab 时定死的，于是切语言就溢出。图标宽度恒定，正好解掉这件事。
/// 但图标不可能覆盖全部情况（库里没画那个键、映射表还没配），所以两者必须能共存并按需切换 ——
/// 本组件就是那个开关。取图标、判断有没有图标，都在外面做（KeyIconMap + InputManager），
/// 本组件只负责"给我一个 sprite，我把它显示出来"。
///
/// ══════════════════ 它【不】碰键格的底图 ══════════════════
/// 这里曾经做过一件事：显示图标时把键格的白色底图（KeyButton 上的 Image）关掉，
/// 免得"键帽图标"叠在"白底键格"上变成双层边框。**那是个 bug，已经删掉了。**
///
/// 原因：`Button` 自己【不做】射线检测，它能收到点击靠的是身上那个 `Graphic`（就是这张底图）。
/// `Image.enabled = false` 会让 `OnDisable` 把该 Graphic 从 `GraphicRegistry` 注销，
/// `GraphicRaycaster` 从此找不到它 —— 界面上的表现是"图标显示得好好的，但点不动"，
/// 而且**不会有任何报错**。实测（真射线）：关掉之后在键格中心打一条射线，
/// 命中的是面板底板，`GetEventHandler&lt;IPointerClickHandler&gt;` 返回 null。
///
/// 也不能改成"底图留着、只设成全透明"：这些按钮是 `ColorTint` 过渡、`targetGraphic`
/// 正是这张底图，鼠标一悬停 `Selectable` 就把颜色写回不透明，白底又冒出来。
///
/// 所以结论是——**底图必须一直 enabled，本组件连碰都不碰它**。
/// 而那本来也不是个问题：库里这些图标本身就是键帽外形，放在白色键格里正好像一颗按键，
/// 这原本就是按键行该有的样子。
///
/// ⚠ 由此得到的验证纪律：要判断"按钮能不能点"，必须走【真实射线】
/// （`EventSystem.RaycastAll`）或真的派发一次点击事件。直接调用 `Button.onClick.Invoke()`
/// 会绕开整套射线与事件系统，把"组件被禁用导致收不到事件"这类问题全部漏掉。
///
/// ══════════════════ 为什么所有引用都懒解析 ══════════════════
/// 手柄那三行在 prefab 里是【激活时全关着】的，它们的 Awake 要等切到"手柄"页签才跑。
/// 依赖 Awake 先后顺序去拿 text/icon 会得到一个"有时行有时不行"的组件，
/// 所以统一在每次调用前 EnsureRefs()，没拿到就报一次错。
/// </summary>
[DisallowMultipleComponent]
public class KeyIconText : MonoBehaviour
{
    [Tooltip("文字。留空则自动取同物体上的 TMP_Text")]
    public TMP_Text text;

    [Tooltip("图标。留空则自动取子节点 'Icon' 上的 Image")]
    public Image icon;

    private Color _iconColor = Color.white;
    private bool _dimmed;
    private bool _showIcon;
    private bool _refsChecked;

    /// <summary>当前显示的是图标还是文字。工具与测试用它判断状态（不用去读 enabled，那太隐晦）。</summary>
    public bool IsIconShown { get { return _showIcon; } }

    /// <summary>当前格子里那句文字。显示图标时它依然有效 —— 见 SetDisplay 的注释。</summary>
    public string CurrentText { get { return text != null ? text.text : ""; } }

    private void Awake()
    {
        EnsureRefs();
    }

    /// <summary>按约定名补齐引用。已经在 Inspector 上拖过的不覆盖。</summary>
    private void EnsureRefs()
    {
        if (_refsChecked) return;
        _refsChecked = true;

        if (text == null) text = GetComponent<TMP_Text>();

        if (icon == null)
        {
            Transform child = transform.Find("Icon");
            if (child != null) icon = child.GetComponent<Image>();
        }

        if (text == null && icon == null)
        {
            ChaosDebug.ChaosLog.Error(ChaosDebug.LogChannel.UI,
                name + " 上既没有 TMP_Text 也没有 'Icon' 子节点，这一格既显示不了文字也显示不了图标。" +
                "节点约定见 KeyIconText 与 SettingRowBase 的注释。");
        }
    }

    /// <summary>
    /// 设置这一格显示什么。sprite 为 null 时显示文字。
    ///
    /// 显示图标时【依然把文字写进 TMP】，只是把它 enabled 关掉。这不是浪费：
    ///   · 排查时能一眼看到"这一格本来是哪个键名"；
    ///   · SettingRowBase 的置灰逻辑是按 TMP 收集颜色的，文字为空会丢掉原始色；
    ///   · 图标丢了（资产被删）时能立刻回退，不需要重新走一遍输入域。
    ///
    /// ⚠ 这里只切【本组件自己管的那两个 Graphic】（文字与图标）。
    /// 键格的底图不是它的职责 —— 关掉它会让按钮收不到点击，理由见类注释。
    /// </summary>
    public void SetDisplay(string displayText, Sprite sprite, Color iconColor)
    {
        EnsureRefs();

        _iconColor = iconColor;
        _showIcon = sprite != null;

        if (text != null)
        {
            text.text = displayText ?? "";
            text.enabled = !_showIcon;
        }

        ApplyIcon(sprite);
    }

    /// <summary>
    /// 只切图标、不碰文字 —— 给"文字归 LocalizedText 管"的节点用（按键页的两个页签）。
    ///
    /// 与 SetDisplay 的分工：SetDisplay 是"文字也由我写"（键名格没有 LocalizedText）；
    /// 本方法是"文字另有主人"（页签的 Label 上挂着 LocalizedText，它自己会在切语言时写译文）。
    /// 于是这里只负责一件事：有图标就把 TMP 关掉，图标缺失就打开 ——
    /// 自动回退成译文，页面不需要再抄一份文案。
    /// </summary>
    public void SetIconOnly(Sprite sprite, Color iconColor)
    {
        EnsureRefs();

        _iconColor = iconColor;
        _showIcon = sprite != null;

        if (text != null) text.enabled = !_showIcon;

        ApplyIcon(sprite);
    }

    /// <summary>图标那一半的显隐与染色。两个入口共用，免得只改一处、另一处漏掉置灰。</summary>
    private void ApplyIcon(Sprite sprite)
    {
        if (icon == null) return;

        icon.sprite = sprite;
        icon.enabled = _showIcon;
        if (_showIcon) icon.color = _dimmed ? SettingRowBase.DisabledColor(_iconColor) : _iconColor;
    }

    /// <summary>整行被置灰时同步调暗图标。文字那一半由 SettingRowBase 负责，这里只补图标。</summary>
    public void SetDimmed(bool dimmed)
    {
        EnsureRefs();

        _dimmed = dimmed;
        if (icon != null && _showIcon)
        {
            icon.color = dimmed ? SettingRowBase.DisabledColor(_iconColor) : _iconColor;
        }
    }
}
