using System;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 存档列表里的**一个档位格子**（`RecordCell.prefab` 的脚本）。
///
/// ══════════════ 为什么 Bind 和 SetData 要分开 ══════════════
/// **绑定只做一次，数据每次刷新**（方案 R3）。`SLPanel` 只 `Instantiate` 3 个格子并长期复用，
/// 所以：
///   · `Bind(...)`   → 记槽位号、挂点击回调（**只允许执行一次**，重复挂会叠加回调 → 点一次触发多次）；
///   · `SetData(...)` → 把当前槽位的状态画到界面上（每次 `OnEnter` / 每次刷新都可以调）。
///
/// ══════════════ 节点名是硬约定（改 prefab 时别改名） ══════════════
///   `Text (TMP)`      → 标题（**挂着 `LocalizedText`**，所以文案随语言切换）
///   `RecordNum_text`  → 槽位号 1/2/3
///   `RecordTime_text` → 时间（本地 `yyyy/MM/dd HH:mm`）或 `--`
///   `Selected`        → 选中高亮（可缺省，缺了只是看不到高亮）
///
/// ══════════════ 本类不做的事 ══════════════
/// 它**不碰存档读写**：不出现 `File` / `JsonUtility` / `persistentDataPath`
/// （方案 §决策 12 折中：View 层只允许调 `SaveSlotService`，而本类连 Service 都不直接调 ——
/// 数据由 `SLPanel` 统一取一次再分发给 3 个格子，避免 3 次重复扫描目录）。
/// </summary>
public class RecordCell : MonoBehaviour
{
    /// <summary>非空档位的标题 Key（与 prefab 上原有的 `LocalizedText.localizationKey` 一致）。</summary>
    public const string TitleKey = "ui_recordcell";

    /// <summary>空档位显示「空档位」。</summary>
    public const string EmptyKey = "ui_slpanel_cell_empty";

    /// <summary>空档位/无时间时的时间占位符。</summary>
    public const string EmptyTimeText = "--";

    /// <summary>时间显示格式：本地时区。</summary>
    public const string TimeFormat = "yyyy/MM/dd HH:mm";

    private int _slot;
    private Button _button;
    private TextMeshProUGUI _titleTmp;
    private LocalizedText _titleLocalized;
    private TextMeshProUGUI _numText;
    private TextMeshProUGUI _timeText;
    private GameObject _selectedMarker;

    private bool _bound;

    /// <summary>本格子对应的槽位号（1..3）；未 Bind 时为 0。</summary>
    public int Slot { get { return _slot; } }

    /// <summary>是否已经绑定过（用于保证"只绑一次"，见 R3）。</summary>
    public bool IsBound { get { return _bound; } }

    private void Awake()
    {
        CacheNodes();
    }

    private void CacheNodes()
    {
        if (_button == null) _button = GetComponent<Button>();

        Transform title = transform.Find("Text (TMP)");
        if (title != null)
        {
            _titleTmp = title.GetComponent<TextMeshProUGUI>();
            _titleLocalized = title.GetComponent<LocalizedText>();
        }
        else
        {
            ChaosLog.Warn(LogChannel.UI, "RecordCell 下找不到 'Text (TMP)' 节点，标题不会显示");
        }

        _numText = FindText("RecordNum_text");
        _timeText = FindText("RecordTime_text");

        Transform selected = transform.Find("Selected");
        _selectedMarker = selected != null ? selected.gameObject : null;
    }

    private TextMeshProUGUI FindText(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            ChaosLog.Warn(LogChannel.UI, "RecordCell 下找不到 " + childName + " 节点");
            return null;
        }

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        if (text == null) ChaosLog.Warn(LogChannel.UI, childName + " 上没有 TextMeshProUGUI 组件");
        return text;
    }

    /// <summary>
    /// 绑定槽位号与点击回调。**只允许成功执行一次** —— 第二次调用会被忽略并留一条 Warn，
    /// 因为 `onClick.AddListener` 是**追加**语义：重复绑定会让"点一下"触发 N 次（方案 R3）。
    /// </summary>
    /// <param name="slot">槽位号（1..3）</param>
    /// <param name="onClick">点击回调（由 SLPanel 传入，内部已带槽位号）</param>
    /// <param name="selectedMarker">选中高亮节点（可为 null）</param>
    public void Bind(int slot, UnityAction onClick, GameObject selectedMarker)
    {
        if (_bound)
        {
            ChaosLog.Warn(LogChannel.UI,
                "RecordCell(槽 " + _slot + ") 被重复 Bind，本次忽略 —— " +
                "重复挂 onClick 会让点一次触发多次");
            return;
        }

        _slot = slot;
        _bound = true;

        if (selectedMarker != null) _selectedMarker = selectedMarker;

        if (_button == null) _button = GetComponent<Button>();
        if (_button != null && onClick != null)
        {
            _button.onClick.AddListener(onClick);
        }
        else if (_button == null)
        {
            ChaosLog.Error(LogChannel.UI, "RecordCell(槽 " + slot + ") 上没有 Button 组件，点击无效");
        }

        //槽位号是静态信息，绑定时就可以画一次
        if (_numText != null) _numText.text = slot.ToString();
    }

    /// <summary>
    /// 把某个槽位的当前状态画到界面上。**只读 `info`，不产生副作用**，可以反复调用。
    /// 空槽（含损坏槽）显示「空档位」与 `--`。
    /// </summary>
    public void SetData(SaveSlotInfo info)
    {
        //第一次调用前若还没走过 Awake（理论上不会，但保持防御），补一次缓存
        if (_titleTmp == null && _titleLocalized == null) CacheNodes();

        int slot = (info != null && info.Slot != 0) ? info.Slot : _slot;
        if (slot == 0) slot = _slot;
        _slot = slot;

        if (_numText != null) _numText.text = slot.ToString();

        bool isEmpty = (info == null) || info.IsEmpty;

        if (isEmpty)
        {
            SetTitleKey(EmptyKey);
            if (_timeText != null) _timeText.text = EmptyTimeText;
            return;
        }

        SetTitleKey(TitleKey);
        if (_timeText != null) _timeText.text = FormatLocalTime(info.LastSaveUtcTicks);
    }

    /// <summary>切换选中高亮。没有 `Selected` 节点时是空操作（不影响功能，只是没有视觉反馈）。</summary>
    public void SetSelected(bool selected)
    {
        if (_selectedMarker != null) _selectedMarker.SetActive(selected);
    }

    /// <summary>把 UTC ticks 转成本地时区的显示串；ticks 非法（&lt;= 0）时给占位符。</summary>
    public static string FormatLocalTime(long utcTicks)
    {
        if (utcTicks <= 0) return EmptyTimeText;

        try
        {
            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().ToString(TimeFormat);
        }
        catch (ArgumentOutOfRangeException)
        {
            //存档被手改成天文数字时不要让界面崩掉
            ChaosLog.Warn(LogChannel.UI, "存档时间戳超出 DateTime 范围，已显示占位符：" + utcTicks);
            return EmptyTimeText;
        }
    }

    /// <summary>
    /// 切换标题的本地化 Key。
    ///
    /// ⚠ `LocalizedText.SetKey` 在 `LocalizationManager` 缺席时会**静默不改文本**
    /// （`UpdateText` 里 `GetInstance() == null` 直接 return）。那种情况下这里直接把
    /// key 原文写进 TMP —— 至少能一眼看出"是哪个 key 没生效"，
    /// 而不是让空档位继续显示上一档的「存档」。
    /// </summary>
    private void SetTitleKey(string key)
    {
        if (_titleLocalized != null)
        {
            _titleLocalized.SetKey(key);

            if (LocalizationManager.GetInstance() == null && _titleTmp != null)
                _titleTmp.text = key;
            return;
        }

        if (_titleTmp != null) _titleTmp.text = key;
        else ChaosLog.Warn(LogChannel.UI, "RecordCell 没有可用的标题组件（Text (TMP) / LocalizedText 都缺失）");
    }
}
