using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ArtPipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace InputIcon.Editor
{
    /// <summary>
    /// 图标文件名 → 输入控制名 的对照表。菜单工具（KeyIconMapWindow）用它把
    /// <c>Art/icon</c> 下的图和"哪个控制"接起来。
    ///
    /// ══════════════════════ 为什么用文件名而不是让美术填表 ══════════════════════
    /// 这套图（Kenney 输入提示包）的文件名本身就是一份规范的字典：
    /// <c>keyboard_space</c> / <c>mouse_left</c> / <c>xbox_button_a</c> / <c>playstation_button_cross</c>。
    /// 让美术照着规范命名，比让他们在 Inspector 里逐个拖 436 次引用可靠得多 ——
    /// 名字错了在扫描报告里立刻看得见，而漏拖一个引用要等到玩家切到那个键才发现。
    ///
    /// ══════════════════════ 控制名从哪来（不能靠猜）══════════════════════
    /// 右侧那一列必须与 Input System 键盘/鼠标/手柄布局里的 <c>control.name</c> 逐字一致。
    /// 本表的值取自 <c>InputSystem.LoadLayout("Keyboard"/"Mouse"/"Gamepad")</c> 的实际输出，例如：
    ///   · 键盘主行数字的控制名是 <c>"1"</c>（不是 "digit1"）；
    ///   · 主回车是 <c>"enter"</c>，小键盘回车是 <c>"numpadEnter"</c>；
    ///   · 反引号键是 <c>"backquote"</c>（不是 "tilde"）；
    ///   · 左右修饰键是 <c>"leftShift"/"rightShift"</c>，另有一组通用的 "shift"/"ctrl"/"alt"；
    ///   · 手柄摇杆轴是两段式 <c>"leftStick/x"</c>，方向键是 <c>"dpad/up"</c>。
    /// 写错一个字母不会报错，只会让那个键【静默退回显示文字】—— 所以核对过再改。
    ///
    /// ══════════════════════ 没有对应控制的图标：不猜，如实报告 ══════════════════════
    /// 库里有一批图在 Input System 里【没有对应的控制】，例如：
    ///   · <c>keyboard_bracket_less</c>（&lt;）与 <c>bracket_greater</c>（&gt;）—— 它们是 Shift+, / Shift+. 的组合，
    ///     键盘布局里没有独立的 &lt; 与 &gt; 控制；
    ///   · <c>keyboard_asterisk</c> / <c>colon</c> / <c>exclamation</c> / <c>question</c> / <c>plus</c> / <c>caret</c>
    ///     —— 同上，都是别的键的 Shift 态；
    ///   · <c>keyboard_function</c>（Fn）—— 布局里没有 Fn；
    ///   · <c>xbox_guide</c> —— 本工程的 Gamepad 布局里没有 guide 控制。
    /// 这些一律标成"未识别"并列进报告，而不是硬塞给一个"看起来差不多"的控制 ——
    /// 塞错的后果是某个键显示了一张毫不相干的图，比显示文字更难查。
    ///
    /// ══════════════════════ 同一控制的多种画法 ══════════════════════
    /// 一个控制常有多张图：风格（普通 / <c>_outline</c> / PS 与 Xbox 面键的 <c>_color_</c>）
    /// 与画法（普通 / <c>_icon</c> / <c>_alternative</c>）。本类把它们都识别出来，
    /// 但只挑【一张】交给映射表 —— 挑法见 PickBest，取舍由映射表上的两个开关决定
    /// （style 选风格、preferGlyphVariant 选"图形版优先还是印字版优先"）。
    /// 图集白名单正是"被挑中的那些"，所以这一步也决定了最终打进包里的图标数量。
    /// </summary>
    public static class IconNaming
    {
        /// <summary>同一控制的画法。用来在多个候选里排序（见 PickBest）。</summary>
        public enum Variant
        {
            /// <summary>普通：<c>keyboard_space</c>（键帽上印 "SPACE"）。</summary>
            Plain = 0,
            /// <summary>图形版：<c>keyboard_space_icon</c>（键帽上画一道横杠，不印字）。</summary>
            Glyph = 1,
            /// <summary>另一种画法：<c>_alternative</c>。</summary>
            Alternative = 2,
            /// <summary>图形版的另一种画法：<c>_icon_alternative</c>。</summary>
            GlyphAlternative = 3,
        }

        /// <summary>一个图标文件识别出来的全部信息。</summary>
        public class IconInfo
        {
            /// <summary>工程内路径，如 <c>Assets/Art/icon/keyboard&amp;mouse/keyboard_space.png</c>。</summary>
            public string assetPath;
            /// <summary>不含扩展名的文件名，如 <c>keyboard_space_icon_outline</c>。</summary>
            public string fileName;
            /// <summary>图标库目录下的相对路径，供报告里定位。</summary>
            public string relativePath;

            /// <summary>属于哪一套显示主题。</summary>
            public InputDeviceType family;
            /// <summary>去掉风格/画法后缀之后的主题内词元，如 <c>space</c>、<c>button_cross</c>。</summary>
            public string token;

            /// <summary>文件里的画风。</summary>
            public KeyIconMap.IconStyle style;
            /// <summary>同一画风下的画法。</summary>
            public Variant variant;

            /// <summary>映射到的控制名（可能一次映射多个，用 "|" 分开）；空串 = 未识别。</summary>
            public string controlKeys;
            /// <summary>多个图标争同一个控制时的优先度，小的优先。0 = 正牌，1 = 可接受的替代。</summary>
            public int rank;

            /// <summary>不是"某个控制"的图（方向簇、整机图例、滚轮方向等），只可能是字形素材。</summary>
            public bool isGlyphOnly;

            /// <summary>对照表里认识这个词元。为 false 才是真正的"读不懂"。</summary>
            public bool tokenKnown;

            /// <summary>识别说明，或"未识别"的原因。报告里直接用。</summary>
            public string note;

            /// <summary>是否识别到了控制。</summary>
            public bool Resolved { get { return !string.IsNullOrEmpty(controlKeys); } }

            /// <summary>被这个图标覆盖的控制名清单。</summary>
            public string[] Controls()
            {
                if (string.IsNullOrEmpty(controlKeys)) return new string[0];
                return controlKeys.Split('|');
            }
        }

        /// <summary>一条对照规则。</summary>
        private class Row
        {
            public string token;
            public string controls;
            public int rank;
            public string note;

            public Row(string token, string controls, int rank, string note)
            {
                this.token = token;
                this.controls = controls;
                this.rank = rank;
                this.note = note;
            }
        }

        // ══════════════════════ 文件名解析 ══════════════════════

        /// <summary>
        /// 把 (目录, 文件名) 切成 (family, token, style, variant)。
        ///
        /// ══════════════ 为什么目录才是主题的权威 ══════════════
        /// 有一批文件名【不带主题前缀】：整机图例在两边都叫 <c>controller_*</c>
        /// （ps/controller_playstation5、xbox/controller_xboxone），只按文件名分不出是哪一台。
        /// 而目录本来就是按主题分的（keyboard&amp;mouse / ps / xbox），它没有歧义 ——
        /// 所以以目录为准，文件名前缀只作为"目录认不出来时"的兜底。
        ///
        /// ══════════════ 解析顺序：先尾部后头部 ══════════════
        /// 后缀是叠加在文件名末尾的（_icon / _alternative / _outline），
        /// 必须【先砍尾部再砍头部前缀】：反过来先砍掉 "mouse" 之后，
        /// <c>mouse_outline</c> 只剩下 "outline"，那个前导下划线就丢了，后缀规则再也匹配不上。
        /// </summary>
        public static bool TryParse(string familyFolder, string fileName, out InputDeviceType family,
            out string token, out KeyIconMap.IconStyle style, out Variant variant)
        {
            family = InputDeviceType.Keyboard;
            token = null;
            style = KeyIconMap.IconStyle.Filled;
            variant = Variant.Plain;

            // ── 1. 主题：目录优先 ──
            bool folderKnown = true;
            if (string.Equals(familyFolder, "ps", StringComparison.OrdinalIgnoreCase))
            {
                family = InputDeviceType.PlayStation;
            }
            else if (string.Equals(familyFolder, "xbox", StringComparison.OrdinalIgnoreCase))
            {
                family = InputDeviceType.Xbox;
            }
            else if (string.Equals(familyFolder, "keyboard&mouse", StringComparison.OrdinalIgnoreCase))
            {
                family = InputDeviceType.Keyboard;
            }
            else
            {
                // 目录不认识（将来加了新目录）：退回按文件名前缀判，还是判不出来就当未识别
                folderKnown = false;
                if (fileName.StartsWith("xbox", StringComparison.OrdinalIgnoreCase)) family = InputDeviceType.Xbox;
                else if (fileName.StartsWith("playstation", StringComparison.OrdinalIgnoreCase)) family = InputDeviceType.PlayStation;
                else if (fileName.StartsWith("keyboard", StringComparison.OrdinalIgnoreCase) ||
                         fileName.StartsWith("mouse", StringComparison.OrdinalIgnoreCase)) family = InputDeviceType.Keyboard;
                else return false;
            }

            string s = fileName;

            // ── 2. 尾部：画风 ──
            if (s.EndsWith("_outline", StringComparison.Ordinal))
            {
                style = KeyIconMap.IconStyle.Outline;
                s = s.Substring(0, s.Length - "_outline".Length);
            }

            // ── 3. 尾部：画法（长的先判，否则 "_icon_alternative" 会被 "_alternative" 抢走一半）──
            if (s.EndsWith("_icon_alternative", StringComparison.Ordinal))
            {
                variant = Variant.GlyphAlternative;
                s = s.Substring(0, s.Length - "_icon_alternative".Length);
            }
            else if (s.EndsWith("_alternative", StringComparison.Ordinal))
            {
                variant = Variant.Alternative;
                s = s.Substring(0, s.Length - "_alternative".Length);
            }
            else if (s.EndsWith("_icon", StringComparison.Ordinal))
            {
                variant = Variant.Glyph;
                s = s.Substring(0, s.Length - "_icon".Length);
            }

            // ── 4. 头部：主题内的词元 ──
            if (string.Equals(s, "mouse", StringComparison.OrdinalIgnoreCase))
            {
                // 裸 mouse = 整只鼠标的图例（不是某个键）
                s = "";
            }
            else if (s.StartsWith("keyboard_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("keyboard_".Length);
            }
            else if (s.StartsWith("mouse_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("mouse_".Length);
            }
            else if (s.StartsWith("xbox_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("xbox_".Length);
            }
            else if (s.StartsWith("controller_", StringComparison.OrdinalIgnoreCase))
            {
                // 整机图例：词元保持原样（controller_playstation5 / controller_xboxone …）
                // 它们不是"某个按键"，对照表里控制是空的
            }
            else if (s.StartsWith("playstation", StringComparison.OrdinalIgnoreCase))
            {
                // playstation_button_cross → button_cross；playstation4_touchpad → 4_touchpad
                s = s.Substring("playstation".Length).TrimStart('_');
            }
            else if (!folderKnown)
            {
                return false;
            }

            // ── 5. 中部：彩色画风（只有 PS / Xbox 的面键有）──
            // button_color_cross → button_cross，画风 = Color
            const string colorMark = "_color_";
            int ci = s.IndexOf(colorMark, StringComparison.Ordinal);
            if (ci > 0)
            {
                style = KeyIconMap.IconStyle.Color;
                s = s.Substring(0, ci) + "_" + s.Substring(ci + colorMark.Length);
            }

            token = s;
            return true;
        }

        // ══════════════════════ 对照表 ══════════════════════

        // ══════════ 键盘 ══════════
        // controls 里用 "|" 一次挂多个控制：左右修饰键印的字一样，共用同一张图。
        // rank 1 = 可接受但非首选的替代画法（同控制有多张图时靠它定胜负）。
        private static readonly Row[] KeyboardRows =
        {
            // ── 字母与主行数字：控制名就是字符本身 ──
            new Row("0", "0", 0, null), new Row("1", "1", 0, null), new Row("2", "2", 0, null),
            new Row("3", "3", 0, null), new Row("4", "4", 0, null), new Row("5", "5", 0, null),
            new Row("6", "6", 0, null), new Row("7", "7", 0, null), new Row("8", "8", 0, null),
            new Row("9", "9", 0, null),
            new Row("a", "a", 0, null), new Row("b", "b", 0, null), new Row("c", "c", 0, null),
            new Row("d", "d", 0, null), new Row("e", "e", 0, null), new Row("f", "f", 0, null),
            new Row("g", "g", 0, null), new Row("h", "h", 0, null), new Row("i", "i", 0, null),
            new Row("j", "j", 0, null), new Row("k", "k", 0, null), new Row("l", "l", 0, null),
            new Row("m", "m", 0, null), new Row("n", "n", 0, null), new Row("o", "o", 0, null),
            new Row("p", "p", 0, null), new Row("q", "q", 0, null), new Row("r", "r", 0, null),
            new Row("s", "s", 0, null), new Row("t", "t", 0, null), new Row("u", "u", 0, null),
            new Row("v", "v", 0, null), new Row("w", "w", 0, null), new Row("x", "x", 0, null),
            new Row("y", "y", 0, null), new Row("z", "z", 0, null),

            // ── 功能键 ──
            new Row("f1", "f1", 0, null), new Row("f2", "f2", 0, null), new Row("f3", "f3", 0, null),
            new Row("f4", "f4", 0, null), new Row("f5", "f5", 0, null), new Row("f6", "f6", 0, null),
            new Row("f7", "f7", 0, null), new Row("f8", "f8", 0, null), new Row("f9", "f9", 0, null),
            new Row("f10", "f10", 0, null), new Row("f11", "f11", 0, null), new Row("f12", "f12", 0, null),

            // ── 命名键 ──
            new Row("space", "space", 0, null),
            new Row("enter", "enter", 0, null),
            new Row("return", "enter", 1, "主回车的另一种画法，与 keyboard_enter 同一控制"),
            new Row("numpad_enter", "numpadEnter", 0, null),
            new Row("escape", "escape", 0, null),
            new Row("tab", "tab", 0, null),
            new Row("backspace", "backspace", 0, null),
            new Row("delete", "delete", 0, null),
            new Row("insert", "insert", 0, null),
            new Row("home", "home", 0, null),
            new Row("end", "end", 0, null),
            new Row("page_up", "pageUp", 0, null),
            new Row("page_down", "pageDown", 0, null),
            new Row("capslock", "capsLock", 0, null),
            new Row("numlock", "numLock", 0, null),
            new Row("printscreen", "printScreen", 0, null),

            // ── 修饰键：左右印字相同，共用一张图 ──
            new Row("shift", "leftShift|rightShift|shift", 0, "左右 Shift 与小键盘外的通用 shift 共图"),
            new Row("ctrl", "leftCtrl|rightCtrl|ctrl", 0, null),
            new Row("alt", "leftAlt|rightAlt|alt", 0, null),
            new Row("option", "leftAlt|rightAlt", 1, "Mac 的 Option 就是 Alt，作为备选画法"),
            new Row("win", "leftMeta|rightMeta", 0, "Windows 徽标键 = Input System 的 Meta 键"),
            new Row("command", "leftMeta|rightMeta", 1, "Mac 的 Command 也是 Meta 键，作为备选画法"),

            // ── 方向键 ──
            new Row("arrow_up", "upArrow", 0, null),
            new Row("arrow_down", "downArrow", 0, null),
            new Row("arrow_left", "leftArrow", 0, null),
            new Row("arrow_right", "rightArrow", 0, null),

            // ── 标点：有同名控制的 ──
            new Row("quote", "quote", 0, null),
            new Row("apostrophe", "quote", 1, "同一物理键的另一种画法"),
            new Row("tilde", "backquote", 0, "键盘布局里这个键叫 backquote"),
            new Row("comma", "comma", 0, null),
            new Row("period", "period", 0, null),
            new Row("semicolon", "semicolon", 0, null),
            new Row("equals", "equals", 0, null),
            new Row("minus", "minus", 0, null),
            new Row("slash_forward", "slash", 0, null),
            new Row("slash_back", "backslash", 0, null),
            new Row("bracket_open", "leftBracket", 0, null),
            new Row("bracket_close", "rightBracket", 0, null),
            new Row("numpad_plus", "numpadPlus", 0, null),

            // ── 没有对应控制的（Shift 组合 / 布局里不存在的键）──
            new Row("asterisk", "", 0, "键盘布局里没有独立的 * 控制（它是 Shift+8）"),
            new Row("caret", "", 0, "没有独立的 ^ 控制"),
            new Row("colon", "", 0, "没有独立的 : 控制（Shift+;）"),
            new Row("exclamation", "", 0, "没有独立的 ! 控制"),
            new Row("question", "", 0, "没有独立的 ? 控制"),
            new Row("plus", "", 0, "没有独立的 + 控制（主行 + 是 Shift+=，小键盘另有 numpadPlus）"),
            new Row("bracket_less", "", 0, "没有独立的 < 控制（Shift+,）"),
            new Row("bracket_greater", "", 0, "没有独立的 > 控制（Shift+.）"),
            new Row("function", "", 0, "Fn 键在 Input System 里没有控制"),
            new Row("any", "anyKey", 0, "Input System 的合成控制 anyKey"),

            // ── 方向簇字形：不是按键，是"方向"素材 ──
            new Row("arrows", "", 0, null), new Row("arrows_all", "", 0, null),
            new Row("arrows_up", "", 0, null), new Row("arrows_down", "", 0, null),
            new Row("arrows_left", "", 0, null), new Row("arrows_right", "", 0, null),
            new Row("arrows_horizontal", "", 0, null), new Row("arrows_vertical", "", 0, null),
            new Row("arrows_none", "", 0, null),
        };

        // ══════════ 鼠标 ══════════
        // 裸 "mouse"（词元为空）= 整只鼠标的图例，用来当"键鼠"页签的图。
        private static readonly Row[] MouseRows =
        {
            new Row("left", "leftButton", 0, null),
            new Row("right", "rightButton", 0, null),
            new Row("scroll", "scroll|middleButton", 0, "滚轮与中键是同一个物理部件，共用一张图"),
            new Row("move", "position|delta", 0, "鼠标位置 / 移动（本作不参与重绑定，配上图是为了显示完整）"),
            new Row("scroll_up", "", 0, null), new Row("scroll_down", "", 0, null),
            new Row("scroll_vertical", "", 0, null),
            new Row("horizontal", "", 0, null), new Row("vertical", "", 0, null),
            new Row("small", "", 0, null),
            new Row("", "", 0, "整只鼠标：没有对应控制，用作页签图例"),
        };

        // ══════════ PlayStation ══════════
        private static readonly Row[] PlayStationRows =
        {
            // 面键：方位与 Xbox 一一对应（✕ 在南、○ 在东、□ 在西、△ 在北）
            new Row("button_cross", "buttonSouth", 0, null),
            new Row("button_circle", "buttonEast", 0, null),
            new Row("button_square", "buttonWest", 0, null),
            new Row("button_triangle", "buttonNorth", 0, null),
            new Row("button_l3", "leftStickPress", 0, null),
            new Row("button_r3", "rightStickPress", 0, null),
            new Row("button_analog", "", 0, "模拟键，Gamepad 布局里没有对应控制"),

            // 肩键 / 扳机
            new Row("trigger_l1", "leftShoulder", 0, null),
            new Row("trigger_l1_alternative", "leftShoulder", 1, null),
            new Row("trigger_l2", "leftTrigger", 0, null),
            new Row("trigger_l2_alternative", "leftTrigger", 1, null),
            new Row("trigger_r1", "rightShoulder", 0, null),
            new Row("trigger_r1_alternative", "rightShoulder", 1, null),
            new Row("trigger_r2", "rightTrigger", 0, null),
            new Row("trigger_r2_alternative", "rightTrigger", 1, null),

            // 摇杆
            new Row("stick_l", "leftStick", 0, null),
            new Row("stick_l_horizontal", "leftStick/x", 0, "两段式路径：左右轴"),
            new Row("stick_l_vertical", "leftStick/y", 0, "两段式路径：上下轴"),
            new Row("stick_l_press", "leftStickPress", 0, null),
            new Row("stick_l_up", "", 0, null), new Row("stick_l_down", "", 0, null),
            new Row("stick_l_left", "", 0, null), new Row("stick_l_right", "", 0, null),
            new Row("stick_r", "rightStick", 0, null),
            new Row("stick_r_horizontal", "rightStick/x", 0, null),
            new Row("stick_r_vertical", "rightStick/y", 0, null),
            new Row("stick_r_press", "rightStickPress", 0, null),
            new Row("stick_r_up", "", 0, null), new Row("stick_r_down", "", 0, null),
            new Row("stick_r_left", "", 0, null), new Row("stick_r_right", "", 0, null),
            new Row("stick_side_l", "", 0, null), new Row("stick_side_r", "", 0, null),
            new Row("stick_top_l", "", 0, null), new Row("stick_top_r", "", 0, null),

            // 方向键
            new Row("dpad", "dpad", 0, null),
            new Row("dpad_up", "dpad/up", 0, null),
            new Row("dpad_down", "dpad/down", 0, null),
            new Row("dpad_left", "dpad/left", 0, null),
            new Row("dpad_right", "dpad/right", 0, null),
            new Row("dpad_all", "", 0, null), new Row("dpad_none", "", 0, null),
            new Row("dpad_horizontal", "", 0, null), new Row("dpad_vertical", "", 0, null),

            // 菜单键：一代一代改了名字，都指同一组控制
            new Row("3_button_start", "start", 0, null),
            new Row("3_button_select", "select", 0, null),
            new Row("4_button_options", "start", 0, null),
            new Row("4_button_share", "select", 0, null),
            new Row("5_button_create", "select", 0, null),
            new Row("5_button_options", "start", 0, null),
            new Row("5_button_mute", "", 0, "静音键，布局里没有对应控制"),
            new Row("4_touchpad", "", 0, null), new Row("4_touchpad_touch", "", 0, null),
            new Row("5_touchpad", "", 0, null), new Row("5_touchpad_touch", "", 0, null),

            // 整机图例：给"手柄"页签用
            new Row("controller_playstation1", "", 0, null),
            new Row("controller_playstation2", "", 0, null),
            new Row("controller_playstation3", "", 0, null),
            new Row("controller_playstation4", "", 0, null),
            new Row("controller_playstation5", "", 0, "整机图例（手柄页签默认用这一张）"),
        };

        // ══════════ Xbox ══════════
        private static readonly Row[] XboxRows =
        {
            new Row("button_a", "buttonSouth", 0, null),
            new Row("button_b", "buttonEast", 0, null),
            new Row("button_x", "buttonWest", 0, null),
            new Row("button_y", "buttonNorth", 0, null),

            new Row("lb", "leftShoulder", 0, null),
            new Row("rb", "rightShoulder", 0, null),
            new Row("lt", "leftTrigger", 0, null),
            new Row("rt", "rightTrigger", 0, null),
            new Row("ls", "leftStickPress", 0, null),
            new Row("rs", "rightStickPress", 0, null),

            new Row("stick_l", "leftStick", 0, null),
            new Row("stick_l_horizontal", "leftStick/x", 0, null),
            new Row("stick_l_vertical", "leftStick/y", 0, null),
            new Row("stick_l_press", "leftStickPress", 1, "与 xbox_ls 同一控制，ls 是常见叫法"),
            new Row("stick_l_up", "", 0, null), new Row("stick_l_down", "", 0, null),
            new Row("stick_l_left", "", 0, null), new Row("stick_l_right", "", 0, null),
            new Row("stick_r", "rightStick", 0, null),
            new Row("stick_r_horizontal", "rightStick/x", 0, null),
            new Row("stick_r_vertical", "rightStick/y", 0, null),
            new Row("stick_r_press", "rightStickPress", 1, "与 xbox_rs 同一控制"),
            new Row("stick_r_up", "", 0, null), new Row("stick_r_down", "", 0, null),
            new Row("stick_r_left", "", 0, null), new Row("stick_r_right", "", 0, null),
            new Row("stick_side_l", "", 0, null), new Row("stick_side_r", "", 0, null),
            new Row("stick_top_l", "", 0, null), new Row("stick_top_r", "", 0, null),

            new Row("dpad", "dpad", 0, null),
            new Row("dpad_up", "dpad/up", 0, null),
            new Row("dpad_down", "dpad/down", 0, null),
            new Row("dpad_left", "dpad/left", 0, null),
            new Row("dpad_right", "dpad/right", 0, null),
            new Row("dpad_round", "dpad", 1, "圆形方向键，作为备选画法"),
            new Row("dpad_round_up", "dpad/up", 1, null),
            new Row("dpad_round_down", "dpad/down", 1, null),
            new Row("dpad_round_left", "dpad/left", 1, null),
            new Row("dpad_round_right", "dpad/right", 1, null),
            new Row("dpad_all", "", 0, null), new Row("dpad_none", "", 0, null),
            new Row("dpad_horizontal", "", 0, null), new Row("dpad_vertical", "", 0, null),
            new Row("dpad_round_all", "", 0, null), new Row("dpad_round_horizontal", "", 0, null),
            new Row("dpad_round_vertical", "", 0, null),

            // 菜单键：View / Back / Share 都指 select，Menu / Start 都指 start
            new Row("button_start", "start", 0, null),
            new Row("button_view", "select", 0, null),
            new Row("button_back", "select", 1, "旧称，与 button_view 同一控制"),
            new Row("button_menu", "start", 1, "旧称，与 button_start 同一控制"),
            new Row("button_share", "select", 1, null),
            new Row("guide", "", 0, "本工程 Gamepad 布局里没有 guide 控制"),

            new Row("controller_xbox360", "", 0, null),
            new Row("controller_xboxone", "", 0, "整机图例（手柄页签默认用这一张）"),
            new Row("controller_xboxseries", "", 0, null),
            new Row("controller_xbox_adaptive", "", 0, null),
        };

        // ══════════════════════ 扫描 ══════════════════════

        /// <summary>
        /// 扫描整个图标库，逐个文件解析出"它是什么"。
        /// 不认识的文件名不会抛异常，只是 <c>Resolved == false</c> 并在 note 里说明原因 ——
        /// 美术以后加了一批新图，这一步就是"哪些没接上"的清单。
        /// </summary>
        public static List<IconInfo> Scan()
        {
            var result = new List<IconInfo>();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconImportPostprocessor.IconRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);

                var info = new IconInfo();
                info.assetPath = path;
                info.fileName = fileName;
                info.relativePath = path.StartsWith(IconImportPostprocessor.IconRoot + "/")
                    ? path.Substring(IconImportPostprocessor.IconRoot.Length + 1)
                    : path;
                info.rank = 0;

                InputDeviceType family;
                string token;
                KeyIconMap.IconStyle style;
                Variant variant;

                // 主题以目录为准，所以把目录一起交给解析器（理由见 TryParse 的注释）
                string folder = "";
                int slash = info.relativePath.LastIndexOf('/');
                if (slash >= 0) folder = info.relativePath.Substring(0, slash);

                if (!TryParse(folder, fileName, out family, out token, out style, out variant))
                {
                    info.family = InputDeviceType.Keyboard;
                    info.token = fileName;
                    info.controlKeys = "";
                    info.note = "文件名不在这套命名规范里（既不是 keyboard_ / mouse 也不是 xbox_ / playstation_ 开头）";
                    result.Add(info);
                    continue;
                }

                info.family = family;
                info.token = token;
                info.style = style;
                info.variant = variant;

                Row row = FindRow(family, token);
                if (row == null)
                {
                    info.controlKeys = "";
                    info.note = "对照表里没有 '" + token + "' 这个词元";
                }
                else
                {
                    info.tokenKnown = true;
                    info.controlKeys = row.controls;
                    info.rank = row.rank;
                    info.note = row.note;
                    info.isGlyphOnly = string.IsNullOrEmpty(row.controls);
                }

                // 不是"某个按键"的图（方向簇 / 摇杆方向 / 整机图例）在表里控制是空的、备注也是空的。
                // 补一句统一说明，否则扫描报告里会出现一堆"路径：   "后面什么都没有的行 ——
                // 那种行读起来像解析失败了，其实只是"这张图本来就不对应控制"
                if (!info.Resolved && string.IsNullOrEmpty(info.note))
                {
                    info.note = "不是某个按键的图（方向簇 / 摇杆方向 / 整机图例），只作显示素材，不映射控制";
                }

                result.Add(info);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.relativePath, b.relativePath));
            return result;
        }

        private static Row FindRow(InputDeviceType family, string token)
        {
            Row[] rows = family == InputDeviceType.PlayStation ? PlayStationRows
                       : family == InputDeviceType.Xbox ? XboxRows
                       : (IsMouseToken(token) ? MouseRows : KeyboardRows);

            for (int i = 0; i < rows.Length; i++)
            {
                if (string.Equals(rows[i].token, token, StringComparison.Ordinal)) return rows[i];
            }
            return null;
        }

        /// <summary>判断一个词元属于鼠标表还是键盘表。两边都在 Keyboard 主题下，只能按内容分。</summary>
        private static bool IsMouseToken(string token)
        {
            switch (token)
            {
                case "left": case "right": case "scroll": case "move":
                case "scroll_up": case "scroll_down": case "scroll_vertical":
                case "horizontal": case "vertical": case "small": case "":
                    return true;
                default:
                    return false;
            }
        }

        // ══════════════════════ 选图 ══════════════════════

        /// <summary>
        /// 从一个控制的所有候选图里挑一张。挑法（顺序即优先级）：
        ///   1. 画风必须匹配（Filled / Outline / Color）；
        ///   2. 画法：按 preferGlyphVariant 决定"图形版优先"还是"印字版优先"，
        ///      Alternative 永远排在两者之后（它是"另一种画法"，不是更好的画法）；
        ///   3. rank 小的优先（正牌画法赢过替代画法）；
        ///   4. 都不分胜负时按文件名排序 —— 结果稳定，重复跑工具不会来回换图。
        ///
        /// 找不到匹配时返回 null（那个控制就没有图标，界面回退显示文字）。
        /// </summary>
        public static IconInfo PickBest(List<IconInfo> all, string controlKey, InputDeviceType family,
            KeyIconMap.IconStyle style, bool preferGlyphVariant)
        {
            IconInfo best = null;
            int bestScore = int.MaxValue;
            string bestName = null;

            for (int i = 0; i < all.Count; i++)
            {
                IconInfo info = all[i];
                if (!info.Resolved) continue;
                if (info.family != family) continue;
                if (info.style != style) continue;
                if (Array.IndexOf(info.Controls(), controlKey) < 0) continue;

                int score = VariantPenalty(info.variant, preferGlyphVariant) * 100 + info.rank;
                if (score < bestScore ||
                    (score == bestScore && string.CompareOrdinal(info.fileName, bestName) < 0))
                {
                    best = info;
                    bestScore = score;
                    bestName = info.fileName;
                }
            }
            return best;
        }

        private static int VariantPenalty(Variant variant, bool preferGlyph)
        {
            switch (variant)
            {
                case Variant.Plain: return preferGlyph ? 1 : 0;
                case Variant.Glyph: return preferGlyph ? 0 : 1;
                case Variant.Alternative: return 2;
                case Variant.GlyphAlternative: return preferGlyph ? 2 : 3;
                default: return 9;
            }
        }

        // ══════════════════════ 页签图例 ══════════════════════

        /// <summary>
        /// 页签用的整机图例文件名（不含扩展名）。
        ///
        /// 它们不是"某个控制"，所以在对照表里控制是空的，只能按文件名精确取。
        /// 注意整机图例的文件名【不带主题前缀】，所以这里给的是整段名字。
        ///
        /// ⚠ 键鼠页签没有理想的素材：库里只有"单个键"与"整只鼠标"，没有"键盘 + 鼠标"的合成图例。
        /// 默认拿整只鼠标的图例顶着；不满意可以在映射表上直接换一张（那两个字段是公开的）。
        /// </summary>
        public static string TabFileName(InputDeviceType family)
        {
            switch (family)
            {
                case InputDeviceType.PlayStation: return "controller_playstation5";
                case InputDeviceType.Xbox: return "controller_xboxone";
                default: return "mouse";   // 键鼠页签：库里没有"键盘+鼠标"合成图例，先用整只鼠标顶着
            }
        }

        // ══════════════════════ 报告 ══════════════════════

        /// <summary>
        /// 把扫描结果整理成一段人能读的报告。
        ///
        /// 刻意分成【三类】而不是"识别/未识别"两类：
        ///   · 控制图：认得出来、并且对应某个按键 —— 自动匹配会用它铺映射表；
        ///   · 素材图：认得出来，但它本来就不对应按键（方向簇、摇杆方向、整机图例）——
        ///     这些不是问题，是全图的弹药库；
        ///   · 未识别：对照表里没有这个词元，或者文件名不合规范 —— 只有这一类需要人处理。
        /// 混成两类的话，"整机图例"会被算进"读不懂"，报告里永远挂着一大串看着像错误的行，
        /// 真正该处理的那几条就被淹掉了。
        /// </summary>
        public static string Describe(List<IconInfo> all)
        {
            int control = 0, glyph = 0;
            var unresolved = new List<string>();
            var controls = new HashSet<string>();

            for (int i = 0; i < all.Count; i++)
            {
                IconInfo info = all[i];
                if (!info.tokenKnown)
                {
                    unresolved.Add(info.relativePath + "：" + info.note);
                    continue;
                }

                if (info.isGlyphOnly)
                {
                    glyph++;
                    continue;
                }

                control++;
                string[] cs = info.Controls();
                for (int c = 0; c < cs.Length; c++) controls.Add(info.family + "/" + cs[c]);
            }

            var sb = new StringBuilder();
            sb.Append("扫描 ").Append(all.Count).Append(" 张图标：")
              .Append("控制图 ").Append(control).Append(" 张、")
              .Append("素材图 ").Append(glyph).Append(" 张、")
              .Append("未识别 ").Append(unresolved.Count).Append(" 张；")
              .Append("覆盖 ").Append(controls.Count).Append(" 个 (主题,控制) 组合。");

            if (unresolved.Count > 0)
            {
                sb.Append("\n\n── 未识别（不会进映射表，也不会进图集）──");
                for (int i = 0; i < unresolved.Count; i++) sb.Append("\n  · ").Append(unresolved[i]);
            }
            return sb.ToString();
        }
    }
}
