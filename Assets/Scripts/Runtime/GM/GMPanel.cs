using System;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GM 面板（开发期调试控制台）：左侧主列表选分类，中间副列表选版块，右侧显示版块页面。
///
/// ══════════════════════ 层级契约（改场景结构必须同步改这里的路径常量） ══════════════════════
/// GMPanel
/// └─ Window
///    ├─ TitleBar / Title、CloseBtn
///    └─ Body
///       ├─ MainList      ← 分类按钮（本类运行时生成，场景里是空容器）
///       ├─ SubList/Scroll View/Viewport/Content ← 版块按钮（本类运行时生成）
///       └─ ContentArea   ← 版块页面（本类运行时按需生成）
///
/// ══════════════════════ 为什么以前按钮点了没反应（修过的坑，别再踩回来） ══════════════════════
/// 症状：GM 按钮能打开面板，但面板里所有按钮都点不动，射线永远打在别的 UI 上。
/// 根因有两条，缺一条都会复发：
///  1) GMPanel 上自己挂了一个 Canvas 组件（序列化成 World Space、没有 Render Camera），
///     它就是一个独立的 root canvas：面板里的 Graphic 全部注册到这个画布上，
///     而 GM_Canvas 的 GraphicRaycaster 只处理注册在自己画布上的 Graphic ——
///     于是面板里的东西根本不参与射线检测。
///     结论：一个 UI 只能有一个 root Canvas + 一个 GraphicRaycaster，GMPanel 不许再挂 Canvas。
///  2) 常规 UI 画布（Canvas/Base_BG）与 GM 画布都在 sortingOrder = 0，射线排序退化成按距离比较，
///     全屏的 Base_BG 会盖在 GM 面板上。现在 GM_Canvas 放在 GMUI 排序层、order = 900，
///     保证 GM 界面整体压在常规 UI 之上（GMBtn 自己那层 override 998 仍在最上面，方便随时开关）。
///
/// ══════════════════════ 怎么加一个新功能 ══════════════════════
/// 只改下面的 BuildCatalog()：加一个 GmSection（标题 + 页面构建委托）即可，场景不用动。
/// 页面里用 AddActionButton / AddHint / AddPanelButton 拼控件；
/// 需要一小段独立逻辑（像"显示主菜单"）就挂一个现成的测试组件。
/// </summary>
public class GMPanel : MonoBehaviour
{
    // ══════════════════════ 场景节点路径 ══════════════════════
    private const string Node_MainList = "Window/Body/MainList";
    private const string Node_SubListContent = "Window/Body/SubList/Scroll View/Viewport/Content";
    private const string Node_ContentArea = "Window/Body/ContentArea";
    private const string Node_CloseBtn = "Window/TitleBar/CloseBtn";
    private const string Node_Title = "Window/TitleBar/Title";

    // ══════════════════════ 配色与尺寸（想换风格只改这里） ══════════════════════
    private static readonly Color CategoryNormalColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    private static readonly Color CategorySelectedColor = new Color(0.20f, 0.45f, 0.85f, 1f);
    private static readonly Color SectionNormalColor = new Color(0.13f, 0.15f, 0.18f, 1f);
    private static readonly Color SectionSelectedColor = new Color(0.24f, 0.33f, 0.48f, 1f);
    private static readonly Color ActionButtonColor = new Color(0.18f, 0.21f, 0.26f, 1f);
    private static readonly Color LabelColor = new Color(0.88f, 0.90f, 0.94f, 1f);
    private static readonly Color HintColor = new Color(0.55f, 0.58f, 0.63f, 1f);

    private const float CategoryButtonHeight = 56f;
    private const float SectionButtonHeight = 48f;
    private const float ActionButtonHeight = 52f;
    private const float ActionButtonWidth = 360f;
    private const float HintWidth = 700f;

    // ══════════════════════ 版块 / 分类的数据结构 ══════════════════════

    /// <summary>一个版块 = 副列表里的一项 + 右侧的一个页面。</summary>
    private sealed class GmSection
    {
        public readonly string Title;

        /// <summary>页面构建委托：只在第一次切到这个版块时调用一次。</summary>
        public readonly Action<Transform> BuildPage;

        public Button Button;
        public GameObject Page;

        public GmSection(string title, Action<Transform> buildPage)
        {
            Title = title;
            BuildPage = buildPage;
        }
    }

    /// <summary>一个分类 = 主列表里的一项，下面挂若干版块。</summary>
    private sealed class GmCategory
    {
        public readonly string Title;
        public readonly GmSection[] Sections;
        public Button Button;

        public GmCategory(string title, GmSection[] sections)
        {
            Title = title;
            Sections = sections;
        }
    }

    [Header("样式（在场景里赋值；字体留空会退化成 TMP 默认字体，中文可能变方块）")]
    public TMP_FontAsset font;
    public Sprite buttonSprite;

    private GmCategory[] _categories;

    private Transform _mainList;
    private Transform _subListContent;
    private Transform _contentArea;
    private Button _closeBtn;
    private TextMeshProUGUI _title;

    private int _categoryIndex = -1;

    // ══════════════════════ 生命周期 ══════════════════════

    private void Awake()
    {
        _categories = BuildCatalog();

        BindNodes();
        BuildCloseButton();

        BuildCategoryButtons();
        SelectCategory(0);

        EventManager.Instance.AddListener(EventConstName.CloseGM, ClosePanelEvent);
    }

    private void OnDestroy()
    {
        EventManager.Instance.RemoveListener(EventConstName.CloseGM, ClosePanelEvent);
    }

    private void BindNodes()
    {
        _mainList = transform.Find(Node_MainList);
        _subListContent = transform.Find(Node_SubListContent);
        _contentArea = transform.Find(Node_ContentArea);
        _closeBtn = FindComponent<Button>(Node_CloseBtn);
        _title = FindComponent<TextMeshProUGUI>(Node_Title);

        if (_mainList == null) ChaosLog.Error(LogChannel.UI, "GMPanel 下找不到 " + Node_MainList + "，主列表不可用");
        if (_subListContent == null) ChaosLog.Error(LogChannel.UI, "GMPanel 下找不到 " + Node_SubListContent + "，副列表不可用");
        if (_contentArea == null) ChaosLog.Error(LogChannel.UI, "GMPanel 下找不到 " + Node_ContentArea + "，版块页面没地方放");
        if (_closeBtn == null) ChaosLog.Error(LogChannel.UI, "GMPanel 下找不到 " + Node_CloseBtn + "，关闭按钮不可用");
        if (_title != null) _title.text = "GM 控制台";
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform node = transform.Find(path);
        if (node == null) return null;

        T component = node.GetComponent<T>();
        if (component == null) ChaosLog.Error(LogChannel.UI, path + " 上没有 " + typeof(T).Name + " 组件");
        return component;
    }

    private void BuildCloseButton()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(ClosePanel);
    }

    // ══════════════════════ 功能清单（加功能只改这里） ══════════════════════

    private GmCategory[] BuildCatalog()
    {
        return new[]
        {
            new GmCategory("界面", new[]
            {
                // 复用现成的 GM_UITestEnter：它自己接按钮，会 PushPanel(MainMenuPanel) 并关掉 GM 面板
                new GmSection("主菜单面板", page => AttachTestEntry(page, "显示 / 隐藏主菜单")),
                new GmSection("设置面板", page => AddPanelButton(page, "打开设置面板", PanelType.SettingPanel)),
                new GmSection("存档面板", page => AddPanelButton(page, "打开存档面板", PanelType.SLPanel)),
            }),

            new GmCategory("系统", new[]
            {
                new GmSection("语言切换", BuildLanguagePage),
                new GmSection("日志与性能", page => AddHint(page, "预留：日志等级 / 频道开关 / 帧率与内存显示，待接入。")),
            }),

            new GmCategory("道具", new[]
            {
                new GmSection("发放道具", page => AddHint(page, "预留：等仓库系统完成后接入（选道具 + 数量 + 发放）。")),
                new GmSection("背包 / 仓库", page => AddHint(page, "预留：等仓库系统完成后接入（查看 / 增删 / 清空）。")),
            }),
        };
    }

    private void BuildLanguagePage(Transform page)
    {
        AddActionButton(page, "简体中文", () => ApplyLanguage(LanguageType.ChineseSimplified));
        AddActionButton(page, "繁體中文", () => ApplyLanguage(LanguageType.ChineseTraditional));
        AddActionButton(page, "English", () => ApplyLanguage(LanguageType.English));
        AddActionButton(page, "日本語", () => ApplyLanguage(LanguageType.Japanese));
        AddActionButton(page, "한국어", () => ApplyLanguage(LanguageType.Korean));
        AddHint(page, "只改运行时显示，不写存档（存档里的语言以设置面板为准）。");
    }

    private void ApplyLanguage(LanguageType language)
    {
        //LocalizationManager 走 SingletonMono，实例入口是 GetInstance() 而不是 Instance
        LocalizationManager manager = LocalizationManager.GetInstance();
        if (manager == null)
        {
            ChaosLog.Error(LogChannel.UI, "场景里没有 LocalizationManager，语言切换不可用");
            return;
        }

        manager.ChangeLanguage(language);
    }

    /// <summary>挂上现成的测试组件（组件自己在 Awake 里接按钮）。</summary>
    private void AttachTestEntry(Transform page, string label)
    {
        Button button = AddActionButton(page, label, null);
        if (button != null) button.gameObject.AddComponent<GM_UITestEnter>();
    }

    private void AddPanelButton(Transform page, string label, PanelType panelType)
    {
        AddActionButton(page, label, () =>
        {
            if (UIManager.Instance == null)
            {
                ChaosLog.Error(LogChannel.UI, "UIManager 还没初始化，打不开 " + panelType);
                return;
            }

            UIManager.Instance.PushPanel(panelType);

            //常规面板盖上来之后 GM 面板就没用了，收掉它，免得两层 UI 压在一起
            ClosePanel();
        });
    }

    // ══════════════════════ 主 / 副列表的构建与切换 ══════════════════════

    private void BuildCategoryButtons()
    {
        if (_mainList == null) return;

        for (int i = 0; i < _categories.Length; i++)
        {
            GmCategory category = _categories[i];
            //闭包捕获：这里必须用局部变量，直接用 i 会让所有按钮都切到最后一个分类
            int index = i;

            category.Button = CreateButton(_mainList, "Btn_" + category.Title, category.Title,
                CategoryNormalColor, CategoryButtonHeight, 24);
            category.Button.onClick.AddListener(() => SelectCategory(index));
        }
    }

    /// <summary>切换分类：重建副列表，并停在第一个版块。</summary>
    private void SelectCategory(int index)
    {
        if (index < 0 || index >= _categories.Length) return;

        _categoryIndex = index;

        for (int i = 0; i < _categories.Length; i++)
        {
            if (_categories[i].Button == null) continue;
            SetButtonColor(_categories[i].Button, i == index ? CategorySelectedColor : CategoryNormalColor);
        }

        RebuildSectionButtons();
        SelectSection(0);
    }

    /// <summary>重建副列表：清掉上一个分类的按钮，按当前分类的版块重新生成。</summary>
    private void RebuildSectionButtons()
    {
        if (_subListContent == null) return;

        //先收掉所有页面。页面是懒创建后缓存的，这里只隐藏不销毁，下次切回来省一次重建
        foreach (GmCategory category in _categories)
        {
            foreach (GmSection section in category.Sections)
            {
                if (section.Page != null) section.Page.SetActive(false);
            }
        }

        for (int i = _subListContent.childCount - 1; i >= 0; i--)
        {
            //Destroy 要到帧末才生效：先失活，否则这一帧里旧按钮还在参与布局与射线检测，
            //同一帧内再切一次分类就会看到新旧按钮叠在一起（新旧都能点到，切错版块）
            GameObject stale = _subListContent.GetChild(i).gameObject;
            stale.SetActive(false);
            Destroy(stale);
        }

        GmSection[] sections = _categories[_categoryIndex].Sections;
        for (int i = 0; i < sections.Length; i++)
        {
            GmSection section = sections[i];
            int index = i;

            section.Button = CreateButton(_subListContent, "Btn_" + section.Title, section.Title,
                SectionNormalColor, SectionButtonHeight, 22);
            section.Button.onClick.AddListener(() => SelectSection(index));
        }
    }

    /// <summary>切换版块：只显示当前版块的页面，页面在第一次切到时才生成。</summary>
    private void SelectSection(int index)
    {
        if (_categoryIndex < 0) return;

        GmSection[] sections = _categories[_categoryIndex].Sections;
        if (index < 0 || index >= sections.Length) return;

        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].Button != null)
            {
                SetButtonColor(sections[i].Button, i == index ? SectionSelectedColor : SectionNormalColor);
            }

            if (sections[i].Page != null && i != index) sections[i].Page.SetActive(false);
        }

        GmSection current = sections[index];
        if (current.Page == null && _contentArea != null)
        {
            current.Page = CreatePage(_contentArea, current);
        }

        if (current.Page != null) current.Page.SetActive(true);
    }

    private static void SetButtonColor(Button button, Color color)
    {
        Graphic graphic = button.targetGraphic;
        if (graphic != null) graphic.color = color;
    }

    // ══════════════════════ 页面与控件的生成 ══════════════════════

    private GameObject CreatePage(Transform parent, GmSection section)
    {
        GameObject page = new GameObject("Page_" + SanitizeNodeName(section.Title), typeof(RectTransform));
        RectTransform rect = (RectTransform)page.transform;
        rect.SetParent(parent, false);
        Stretch(rect);

        VerticalLayoutGroup layout = page.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (section.BuildPage != null) section.BuildPage(page.transform);
        return page;
    }

    /// <summary>页面上加一个动作按钮。<paramref name="onClick"/> 为 null 表示由别的组件自己接。</summary>
    private Button AddActionButton(Transform page, string label, Action onClick)
    {
        Button button = CreateButton(page, "Btn_" + label, label, ActionButtonColor, ActionButtonHeight, 24);
        if (button == null) return null;

        LayoutElement element = button.GetComponent<LayoutElement>();
        element.preferredWidth = ActionButtonWidth;
        element.flexibleWidth = 0f;

        if (onClick != null) button.onClick.AddListener(() => onClick());
        return button;
    }

    private void AddHint(Transform page, string text)
    {
        GameObject go = new GameObject("Hint", typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(page, false);
        rect.sizeDelta = new Vector2(HintWidth, 30f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 20;
        label.color = HintColor;
        label.alignment = TextAlignmentOptions.TopLeft;
        //提示语通常比按钮长，必须换行，否则会横向溢出到页面外面
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        label.text = text;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = HintWidth;
        element.preferredHeight = 60f;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color, float height, int fontSize)
    {
        GameObject go = new GameObject(SanitizeNodeName(name), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        //tint 的常态必须是白色：按钮底色由 Image.color 决定，这里只做高亮/按下的叠加
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(rect, false);
        Stretch(labelRect);

        TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = LabelColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = label;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        return button;
    }

    /// <summary>
    /// 节点名里不能出现 '/'（它是 Transform.Find 的路径分隔符），
    /// 而版块标题里很可能会写（例如"背包 / 仓库"），所以生成节点名前统一换掉。
    /// 只改节点名，按钮上的文案仍是原标题。
    /// </summary>
    private static string SanitizeNodeName(string name)
    {
        return string.IsNullOrEmpty(name) ? "Node" : name.Replace('/', '-');
    }

    private static void Stretch(RectTransform rect)
    {        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    // ══════════════════════ 开关面板 ══════════════════════

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void ClosePanelEvent(object sender, EventArgs e)
    {
        ClosePanel();
    }
}
