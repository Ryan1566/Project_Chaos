using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Chaos.MVP.Model.SaveSlots;
using Chaos.MVP.Presenter.SaveSlots;

/// <summary>
/// 存档面板（读存档）：固定 3 个档位，1/2/3 顺序永不变。
///
/// ══════════════ 它只做三件事 ══════════════
/// ① 把 3 个档位的状态画出来（数据全部来自 `SaveSlotService`）；
/// ② 把玩家意图转成 `SaveSlotService` 调用（新建 / 读取 / 删除）；
/// ③ 写盘成功后写 `GameSession` 并切场景。
///
/// ══════════════ ⚠ R3：绑定只放 Awake，OnEnter 只刷新 ══════════════
/// `UIManager` **缓存面板实例并复用**（`PanelDic`），所以面板每一次打开走的是 `OnEnter`，
/// 而 `Awake` **只跑一次**。如果按直觉把"找节点 + 绑按钮"写在 `OnEnter`：
///   · `Button.onClick.AddListener` 是**追加**语义 → 点一次触发多次（第 2 次打开就叠加两遍）；
///   · 3 个格子会被反复 `Instantiate` → 列表里堆出一堆重复格子。
/// 所以**绑定/建格子一律在 `Awake` 且幂等**，`OnEnter` **只调 `Refresh()`**。
///
/// ══════════════ 节点名是硬约定（改 prefab 时别改名） ══════════════
///   `RecordsList`                              → 3 个格子的父节点（HorizontalLayoutGroup）
///   `BottomBar/DeleteBtn`、`BottomBar/ReturnBtn`
///   `ConfirmGroup/ConfirmText`（**挂 `LocalizedText`**）、`ConfirmGroup/ConfirmOkBtn`、`ConfirmGroup/ConfirmCancelBtn`
/// `ConfirmGroup` 在 prefab 里默认 inactive，由本类按需 `SetActive`。
///
/// ══════════════ 为什么用 ConfirmPurpose 状态机 ══════════════
/// 二次确认**复用同一个 `ConfirmGroup`**（方案 §决策 10）：空槽开始与删除走的是同一套
/// 文本+OK/取消按钮。若不记录"这次确认是为什么弹的"，确认回调里就不知道该执行
/// `CreateNew` 还是 `Delete` —— 没有状态机就只能靠猜。
///
/// ══════════════ View 层的纪律（方案 §决策 12 折中） ══════════════
/// 本文件**不得**出现 `Application.persistentDataPath` / `File.` / `JsonUtility` ——
/// 那些只允许出现在 `SaveSlotService`。这样将来抽 Presenter 只需挪一层，不必改数据访问。
/// </summary>
public class SLPanel : BasePanel, ISaveSlotsView
{
    // ══════════════════ 节点名（硬约定） ══════════════════

    private const string NodeRecordsList = "RecordsList";
    private const string NodeBottomBar = "BottomBar";
    private const string NodeDeleteBtn = "DeleteBtn";
    private const string NodeReturnBtn = "ReturnBtn";
    private const string NodeConfirmGroup = "ConfirmGroup";
    private const string NodeConfirmText = "ConfirmText";
    private const string NodeConfirmOkBtn = "ConfirmOkBtn";
    private const string NodeConfirmCancelBtn = "ConfirmCancelBtn";

    /// <summary>`RecordCell` 预制体路径（`Resources` 相对、不带扩展名）。</summary>
    private const string CellPrefabPath = "UIPanels/SubUI_Prefab/RecordCell";

    // A usable slot enters the physical village through VillageSceneFlow.
    // Expedition loading remains available from the world map's dispatch button.

    // ══════════════════ 本地化 Key ══════════════════

    /// <summary>点空档位时的询问文案。</summary>
    public const string KeyConfirmStart = "ui_slpanel_confirm_start";
    /// <summary>删除存档时的询问文案。</summary>
    public const string KeyConfirmDelete = "ui_slpanel_confirm_delete";

    // ══════════════════ 字段 ══════════════════

    private Transform _recordsList;
    private readonly List<RecordCell> _cells = new List<RecordCell>();

    private Button _deleteBtn;
    private Button _returnBtn;

    private GameObject _confirmGroup;
    private LocalizedText _confirmText;
    private TextMeshProUGUI _confirmTextTmp;
    private Button _confirmOkBtn;
    private Button _confirmCancelBtn;

    private bool _cellsReady;
    private SaveSlotsModel _model;
    private SaveSlotsPresenter _presenter;
    private TextMeshProUGUI _errorText;
    public event System.Action<SaveSlotsIntent> Intent;
    public bool CanShowConfirmation => _confirmGroup != null;
    public void ShowEntryFailure(string reason)
    {
        Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.EntryFailure, message: reason));
    }

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindNodes();
        EnsureCells();   //只在第一次 Awake 建 3 个格子（幂等，见 _cellsReady）
        BindButtons();   //只在这里绑一次（R3）
    }

    public override void OnEnter()
    {
        _presenter?.Dispose();
        _model?.Dispose();
        _model = new SaveSlotsModel();
        _presenter = new SaveSlotsPresenter(_model);
        _presenter.Attach(this);
    }

    public override void OnExit()
    {
        //退场动画期间面板还活着、按钮仍可点：先把确认浮层收掉，
        //避免"下次打开就带着浮层"；同时清掉待确认动作，防止迟到的确认回调打错目标
        HideConfirm();
        _presenter?.Dispose();
        _presenter = null;
        _model?.Dispose();
        _model = null;
    }

    private void OnDestroy() { OnExit(); }

    // ══════════════════ 查找节点 / 绑定（只做一次） ══════════════════

    private void FindNodes()
    {
        _recordsList = transform.Find(NodeRecordsList);
        if (_recordsList == null)
        {
            ChaosLog.Error(LogChannel.UI, "SLPanel 下找不到 " + NodeRecordsList + "，档位列表无法显示");
        }

        Transform bottomBar = transform.Find(NodeBottomBar);
        if (bottomBar == null)
        {
            ChaosLog.Error(LogChannel.UI,
                "SLPanel 下找不到 " + NodeBottomBar + "，删除/返回按钮都不可用" +
                "（需要在 prefab 里新建 BottomBar/DeleteBtn、BottomBar/ReturnBtn）");
        }
        else
        {
            _deleteBtn = FindButton(bottomBar, NodeDeleteBtn);
            _returnBtn = FindButton(bottomBar, NodeReturnBtn);
        }

        Transform group = transform.Find(NodeConfirmGroup);
        if (group == null)
        {
            //可选节点：缺了不报错，但会退化成"不询问直接执行"（见 ShowConfirm）
            ChaosLog.Warn(LogChannel.UI,
                "SLPanel 下没有 " + NodeConfirmGroup + " 节点：空档位开始/删除存档将【不询问玩家】直接执行。" +
                "要开启询问就摆一个 ConfirmGroup，内含 ConfirmText 与 ConfirmOkBtn / ConfirmCancelBtn");
        }
        else
        {
            _confirmGroup = group.gameObject;

            Transform textNode = group.Find(NodeConfirmText);
            if (textNode == null)
            {
                ChaosLog.Error(LogChannel.UI,
                    NodeConfirmGroup + " 下找不到 " + NodeConfirmText + "，确认文案不会显示");
            }
            else
            {
                _confirmText = textNode.GetComponent<LocalizedText>();
                _confirmTextTmp = textNode.GetComponent<TextMeshProUGUI>();
                if (_confirmText == null)
                {
                    ChaosLog.Warn(LogChannel.UI,
                        NodeConfirmText + " 上没有 LocalizedText 组件，确认文案将显示本地化 Key 原文");
                }
            }

            _confirmOkBtn = FindButton(group, NodeConfirmOkBtn);
            _confirmCancelBtn = FindButton(group, NodeConfirmCancelBtn);

            _confirmGroup.SetActive(false);
        }
    }

    private void BindButtons()
    {
        if (_deleteBtn != null) _deleteBtn.onClick.AddListener(OnDeleteClicked);
        if (_returnBtn != null) _returnBtn.onClick.AddListener(OnReturnClicked);
        if (_confirmOkBtn != null) _confirmOkBtn.onClick.AddListener(OnConfirmOkClicked);
        if (_confirmCancelBtn != null) _confirmCancelBtn.onClick.AddListener(OnConfirmCancelClicked);
    }

    /// <summary>
    /// 建 3 个档位格子。**只建一次**并长期复用 —— 反复 `Instantiate` 会让列表里堆出重复格子（R3）。
    /// `ResManager.Load&lt;GameObject&gt;` 内部已经 `Instantiate`，这里拿到的就是实例。
    /// </summary>
    private void EnsureCells()
    {
        if (_cellsReady) return;
        if (_recordsList == null) return;

        //清掉 prefab 里可能预置的旧格子，避免与下面新建的重复（正常情况下一个都没有）
        for (int i = _recordsList.childCount - 1; i >= 0; i--)
        {
            RecordCell existing = _recordsList.GetChild(i).GetComponent<RecordCell>();
            if (existing != null) Destroy(existing.gameObject);
        }

        for (int slot = 1; slot <= GlobalPath.save_SlotCount; slot++)
        {
            GameObject go = ResManager.Instance.Load<GameObject>(CellPrefabPath);
            if (go == null)
            {
                ChaosLog.Error(LogChannel.UI,
                    "档位格子预制体加载失败：Assets/Resources/" + CellPrefabPath + ".prefab（缺文件？）");
                break;
            }

            go.name = "RecordCell_Slot" + slot;
            go.transform.SetParent(_recordsList, false);

            RecordCell cell = go.GetComponent<RecordCell>();
            if (cell == null)
            {
                ChaosLog.Error(LogChannel.UI, CellPrefabPath + " 上没有 RecordCell 组件");
                Destroy(go);
                break;
            }

            //闭包捕获：不能直接用 slot，否则 3 个格子点到的是同一个槽位
            int captured = slot;
            cell.Bind(captured, () => OnCellClicked(captured), FindSelectedMarker(go.transform));
            _cells.Add(cell);
        }

        _cellsReady = _cells.Count > 0;
        ChaosLog.Info(LogChannel.UI, "存档面板已创建 " + _cells.Count + " 个档位格子（一次性，之后复用）");
    }

    private static GameObject FindSelectedMarker(Transform cellRoot)
    {
        Transform marker = cellRoot.Find("Selected");
        return marker != null ? marker.gameObject : null;
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

    public void Render(SaveSlotsState state)
    {
        EnsureCells();
        for (int i = 0; i < _cells.Count; i++)
        {
            int slot = _cells[i].Slot;
            _cells[i].Render(state.Cells.Find(info => info.Slot == slot));
            _cells[i].SetSelected(slot == state.SelectedSlot);
        }
        if (_deleteBtn != null) _deleteBtn.interactable = state.CanDelete;
        if (_confirmGroup != null) _confirmGroup.SetActive(!string.IsNullOrEmpty(state.ConfirmationKey));
        if (!string.IsNullOrEmpty(state.ConfirmationKey)) SetConfirmText(state.ConfirmationKey);
        if (_errorText == null && !string.IsNullOrEmpty(state.Error))
        {
            RectTransform error = GameplayUi.Rect(transform, "LoadError", new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(0, 155), new Vector2(900, 80));
            _errorText = GameplayUi.Text(error, "Text", "", 22);
            _errorText.color = new Color(1f, .55f, .45f);
        }
        if (_errorText != null)
        {
            _errorText.text = state.Error ?? "";
            _errorText.gameObject.SetActive(!string.IsNullOrEmpty(state.Error));
        }
    }

    private void Refresh() { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Refresh)); }
    private void OnCellClicked(int slot) { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Select, slot)); }
    private void OnDeleteClicked() { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Delete)); }
    private void OnReturnClicked() { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Return)); }
    private void OnConfirmOkClicked() { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Confirm)); }
    private void OnConfirmCancelClicked() { Intent?.Invoke(new SaveSlotsIntent(SaveSlotsAction.Cancel)); }
    private void HideConfirm() { if (_confirmGroup != null) _confirmGroup.SetActive(false); }
    private void SetConfirmText(string key)
    {
        if (_confirmText != null) _confirmText.SetKey(key);
        else if (_confirmTextTmp != null) _confirmTextTmp.text = key;
    }
}
