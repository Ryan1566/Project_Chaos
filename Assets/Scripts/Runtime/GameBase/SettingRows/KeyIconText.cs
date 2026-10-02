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
/// ══════════════════ 为什么同时要控制底图（frameImage）══════════════════
/// 键格本身有一层白色底（KeyButton 上的 9-slice 键帽），而库里的图标【自带键帽外形】。
/// 两个键帽叠在一起会变成双层边框，所以显示图标时要把白底关掉。
/// 这个白底归行控件管（它按约定名找 KeyButton / MouseButton），所以由它注入 ——
/// 本组件不去父子树上瞎猜"哪个 Image 是底图"，那种猜法在页签上就会猜错（页签的底图正该留着）。
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

    [Tooltip("显示图标时要隐藏的底图（键格的白色键帽底）。由 SettingRow_Keybind 注入；页签上没有底图可隐藏，留空即可")]
    public Image frameImage;

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
    /// 设置这一格显示什么。sprite 为 null（或禁用）时显示文字。
    ///
    /// 显示图标时【依然把文字写进 TMP】，只是把它 enabled 关掉。这不是浪费：
    ///   · 排查时能一眼看到"这一格本来是哪个键名"；
    ///   · SettingRowBase 的置灰逻辑是按 TMP 收集颜色的，文字为空会丢掉原始色；
    ///   · 图标丢了（资产被删）时能立刻回退，不需要重新走一遍输入域。
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

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = _showIcon;
            if (_showIcon) icon.color = _dimmed ? SettingRowBase.DisabledColor(_iconColor) : _iconColor;
        }

        //底图只在"有图标"时让位。没有图标时它必须回来 —— 否则那一格看起来会像没有键帽的空框
        if (frameImage != null && ShouldHideFrame())
        {
            frameImage.enabled = !_showIcon;
        }
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

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = _showIcon;
            if (_showIcon) icon.color = _dimmed ? SettingRowBase.DisabledColor(_iconColor) : _iconColor;
        }

        //页签的底图是圆角条、不是键帽，本来就不该隐藏；frameImage 没人注入时这段自然不参与
        if (frameImage != null && ShouldHideFrame()) frameImage.enabled = !_showIcon;
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

    /// <summary>
    /// 底图藏不藏。开关放在映射表上（那是"美术口径"，改一次全局生效），
    /// 表不在时按 true 处理：库里的图都自带键帽，藏底图是更常见的正确选择。
    /// </summary>
    private static bool ShouldHideFrame()
    {
        KeyIconMap map = KeyIconMap.Active;
        return map == null || map.hideKeyFrameWhenIconShown;
    }
}
