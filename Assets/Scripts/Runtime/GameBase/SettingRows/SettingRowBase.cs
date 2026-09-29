using ChaosDebug;
using TMPro;
using UnityEngine;

/// <summary>
/// 设置行的公共基类。
///
/// ══════════ 设计分工（全静态摆放的前提）══════════
/// 行【只负责显示与采集输入】，不知道自己的值对应 SettingsData 的哪个字段；
/// "id → 字段"的映射集中在各页的 OnBind() 里（见 SettingsPageBase.BindSlider 等）。
/// 这样做的原因：如果让每行自己去 switch(settingId) 读写字段，
/// 20 多个行就会有 20 多处散落的映射，加一个设置项要在两个地方改，还容易漏。
///
/// ══════════ 子节点命名约定 ══════════
/// 行模板通过 transform.Find("名字") 找控件，所以【子节点名字是硬契约】，改名会让这一行失效：
///   SettingRow_Slider    → Label / Slider / ValueText
///   SettingRow_Selector  → Label / Prev / ValueText / Next
///   SettingRow_Toggle    → Label / Toggle
///   SettingRow_Button    → Label / Button
///   SettingRow_Keybind   → Label / KeyButton / KeyText / ResetButton
///                          ＋ 可选的第二格 MouseButton / MouseText
/// 之所以用名字查找而不是序列化引用：这些行是手工摆放的实例，用名字就不必在
/// Inspector 里逐行拖引用，拼装阶段也不容易漏。缺节点时会报一条明确的错误而不是静默失效。
///
/// 可选的那两个节点（鼠标格）只有"键鼠方案的按键行"才有，所以用 Find(..., required: false) 找：
/// 手柄方案的行没有它们，成员全为 null，行为与单格行完全一致。
/// </summary>
public abstract class SettingRowBase : MonoBehaviour
{
    [Tooltip("这一行编辑的是哪个设置项。取值必须是 SettingIds 里的常量，拼错会在 Play 时报警告。")]
    public string settingId;

    /// <summary>
    /// 按约定名找子节点上的组件。找不到时按 required 决定是报错还是静默返回 null。
    ///
    /// 注意这里【不会向上/向下递归】：行模板结构是固定的，写死一层查找能让"改错层级"
    /// 立刻暴露成一条错误日志，而不是悄悄找错了控件。
    /// </summary>
    protected T Find<T>(string childName, bool required = true) where T : Component
    {
        Transform child = transform.Find(childName);
        T comp = (child != null) ? child.GetComponent<T>() : null;

        if (comp == null && required)
        {
            ChaosLog.Error(LogChannel.UI,
                GetType().Name + " 上找不到子节点 '" + childName + "'（或它没挂 " + typeof(T).Name + "），这一行会失效。" +
                "子节点命名约定见 SettingRowBase 的注释。");
        }
        return comp;
    }

    /// <summary>安全地写文本：文本组件缺失时只跳过，不抛异常（缺控件已经在 Find 里报过了）。</summary>
    protected static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null) text.text = value;
    }

    /// <summary>
    /// 行当前能不能点。子类在临时态（按键行"正在等待玩家按键"）恢复时要读它，
    /// 免得把"这一行本来就点不了"的状态一起点亮。
    /// </summary>
    protected bool Clickable { get; private set; } = true;

    /// <summary>
    /// 只切可点性，不动颜色。用于【临时锁住】：等待按键期间要挡住重复点击，
    /// 但那一行正显示着"请按键…"的提示，调暗反而看不清该按哪儿。
    ///
    /// 这是子类的 override 点（各自持有不同的控件类型）。
    /// </summary>
    public virtual void SetClickable(bool clickable)
    {
        Clickable = clickable;
    }

    /// <summary>
    /// 整行置灰：文字调暗 + 不可点。用于【语义上的不可用】——
    /// 比如手柄方案下的鼠标灵敏度：这一项留着给玩家看见，但当前改了没意义。
    ///
    /// 惰性缓存原始颜色而不是在 Awake 里抓：五个行控件的 Awake 都是 private，
    /// 为了加一个基类 Awake 就得把它们全改成 override —— 代价大于收益，
    /// 而且首次置灰前一定已经渲染过，颜色抓出来就是对的。
    /// </summary>
    public virtual void SetInteractable(bool interactable)
    {
        EnsureTextCache();

        for (int i = 0; i < _texts.Length; i++)
        {
            if (_texts[i] == null) continue;
            _texts[i].color = interactable
                ? _baseColors[i]
                : Color.Lerp(_baseColors[i], DisabledTint, DisabledBlend);
        }

        SetClickable(interactable);
    }

    /// <summary>置灰时文字往这个颜色靠：中灰 + 半透明，落在浅灰行底上能明显看出"不可用"。</summary>
    private static readonly Color DisabledTint = new Color(0.62f, 0.62f, 0.62f, 0.55f);

    /// <summary>混合比例。取 0.75 是为了让深色文字(0.1)与浅色行底(0.93)之间的对比明显掉下来。</summary>
    private const float DisabledBlend = 0.75f;

    private TextMeshProUGUI[] _texts;
    private Color[] _baseColors;

    private void EnsureTextCache()
    {
        if (_texts != null) return;

        //带上未激活的子节点：鼠标格之类的节点在切换页签时会被 SetActive(false)，
        //不抓进来的话切回来时它的颜色就还原不了
        _texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        _baseColors = new Color[_texts.Length];
        for (int i = 0; i < _texts.Length; i++)
        {
            _baseColors[i] = (_texts[i] != null) ? _texts[i].color : Color.white;
        }
    }

    /// <summary>编辑器期就把 settingId 为空的实例指出来，避免拼新行时忘了填。</summary>
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(settingId))
        {
            ChaosLog.Warn(LogChannel.UI, name + " 的 settingId 为空，这一行不会响应任何设置");
        }
    }
}
