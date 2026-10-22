using ChaosDebug;
using UnityEngine;
using UnityEngine.UI;
using Chaos.MVP.Model.Settings;
using Chaos.MVP.Presenter.Settings;

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
public class SettingPanel : BasePanel, ISettingsView
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

    private SettingsEditingModel _model;
    private SettingsPresenter _presenter;
    private bool _hasRendered;
    public event System.Action<SettingsIntent> Intent;
    public bool CanShowConfirmation => _confirmGroup != null;
    public bool IsSubPageOpen => _pages[_current] != null && _pages[_current].IsSubPageOpen;

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindTabsAndPages();
        FindBottomBar();
        FindConfirmGroup();

        if (TabNames.Length != SettingsLabels.Category.Length)
        {
            ChaosLog.Error(LogChannel.UI,
                "SettingPanel 的主选按钮名字表有 " + TabNames.Length + " 项，而 SettingCategory 有 " +
                SettingsLabels.Category.Length + " 项。两张表必须一一对应，否则会切错页。");
        }
    }

    public override void OnEnter()
    {
        _presenter?.Dispose();
        if (_model == null || _model.Completed) _model = new SettingsEditingModel { Category = _current };
        _presenter = new SettingsPresenter(_model);
        _presenter.Attach(this);
    }

    public override void OnExit()
    {
        HideConfirm();
        CancelPendingEdit();
        _presenter?.Dispose();
        _presenter = null;
        bool covered = uiManager != null && uiManager.panelStack.Contains(this);
        if (_model != null && (_model.Completed || !covered))
        {
            if (_pages[_current] != null) _pages[_current].CollapseSubPage();
            _model.Dispose();
            _model = null;
        }
    }

    private void OnDestroy()
    {
        OnExit();
        _model?.Dispose();
        _model = null;
    }

    public void BindEditing(SettingsPresenter presenter)
    {
        SettingsPageBase[] pages = GetComponentsInChildren<SettingsPageBase>(true);
        for (int i = 0; i < pages.Length; i++) pages[i].BindEditing(presenter);
    }

    public void Render(SettingsState state)
    {
        if (!_hasRendered || state.Category != _current || (_pages[_current] != null
            && !_pages[_current].gameObject.activeSelf && !_pages[_current].IsSubPageOpen))
        {
            _hasRendered = true;
            ShowCategory(state.Category);
        }
        else if (state.RefreshPage && _pages[_current] != null) _pages[_current].RefreshAll();
        if (_applyBtn != null) _applyBtn.interactable = state.CanApply;
        if (_confirmGroup != null) _confirmGroup.SetActive(state.Confirmation);
    }

    public void CloseSubPage() { if (_pages[_current] != null) _pages[_current].CloseSubPage(); }
    /// <summary>把"收掉未结束的编辑"转给当前页，由它一路传到开着的子页。</summary>
    public void CancelPendingEdit()
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
            _tabButtons[i].onClick.AddListener(() => Intent?.Invoke(new SettingsIntent(SettingsAction.Category, index)));
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

    private void OnApplyClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.Apply)); }
    private void OnResetClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.Reset)); }
    private void OnReturnClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.Return)); }
    private void OnConfirmApplyClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.ConfirmApply)); }
    private void OnConfirmDiscardClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.ConfirmDiscard)); }
    private void OnConfirmCancelClicked() { Intent?.Invoke(new SettingsIntent(SettingsAction.ConfirmCancel)); }
    private void HideConfirm() { if (_confirmGroup != null) _confirmGroup.SetActive(false); }
}
