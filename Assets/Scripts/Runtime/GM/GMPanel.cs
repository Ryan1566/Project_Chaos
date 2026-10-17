using System;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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

    // ── GM：局内地图生成（原型验证用，不进正式流程）──
    private TMP_InputField _runMapSeedInput;
    private TextMeshProUGUI _runMapStatus;
    /// <summary>原型·进度页的状态行。</summary>
    private TextMeshProUGUI _protoProgressStatus;
    /// <summary>原型·存档页的状态行。</summary>
    private TextMeshProUGUI _protoSaveStatus;
    /// <summary>原型·地图页的状态行。</summary>
    private TextMeshProUGUI _protoMapStatus;
    /// <summary>原型·地图页的场景号输入（1~3）。</summary>
    private TMP_InputField _protoSceneIndexInput;
    /// <summary>原型·族人页的状态行。</summary>
    private TextMeshProUGUI _protoRetainerStatus;
    /// <summary>原型·道具发放页的状态行。</summary>
    private TextMeshProUGUI _protoItemStatus;
    /// <summary>原型·库存页的状态行。</summary>
    private TextMeshProUGUI _protoInventoryStatus;
    private TextMeshProUGUI _protoVillageStatus;
    private TextMeshProUGUI _protoFamilyTreeStatus;
    private uint _runMapSeed = 20261010u;

    /// <summary>GM 生成的地图挂在哪个容器下（重复生成时先删掉它）。</summary>
    private const string RunMapContainerName = "RunMap_GM";

    /// <summary>GM 只生成"本局第 1 个场景"；一局内三个场景的种子由 RunSeed 派生、互不相同。</summary>
    private const int RunMapSceneIndex = 1;

    /// <summary>走真实读条流程时要进的场景（与 SLPanel 读档后的目标一致）。</summary>
    private const string RunMapWorldScene = "WorldScene";

    /// <summary>读条场景名（GM 跳转用；与 SLPanel 用的是同一个）。</summary>
    private const string RunMapLoadingScene = "LoadingScene";

    /// <summary>局外（世界观地图）所在的常驻场景；GM 的【返回局外】用它。</summary>
    private const string ReturnWorldMapScene = "EnterScene";

    private GmCategory[] _categories;

    private Transform _mainList;
    private Transform _subListContent;
    private Transform _contentArea;
    /// <summary>主列表（分类）滚动容器（运行时构建）。</summary>
    private ScrollRect _mainScroll;
    /// <summary>页面区（版块页面）滚动容器（运行时构建）。</summary>
    private ScrollRect _contentScroll;
    private Button _closeBtn;
    private TextMeshProUGUI _title;

    private int _categoryIndex = -1;

    // ══════════════════════ 生命周期 ══════════════════════

    private void Awake()
    {
        _categories = BuildCatalog();

        BindNodes();
        SetupScrollViews();
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
                new GmSection("返回局外", page => AddActionButton(page, "返回局外（→ EnterScene）", ReturnToWorldMap)),
            }),

            new GmCategory("系统", new[]
            {
                new GmSection("语言切换", BuildLanguagePage),
                new GmSection("日志与性能", page => AddHint(page, "预留：日志等级 / 频道开关 / 帧率与内存显示，待接入。")),
                new GmSection("族人", BuildRetainerPage),
                new GmSection("村庄", BuildVillagePage),
                new GmSection("家族树", BuildFamilyTreePage),
            }),

            new GmCategory("局内生成", new[]
            {
                new GmSection("生成地图", BuildRunMapPage),
            }),

            new GmCategory("道具", new[]
            {
                new GmSection("发放道具", BuildItemGrantPage),
                new GmSection("背包 / 仓库", BuildInventoryPage),
            }),

            new GmCategory("原型", new[]
            {
                new GmSection("进度", BuildPrototypeProgressPage),
                new GmSection("存档", BuildPrototypeSavePage),
                new GmSection("地图", BuildPrototypeMapPage),
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


    // ══════════════════════ 原型：进度 / 存档 / 地图（W0）══════════════════════

    private void BuildPrototypeProgressPage(Transform page)
    {
        AddHint(page, "W0 地基：时代(M13) 单向 + 深度(M13b) 上限=时代序号。深度截断：era=1 时连点通关，深度停 1。");

        AddActionButton(page, "打印进度状态", () =>
        {
            string text = MetaProgressService.Instance.DumpState();
            SetProtoStatus(_protoProgressStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "模拟通关一次（clears++ 且深度+1）", () =>
        {
            MetaProgressService.Instance.SimulateClear();
            string text = MetaProgressService.Instance.DumpState();
            SetProtoStatus(_protoProgressStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "模拟满足里程碑（本局打过第3场景boss）", () =>
        {
            MetaProgressService.Instance.SimulateSatisfyMilestone();
            string text = MetaProgressService.Instance.DumpState();
            SetProtoStatus(_protoProgressStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "模拟推进时代（双门槛判定）", () =>
        {
            bool advanced = MetaProgressService.Instance.TryAdvanceEra();
            string text = (advanced ? "推进成功：" : "推进被拒：") + MetaProgressService.Instance.DumpState();
            SetProtoStatus(_protoProgressStatus, text);
            Debug.Log("[原型·进度] " + text);
        });

        AddActionButton(page, "重置进度", () =>
        {
            MetaProgressService.Instance.ResetAll();
            string text = MetaProgressService.Instance.DumpState();
            SetProtoStatus(_protoProgressStatus, text);
            Debug.Log(text);
        });

        _protoProgressStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoProgressStatus, MetaProgressService.Instance.DumpState());
    }

    private void BuildPrototypeSavePage(Transform page)
    {
        AddHint(page, "写槽位 1 → 读回并打印。⚠ 会覆盖槽位 1 的现有存档（自测请先备份）。");

        AddActionButton(page, "打印当前 payload", () =>
        {
            SavePayload p = MetaProgressService.Instance.Payload;
            string text = "[原型·存档] era=" + p.era + " depth=" + p.depth
                + " 资源(材料/图纸/货物/钱)=" + p.resMaterial + "/" + p.resBlueprint + "/" + p.resGoods + "/" + p.resMoney
                + " runs=" + p.runs + " clears=" + p.clears + " scenesCleared=" + p.scenesCleared
                + " 族人=" + (p.retainers == null ? 0 : p.retainers.Count)
                + " 建筑=" + (p.buildings == null ? 0 : p.buildings.Count)
                + " 家族树节点=" + (p.familyTreeNodes == null ? 0 : p.familyTreeNodes.Count);
            SetProtoStatus(_protoSaveStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "写入槽位 1", () =>
        {
            bool ok = SaveSlotService.Instance.Save(1, MetaProgressService.Instance.Payload);
            string text = "[原型·存档] 写槽位1 " + (ok ? "成功" : "失败");
            SetProtoStatus(_protoSaveStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "读回槽位 1 并打印", () =>
        {
            SaveSlotFile file;
            if (!SaveSlotService.Instance.TryLoad(1, out file) || file == null || file.payload == null)
            {
                string failText = "[原型·存档] 读槽位1 失败（空槽或损坏）";
                SetProtoStatus(_protoSaveStatus, failText);
                Debug.Log(failText);
                return;
            }

            SavePayload p = file.payload;
            string text = "[原型·存档] 读回槽位1：era=" + p.era + " depth=" + p.depth
                + " 资源(材料/图纸/货物/钱)=" + p.resMaterial + "/" + p.resBlueprint + "/" + p.resGoods + "/" + p.resMoney
                + " runs=" + p.runs + " clears=" + p.clears + " scenesCleared=" + p.scenesCleared
                + " version=" + p.version;
            SetProtoStatus(_protoSaveStatus, text);
            Debug.Log(text);
        });

        _protoSaveStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoSaveStatus, "槽位 1 路径：" + SaveSlotService.Instance.GetSlotPath(1));
    }

    private void BuildPrototypeMapPage(Transform page)
    {
        AddHint(page, "在当前 TestScene 里**就地**生成 RunMap 地图（不切场景、不走读条）。容器名固定 PrototypeRunMap。");

        _protoSceneIndexInput = CreateSceneIndexInput(page);
        if (_protoSceneIndexInput != null) _protoSceneIndexInput.SetTextWithoutNotify("1");

        AddActionButton(page, "在场景中生成第 N 场景（输入 1~3）", () =>
        {
            int sceneIndex = 1;
            string raw = _protoSceneIndexInput != null ? _protoSceneIndexInput.text : null;
            if (!int.TryParse(raw, out sceneIndex) || sceneIndex < 1 || sceneIndex > 3)
            {
                SetProtoStatus(_protoMapStatus, "场景号无效：请输入 1 ~ 3 的整数");
                return;
            }
            SpawnPrototypeMap(sceneIndex);
        });

        AddActionButton(page, "生成第 1 场景", () => SpawnPrototypeMap(1));
        AddActionButton(page, "生成第 2 场景", () => SpawnPrototypeMap(2));
        AddActionButton(page, "生成第 3 场景", () => SpawnPrototypeMap(3));

        AddActionButton(page, "清除已生成地图", () =>
        {
            PrototypeMapSpawner.Clear();
            SetProtoStatus(_protoMapStatus, "已清除：" + PrototypeMapSpawner.ContainerName + " 存在=" + PrototypeMapSpawner.IsSpawned);
            Debug.Log("[原型·地图] 已清除，IsSpawned=" + PrototypeMapSpawner.IsSpawned);
        });

        AddActionButton(page, "打印生成状态", () =>
        {
            string text = "[原型·地图] IsSpawned=" + PrototypeMapSpawner.IsSpawned
                + " 当前场景=" + SceneManager.GetActiveScene().name
                + " RunMap根=" + (RunMap.RunMapBootstrap.CurrentMapRoot == null ? "null" : RunMap.RunMapBootstrap.CurrentMapRoot.name)
                + " 阶段=" + RunMap.RunMapBootstrap.CurrentStageText;
            SetProtoStatus(_protoMapStatus, text);
            Debug.Log(text);
        });

        _protoMapStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoMapStatus, "点【生成第 1/2/3 场景】在**当前场景**就地出图。");
    }

    private void SpawnPrototypeMap(int sceneIndex)
    {
        GameObject container = PrototypeMapSpawner.Spawn(sceneIndex);
        string text = container == null
            ? "[原型·地图] 生成失败（看 Console 的 Warn/Error）"
            : "[原型·地图] 已生成 场景" + sceneIndex
              + " 容器=" + container.name
              + " 子节点=" + container.transform.childCount
              + " 当前场景=" + SceneManager.GetActiveScene().name;
        SetProtoStatus(_protoMapStatus, text);
        Debug.Log(text);
    }

    /// <summary>原型页的状态行（AddHint 不返回引用，这里要能反复改写）。</summary>
    private void SetProtoStatus(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }

    /// <summary>场景号输入框（1~3，数字，最多 1 位）。</summary>
    private TMP_InputField CreateSceneIndexInput(Transform page)
    {
        GameObject go = new GameObject("Input_ProtoSceneIndex", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(page, false);

        Image bg = go.GetComponent<Image>();
        bg.sprite = buttonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.10f, 0.12f, 0.16f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        Stretch(textRect);
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 24;
        text.color = LabelColor;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.textComponent = text;
        input.textViewport = rect;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 1;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = ActionButtonWidth;
        element.preferredHeight = ActionButtonHeight;
        element.flexibleWidth = 0f;
        return input;
    }



    // ══════════════════════ 原型：族人 / 道具 / 库存（W1a）══════════════════════

    private void BuildRetainerPage(Transform page)
    {
        AddHint(page, "W1a：可出战 = 未重伤 且 信心 ≥ 30；重伤 30 秒倒计时（只按运行中时间）。上限 8 人。");

        AddActionButton(page, "给 1 个随机族人（按 RecruitWeight）", () =>
        {
            RetainerSave r = RetainerService.Instance.RecruitRandom();
            string text = r == null ? "[族人] 招募失败（上限/无模板，看 Console）"
                : ("[族人] 招募 #" + r.id + " proto=" + r.protoId + " 品质=" + r.quality + " 信心=" + r.confidence);
            SetProtoStatus(_protoRetainerStatus, text + "\n" + RetainerService.Instance.DumpAll());
            Debug.Log(text);
        });

        AddActionButton(page, "打伤第 1 个族人（30s）", () =>
        {
            List<RetainerSave> all = RetainerService.Instance.GetAll();
            if (all.Count == 0) { SetProtoStatus(_protoRetainerStatus, "没有族人可打伤"); return; }
            RetainerService.Instance.Wound(all[0].id);
            SetProtoStatus(_protoRetainerStatus, RetainerService.Instance.DumpAll());
            Debug.Log("[族人] 打伤 #" + all[0].id + "\n" + RetainerService.Instance.DumpAll());
        });

        AddActionButton(page, "第 1 个族人信心 -40", () =>
        {
            List<RetainerSave> all = RetainerService.Instance.GetAll();
            if (all.Count == 0) { SetProtoStatus(_protoRetainerStatus, "没有族人"); return; }
            RetainerService.Instance.ChangeConfidence(all[0].id, -40);
            SetProtoStatus(_protoRetainerStatus, RetainerService.Instance.DumpAll());
            Debug.Log("[族人] 信心-40\n" + RetainerService.Instance.DumpAll());
        });

        AddActionButton(page, "快进 30 秒（Tick）", () =>
        {
            RetainerService.Instance.Tick(30f);
            SetProtoStatus(_protoRetainerStatus, RetainerService.Instance.DumpAll());
            Debug.Log("[族人] 快进 30s\n" + RetainerService.Instance.DumpAll());
        });

        AddActionButton(page, "打印全部族人状态", () =>
        {
            string text = RetainerService.Instance.DumpAll();
            SetProtoStatus(_protoRetainerStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "打开族人面板", () =>
        {
            if (UIManager.Instance == null) { Debug.LogError("[族人] UIManager 未初始化"); return; }
            UIManager.Instance.PushPanel(PanelType.RetainerPanel);
            ClosePanel();
        });

        _protoRetainerStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoRetainerStatus, RetainerService.Instance.DumpAll());
    }

    private void BuildItemGrantPage(Transform page)
    {
        AddHint(page, "资源是 4 个全局数字（不占格位）；道具占格位（种类数）。");

        AddActionButton(page, "物资 +10", () => { GrantResource(InventoryService.ResourceKind.Goods, 10u); });
        AddActionButton(page, "材料 +10", () => { GrantResource(InventoryService.ResourceKind.Material, 10u); });
        AddActionButton(page, "图纸 +10", () => { GrantResource(InventoryService.ResourceKind.Blueprint, 10u); });
        AddActionButton(page, "钱财 +10", () => { GrantResource(InventoryService.ResourceKind.Money, 10u); });

        AddActionButton(page, "给 1 个药品（itemId=1）", () =>
        {
            string reason;
            bool ok = InventoryService.Instance.AddItem(1u, 1u, out reason);
            string text = "[库存] 给药品 " + (ok ? "成功" : ("失败：" + reason)) + "\n" + InventoryService.Instance.DumpState();
            SetProtoStatus(_protoItemStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "给 10 个药品（叠加上限测试）", () =>
        {
            string reason;
            bool ok = InventoryService.Instance.AddItem(1u, 10u, out reason);
            string text = "[库存] 给药品×10 " + (ok ? "成功" : ("失败：" + reason)) + "\n" + InventoryService.Instance.DumpState();
            SetProtoStatus(_protoItemStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "塞满格位（塞 8 种）", () =>
        {
            for (uint id = 1; id <= 8; id++)
            {
                string reason;
                InventoryService.Instance.AddItem(id, 1u, out reason);
            }
            string dump = InventoryService.Instance.DumpState();
            SetProtoStatus(_protoItemStatus, "[库存] 塞格完成\n" + dump);
            Debug.Log("[库存] 塞格完成\n" + dump);
        });

        AddActionButton(page, "清空库存（资源不变）", () =>
        {
            InventoryService.Instance.ClearItems();
            string dump = InventoryService.Instance.DumpState();
            SetProtoStatus(_protoItemStatus, "[库存] 已清空\n" + dump);
            Debug.Log("[库存] 已清空\n" + dump);
        });

        _protoItemStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoItemStatus, InventoryService.Instance.DumpState());
    }

    private void BuildInventoryPage(Transform page)
    {
        AddHint(page, "格位 = 物品种类数上限（基础 8，Effect 可提升，原型硬上限 24）；同名叠加 99。");

        AddActionButton(page, "挂 1 个格位效果（Effect.Index=1）→ 总格位应变 10", () =>
        {
            InventoryService.Instance.ApplySlotEffects(new List<uint> { 1u });
            string text = "[库存] 已挂 Effect.Index=1（类型2 值2）→ 总格位 " + InventoryService.Instance.TotalSlots
                + "\n" + InventoryService.Instance.DumpState();
            SetProtoStatus(_protoInventoryStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "卸掉格位效果（总格位回 8）", () =>
        {
            InventoryService.Instance.ApplySlotEffects(new List<uint>());
            string text = "[库存] 已卸效果 → 总格位 " + InventoryService.Instance.TotalSlots;
            SetProtoStatus(_protoInventoryStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "打印库存状态", () =>
        {
            string text = InventoryService.Instance.DumpState();
            SetProtoStatus(_protoInventoryStatus, text);
            Debug.Log(text);
        });

        AddActionButton(page, "打开库存面板", () =>
        {
            if (UIManager.Instance == null) { Debug.LogError("[库存] UIManager 未初始化"); return; }
            UIManager.Instance.PushPanel(PanelType.InventoryPanel);
            ClosePanel();
        });

        _protoInventoryStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoInventoryStatus, InventoryService.Instance.DumpState());
    }

    private void GrantResource(InventoryService.ResourceKind kind, uint amount)
    {
        uint now = InventoryService.Instance.AddResource(kind, amount);
        string text = "[库存] " + InventoryService.ResourceDisplayName(kind) + " +" + amount + " → " + now;
        SetProtoStatus(_protoItemStatus, text + "\n" + InventoryService.Instance.DumpState());
        Debug.Log(text);
    }



    // ══════════════════════ 原型：村庄 / 家族树（W1b）══════════════════════

    private void BuildVillagePage(Transform page)
    {
        AddHint(page, "W1b 村庄：4 建筑 × 2 级；同时 1 条队列；取消返 50%；仅运行中计时。");
        AddActionButton(page, "打开村庄面板", () => { if (UIManager.Instance == null) return; UIManager.Instance.PushPanel(PanelType.VillagePanel); ClosePanel(); });
        AddActionButton(page, "给 100 各资源", () => { GrantAllResources(100u); });
        AddActionButton(page, "建造农田 2001", () => { string why; bool ok = VillageService.Instance.StartBuild(2001u, out why); Debug.Log("[村庄] 建造农田 " + (ok ? "成功" : ("失败：" + why))); SetProtoStatus(_protoVillageStatus, VillageService.Instance.DumpState()); });
        AddActionButton(page, "快进 30 秒", () => { VillageService.Instance.Tick(30f); Debug.Log("[村庄] 快进 30s\n" + VillageService.Instance.DumpState()); SetProtoStatus(_protoVillageStatus, VillageService.Instance.DumpState()); });
        AddActionButton(page, "一键建成（快进 999s）", () => { VillageService.Instance.Tick(999f); Debug.Log("[村庄] 一键建成\n" + VillageService.Instance.DumpState()); SetProtoStatus(_protoVillageStatus, VillageService.Instance.DumpState()); });
        AddActionButton(page, "收取农田（2001）", () => { uint amt; string why; bool ok = VillageService.Instance.Harvest(2001u, out amt, out why); Debug.Log("[村庄] 收取 " + (ok ? ("+" + amt) : ("失败：" + why))); SetProtoStatus(_protoVillageStatus, VillageService.Instance.DumpState()); });
        AddActionButton(page, "打印村庄状态", () => { string s = VillageService.Instance.DumpState(); Debug.Log(s); SetProtoStatus(_protoVillageStatus, s); });
        _protoVillageStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoVillageStatus, VillageService.Instance.DumpState());
    }

    private void BuildFamilyTreePage(Transform page)
    {
        AddHint(page, "W1b 家族树：5 板块 × 3 节点；点亮需前置已点亮 + 资源足够。");
        AddActionButton(page, "打开家族树面板", () => { if (UIManager.Instance == null) return; UIManager.Instance.PushPanel(PanelType.FamilyTreePanel); ClosePanel(); });
        AddActionButton(page, "给 100 各资源", () => { GrantAllResources(100u); });
        AddActionButton(page, "点亮第一个（101）", () => { string why; bool ok = FamilyTreeService.Instance.Unlock(101u, out why); Debug.Log("[家族树] 点亮101 " + (ok ? "成功" : ("失败：" + why))); SetProtoStatus(_protoFamilyTreeStatus, EffectAggregator.Instance.Snapshot.Dump()); });
        AddActionButton(page, "一键点亮全部（按前置顺序）", () => { GrantAllResources(9999u); int n = 0; var defs = FamilyTreeService.Instance.GetAll(); for (int i = 0; i < defs.Count; i++) { if (defs[i] == null) continue; string why; if (FamilyTreeService.Instance.Unlock(defs[i].Index, out why)) n++; } Debug.Log("[家族树] 一键点亮 " + n + " 个\n" + EffectAggregator.Instance.Snapshot.Dump()); SetProtoStatus(_protoFamilyTreeStatus, EffectAggregator.Instance.Snapshot.Dump()); });
        AddActionButton(page, "重置家族树", () => { FamilyTreeService.Instance.ResetAll(); SetProtoStatus(_protoFamilyTreeStatus, EffectAggregator.Instance.Snapshot.Dump()); });
        AddActionButton(page, "打印效果快照", () => { string s = EffectAggregator.Instance.Snapshot.Dump(); Debug.Log(s); SetProtoStatus(_protoFamilyTreeStatus, s); });
        _protoFamilyTreeStatus = CreateStatusLabel(page);
        SetProtoStatus(_protoFamilyTreeStatus, EffectAggregator.Instance.Snapshot.Dump());
    }

    private void GrantAllResources(uint amount)
    {
        InventoryService.Instance.AddResource(InventoryService.ResourceKind.Goods, amount);
        InventoryService.Instance.AddResource(InventoryService.ResourceKind.Material, amount);
        InventoryService.Instance.AddResource(InventoryService.ResourceKind.Blueprint, amount);
        InventoryService.Instance.AddResource(InventoryService.ResourceKind.Money, amount);
        Debug.Log("[原型] 已给四种资源各 +" + amount);
    }


    // ══════════════════════ 运行时滚动容器（T35）══════════════════════
    //
    // 现状：MainList / ContentArea 在场景里都**没有 ScrollRect** → 元素一多就超框
    //（家族树页 15 节点最明显）。这里**不改场景资产**，运行时给它们各套一层
    // ScrollView(Viewport + RectMask2D + Content[VL+ContentSizeFitter])，
    // 并把 `_mainList` / `_contentArea` 直接改指向新的 **Content** ——
    // 这样所有既有构建代码（CreateButton/CreatePage）**一行都不用改**。

    /// <summary>绑定节点后，给主列表与页面区各建/复用滚动容器。</summary>
    private void SetupScrollViews()
    {
        if (_mainList != null)
            _mainList = WrapInScrollView(_mainList, "主列表 MainList", out _mainScroll);

        if (_contentArea != null)
            _contentArea = WrapInScrollView(_contentArea, "页面区 ContentArea", out _contentScroll);

        //分类按钮按视口宽度**拉伸**（否则按钮比视口宽 → 横向溢出被 RectMask2D 裁剪）
        if (_mainList != null)
        {
            VerticalLayoutGroup mainLayout = _mainList.GetComponent<VerticalLayoutGroup>();
            if (mainLayout != null)
            {
                mainLayout.childControlWidth = false;
                mainLayout.childForceExpandWidth = true;
            }
        }

        ValidateSubListScroll();
    }

    /// <summary>
    /// **共用的**滚动容器构建：在 host 的父节点下建 `xxx_ScrollView → Viewport(RectMask2D) → Content(VL+CSF)`，
    /// 复用 host 的矩形；原 host **保留但停用**（不删除场景里的东西）。已存在 ScrollRect 则直接复用。
    /// 返回可放内容的 `Content`。
    /// </summary>
    private Transform WrapInScrollView(Transform host, string label, out ScrollRect scrollRect)
    {
        //已有可用滚动容器 → 直接复用它的 content
        ScrollRect existing = host.GetComponentInChildren<ScrollRect>(true);
        if (existing != null && existing.content != null)
        {
            scrollRect = existing;
            ChaosLog.Info(LogChannel.UI, "[GM] " + label + " 已有可用的 ScrollRect，直接复用");
            return existing.content;
        }

        Transform parent = host.parent;
        var hostRect = host as RectTransform;

        //① ScrollView
        var scrollGo = new GameObject(host.name + "_ScrollView", typeof(RectTransform), typeof(ScrollRect));
        var scrollRectTransform = (RectTransform)scrollGo.transform;
        scrollRectTransform.SetParent(parent, false);
        if (hostRect != null)
        {
            scrollRectTransform.anchorMin = hostRect.anchorMin;
            scrollRectTransform.anchorMax = hostRect.anchorMax;
            scrollRectTransform.pivot = hostRect.pivot;
            scrollRectTransform.anchoredPosition = hostRect.anchoredPosition;
            scrollRectTransform.sizeDelta = hostRect.sizeDelta;
        }
        else
        {
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
        }

        //② Viewport（RectMask2D 裁剪 + 透明 Image 吃射线）
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        var viewportRect = (RectTransform)viewportGo.transform;
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0f, 1f);
        Image viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;

        //③ Content（竖直布局 + 高度自适应）
        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = (RectTransform)contentGo.transform;
        contentRect.SetParent(viewportRect, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        //④ ScrollRect 参数
        ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
        sr.viewport = viewportRect;
        sr.content = contentRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        sr.inertia = true;
        sr.decelerationRate = 0.135f;

        //⑤ 原 host **保留但停用**（不删场景对象）；已有子物体整体挪进 Content 以保持兼容
        if (host.childCount > 0)
        {
            for (int i = host.childCount - 1; i >= 0; i--)
            {
                Transform child = host.GetChild(i);
                child.SetParent(contentRect, false);
            }
        }
        host.gameObject.SetActive(false);

        scrollRect = sr;
        ChaosLog.Info(LogChannel.UI, "[GM] 已为 " + label + " 运行时构建 ScrollView（原节点保留并停用）");
        return contentRect;
    }

    /// <summary>副列表（场景里已有 ScrollRect）的**防守性校验**：缺什么就在运行时补什么并 Warn。</summary>
    private void ValidateSubListScroll()
    {
        if (_subListContent == null) return;

        ScrollRect sr = _subListContent.GetComponentInParent<ScrollRect>(true);
        if (sr == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[GM] 副列表 SubList 找不到 ScrollRect（场景里缺，未自动补 —— 版块多了可能超框）");
            return;
        }

        if (sr.content == null)
        {
            sr.content = _subListContent as RectTransform;
            ChaosLog.Warn(LogChannel.UI, "[GM] 场景里 SubList 的 ScrollRect.content 未赋值，已运行时补上");
        }

        Transform viewport = sr.viewport;
        if (viewport == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[GM] 场景里 SubList 的 ScrollRect.viewport 未赋值（无法自动补 Viewport，跳过）");
        }
        else if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
            ChaosLog.Warn(LogChannel.UI, "[GM] 场景里 SubList 的 Viewport 缺遮罩，已运行时补 RectMask2D");
        }

        if (_subListContent.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup vl = _subListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;
            vl.spacing = 8f;
            ChaosLog.Warn(LogChannel.UI, "[GM] 场景里 SubList Content 缺 VerticalLayoutGroup，已运行时补上");
        }

        if (_subListContent.GetComponent<ContentSizeFitter>() == null)
        {
            ContentSizeFitter fitter = _subListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ChaosLog.Warn(LogChannel.UI, "[GM] 场景里 SubList Content 缺 ContentSizeFitter，已运行时补上");
        }
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

    // ══════════════════════ 局内地图生成（GM 原型入口） ══════════════════════

    /// <summary>
    /// GM 页：输入本局种子（RunSeed）+ 两个按钮（随机种子 / 按当前种子生成）。
    ///
    /// 这里**完全走正式流程**：登记本局种子 → 切到读条场景 → 读条期生成地图 → 激活目标场景，
    /// 进到 WorldScene 看到的就是真实生成结果（和将来"开始游戏"走的是同一条链路）。
    /// 正式流程里同一个 RunSeed 会派生出一局内三个**互不相同**的场景种子（见状态行）。
    /// </summary>
    private void BuildRunMapPage(Transform page)
    {
        _runMapSeedInput = CreateSeedInput(page);
        if (_runMapSeedInput != null) _runMapSeedInput.SetTextWithoutNotify(_runMapSeed.ToString());

        AddActionButton(page, "随机生成种子", RandomizeRunMapSeed);
        AddActionButton(page, "按当前种子生成", GenerateRunMapFromSeed);

        _runMapStatus = CreateStatusLabel(page);
        SetRunMapStatus("输入本局种子（RunSeed），或先随机一个。点生成会走读条流程进入 " + RunMapWorldScene + "（读条期生成地图）。");
    }

    /// <summary>种子输入框（数字，最多 10 位 = uint 上限）。</summary>
    private TMP_InputField CreateSeedInput(Transform page)
    {
        GameObject go = new GameObject("Input_RunSeed", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(page, false);

        Image bg = go.GetComponent<Image>();
        bg.sprite = buttonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.10f, 0.12f, 0.16f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        Stretch(textRect);
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 24;
        text.color = LabelColor;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.textComponent = text;
        input.textViewport = rect;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;   // 种子是数字，挡掉非法输入
        input.characterLimit = 10;                                      // uint 最多 10 位

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = ActionButtonWidth;
        element.preferredHeight = ActionButtonHeight;
        element.flexibleWidth = 0f;
        return input;
    }

    /// <summary>可变状态行（AddHint 不返回引用，这里要能反复改写）。</summary>
    private TextMeshProUGUI CreateStatusLabel(Transform page)
    {
        GameObject go = new GameObject("RunMapStatus", typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(page, false);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 20;
        label.color = HintColor;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = true;
        label.raycastTarget = false;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = HintWidth;
        element.preferredHeight = 120f;
        return label;
    }

    private void RandomizeRunMapSeed()
    {
        // 1 ~ int.MaxValue：避开 0（RunMapRng 把 0 当退化值换成固定常量）
        uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        _runMapSeed = seed;
        if (_runMapSeedInput != null) _runMapSeedInput.SetTextWithoutNotify(seed.ToString());
        SetRunMapStatus("已随机得到本局种子 " + seed + "。点【按当前种子生成】出图。");
    }

    private void GenerateRunMapFromSeed()
    {
        uint seed;
        string raw = _runMapSeedInput != null ? _runMapSeedInput.text : null;
        if (string.IsNullOrEmpty(raw) || !uint.TryParse(raw, out seed) || seed == 0)
        {
            SetRunMapStatus("种子无效：请输入 1 ~ 4294967295 的整数（0 会被当成退化值）。");
            return;
        }
        _runMapSeed = seed;

        // 模拟"开始游戏 → 读档 → 读条 → 进世界场景"：
        //   ① 登记本局种子（读条期据此派生场景种子并生成地图）
        //   ② 设定目标场景，③ 切到读条场景 —— 与 SLPanel 读档后的走法一致
        RunMap.RunMapBootstrap.RequestForScene(RunMapWorldScene, seed, RunMapSceneIndex);

        if (GameSession.Instance == null)
        {
            SetRunMapStatus("GameSession 不存在，走不了读条流程。");
            return;
        }
        GameSession.Instance.SetTargetScene(RunMapWorldScene);

        // 读条场景是"单开切换"，而 GM 面板是 DontDestroyOnLoad 的：不收掉就会盖在地图上，看不清实图
        SetRunMapStatus("按种子 " + seed + " 走读条流程进入 " + RunMapWorldScene + "（生成阶段文案会显示在读条上）…");
        ClosePanel();
        ScenesLoadManager.Instance.LoadScene(RunMapLoadingScene, () => { });
    }

    /// <summary>
    /// GM 页：返回局外主界面（`EnterScene`）。
    ///
    /// 与"局内生成"走同一条链路：设目标场景 → 收掉 GM 面板 → 切到读条场景；
    /// 读条结束后 `Entry` 按 `GameSession.HasSession` 把根面板设成局外主界面。
    /// </summary>
    private void ReturnToWorldMap()
    {
        if (GameSession.Instance == null)
        {
            ChaosLog.Error(LogChannel.UI, "GameSession 不存在，无法返回局外");
            return;
        }

        GameSession.Instance.SetTargetScene(ReturnWorldMapScene);
        ChaosLog.Info(LogChannel.UI, "[GM/返回局外] 目标场景 " + ReturnWorldMapScene + "，走读条切换");

        //与 GM 生成地图同理：GM 面板是常驻的，不收掉会盖在局外 UI 上
        ClosePanel();
        ScenesLoadManager.Instance.LoadScene(RunMapLoadingScene, () => { });
    }

    private void SetRunMapStatus(string text)
    {
        if (_runMapStatus != null) _runMapStatus.text = text;
        ChaosLog.Info(LogChannel.UI, "[GM/局内生成] " + text);
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
