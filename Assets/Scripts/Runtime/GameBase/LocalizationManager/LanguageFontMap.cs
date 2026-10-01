using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LocalizationSystem
{
    /// <summary>
    /// 语言 → 字体 的映射表。
    ///
    /// ══════════════════════ 为什么需要它 ══════════════════════
    /// 不同语言的字形来自不同字体：微软雅黑（MSYH）没有假名与谚文，
    /// 仿宋（STFANGSO）连繁体都不全。工程里每个文字对象的字体是【摆 prefab 时烘进去】的，
    /// 那时还不知道玩家会选哪种语言 —— 所以必须有一个运行时按当前语言换字体的机制，
    /// 这张资产就是那份"哪种语言用哪个字体"的对照表。
    ///
    /// 它是一张【独立资产】而不是代码里的常量，原因有两个：
    ///   · 加语言/换字体不该改代码、重新编译；
    ///   · 这张表与语言枚举一一对应，放在资产里才能被编辑器工具可视化地检查覆盖情况。
    ///
    /// ══════════════════════ 它只管字体，不管文字 ══════════════════════
    /// 文字由 LocalizedText 负责（查表取译文），本类只回答"这个语言该用哪个字体"。
    /// 两者互相不知道对方，唯一的交汇点是都订阅了 LocalizationManager.OnLanguageChanged。
    ///
    /// ══════════════════════ 缺字体时的行为：什么都不做 ══════════════════════
    /// 某个语言没有配字体（或配了但资产丢了）时，本表返回 null，
    /// 使用方会【保留 prefab 里原本的字体】而不是换成别的。
    /// 这是刻意的：宁可显示成方框让人一眼看出漏配，也不要"悄悄换成另一个字体"导致
    /// 中文界面被换成日文字体这种莫名其妙的问题。
    /// </summary>
    [CreateAssetMenu(fileName = "LanguageFontMap", menuName = "本地化/语言字体映射表")]
    public class LanguageFontMap : ScriptableObject
    {
        /// <summary>一条"某语言用某字体"。</summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("这条规则对哪种语言生效")]
            public LanguageType language;

            [Tooltip("这种语言用的字体。留空表示不改动 —— 界面保持 prefab 里原本的字体（缺字会显示成方框）")]
            public TMP_FontAsset font;

            [Tooltip("备注：谁改的、为什么用这个字体")]
            public string note;
        }

        [Tooltip("按语言查字体。同一种语言只需要一条；重复时以第一条为准")]
        public List<Entry> entries = new List<Entry>();

        // ══════════════════ 全局注册 ══════════════════
        //
        // ══════════════ 为什么需要它，而不是"运行时自动查找" ══════════════
        // ScriptableObject 资产要能被运行时拿到，只有两条路：
        //   · 放在 Resources/ 下用 Resources.Load 取；
        //   · 由某个 MonoBehaviour 在 Inspector 里持有引用。
        // 本资产放在 Assets/Data/Localization 下（和其余本地化配置在一起），
        // 不在 Resources 里 —— 所以【运行时根本查不到它】，任何"自动查找"都是假的，
        // 只会变成一个静默失效的坑（组件以为找过了，其实永远拿不到表）。
        //
        // 所以这里给一个显式注册口：启动时谁持有它就注册一次。
        // 没有注册、组件也没在 Inspector 上指定时，行为是【保留原字体并只警告一次】——
        // 明确失败，不假装成功。
        private static LanguageFontMap _active;

        /// <summary>当前生效的映射表。未注册时为 null。</summary>
        public static LanguageFontMap Active { get { return _active; } }

        /// <summary>启动时注册（持有该资产的一方调用一次）。</summary>
        public static void SetActive(LanguageFontMap map)
        {
            _active = map;
        }

        /// <summary>取某种语言该用的字体；没配或字体资产丢失时返回 null（调用方应保留原字体）。</summary>
        public TMP_FontAsset GetFont(LanguageType language)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null) continue;
                if (entries[i].language == language) return entries[i].font;
            }
            return null;
        }

        /// <summary>确保每种启用的语言都有一条可编辑的记录（编辑器改表时用）。返回是否有改动。</summary>
        public bool EnsureAllLanguages()
        {
            if (entries == null) entries = new List<Entry>();

            bool changed = false;
            Array all = Enum.GetValues(typeof(LanguageType));
            for (int i = 0; i < all.Length; i++)
            {
                LanguageType lang = (LanguageType)all.GetValue(i);
                bool found = false;
                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j] != null && entries[j].language == lang) { found = true; break; }
                }
                if (!found)
                {
                    entries.Add(new Entry { language = lang });
                    changed = true;
                }
            }

            // 按枚举顺序排一下，界面上顺序稳定、也方便与枚举对照
            entries.Sort((a, b) =>
            {
                int ai = a == null ? int.MaxValue : (int)a.language;
                int bi = b == null ? int.MaxValue : (int)b.language;
                return ai.CompareTo(bi);
            });

            return changed;
        }
    }
}
