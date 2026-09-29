using ChaosDebug;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板：主选列表（左侧分类）与副设置列表（右侧页面）的容器，外加底部按钮条。
///
/// ══════════════════════ 它是"容器"，不是"设置项" ══════════════════════
/// 本类【不碰任何设置值】。设置项的值全部由各页（SettingsPageBase 的子类）负责，
/// 这里只做四件事：切页、把玩家意图转给 SettingsManager、刷新「应用」按钮、退出时处理未应用的改动。
/// 这样加一个设置项不需要动这个文件，加一个分类才需要（并在 SettingsEnums 里补枚举与文案）。
///
/// ══════════════ 节点路径是硬约定，改 prefab 时别改名 ══════════════
/// 主选按钮：MainList/Btn_Gameplay、Btn_Keybind、Btn_Graphics、Btn_Audio
/// 页面节点：ContentArea/Page_Gameplay、Page_Keybind、Page_Graphics、Page_Audio
/// 底部按钮：BottomBar/ApplyBtn、ResetBtn、ReturnBtn
/// 确认浮层：ConfirmGroup/ConfirmApplyBtn、ConfirmDiscardBtn、ConfirmCancelBtn（可整组缺省，见下）
///
/// 下标一律对应 SettingCategory 的枚举值（Gameplay=0 / Keybind=1 / Graphics=2 / Audio=3），
/// 所以上面两张名字表必须与 SettingEnums 里的枚举顺序一致 —— Awake 里做了长度校验。
///
/// ══════════════ 页面的激活与绑定顺序（踩过的坑）══════════════
/// 各行的控件引用是在 Awake 里找的，而 Awake 只在物体被激活时才跑。
/// 所以切页时必须【先 SetActive(true) 再 EnsureBound()】，反过来会在未激活状态下绑定，
/// 结果取值域没设、回调没接上，但一声不响。SettingsPageBase.EnsureBound 会检查并报错。
///
/// ══════════════ 退出时的确认浮层是可选的 ══════════════
/// 底部只有「应用」「恢复默认」「返回」三个按钮（需求第 4 项），
/// "有未应用的改动时先问一句"是额外的保护：没有它的话，玩家改了半天直接按返回，
/// 改动会静默丢弃 —— 而他以为已经生效了。
/// 若 prefab 里没摆 ConfirmGroup，这里会退化成"直接丢弃 + 一条警告日志"，不会报错卡住。
/// </summary>
public class SettingPanel : BasePanel
{
    /// <summary>主选按钮的节点名，下标 = SettingCategory。</summary>
    private static readonly string[] TabNames = { "Btn_Gameplay", "Btn_Keybind", "Btn_Graphics", "Btn_Audio" };

    /// <summary>页面节点的名称，下标 = SettingCategory。</summary>
    private static readonly string[] PageNames = { "Page_Gameplay", "Page_Keybind", "Page_Graphics", "Page_Audio" };

    private readonly Button[] _tabButtons = new Button[TabNames.Length];

    /// <summary>每个主选按钮下的选中指示物（子节点 "Selected"），没有就只是看不到高亮，不算错误。</summary>
    private readonly GameObject[] _tabSelectedMarkers = new GameObject[TabNames.Length];

    private readonly SettingsPageBase[] _pages = new SettingsPageBase[TabNames.Length];

    private Button _applyBtn;
    private Button _resetBtn;
    private Button _returnBtn;

    private GameObject _confirmGroup;
    private Button _confirmApplyBtn;
    private Button _confirmDiscardBtn;
    private Button _confirmCancelBtn;

    /// <summary>当前显示的分类，下标 = SettingCategory。</summary>
    private int _current = (int)SettingCategory.Gameplay;

    private bool _listeningPendingChanged;

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindTabsAndPages();
        FindBottomBar();
        FindConfirmGroup();

        //必须在 Show 之前订阅：面板每次显示都要按当前存档重置一遍按钮状态
        //（订阅放 OnEnable 是因为面板被关闭时物体失活，这里跟着自动退订，不会漏）
        if (TabNames.Length != SettingsLabels.Category.Length)
        {
            ChaosLog.Error(LogChannel.UI,
                "SettingPanel 的主选按钮名字表有 " + TabNames.Length + " 项，而 SettingCategory 有 " +
                SettingsLabels.Category.Length + " 项。两张表必须一一对应，否则会切错页。");
        }
    }

    private void OnEnable()
    {
        if (!_listeningPendingChanged)
        {
            SettingsManager.Instance.OnPendingChanged += RefreshApplyButton;
            _listeningPendingChanged = true;
        }
    }

    private void OnDisable()
    {
        if (_listeningPendingChanged)
        {
            SettingsManager.Instance.OnPendingChanged -= RefreshApplyButton;
            _listeningPendingChanged = false;
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();

        //进入编辑态：暂存区 = 已应用的一份。之后所有控件的改动都只写暂存区
        SettingsManager.Instance.BeginEdit();

        HideConfirm();

        //切到当前分类并强制绑定。这里不能只写"if (index != _current)" ——
        //面板可能是第二次打开，_current 恰好没变，但页面在关闭时已被失活，
        //不重新激活就看不到内容
        ShowCategory(_current);

        RefreshApplyButton();
    }

    public override void OnExit()
    {
        base.OnExit();

        //退场动画期间面板还活着，玩家理论上还能点到按钮，所以这里先把确认浮层收掉，
        //避免它跟着面板一起退场时留下"下次打开就带着浮层"的错觉
        HideConfirm();

        //兜底：面板也可能不是从「返回」退出的（别的流程直接 PopPanel）。
        //退场动画期间面板仍然激活、行仍然可点，这里再收一次等待中的重绑，
        //保证"面板关掉之后不会有键被偷偷写进资产"。没有在跑时是空操作
        CancelPendingEdit();
    }

    /// <summary>把"收掉未结束的编辑"转给当前页，由它一路传到开着的子页。</summary>
    private void CancelPendingEdit()
    {
        SettingsPageBase page = _pages[_current];
        if (page != null) page.CancelPendingEdit();
    }

    // ══════════════════ 查找节点 ══════════════════

    private void FindTabsAndPages()
    {
        Transform mainList = transform.Find("MainList");
        Transform contentArea = transform.Find("ContentArea");

        if (mainList == null) ChaosLog.Error(LogChannel.UI, "SettingPanel 下找不到 MainList 节点，主选列表不可用");
        if (contentArea == null) ChaosLog.Error(LogChannel.UI, "SettingPanel 下找不到 ContentArea 节点，副设置列表不可用");

        for (int i = 0; i < TabNames.Length; i++)
        {
            if (mainList != null)
            {
                Transform tab = mainList.Find(TabNames[i]);
                if (tab != null)
                {
                    _tabButtons[i] = tab.GetComponent<Button>();
                    Transform marker = tab.Find("Selected");
                    _tabSelectedMarkers[i] = marker != null ? marker.gameObject : null;
                }
                else
                {
                    ChaosLog.Error(LogChannel.UI, "MainList 下找不到 " + TabNames[i] + "，这个分类将无法切换");
                }
            }

            if (contentArea != null)
            {
                Transform page = contentArea.Find(PageNames[i]);
                if (page != null)
                {
                    _pages[i] = page.GetComponent<SettingsPageBase>();
                    if (_pages[i] == null)
                    {
                        ChaosLog.Error(LogChannel.UI,
                            PageNames[i] + " 上没有挂 SettingsPageBase 的子类组件，" +
                            "这一页的设置项不会被绑定");
                    }
                }
                else
                {
                    ChaosLog.Error(LogChannel.UI, "ContentArea 下找不到 " + PageNames[i]);
                }
            }
        }

        //闭包捕获：这里不能直接用 i，否则所有按钮都会切到最后一个分类
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (_tabButtons[i] == null) continue;

            int index = i;//局部变量参与闭包才是"每个按钮一个值"
            _tabButtons[i].onClick.AddListener(() => ShowCategory(index));
        }
    }

    private void FindBottomBar()
    {
        Transform bottomBar = transform.Find("BottomBar");
        if (bottomBar == null)
        {
            ChaosLog.Error(LogChannel.UI, "SettingPanel 下找不到 BottomBar 节点，应用/恢复默认/返回按钮都不可用");
            return;
        }

        _applyBtn = FindButton(bottomBar, "ApplyBtn");
        _resetBtn = FindButton(bottomBar, "ResetBtn");
        _returnBtn = FindButton(bottomBar, "ReturnBtn");

        if (_applyBtn != null) _applyBtn.onClick.AddListener(OnApplyClicked);
        if (_resetBtn != null) _resetBtn.onClick.AddListener(OnResetClicked);
        if (_returnBtn != null) _returnBtn.onClick.AddListener(OnReturnClicked);
    }

    private void FindConfirmGroup()
    {
        Transform group = transform.Find("ConfirmGroup");
        if (group == null)
        {
            ChaosLog.Warn(LogChannel.UI,
                "SettingPanel 下没有 ConfirmGroup 节点：返回时若有未应用的改动会【直接丢弃】，不会询问玩家。" +
                "要开启询问就摆一个 ConfirmGroup，内含 ConfirmText 与 " +
                "ConfirmApplyBtn / ConfirmDiscardBtn / ConfirmCancelBtn");
            return;
        }

        _confirmGroup = group.gameObject;
        _confirmApplyBtn = FindButton(group, "ConfirmApplyBtn");
        _confirmDiscardBtn = FindButton(group, "ConfirmDiscardBtn");
        _confirmCancelBtn = FindButton(group, "ConfirmCancelBtn");

        if (_confirmApplyBtn != null) _confirmApplyBtn.onClick.AddListener(OnConfirmApplyClicked);
        if (_confirmDiscardBtn != null) _confirmDiscardBtn.onClick.AddListener(OnConfirmDiscardClicked);
        if (_confirmCancelBtn != null) _confirmCancelBtn.onClick.AddListener(OnConfirmCancelClicked);

        _confirmGroup.SetActive(false);
    }

    private static Button FindButton(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            ChaosLog.Error(LogChannel.UI, parent.name + " 下找不到 " + name + "，这个按钮不可用");
            return null;
        }

        Button button = child.GetComponent<Button>();
        if (button == null) ChaosLog.Error(LogChannel.UI, parent.name + "/" + name + " 上没有 Button 组件");
        return button;
    }

    // ══════════════════ 切页 ══════════════════

    /// <summary>显示指定分类的页面。重复调用同一个分类是安全的（会重新激活并刷新）。</summary>
    private void ShowCategory(int index)
    {
        if (index < 0 || index >= _pages.Length) return;

        _current = index;

        for (int i = 0; i < _pages.Length; i++)
        {
            SettingsPageBase page = _pages[i];
            bool visible = (i == index);

            if (page == null)
            {
                if (visible)
                {
                    ChaosLog.Error(LogChannel.UI,
                        "分类 '" + SettingsLabels.CategoryName(i) + "' 没有对应的页面节点，" +
                        "点它只会看到空白");
                }
                continue;
            }

            //先收掉可能开着的子页：下面 SetActive(true) 会把本页点亮，
            //如果它正停在子页上（本页是隐藏的），不收子页就会出现"一级页和二级页同屏"。
            //对没开子页的页是空操作
            page.CollapseSubPage();

            page.gameObject.SetActive(visible);
            if (!visible) continue;

            //先激活再绑定：Awake 要在绑定之前跑过，否则行控件找不到自己的子节点
            page.EnsureBound();
            page.RefreshAll();
        }

        for (int i = 0; i < _tabSelectedMarkers.Length; i++)
        {
            if (_tabSelectedMarkers[i] != null)
            {
                _tabSelectedMarkers[i].SetActive(i == index);
            }
        }

        //恢复默认按钮跟着换分类：它作用于当前显示的这一页
        if (_resetBtn != null) _resetBtn.interactable = (_pages[index] != null);

        ChaosLog.Info(LogChannel.UI, "设置面板切换到 '" + SettingsLabels.CategoryName(index) + "'");
    }

    // ══════════════════ 底部按钮 ══════════════════

    private void OnApplyClicked()
    {
        //先收掉还没结束的编辑（现在是"等玩家按键"）。不先收的话，提交之后那次重绑仍在采集输入，
        //玩家随后按下的任意一个键都会写进一个他以为已经提交完的数据里
        CancelPendingEdit();

        SettingsManager.Instance.CommitEdit();

        //应用后再刷一次当前页：分辨率这类设置可能会被引擎夹到实际可用的值上，
        //（比如窗口模式下引擎强制改回桌面分辨率）刷新能让界面显示的就是真实生效的值
        SettingsPageBase page = _pages[_current];
        if (page != null) page.RefreshAll();

        HideConfirm();
        RefreshApplyButton();
    }

    private void OnResetClicked()
    {
        //同上：恢复默认会把按键覆盖整体清掉，一个还在等待中的重绑随后写下来的键
        //会和"已恢复默认"的界面显示对不上
        CancelPendingEdit();

        //只恢复当前分类，别的分类不动 —— 玩家在"画面"页点恢复默认，
        //不该把辛苦调好的按键一起清掉
        SettingsManager.Instance.ResetPendingSection((SettingCategory)_current);

        SettingsPageBase page = _pages[_current];
        if (page != null) page.RefreshAll();

        RefreshApplyButton();
    }

    private void OnReturnClicked()
    {
        //两条分支都是"玩家想走"：一条回一级界面、一条要退面板。两条都不能留着
        //一个还在等按键的重绑 —— 他按返回之后按下的第一个键会被当成改键写进资产
        CancelPendingEdit();

        //停在二级界面时，「返回」先回一级界面，而不是直接退面板 ——
        //玩家在改键页里按返回，期望的是"回到按键设置"，不是"关掉整个设置面板"。
        //这一级跳转不弹确认浮层：改动还在暂存区里，没被丢弃，没必要问
        SettingsPageBase page = _pages[_current];
        if (page != null && page.IsSubPageOpen)
        {
            page.CloseSubPage();
            RefreshApplyButton();
            return;
        }

        if (SettingsManager.Instance.HasPendingChanges)
        {
            ShowConfirm();
            return;
        }

        OnConfirmDiscardClicked();//没有未应用的改动，直接走"放弃"这条路径（它会关面板）
    }

    private void RefreshApplyButton()
    {
        if (_applyBtn == null) return;

        //「应用」只在真的有差异时可点。一直可点会让玩家以为点了就一定有效果，
        //而实际上没改任何东西时点它只是白写一次文件
        _applyBtn.interactable = SettingsManager.Instance.HasPendingChanges;
    }

    // ══════════════════ 返回确认 ══════════════════

    private void ShowConfirm()
    {
        if (_confirmGroup == null)
        {
            //没有确认浮层时的退路：直接丢弃并关面板。这条警告是让开发者意识到
            //"玩家的改动被静默吞掉了"，而不是让它悄悄发生
            ChaosLog.Warn(LogChannel.UI,
                "有未应用的设置改动，但 prefab 里没有 ConfirmGroup，改动已被丢弃");
            OnConfirmDiscardClicked();
            return;
        }

        _confirmGroup.SetActive(true);
    }

    private void HideConfirm()
    {
        if (_confirmGroup != null) _confirmGroup.SetActive(false);
    }

    /// <summary>「应用并退出」：先提交再关面板。</summary>
    private void OnConfirmApplyClicked()
    {
        SettingsManager.Instance.CommitEdit();
        ClosePanel();
    }

    /// <summary>
    /// 「放弃改动」/ 无改动时直接退出。
    /// 必须调 RevertEdit：按键重绑定是直接写在 InputActionAsset 上的，
    /// 不像别的设置只躺在数据里，不回滚就会"没点应用但键位已经变了"。
    /// </summary>
    private void OnConfirmDiscardClicked()
    {
        SettingsManager.Instance.RevertEdit();
        ClosePanel();
    }

    private void OnConfirmCancelClicked()
    {
        HideConfirm();
    }

    private void ClosePanel()
    {
        HideConfirm();
        UIManager.Instance.PopPanel();
    }
}
