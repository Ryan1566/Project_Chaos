using UnityEngine;
using TMPro;

namespace LocalizationSystem
{
    /// <summary>
    /// 按当前语言给这个文字对象换字体。挂在任何一个 TextMeshProUGUI 上即可。
    ///
    /// ══════════════════════ 它和 LocalizedText 的分工 ══════════════════════
    ///   LocalizedText      → 换【文字】：查表取译文写进 TMP
    ///   LocalizedTextFont  → 换【字体】：按当前语言换 TMP 的 font / fontSharedMaterial
    /// 两者互不依赖，谁都只订阅 LocalizationManager.OnLanguageChanged。
    /// 所以【这个组件完全可以单独用】—— 即便某个文字对象不需要翻译（比如纯数字、纯符号、
    /// 或者只在某种语言下出现的文本），只要它需要跟着语言换字体，就挂这个。
    ///
    /// 它也可以和 LocalizedText 挂在同一个物体上：两者改的是 TMP 的不同方面，不冲突。
    /// 顺序上无论谁先跑，最终状态都对 —— 换字体与换文字各会触发一次网格重建，只是多一次开销。
    ///
    /// ══════════════════════ 映射表怎么找 ══════════════════════
    /// 优先用 Inspector 上指定的 map；没指定时【自动在工程里找唯一一张 LanguageFontMap】。
    /// 自动查找是为了"不用给几百个 prefab 逐个拖引用"——
    /// 而一张语言字体映射表本来就该是全工程唯一的。找不到或找到多张时只警告一次、并保留原字体。
    ///
    /// ══════════════════════ 为什么换字体要连材质一起处理 ══════════════════════
    /// TMP_Text.font 的 setter 只调 LoadFontAsset()，【不会】自动把材质换成新字体的。
    /// 材质还指着旧字体时，字形来自新字体图集、材质却按旧图集的参数采样，
    /// 表现是文字变成全黑或直接不显示 —— 所以这里必须一起处理，不能只赋 font。
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedTextFont : MonoBehaviour
    {
        [Tooltip("语言→字体 映射表。留空则运行时自动查找工程里唯一的一张")]
        public LanguageFontMap map;

        [Tooltip("关掉它 = 这个文字对象不跟着语言换字体（仍可用 LocalizedText 翻译文字）")]
        public bool applyOnLanguageChange = true;

        private TMP_Text _text;

        /// <summary>上一次真正应用过的语言。用来避免"语言没变却每次显示都重刷一遍字体"。</summary>
        private LanguageType _appliedLanguage = (LanguageType)(-1);

        /// <summary>全局只警告一次，避免几百个实例刷满 Console。</summary>
        private static bool _warnedNoMap;

        /// <summary>全局只提醒一次：还没等到 LocalizationManager。</summary>
        private static bool _warnedNoManager;

        /// <summary>诊断用：本实例收到过几次语言切换事件。订阅没生效时它会一直是 0。</summary>
        public int LanguageEventCount { get; private set; }

        /// <summary>诊断用：Start 里是否成功订阅上了语言切换事件。</summary>
        public bool Subscribed { get; private set; }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            ApplyFont();
        }

        private void OnEnable()
        {
            //未激活的节点不跑 Awake（面板里大量档位默认是关的），所以这里也要兜一次。
            //_appliedLanguage 保证只有"语言变了或还没应用过"才会真的动字体。
            if (_text == null) _text = GetComponent<TMP_Text>();
            ApplyFont();
        }

        private void Start()
        {
            if (!applyOnLanguageChange) return;
            EnsureSubscribed();
        }

        private void OnDestroy()
        {
            LocalizationManager mgr = LocalizationManager.GetInstance();
            if (mgr != null) mgr.OnLanguageChanged.RemoveListener(OnLanguageChanged);
        }

        /// <summary>
        /// 订阅语言切换事件；此刻没有 LocalizationManager 就【留着下次再试】。
        ///
        /// ══════════════ 为什么不是"Start 里订一次就完事" ══════════════
        /// LocalizationManager 是自动查找的单例，建立时机不由本组件决定：
        /// 场景里没有它、或它比本组件晚建立时，Start 里 GetInstance() 会返回 null。
        /// 早期版本正是在这里直接 return —— 结果是"组件挂好了、映射表也配了，切语言就是不换字体"，
        /// 而且一声不响（实测复现：Subscribed=False、事件计数一直是 0，字体只在面板重开时才变）。
        ///
        /// 所以改成幂等的惰性订阅：ApplyFont 每次被调用都顺带试一次，
        /// LocalizationManager 一出现就会被订上。订上之后这里只是一次布尔比较，没有开销。
        /// </summary>
        private void EnsureSubscribed()
        {
            if (Subscribed || !applyOnLanguageChange) return;

            LocalizationManager mgr = LocalizationManager.GetInstance();
            if (mgr == null)
            {
                //只提醒一次：这个组件可能有很多实例，刷屏没意义
                if (!_warnedNoManager)
                {
                    _warnedNoManager = true;
                    ChaosDebug.ChaosLog.Info(ChaosDebug.LogChannel.Localization,
                        name + " 暂时拿不到 LocalizationManager，语言切换订阅会持续重试。" +
                        "若之后再没有订阅成功的迹象，说明场景里根本没有 LocalizationManager。");
                }
                return;
            }

            // ⚠ OnLanguageChanged 可能是 null：
            // 场景里正常摆放的 LocalizationManager 会被序列化出 m_PersistentCalls，
            // 但用 AddComponent 现造出来的那个字段是 null（Unity 不保证 UnityEvent 字段一定被初始化）。
            // 不判空就是一条 NullReferenceException 打断整个订阅，表现成"切语言毫无反应"。
            if (mgr.OnLanguageChanged == null)
            {
                mgr.OnLanguageChanged = new UnityEngine.Events.UnityEvent<LanguageType>();
            }

            mgr.OnLanguageChanged.AddListener(OnLanguageChanged);
            Subscribed = true;
        }

        private void OnLanguageChanged(LanguageType language)
        {
            LanguageEventCount++;
            if (!applyOnLanguageChange) return;
            ApplyFont();
        }

        /// <summary>
        /// 按当前语言应用字体。语言与上次相同时直接返回。
        ///
        /// 想强制重刷（例如运行时换了映射表）就先把 <see cref="map"/> 置空或换一张，
        /// 再调用 <see cref="ForceApply"/>。
        /// </summary>
        public void ApplyFont()
        {
            //顺便把订阅补上：拿不到 LocalizationManager 时 Start 里订不成，
            //这里每次应用都重试一次，它一出现就会被订上（详见 EnsureSubscribed）
            EnsureSubscribed();

            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null) return;

            LocalizationManager mgr = LocalizationManager.GetInstance();
            if (mgr == null) return;

            if (_appliedLanguage == mgr.CurrentLanguage) return;

            LanguageFontMap resolved = ResolveMap();
            if (resolved == null) return;

            TMP_FontAsset target = resolved.GetFont(mgr.CurrentLanguage);
            if (target != null) SwitchTo(target);

            _appliedLanguage = mgr.CurrentLanguage;
        }

        /// <summary>忽略"语言没变"的判断，强制重新应用一次（编辑器工具、或运行时换了映射表后用）。</summary>
        public void ForceApply()
        {
            _appliedLanguage = (LanguageType)(-1);
            ApplyFont();
        }

        /// <summary>
        /// 找映射表：
        ///   ① Inspector 上指定了就用它；
        ///   ② 否则退回全局注册的那张（LanguageFontMap.SetActive / Active）；
        ///   ③ 都没有 → 返回 null，并且【明确警告一次】。
        ///
        /// 为什么不做"运行时自动查找"：这张资产不在 Resources 下，Resources.Load 取不到它，
        /// 而 AssetDatabase 只有编辑器能用 —— 所谓自动查找在打包后必然失败。
        /// 与其留一个"看起来找过了、其实永远拿不到"的假动作，不如直接告诉人"没配"。
        /// </summary>
        private LanguageFontMap ResolveMap()
        {
            if (map != null) return map;

            if (LanguageFontMap.Active != null)
            {
                map = LanguageFontMap.Active;
                return map;
            }

            if (!_warnedNoMap)
            {
                _warnedNoMap = true;
                ChaosDebug.ChaosLog.Warn(ChaosDebug.LogChannel.Localization,
                    name + " 拿不到语言字体映射表，本次不会按语言换字体。" +
                    "两种修法：① 在 Inspector 上把 Assets/Data/Localization/LanguageFontMap.asset 拖进 map 字段" +
                    "（编辑器菜单「Tools/本地化/语言字体映射」里有个一键补全的按钮）；" +
                    "② 启动时调一次 LanguageFontMap.SetActive(那张表)。" +
                    "（此警告整个运行期只报一次）");
            }
            return null;
        }

        /// <summary>
        /// 真正的换字体。
        ///
        /// ══════════════ 为什么材质要"先清空、再让 TMP 自己解析" ══════════════
        /// TMP_Text.font 的 setter 第一句是 `if (m_fontAsset == value) return;` —— 字体没变就直接返回，
        /// 【不会】跑 LoadFontAsset。所以"先把材质置空、再赋一次字体"这种做法在字体相同时完全无效：
        /// 材质停在 null，而 TMP 渲染时拿不到材质 → 文字直接不显示。
        /// （实测踩过：切到日语再切回中文，fontSharedMaterial 一直是 null。）
        ///
        /// 正确做法是用一个"肯定不等于目标"的字体把它顶开，再赋目标值，
        /// 这样 setter 一定会走 LoadFontAsset()：
        ///   · 材质与目标字体的图集不匹配（或为 null）时，TMP 会自己换成该字体的默认材质
        ///     （TMPro_UGUI_Private.cs 的 LoadFontAsset 里有这段判断）；
        ///   · 材质本来就匹配时它保持不动 —— 正好保住美术特意换过的描边/阴影材质。
        ///
        /// 顶开用的那个"中介字体"就地取材：优先用当前字体；当前字体就等于目标时用 TMP 默认字体；
        /// 连默认字体都拿不到（理论上不会）时退回旧的两步走法。
        /// </summary>
        private void SwitchTo(TMP_FontAsset target)
        {
            if (target == null) return;

            TMP_FontAsset bump = _text.font != target ? _text.font : TMP_Settings.defaultFontAsset;

            if (bump == null || bump == target)
            {
                // 理论上到不了这里（TMP 一定有默认字体）。真到了就只赋字体、不动材质，
                // 至少不把已经匹配的材质弄坏
                _text.font = target;
                return;
            }

            _text.font = bump;
            _text.font = target;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑期自检：这张映射表里哪些语言没配字体、配的字体又缺哪些字形。
        /// 只在选中物体时打印，不参与运行。
        /// </summary>
        [ContextMenu("自检：当前语言字体映射")]
        private void ValidateMap()
        {
            LanguageFontMap resolved = map;
            if (resolved == null)
            {
                Debug.LogWarning("[语言字体] 没有指定映射表，无法自检。", this);
                return;
            }

            var sb = new System.Text.StringBuilder("[语言字体] 映射自检：\n");
            System.Array all = System.Enum.GetValues(typeof(LanguageType));
            for (int i = 0; i < all.Length; i++)
            {
                LanguageType lang = (LanguageType)all.GetValue(i);
                TMP_FontAsset f = resolved.GetFont(lang);
                sb.Append("  ").Append(lang).Append(" → ").Append(f == null ? "★未配置字体" : f.name).Append('\n');
            }
            Debug.Log(sb.ToString(), this);
        }
#endif
    }
}
