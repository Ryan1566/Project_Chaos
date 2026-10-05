using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
public class SLPanel : BasePanel
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

    /// <summary>
    /// **目标场景名**：读条结束后真正要进入的玩法场景。
    /// 本面板不直接切它，而是写进 `GameSession.TargetSceneName`，由 `LoadingController` 去加载
    /// （见 `06_加载读条界面落地方案.md` §4）。必须在 `EditorBuildSettings.scenes` 里。
    /// </summary>
    private const string GameSceneName = "WorldScene";

    /// <summary>
    /// **读条场景名**：本面板实际切过去的场景。它很小、出现快，负责显示轮播背景 + 进度条，
    /// 加载完目标场景后再激活。必须在 `EditorBuildSettings.scenes` 里。
    /// </summary>
    private const string LoadingSceneName = "LoadingScene";

    // ══════════════════ 本地化 Key ══════════════════

    /// <summary>点空档位时的询问文案。</summary>
    public const string KeyConfirmStart = "ui_slpanel_confirm_start";
    /// <summary>删除存档时的询问文案。</summary>
    public const string KeyConfirmDelete = "ui_slpanel_confirm_delete";

    /// <summary>确认浮层当前是为了哪件事弹出来的（没有它就无法在回调里正确分派）。</summary>
    private enum ConfirmPurpose
    {
        None = 0,
        /// <summary>空档位 → 询问"是否选择该档位开始游戏"（确认后 CreateNew）。</summary>
        StartFromEmpty = 1,
        /// <summary>删除选中档位（确认后 Delete）。</summary>
        Delete = 2,
    }

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

    private ConfirmPurpose _confirmPurpose = ConfirmPurpose.None;

    /// <summary>当前选中的槽位（1..3）。决定"删除"作用于哪一格、以及哪一格高亮。</summary>
    private int _selectedSlot = 1;

    /// <summary>最近一次 `ReadAllSlots()` 的结果，供点击回调查询而无需重复扫目录。</summary>
    private List<SaveSlotInfo> _infos;

    private bool _cellsReady;

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindNodes();
        EnsureCells();   //只在第一次 Awake 建 3 个格子（幂等，见 _cellsReady）
        BindButtons();   //只在这里绑一次（R3）
    }

    public override void OnEnter()
    {
        base.OnEnter();

        HideConfirm();

        //每次打开都重算默认选中：第一个"有存档文件"的档位；都没有 → 槽 1
        _selectedSlot = SaveSlotService.Instance.FindFirstNonEmptySlot();

        Refresh();
    }

    public override void OnExit()
    {
        base.OnExit();

        //退场动画期间面板还活着、按钮仍可点：先把确认浮层收掉，
        //避免"下次打开就带着浮层"；同时清掉待确认动作，防止迟到的确认回调打错目标
        HideConfirm();
        _confirmPurpose = ConfirmPurpose.None;
    }

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

    // ══════════════════ 刷新（OnEnter 只做这个） ══════════════════

    /// <summary>重读 3 个档位并把状态画到界面。幂等、可反复调用。</summary>
    private void Refresh()
    {
        EnsureCells();

        _infos = SaveSlotService.Instance.ReadAllSlots();

        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].SetData(GetInfo(_cells[i].Slot));
        }

        ApplySelectionHighlight();
        RefreshDeleteButton();
    }

    private SaveSlotInfo GetInfo(int slot)
    {
        if (_infos == null) return null;

        for (int i = 0; i < _infos.Count; i++)
        {
            if (_infos[i].Slot == slot) return _infos[i];
        }
        return null;
    }

    private void ApplySelectionHighlight()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].SetSelected(_cells[i].Slot == _selectedSlot);
        }
    }

    /// <summary>
    /// 删除按钮只在**选中槽位确实有文件**时可用。
    /// 「有文件」= 有效存档 **或** 损坏存档 —— 损坏槽必须可删（方案 R8：给玩家一个恢复手段），
    /// 而完全空白的槽位不可删（没什么可删的）。
    /// </summary>
    private void RefreshDeleteButton()
    {
        if (_deleteBtn == null) return;
        _deleteBtn.interactable = SaveSlotService.HasSaveFile(GetInfo(_selectedSlot));
    }

    // ══════════════════ 点击档位 ══════════════════

    private void OnCellClicked(int slot)
    {
        if (!SaveSlotService.IsValidSlot(slot)) return;

        SaveSlotInfo info = GetInfo(slot);
        if (info == null)
        {
            //缓存里没有（理论上不会）：刷一次再取
            Refresh();
            info = GetInfo(slot);
        }

        if (info != null && info.HasUsableSave)
        {
            //非空档位：直接读取进入（验收标准 5）
            EnterGame(slot);
            return;
        }

        //空档位 / 损坏档位：先把它选中（让删除按钮状态跟着更新），再二次确认（验收标准 6、R8）
        _selectedSlot = slot;
        ApplySelectionHighlight();
        RefreshDeleteButton();

        ShowConfirm(ConfirmPurpose.StartFromEmpty, KeyConfirmStart);
    }

    private void OnDeleteClicked()
    {
        //按钮理论上已被禁用；这里再挡一次，避免"空槽也被问删除"
        if (!SaveSlotService.HasSaveFile(GetInfo(_selectedSlot)))
        {
            RefreshDeleteButton();
            return;
        }

        ShowConfirm(ConfirmPurpose.Delete, KeyConfirmDelete);
    }

    private void OnReturnClicked()
    {
        HideConfirm();
        _confirmPurpose = ConfirmPurpose.None;
        UIManager.Instance.PopPanel();
    }

    // ══════════════════ 二次确认（状态机） ══════════════════

    private void ShowConfirm(ConfirmPurpose purpose, string localizeKey)
    {
        _confirmPurpose = purpose;

        if (_confirmGroup == null)
        {
            //没有确认浮层时的退路：直接执行 + 警告。
            //不能"什么都不做" —— 那会让玩家觉得点了没反应
            ChaosLog.Warn(LogChannel.UI,
                "SLPanel 的 prefab 里没有 ConfirmGroup，本次确认（" + purpose + "）将直接执行、不询问玩家");
            ExecuteConfirm();
            return;
        }

        //时序照 SettingPanel.cs:24 的教训：**先 SetActive(true) 再 SetKey**，
        //否则在未激活状态下刷文本，可能出现"打开了却显示上一次的文案/空文案"
        _confirmGroup.SetActive(true);
        SetConfirmText(localizeKey);
    }

    private void HideConfirm()
    {
        if (_confirmGroup != null) _confirmGroup.SetActive(false);
    }

    private void SetConfirmText(string localizeKey)
    {
        if (_confirmText != null)
        {
            _confirmText.SetKey(localizeKey);

            //LocalizedText 在 LocalizationManager 缺席时不会改文本（UpdateText 里直接 return），
            //这时退化成显示 Key 原文，至少能一眼看出是哪条文案没配
            if (LocalizationManager.GetInstance() == null && _confirmTextTmp != null)
                _confirmTextTmp.text = localizeKey;
            return;
        }

        if (_confirmTextTmp != null) _confirmTextTmp.text = localizeKey;
    }

    private void OnConfirmOkClicked()
    {
        ExecuteConfirm();
    }

    private void OnConfirmCancelClicked()
    {
        //取消 = 什么都不做：不产生文件、不删文件（验收标准 6）
        HideConfirm();
        _confirmPurpose = ConfirmPurpose.None;
        ChaosLog.Info(LogChannel.Save, "玩家取消了二次确认");
    }

    /// <summary>按 `ConfirmPurpose` 分派到正确动作 —— 这就是状态机存在的全部意义。</summary>
    private void ExecuteConfirm()
    {
        ConfirmPurpose purpose = _confirmPurpose;

        _confirmPurpose = ConfirmPurpose.None;
        HideConfirm();

        switch (purpose)
        {
            case ConfirmPurpose.StartFromEmpty:
                StartFromEmpty(_selectedSlot);
                break;

            case ConfirmPurpose.Delete:
                DeleteSelected();
                break;

            case ConfirmPurpose.None:
            default:
                //没有待确认动作：按钮被点到也不该误伤任何存档
                ChaosLog.Warn(LogChannel.UI, "确认回调触发时没有待确认的动作，已忽略");
                break;
        }
    }

    // ══════════════════ 三个业务动作 ══════════════════

    /// <summary>空档位开始游戏：先新建存档文件，**写盘成功才切场景**（验收标准 6、R7）。</summary>
    private void StartFromEmpty(int slot)
    {
        if (!SaveSlotService.Instance.CreateNew(slot))
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 新建存档失败，已中止进入游戏（不切场景）");
            Refresh();
            return;
        }

        EnterGame(slot);
    }

    /// <summary>
    /// 非空档位开始游戏：读存档 → 写入 `GameSession`（含**目标场景名**）→ 切到读条场景。
    ///
    /// ══════════════ 为什么不再直接切 WorldScene ══════════════
    /// 见 `06_加载读条界面落地方案.md` §4：本面板只负责"把目标场景名告诉 `GameSession`"，然后切到
    /// `LoadingScene`；由 `LoadingController` 去流式加载 `GameSceneName` 并显示读条。
    /// 这样 `LoadingScene` 对**任意**目标场景都可复用，本面板不必知道加载是怎么做的。
    /// </summary>
    private void EnterGame(int slot)
    {
        SaveSlotFile file;
        if (!SaveSlotService.Instance.TryLoad(slot, out file))
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 读取失败，已中止进入游戏（不切场景）");
            Refresh();
            return;
        }

        //把"要进哪个场景"写进跨场景静态单例：LoadingScene 里没有存档信息，只能靠它传过去
        GameSession.Instance.Begin(slot, file.payload, GameSceneName);

        ChaosLog.Info(LogChannel.Save,
            "开始游戏：槽 " + slot + " → 已写入 GameSession（目标场景 " + GameSceneName +
            "），准备切到读条场景 " + LoadingSceneName);

        //R2：ScenesLoadManager.LoadScene 的第 19 行【无条件】调 action()，传 null 必 NRE → 传空委托
        ScenesLoadManager.Instance.LoadScene(LoadingSceneName, () => { });
    }

    /// <summary>删除选中档位；删完重新选（第一个有文件的档位；都没有 → 槽 1）（验收标准 7）。</summary>
    private void DeleteSelected()
    {
        int slot = _selectedSlot;

        if (!SaveSlotService.Instance.Delete(slot))
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 删除失败");
        }

        //删除后该槽回到空态：重新落选中，避免"选着一个已经不存在的档位"
        _selectedSlot = SaveSlotService.Instance.FindFirstNonEmptySlot();
        Refresh();
    }
}
