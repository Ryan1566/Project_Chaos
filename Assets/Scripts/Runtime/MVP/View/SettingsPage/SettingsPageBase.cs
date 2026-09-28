using System;
using System.Collections.Generic;
using ChaosDebug;
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
    /// </summary>
    protected SettingRow_Selector BindSelector(string id, string[] displayTexts,
        Func<SettingsData, int> getIndex, Action<SettingsData, int> setIndex,
        Action<int> onChanged = null)
    {
        SettingRow_Selector row = FindRow<SettingRow_Selector>(id);
        if (row == null) return null;

        row.Configure(displayTexts);
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
