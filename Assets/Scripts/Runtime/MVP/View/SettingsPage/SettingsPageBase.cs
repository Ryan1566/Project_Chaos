using System;
using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using UnityEngine;

/// <summary>
/// 副设置列表里"一页"的基类：把这一页下的行控件与 SettingsData 的字段接起来。
///
/// ══════════════════════ 数据流 ══════════════════════
/// 行（SettingRow_*）只显示、只采集输入，不知道字段；
/// 页在 OnBind() 里用 BindSlider / BindSelector / BindToggle / BindButton 声明"这一行改哪个字段"。
/// 于是"id → 字段"的映射全项目只有这几处，加设置项时改一页即可。
///
/// 玩家改动 → 行回调 → 页写进 SettingsManager.Instance.Pending（暂存区）
///          → NotifyPendingChanged() 让「应用」按钮亮起来。
/// 数据改回去 → 面板调 RefreshAll() → 页把 Pending 的值灌回各控件（不触发回调）。
///
/// ══════════ 为什么刷新只用 SetValueWithoutNotify ══════════
/// 回填控件若走正常赋值会派发 onValueChanged，而那是"玩家改了值"的信号 ——
/// 就会变成 刷新 → 回调 → 写数据 → 通知刷新 → 回调… 的自激循环。
/// 各行的 SetXxxWithoutNotify 已经把这一点封在里面了。
///
/// ══════════════════════ 绑定时机（重要）══════════════════════
/// 页是"用到才绑"：第一次被激活显示时由面板调 EnsureBound()。
/// 必须【先激活再绑定】，因为各行的控件引用是在 Awake 里找的，
/// 未激活的物体 Awake 不会跑 —— 那时绑定会静默失效（取值域没设、回调没接上）。
/// EnsureBound 里对此做了检查并会明确报错，避免这个坑静默发生。
/// </summary>
public abstract class SettingsPageBase : MonoBehaviour
{
    /// <summary>每个刷新动作 = 把 Pending 的某个字段灌回某个控件。</summary>
    private readonly List<Action<SettingsData>> _refreshers = new List<Action<SettingsData>>();

    /// <summary>已经被 OnBind 认领过的 settingId，用于校验"摆了行但没绑"。</summary>
    private readonly HashSet<string> _boundIds = new HashSet<string>();

    private bool _bound;

    // ══════════════════════ 二级界面 ══════════════════════
    // 一页可以再挂一个"二级界面"（子页）来放不常改、或者需要独立空间的东西，
    // 按键页就是这么把"设备选择 + 改键"从一级页里拆出去的。
    //
    // ⚠ 子页必须是本页节点的【兄弟】，不能摆在本页子树里：
    //   FindRow / ValidateRows 用 GetComponentsInChildren(true) 收集行，
    //   嵌进来会让父页把子页的行也收进来 —— 表现是一串"这一行没有被 OnBind 绑定"的假警告，
    //   而且 settingId 重名的行会有一半永远找不到。
    //   所以子页挂在与 Page_X 平级的另一页节点上，由面板与本页协作控制显隐。

    /// <summary>当前开着的子页；null = 停在本页。</summary>
    private SettingsPageBase _openSubPage;

    /// <summary>子页请求"回到上一级"。父页在 OnBind 里接到 CloseSubPage 即可。</summary>
    public Action OnBackRequested;

    /// <summary>本页当前是不是停在子页上。面板的底部「返回」用它决定"上一级"还是"退面板"。</summary>
    public bool IsSubPageOpen { get { return _openSubPage != null; } }

    /// <summary>在本页的兄弟节点里找子页。找不到会报错并返回 null（调用方要判空）。</summary>
    protected SettingsPageBase FindSubPage(string nodeName)
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            ChaosLog.Error(LogChannel.UI, GetType().Name + " 没有父节点，找不到子页 " + nodeName);
            return null;
        }

        Transform node = parent.Find(nodeName);
        if (node == null)
        {
            ChaosLog.Error(LogChannel.UI,
                GetType().Name + " 的同级里找不到子页节点 '" + nodeName + "'。" +
                "子页必须与各 Page_X 平级摆在 ContentArea 下，不能塞进本页子树。");
            return null;
        }

        SettingsPageBase sub = node.GetComponent<SettingsPageBase>();
        if (sub == null)
        {
            ChaosLog.Error(LogChannel.UI, nodeName + " 上没有挂 SettingsPageBase 的子类组件");
        }
        return sub;
    }

    /// <summary>打开子页：隐藏本页、显示子页。</summary>
    protected void ShowSubPage(SettingsPageBase sub)
    {
        if (sub == null || _openSubPage == sub) return;

        CollapseSubPage();
        _openSubPage = sub;

        //必须先激活再绑定：行控件的 Awake 要在 OnBind 之前跑过（理由见 EnsureBound 的注释）。
        //EnsureBound 对已绑过的页会直接返回、不再刷新，所以后面补一次 RefreshAll
        sub.gameObject.SetActive(true);
        sub.EnsureBound();
        sub.RefreshAll();

        gameObject.SetActive(false);
    }

    /// <summary>收掉子页但不重新激活本页（面板切分类时用：紧接着它会自行决定本页的显隐）。</summary>
    public void CollapseSubPage()
    {
        if (_openSubPage == null) return;
        _openSubPage.gameObject.SetActive(false);
        _openSubPage = null;
    }

    /// <summary>从子页回到本页（子页里的"返回"和面板底部的「返回」都走这里）。</summary>
    public void CloseSubPage()
    {
        if (_openSubPage == null) return;

        CollapseSubPage();
        gameObject.SetActive(true);
        RefreshAll();
    }

    /// <summary>
    /// 让本页收掉"还没结束的编辑"，并一路传到开着的子页。
    ///
    /// 面板级的「应用」「恢复默认」「返回」在动手之前都要先调它。现在唯一的例子是按键页的
    /// "等玩家按键"：点应用之后如果那次重绑还在跑，玩家随后按下的任意一个键都会写进
    /// 一个他以为已经提交完的数据里，然后被下一次刷新覆盖掉 —— 表现为"改键自己变了"。
    ///
    /// 做成"先收尾再执行"而不是"拒绝执行"：玩家点应用的意思是"我要提交"，
    /// 不该因为他恰好还停在等待状态就把整个操作吞掉。
    /// </summary>
    public void CancelPendingEdit()
    {
        OnCancelPendingEdit();

        //子页开着的时候面板拿到的只是父页引用，不转发就会漏掉子页上正在进行的编辑
        if (_openSubPage != null) _openSubPage.CancelPendingEdit();
    }

    /// <summary>子类在这里取消自己那点临时状态（默认没有）。</summary>
    protected virtual void OnCancelPendingEdit() { }

    // ══════════════════════ 生命周期 ══════════════════════

    /// <summary>第一次显示前调用一次：建立映射、校验、回填。重复调用无副作用。</summary>
    public void EnsureBound()
    {
        if (_bound) return;

        if (!gameObject.activeInHierarchy)
        {
            ChaosLog.Error(LogChannel.UI,
                GetType().Name + " 在【未激活】状态下绑定：行控件的 Awake 还没跑，" +
                "取值域与回调会漏掉。请先 SetActive(true) 再绑定（面板的页面切换逻辑负责这点）。");
        }

        _bound = true;
        OnBind();
        ValidateRows();
        RefreshAll();
    }

    /// <summary>子类在这里声明"哪一行改哪个字段"。只会被调用一次。</summary>
    protected abstract void OnBind();

    /// <summary>把 Pending 的当前值灌回本页所有控件。切换页面、应用、恢复默认之后都要调。</summary>
    public void RefreshAll()
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        for (int i = 0; i < _refreshers.Count; i++)
        {
            _refreshers[i](data);
        }

        //子页开着就一并刷新。面板的「应用」「恢复默认」只拿到当前分类页这一个引用，
        //不转发的话子页上的键名会停在提交前的样子。
        //只往下走一跳 —— 子页自己不会再持有另一个子页，不会递归
        if (_openSubPage != null) _openSubPage.RefreshAll();
    }

    // ══════════════════════ 给子类用的绑定工具 ══════════════════════

    /// <summary>
    /// 注册一个"回填动作"。Bind* 内部已经调过，子类只有在需要特殊回填时才直接用。
    /// </summary>
    protected void AddRefresher(Action<SettingsData> refresher)
    {
        if (refresher != null) _refreshers.Add(refresher);
    }

    /// <summary>
    /// 拉条行。min/max 是取值域，preview 是"拖动过程中先让玩家听到/看到效果"的可选回调
    /// （目前只有音量用它做试听 —— 音量拉条听不到声音就没法调）。
    /// preview 改的是运行时效果，不是存档值；存档值只在点「应用」时才提交。
    /// </summary>
    protected SettingRow_Slider BindSlider(string id,
        Func<SettingsData, int> get, Action<SettingsData, int> set,
        int min, int max, Action<int> preview = null)
    {
        SettingRow_Slider row = FindRow<SettingRow_Slider>(id);
        if (row == null) return null;

        row.Configure(min, max);
        row.OnValueChanged = value =>
        {
            SettingsData data = SettingsManager.Instance.Pending;
            set(data, value);
            if (preview != null) preview(value);
            SettingsManager.Instance.NotifyPendingChanged();
        };

        AddRefresher(data => row.SetValueWithoutNotify(get(data)));
        return row;
    }

    /// <summary>
    /// 左右箭头选择行。displayTexts 是各档的文案，getIndex/setIndex 在"序号"与"字段值"之间换算
    /// —— 换算交给子类是因为只有它知道自己那套档位表长什么样（分辨率是动态过滤出来的，帧率是 30/40/60）。
    ///
    /// ══════════════ 两种形态自动分流 ══════════════
    /// 这个 settingId 在 SettingKeys 里有预摆档位的 Key 清单（视窗模式/画质/触发方式/语言）
    /// 就走预摆形态，否则走运行时写文字的形态。分流【只在这一处】判断，
    /// 各地页面不需要知道自己的行是哪种形态 —— 否则五个页面里会散落五份同样的判断。
    /// </summary>
    protected SettingRow_Selector BindSelector(string id, string[] displayTexts,
        Func<SettingsData, int> getIndex, Action<SettingsData, int> setIndex,
        Action<int> onChanged = null)
    {
        SettingRow_Selector row = FindRow<SettingRow_Selector>(id);
        if (row == null) return null;

        string[] optionKeys = SettingKeys.ForSetting(id);
        if (optionKeys != null)
        {
            //两张表必须同序同长：对不上时档位会显示隔壁那一档，而且不会有任何报错 ——
            //这种"看起来能用但内容错了"的问题最难查，所以在绑定期就吼一声
            if (displayTexts != null && optionKeys.Length != displayTexts.Length)
            {
                ChaosLog.Error(LogChannel.UI,
                    "设置项 " + id + " 的预摆档位 Key 有 " + optionKeys.Length +
                    " 个，而档位文案表有 " + displayTexts.Length + " 个，两张表顺序已经对不上了。");
            }
            row.ConfigureOptions(optionKeys);
        }
        else
        {
            row.Configure(displayTexts);
        }

        row.OnValueChanged = index =>
        {
            SettingsData data = SettingsManager.Instance.Pending;
            setIndex(data, index);
            if (onChanged != null) onChanged(index);
            SettingsManager.Instance.NotifyPendingChanged();
        };

        AddRefresher(data => row.SetIndexWithoutNotify(getIndex(data)));
        return row;
    }

    /// <summary>开关行。</summary>
    protected SettingRow_Toggle BindToggle(string id,
        Func<SettingsData, bool> get, Action<SettingsData, bool> set,
        Action<bool> onChanged = null)
    {
        SettingRow_Toggle row = FindRow<SettingRow_Toggle>(id);
        if (row == null) return null;

        row.OnValueChanged = value =>
        {
            SettingsData data = SettingsManager.Instance.Pending;
            set(data, value);
            if (onChanged != null) onChanged(value);
            SettingsManager.Instance.NotifyPendingChanged();
        };

        AddRefresher(data => row.SetValueWithoutNotify(get(data)));
        return row;
    }

    /// <summary>按钮行。它不承载值，所以没有回填动作。</summary>
    protected SettingRow_Button BindButton(string id, Action onClick)
    {
        SettingRow_Button row = FindRow<SettingRow_Button>(id);
        if (row == null) return null;

        row.OnClick = onClick;
        return row;
    }

    // ══════════════════════ 内部工具 ══════════════════════

    /// <summary>
    /// 在本页子树里按 settingId 找行控件。找不到会报错并返回 null（调用方要判空）。
    /// </summary>
    protected T FindRow<T>(string id) where T : SettingRowBase
    {
        T[] rows = GetComponentsInChildren<T>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].settingId == id)
            {
                _boundIds.Add(id);
                return rows[i];
            }
        }

        ChaosLog.Error(LogChannel.UI,
            GetType().Name + " 里找不到 settingId = '" + id + "' 的 " + typeof(T).Name + " 行控件。" +
            "检查这一页的 prefab：行是不是摆在了本页节点下、settingId 是不是拼错。");
        return null;
    }

    /// <summary>
    /// 校验"摆了但没绑"的行。手摆的 prefab 很容易加了一行却忘了写绑定，
    /// 那种情况下改动会被静默丢弃 —— 这里把它变成一条明确的警告。
    /// </summary>
    private void ValidateRows()
    {
        SettingRowBase[] rows = GetComponentsInChildren<SettingRowBase>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            string id = rows[i].settingId;
            if (string.IsNullOrEmpty(id) || !_boundIds.Contains(id))
            {
                ChaosLog.Warn(LogChannel.UI,
                    GetType().Name + " 下的行 '" + rows[i].name + "'（settingId='" + id + "'）没有被 OnBind 绑定，" +
                    "玩家改它不会有任何效果");
            }
        }
    }

    /// <summary>
    /// 在一张 int 档位表里找值的序号，找不到返回 0。
    /// 用在一一对应的档位换算上（帧率 30/40/60、画质 0/1/2 之类）。
    /// 返回 0 而不是 -1 是因为存档里的非法值应该显示成第一档，而不是让选择器崩掉。
    /// </summary>
    protected static int IndexOfValue(int[] values, int value)
    {
        if (values == null || values.Length == 0) return 0;
        int index = Array.IndexOf(values, value);
        return index < 0 ? 0 : index;
    }
}
