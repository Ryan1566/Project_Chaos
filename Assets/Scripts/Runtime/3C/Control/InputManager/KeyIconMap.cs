using System;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 按键图标映射表：把"一个输入控制"（空格、鼠标左键、左摇杆 左右…）映射到一张图标。
///
/// ══════════════════ 为什么需要它 ══════════════════
/// 按键界面上的键名原本是【本地化字符串】，而字符串的长度随语言剧烈变化：
/// 中文"空格"两个字，英文是 "Keyboard &amp; Mouse" 这样的整句，日文/韩文更长。
/// 键格与页签的宽度是摆 prefab 时定死的，于是切到英文/日文/韩文就会溢出、压到隔壁格子上。
/// 图标与语言无关、宽度恒定，正好解掉这个矛盾 —— 这也是本表存在的唯一理由。
///
/// ══════════════════ 它是"数据"而不是"代码里的 switch" ══════════════════
/// 一个开关对应哪张图，是美术资产的事（库里有哪些图、哪张更合适），
/// 不是逻辑的事。做成资产之后：
///   · 加键加图不用改代码、不用重新编译；
///   · 覆盖率能被编辑器工具可视化地检查（哪些键还没配图，一眼看到）。
/// 映射表的维护入口见编辑器工具 Tools/图标/按键图标映射。
///
/// ══════════════════ 查表键是"控制名"而不是"显示名" ══════════════════
/// 用 controlKey（如 "space"、"leftbutton"、"buttonSouth"、"leftStick/x"）查表，
/// 而不是用"空格""鼠标左键"这种显示名。原因有两个：
///   1. 显示名随语言变，用它查表等于把图标又绑回了语言；
///   2. 显示名是 DescribePath 算出来的，和本表各自独立才不会互相牵制。
/// 归一化规则只有一处：InputManager.ControlKey(path) —— 本表只负责按那个键查。
///
/// ══════════════════ family 是"显示主题"不是"数据分组" ══════════════════
/// 同一个控制在不同手柄型号下要显示不同的图（PS 的 ✕ 与 Xbox 的 A 是同一个 buttonSouth）。
/// 所以条目按 (controlKey, family) 配对，family 取 InputDeviceType：
/// Keyboard = 键鼠，PlayStation / Xbox = 手柄的两种显示主题。
/// 查不到本型号时会退而用另一个手柄型号那一列（见 GetIcon），而不是直接放弃 ——
/// "有个差不多的图标"比"退回一长串文字"更接近玩家想要的东西。
///
/// ══════════════════ 缺表 / 缺图时的行为：明确失败 ══════════════════
/// 表资产丢失或某个控制没有配图标时返回 null，使用方【回退显示原字符串】并只警告一次。
/// 刻意不做"猜一个相近的图标"：配错了的图标会让人以为功能是好的，
/// 而一长串文字至少能看出"这里没配图"。
/// </summary>
[CreateAssetMenu(fileName = "KeyIconMap", menuName = "输入/按键图标映射表")]
public class KeyIconMap : ScriptableObject
{
    /// <summary>同一控制有多个画法时用哪一套。与图标文件名后缀对应（见编辑器 IconNaming）。</summary>
    public enum IconStyle
    {
        /// <summary>填充实心键帽（文件名无后缀，如 keyboard_space.png）。默认。</summary>
        Filled = 0,
        /// <summary>描边键帽（文件名以 _outline 结尾）。</summary>
        Outline = 1,
        /// <summary>彩色面键（只有 PS 面键与 Xbox A/B/X/Y 有，文件名含 _color_）。这类【不能染色】。</summary>
        Color = 2,
    }

    /// <summary>一条"某控制（某显示主题）用某图标"。</summary>
    [Serializable]
    public class Entry
    {
        [Tooltip("归一化控制名，由 InputManager.ControlKey(path) 算出来。例：space / leftbutton / buttonSouth / leftStick/x / dpad/up")]
        public string controlKey;

        [Tooltip("显示主题。Keyboard = 键鼠；PlayStation / Xbox = 手柄的两种印字列")]
        public InputDeviceType family;

        [Tooltip("该控制用的图标。留空 = 没有图标，界面回退显示原字符串")]
        public Sprite icon;

        [Tooltip("不做染色（Image.color 保持白色）。彩色面键（_color_*）必须勾上，否则颜色会被乘成近黑")]
        public bool rawColor;

        [Tooltip("图标来源资产路径。由工具写入，只为排查用 —— 运行时完全不读它")]
        public string sourceAsset;

        [Tooltip("备注：为什么挑这张图 / 谁改的")]
        public string note;
    }

    [Tooltip("按 (controlKey, family) 查图标。同一对只需要一条；重复时以第一条为准")]
    public List<Entry> entries = new List<Entry>();

    // ══════════════════ 全局外观 ══════════════════

    [Tooltip("同一控制有多个画法时用哪一套（由工具在自动匹配时按它挑图）")]
    public IconStyle style = IconStyle.Filled;

    [Tooltip("宽键优先用'图形版'而不是'印字版'：space/tab/shift/capslock/backspace 同时有 keyboard_space（键帽上印 SPACE）与 keyboard_space_icon（键帽上画一道横杠）两种。勾上取后者 —— 少一个词就少一分语言差异")]
    public bool preferGlyphVariant = true;

    [Tooltip("图标染色。库里的图都是白色线条（为深色界面画的），而设置面板是浅底深字，不染色就看不见。白 × 色 = 色，所以这个值直接就是图标在界面上的颜色")]
    public Color tint = new Color(0.1f, 0.1f, 0.1f, 1f);

    // ══════════════════ 曾经有过、已经删掉的开关 ══════════════════
    // 这里原来有一个 hideKeyFrameWhenIconShown（显示图标时隐藏键格的白色底图），
    // 想法是"图标自带键帽外形，再叠一层白底键帽会变成双层边框"。
    // 它实现成 KeyButton.image.enabled = false —— 而那正是 Button 接收点击的 Graphic：
    // 关掉之后 OnDisable 把它从 GraphicRegistry 注销，整个键格再也收不到射线，
    // 表现为"图标显示正常但点不动"，且不报错。
    // 结论：这个开关不该存在（底图必须一直 enabled）。详见 KeyIconText 的类注释。

    // ══════════════════ 页签图标 ══════════════════
    // 页签（键鼠 / 手柄）的文案是溢出最严重的一处：英文 "Keyboard & Mouse" 有 16 个字符，
    // 而它的 Label 节点被摆成 76x50，TMP 又是 38pt 不换行 —— 直接把旁边的页签压住。
    // 页签不是"某个控制"，查不到表里，所以单独开三个字段。

    [Tooltip("键鼠页签的图标")]
    public Sprite keyboardTabIcon;

    [Tooltip("手柄页签图标（当前型号是 PS 时用）")]
    public Sprite gamepadTabIconPs;

    [Tooltip("手柄页签图标（当前型号是 Xbox 时用）")]
    public Sprite gamepadTabIconXbox;

    // ══════════════════ 全局注册 ══════════════════

    // ══════════════ 为什么放在 Resources 下 ══════════════
    // ScriptableObject 要能被运行时拿到只有两条路：放在 Resources 下用 Resources.Load 取，
    // 或者由某个 MonoBehaviour 在 Inspector 里持有引用。
    // 本表的使用方是 InputManager —— 它是个普通单例，没有"实例"可以挂引用
    // （LanguageFontMap 就是靠每个 LocalizedTextFont 组件各挂一份引用才活下来的），
    // 所以只有 Resources 这一条路。路径见 GlobalPath.res_KeyIconMapPath。
    private static KeyIconMap _active;
    private static bool _loadAttempted;

    /// <summary>当前生效的映射表。首次访问时从 Resources 懒加载；取不到时为 null（只警告一次）。</summary>
    public static KeyIconMap Active
    {
        get
        {
            if (_active == null && !_loadAttempted)
            {
                _loadAttempted = true;
                _active = Load();
                if (_active == null)
                {
                    ChaosLog.Warn(LogChannel.Input,
                        "找不到按键图标映射表（Resources/" + GlobalPath.res_KeyIconMapPath +
                        "），按键格将全部回退显示文字。用 Tools/图标/按键图标映射 建一张即可。");
                }
            }
            return _active;
        }
    }

    /// <summary>从 Resources 读映射表。放在这里而不是散在调用方，是为了让"路径从哪来"只有一处。</summary>
    public static KeyIconMap Load()
    {
        return Resources.Load<KeyIconMap>(GlobalPath.res_KeyIconMapPath);
    }

    /// <summary>
    /// 清掉懒加载缓存，下次访问重新 Load。
    /// 编辑器工具改完资产后调用 —— 不清的话工具里看到的还是旧表（Playing 时改资产也一样）。
    /// </summary>
    public static void ResetCache()
    {
        _active = null;
        _loadAttempted = false;
    }

    // ══════════════════ 查询 ══════════════════

    /// <summary>取某个控制在该显示主题下的条目，没有返回 null。</summary>
    public Entry FindEntry(string controlKey, InputDeviceType family)
    {
        if (string.IsNullOrEmpty(controlKey) || entries == null) return null;

        Entry otherFamily = null;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e == null || string.IsNullOrEmpty(e.controlKey)) continue;
            if (!string.Equals(e.controlKey, controlKey, StringComparison.OrdinalIgnoreCase)) continue;

            if (e.family == family) return e;

            // 记下"另一个手柄型号"的那一条作为备选。键鼠没有这个问题（键鼠只有一个 family）
            if (otherFamily == null && e.family != InputDeviceType.Keyboard && family != InputDeviceType.Keyboard)
            {
                otherFamily = e;
            }
        }
        return otherFamily;
    }

    /// <summary>取某个控制该用的图标；没配返回 null（调用方应回退显示文字）。</summary>
    public Sprite GetIcon(string controlKey, InputDeviceType family)
    {
        Entry e = FindEntry(controlKey, family);
        return e != null ? e.icon : null;
    }

    /// <summary>
    /// 取某个控制该用的图标颜色。彩色面键（rawColor）返回白色（不染色），其余返回统一 tint。
    /// 与 GetIcon 分开是因为"没配图标"时颜色没有意义 —— 调用方只在拿到非空 sprite 时才用这个。
    /// </summary>
    public Color GetIconColor(string controlKey, InputDeviceType family)
    {
        Entry e = FindEntry(controlKey, family);
        return (e != null && e.rawColor) ? Color.white : tint;
    }

    /// <summary>页签图标。family 传 Keyboard 得到键鼠页签那一个。</summary>
    public Sprite GetTabIcon(InputDeviceType family)
    {
        if (family == InputDeviceType.Keyboard) return keyboardTabIcon;
        return family == InputDeviceType.PlayStation ? gamepadTabIconPs : gamepadTabIconXbox;
    }

    // ══════════════════ 给工具与图集用 ══════════════════

    /// <summary>
    /// 精确匹配，不做跨型号回退。给编辑器工具用 ——
    /// 工具在改表，它需要的是"这条到底存不存在"，而不是"运行时能不能凑合查到一个"。
    /// </summary>
    public Entry FindEntryExact(string controlKey, InputDeviceType family)
    {
        if (string.IsNullOrEmpty(controlKey) || entries == null) return null;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e == null) continue;
            if (e.family != family) continue;
            if (string.Equals(e.controlKey, controlKey, StringComparison.OrdinalIgnoreCase)) return e;
        }
        return null;
    }

    /// <summary>取一条已存在的条目，没有就建一条空的。工具的自动匹配用它铺表。</summary>
    public Entry GetOrAddEntry(string controlKey, InputDeviceType family)
    {
        if (entries == null) entries = new List<Entry>();

        Entry e = FindEntryExact(controlKey, family);
        if (e != null) return e;

        e = new Entry { controlKey = controlKey, family = family };
        entries.Add(e);
        return e;
    }

    /// <summary>条目按 (family, controlKey) 排序，让工具里的表与两张表（配置/图标）对照起来稳定。</summary>
    public void SortEntries()
    {
        if (entries == null) return;
        entries.Sort((a, b) =>
        {
            if (a == null) return b == null ? 0 : 1;
            if (b == null) return -1;
            int f = ((int)a.family).CompareTo((int)b.family);
            return f != 0 ? f : string.CompareOrdinal(a.controlKey, b.controlKey);
        });
    }

    /// <summary>
    /// 本表引用到的全部图标（去重）。
    /// 图集白名单就是它 —— 见 IconAtlasBuilder：只把"运行时会用到的"打进图集，
    /// 而不是把 Art/icon 整个目录 436 张全打进去。
    /// </summary>
    public List<Sprite> CollectSprites()
    {
        var result = new List<Sprite>();
        System.Action<Sprite> add = s =>
        {
            if (s == null) return;
            if (result.Contains(s)) return;
            result.Add(s);
        };

        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null) add(entries[i].icon);
            }
        }

        add(keyboardTabIcon);
        add(gamepadTabIconPs);
        add(gamepadTabIconXbox);
        return result;
    }
}
