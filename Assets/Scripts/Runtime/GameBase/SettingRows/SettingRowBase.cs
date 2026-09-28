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
/// 之所以用名字查找而不是序列化引用：这些行是手工摆放的实例，用名字就不必在
/// Inspector 里逐行拖引用，拼装阶段也不容易漏。缺节点时会报一条明确的错误而不是静默失效。
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

    /// <summary>编辑器期就把 settingId 为空的实例指出来，避免拼新行时忘了填。</summary>
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(settingId))
        {
            ChaosLog.Warn(LogChannel.UI, name + " 的 settingId 为空，这一行不会响应任何设置");
        }
    }
}
