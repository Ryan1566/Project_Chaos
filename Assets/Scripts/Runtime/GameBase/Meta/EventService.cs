using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 剧情事件（M4 / W5.1）的服务层 —— **触发判定 / 排队 / 奖励发放 / 一次性记录**的唯一入口。
///
/// ══════════════ 三条铁律（16_剧情事件.md §3）══════════════
/// 1. **只有 3 种触发条件**（用户裁决，原型期不得扩展）：
///      `1` → `Payload.runs &gt;= TriggerValue`
///      `2` → `Payload.maxClearedDepth &gt;= TriggerValue`
///      `3` → `Payload.familyTreeNodes.Contains(TriggerValue)`（值 = 家族树节点 ID）
///    其它值 → Warn + 视为不满足（E6）。
/// 2. **一次性**：`Repeatable=false` 的事件处置后写 `Payload.firedEvents`，永不再弹（R1/R4）；
///    `Repeatable=true` 不落盘，只进**运行时会话集合**，避免同一条件反复弹（R3）。
/// 3. **排队**：`Priority` 降序 → `Index` 升序；**一次只弹一个**；单次最多收集 **5** 个（Q2/Q3/Q5）。
///
/// ══════════════ 检查时机 ══════════════
/// `DispatchService.ReturnToMeta()` 末尾调 <see cref="CheckAndShowOnReturnToMeta"/>（唯一正式时机）；
/// GM 的「强制检查并弹事件」也走它。**禁止每帧**（§3.3 C4）。
///
/// ══════════════ 奖励 ══════════════
/// 执行顺序固定 **资源 → 物品 → 门客 → 信心**（§3.4）；**单项失败不回滚**，其它奖励照给，
/// 失败项写进 `reason` 由面板逐条提示（A8/E10）。全部为 0 的选项 = 纯对话选项（合法）。
/// </summary>
public class EventService : SingletonBase<EventService>
{
    /// <summary>单次检查最多收集多少个事件（§3.6 Q5，超出留到下次）。</summary>
    public const int MaxPerCheck = 5;

    /// <summary>触发类型：进入局内次数。</summary>
    public const uint TriggerRuns = 1u;
    /// <summary>触发类型：通关过的最高深度。</summary>
    public const uint TriggerMaxClearedDepth = 2u;
    /// <summary>触发类型：已点亮家族树节点。</summary>
    public const uint TriggerFamilyTreeNode = 3u;

    private List<EventConfig> _eventDefs;
    private List<EventOptionConfig> _optionDefs;
    private bool _loaded;

    /// <summary>待弹队列（运行时，不落盘）。队首 = 当前该弹的事件。</summary>
    private readonly List<EventConfig> _queue = new List<EventConfig>();

    /// <summary>本次会话已弹过的**可重复**事件 ID（不落盘；JsonUtility 不支持 HashSet 落盘）。</summary>
    private readonly HashSet<uint> _poppedThisSession = new HashSet<uint>();

    // ══════════════════ 表加载 ══════════════════

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _eventDefs = LoadTable<EventConfig>("EventConfig");
        _optionDefs = LoadTable<EventOptionConfig>("EventOptionConfig");

        ChaosLog.Info(LogChannel.Config,
            "[剧情事件] 表已加载：Event " + _eventDefs.Count + " 行，EventOption " + _optionDefs.Count + " 行");
    }

    private static List<T> LoadTable<T>(string tableName) where T : class
    {
        string path = GlobalPath.data_JsonPathToRead + tableName;
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] 找不到 " + path + "（事件功能会退化，不抛异常）");
            return new List<T>();
        }

        DataList<T> list = JsonUtility.FromJson<DataList<T>>(asset.text);
        return (list == null || list.datas == null) ? new List<T>() : list.datas;
    }

    public void Reload()
    {
        _loaded = false;
        _eventDefs = null;
        _optionDefs = null;
        EnsureLoaded();
    }

    public int EventCount { get { EnsureLoaded(); return _eventDefs == null ? 0 : _eventDefs.Count; } }
    public int OptionCount { get { EnsureLoaded(); return _optionDefs == null ? 0 : _optionDefs.Count; } }

    public EventConfig GetEvent(uint eventIndex)
    {
        EnsureLoaded();
        if (_eventDefs == null) return null;
        for (int i = 0; i < _eventDefs.Count; i++)
            if (_eventDefs[i] != null && _eventDefs[i].Index == eventIndex) return _eventDefs[i];
        return null;
    }

    public EventOptionConfig GetOption(uint optionIndex)
    {
        EnsureLoaded();
        if (_optionDefs == null) return null;
        for (int i = 0; i < _optionDefs.Count; i++)
            if (_optionDefs[i] != null && _optionDefs[i].Index == optionIndex) return _optionDefs[i];
        return null;
    }

    /// <summary>某事件的选项（`Enable=true`，按 `OptionIndex` 升序，最多 3 个）。</summary>
    public List<EventOptionConfig> GetOptions(uint eventId)
    {
        EnsureLoaded();
        var result = new List<EventOptionConfig>();
        if (_optionDefs == null) return result;

        for (int i = 0; i < _optionDefs.Count; i++)
        {
            EventOptionConfig opt = _optionDefs[i];
            if (opt == null || !opt.Enable || opt.EventId != eventId) continue;
            result.Add(opt);
        }

        //OptionIndex 升序 = 按钮从上到下的顺序
        for (int i = 1; i < result.Count; i++)
        {
            EventOptionConfig key = result[i];
            int j = i - 1;
            while (j >= 0 && result[j].OptionIndex > key.OptionIndex) { result[j + 1] = result[j]; j--; }
            result[j + 1] = key;
        }

        if (result.Count > 3)
        {
            ChaosLog.Warn(LogChannel.Config,
                "[剧情事件] 事件 " + eventId + " 有 " + result.Count + " 个选项，超过 3 个上限，只显示前 3 个（C5）");
            result.RemoveRange(3, result.Count - 3);
        }
        if (result.Count == 0)
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] 事件 " + eventId + " 没有任何选项行（E1：只显示关闭按钮）");

        return result;
    }

    // ══════════════════ 队列（面板读取）══════════════════

    /// <summary>队首事件（该弹的那个）；队列空返回 null。</summary>
    public EventConfig Current { get { return _queue.Count > 0 ? _queue[0] : null; } }

    /// <summary>队列长度（含队首）。</summary>
    public int PendingCount { get { return _queue.Count; } }

    /// <summary>队首之后还剩几个（面板「还有 N 个事件」用）。</summary>
    public int RemainingAfterCurrent { get { return _queue.Count > 0 ? _queue.Count - 1 : 0; } }

    public bool HasPending { get { return _queue.Count > 0; } }

    // ══════════════════ 触发判定 ══════════════════

    /// <summary>逐字口径（§3.2）：只判"是否达到"，不判"是否刚达到"。</summary>
    public bool IsSatisfied(EventConfig def, SavePayload p, out string why)
    {
        why = null;
        if (def == null) { why = "事件为空"; return false; }

        switch (def.TriggerType)
        {
            case TriggerRuns:
                why = "runs " + p.runs + " / 需要 " + def.TriggerValue;
                return p.runs >= def.TriggerValue;

            case TriggerMaxClearedDepth:
                why = "maxClearedDepth " + p.maxClearedDepth + " / 需要 " + def.TriggerValue;
                return p.maxClearedDepth >= def.TriggerValue;

            case TriggerFamilyTreeNode:
                if (def.TriggerValue == 0u)
                {
                    why = "TriggerType=3 但 TriggerValue=0（配置错误，T2）";
                    return false;
                }
                why = "节点 " + def.TriggerValue + (p.familyTreeNodes != null && p.familyTreeNodes.Contains(def.TriggerValue) ? " 已点亮" : " 未点亮");
                return p.familyTreeNodes != null && p.familyTreeNodes.Contains(def.TriggerValue);

            default:
                why = "TriggerType=" + def.TriggerType + " 不在 1~3（E6：视为不满足）";
                return false;
        }
    }

    /// <summary>
    /// **收集本轮该弹的事件**（§3.6 Q1/Q2/Q5 + §3.5 R4）。
    /// 返回的列表就是队列本身（面板只读）。队列非空时**直接返回**（E9 幂等，不重复入队）。
    /// </summary>
    public List<EventConfig> CheckAndCollect()
    {
        EnsureLoaded();

        //E9：同一帧/同一次回局外重复检查 → 幂等，不重复入队
        if (_queue.Count > 0)
        {
            ChaosLog.Info(LogChannel.Config, "[剧情事件] 队列已存在 " + _queue.Count + " 个，跳过重复收集（幂等）");
            return _queue;
        }

        //一次新的"回局外检查" = 新的会话：可重复事件的去重集合只对本轮有效（R3）
        _poppedThisSession.Clear();

        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.firedEvents == null) p.firedEvents = new List<uint>();

        var candidates = new List<EventConfig>();
        int total = _eventDefs == null ? 0 : _eventDefs.Count;
        for (int i = 0; i < total; i++)
        {
            EventConfig def = _eventDefs[i];
            if (def == null || !def.Enable) continue;

            if (p.firedEvents.Contains(def.Index))
            {
                //R4：已处置的一次性事件永不参与排队
                continue;
            }

            if (def.Repeatable && _poppedThisSession.Contains(def.Index)) continue;

            string why;
            if (!IsSatisfied(def, p, out why)) continue;

            candidates.Add(def);
        }

        //Q2 排序：Priority 降序 → Index 升序（手写插入排序，避免闭包分配）
        for (int i = 1; i < candidates.Count; i++)
        {
            EventConfig key = candidates[i];
            int j = i - 1;
            while (j >= 0 && ComesBefore(key, candidates[j])) { candidates[j + 1] = candidates[j]; j--; }
            candidates[j + 1] = key;
        }

        //Q5 单次最多 5 个
        int take = candidates.Count < MaxPerCheck ? candidates.Count : MaxPerCheck;
        for (int i = 0; i < take; i++)
        {
            _queue.Add(candidates[i]);
            if (candidates[i].Repeatable) _poppedThisSession.Add(candidates[i].Index);
        }

        ChaosLog.Info(LogChannel.Config,
            "[剧情事件] 触发检查：runs=" + p.runs + " maxClearedDepth=" + p.maxClearedDepth
            + " 家族树节点=" + (p.familyTreeNodes == null ? 0 : p.familyTreeNodes.Count)
            + " firedEvents=" + p.firedEvents.Count
            + " → 满足 " + candidates.Count + " 个，本次入队 " + _queue.Count + " 个（上限 " + MaxPerCheck + "）");

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _queue.Count; i++)
            sb.Append(i == 0 ? "" : " → ").Append(_queue[i].Index).Append("《").Append(_queue[i].TitleText).Append("》");
        if (_queue.Count > 0) ChaosLog.Info(LogChannel.Config, "[剧情事件] 队列顺序：" + sb);
        else ChaosLog.Info(LogChannel.Config, "[剧情事件] 队列为空（没有满足条件的事件）");

        return _queue;
    }

    /// <summary>排序判据：`Priority` 大在前；相同则 `Index` 小在前。</summary>
    private static bool ComesBefore(EventConfig a, EventConfig b)
    {
        if (a.Priority != b.Priority) return a.Priority > b.Priority;
        return a.Index < b.Index;
    }

    /// <summary>
    /// **回局外时的统一入口**：检查一次 → 有事件就弹面板。返回入队数量。
    /// 由 `DispatchService.ReturnToMeta()` 末尾调用；GM「强制检查并弹事件」也调它。
    /// </summary>
    public int CheckAndShowOnReturnToMeta()
    {
        List<EventConfig> queue = CheckAndCollect();
        if (queue == null || queue.Count == 0) return 0;

        ChaosLog.Success(LogChannel.Config,
            "[剧情事件] 弹事件面板：队首 " + queue[0].Index + "《" + queue[0].TitleText + "》，队列 " + queue.Count + " 个");

        //框架层**不直接引用**具体面板（arch 规则 framework-never-to-view）：走 PanelType 注册表
        if (UIManager.Instance == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] UIManager 未初始化，事件面板打不开（队列保留 " + queue.Count + " 个）");
            return 0;
        }
        UIManager.Instance.PushPanel(PanelType.EventPanel);
        return queue.Count;
    }

    /// <summary>
    /// **标记已处置**（单选/关闭都算，§13 Q4 按"算"）：移除队首并把一次性事件写进 `firedEvents`。
    /// 可重复事件不落盘（R3）。
    /// </summary>
    public void MarkFired(uint eventIndex)
    {
        EventConfig def = GetEvent(eventIndex);
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.firedEvents == null) p.firedEvents = new List<uint>();

        if (def != null && def.Repeatable)
        {
            ChaosLog.Info(LogChannel.Config,
                "[剧情事件] 事件 " + eventIndex + " 已处置（可重复，不写 firedEvents）");
        }
        else
        {
            if (!p.firedEvents.Contains(eventIndex)) p.firedEvents.Add(eventIndex);
            ChaosLog.Info(LogChannel.Config,
                "[剧情事件] 事件 " + eventIndex + " 已处置 → 写入 firedEvents（共 " + p.firedEvents.Count + " 个："
                + JoinIds(p.firedEvents) + "）");
        }

        for (int i = _queue.Count - 1; i >= 0; i--)
            if (_queue[i] != null && _queue[i].Index == eventIndex) _queue.RemoveAt(i);
    }

    // ══════════════════ 奖励发放 ══════════════════

    /// <summary>
    /// **执行一个选项的奖励**：资源 → 物品 → 门客 → 信心（§3.4 顺序，P2）。
    /// 单项失败**不回滚**其它项（P3）；`reason` 是给玩家看的逐条结果（用「；」分隔）。
    /// 返回是否至少发放成功一项（纯对话选项也算成功，`reason` 为「无奖励」）。
    /// </summary>
    public bool ApplyOption(uint optionIndex, out string reason)
    {
        EnsureLoaded();
        reason = null;

        EventOptionConfig opt = GetOption(optionIndex);
        if (opt == null)
        {
            reason = "选项 " + optionIndex + " 不存在";
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] " + reason + "（E2：忽略该选项）");
            return false;
        }
        if (!opt.Enable)
        {
            reason = "选项 " + optionIndex + " 已禁用（Enable=false）";
            ChaosLog.Warn(LogChannel.Config, "[剧情事件] " + reason);
            return false;
        }

        var parts = new List<string>();
        bool any = false;

        // ── A1 资源 ──
        if (opt.GiveResourceType != 0u)
        {
            InventoryService.ResourceKind kind;
            if (TryMapResource(opt.GiveResourceType, out kind))
            {
                uint now = InventoryService.Instance.AddResource(kind, opt.GiveResourceAmount);
                parts.Add(InventoryService.ResourceDisplayName(kind) + " +" + opt.GiveResourceAmount + "（现有 " + now + "）");
                any = true;
            }
            else
            {
                parts.Add("资源类型 " + opt.GiveResourceType + " 非法（跳过）");
                ChaosLog.Warn(LogChannel.Config,
                    "[剧情事件] 选项 " + optionIndex + " 的 GiveResourceType=" + opt.GiveResourceType + " 不在 1~4（E5：跳过资源奖励）");
            }
        }

        // ── A2 物品 ──
        if (opt.GiveItemId != 0u)
        {
            string itemReason;
            if (InventoryService.Instance.AddItem(opt.GiveItemId, opt.GiveItemCount, out itemReason))
            {
                parts.Add("物品 " + opt.GiveItemId + " ×" + opt.GiveItemCount + " 已放入");
                any = true;
                if (!string.IsNullOrEmpty(itemReason)) parts.Add(itemReason);   //部分成功
            }
            else
            {
                parts.Add("物品未能放入（" + itemReason + "）");
            }
        }

        // ── A3 门客 ──
        if (opt.GiveRetainerId != 0u)
        {
            string retReason;
            RetainerSave retainer = RetainerService.Instance.RecruitByProtoId(opt.GiveRetainerId, out retReason);
            if (retainer != null)
            {
                parts.Add("新门客：" + retainer.protoId + "（#" + retainer.id + "，信心 " + retainer.confidence + "）");
                any = true;
            }
            else
            {
                parts.Add("门客未能加入（" + retReason + "）");
            }
        }

        // ── A4 信心（全部门客，§13 Q6 按"全部"）──
        if (opt.ConfidenceDelta != 0)
        {
            List<RetainerSave> all = RetainerService.Instance.GetAll();
            int changed = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) continue;
                if (RetainerService.Instance.ChangeConfidence(all[i].id, opt.ConfidenceDelta)) changed++;
            }
            if (changed > 0)
            {
                parts.Add("全部门客信心 " + (opt.ConfidenceDelta > 0 ? "+" : "") + opt.ConfidenceDelta + "（" + changed + " 位）");
                any = true;
            }
            else
            {
                parts.Add("信心未变（当前没有门客）");
            }
        }

        //A5 EffectId：本期预留不实现（§13 Q2）
        if (opt.EffectId != 0u)
        {
            ChaosLog.Warn(LogChannel.Config,
                "[剧情事件] 选项 " + optionIndex + " 配了 EffectId=" + opt.EffectId
                + "，本期预留不实现（需 grantedEffects 存档字段，见 16_剧情事件.md Q2），已忽略");
        }

        if (parts.Count == 0)
        {
            reason = "无奖励（纯对话选项）";
            ChaosLog.Info(LogChannel.Config,
                "[剧情事件] 选项 " + optionIndex + "《" + opt.OptionText + "》" + reason);
            return true;
        }

        reason = string.Join("；", parts.ToArray());
        ChaosLog.Success(LogChannel.Config,
            "[剧情事件] 选项 " + optionIndex + "《" + opt.OptionText + "》结算：" + reason);
        return any;
    }

    /// <summary>表的资源类型（1 物资 / 2 材料 / 3 钱财 / 4 图纸）→ 服务层枚举（16_EventOption §3.2）。</summary>
    private static bool TryMapResource(uint giveResourceType, out InventoryService.ResourceKind kind)
    {
        switch (giveResourceType)
        {
            case 1u: kind = InventoryService.ResourceKind.Goods; return true;
            case 2u: kind = InventoryService.ResourceKind.Material; return true;
            case 3u: kind = InventoryService.ResourceKind.Money; return true;
            case 4u: kind = InventoryService.ResourceKind.Blueprint; return true;
            default: kind = InventoryService.ResourceKind.Goods; return false;
        }
    }

    // ══════════════════ 调试 / GM ══════════════════

    /// <summary>清空 `firedEvents`（一次性事件恢复可弹）+ 清队列 + 清会话集合。</summary>
    public void ResetFired()
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.firedEvents == null) p.firedEvents = new List<uint>();
        int before = p.firedEvents.Count;
        p.firedEvents.Clear();
        _poppedThisSession.Clear();
        _queue.Clear();
        ChaosLog.Info(LogChannel.Config, "[剧情事件] 已清空 firedEvents（清掉 " + before + " 个）+ 会话集合 + 队列");
    }

    /// <summary>GM「打印触发状态」：4 个触发数据源 + firedEvents 列表。</summary>
    public string DumpState()
    {
        EnsureLoaded();
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.firedEvents == null) p.firedEvents = new List<uint>();

        var sb = new System.Text.StringBuilder();
        sb.Append("[剧情事件] 表 Event=").Append(EventCount).Append(" 行 EventOption=").Append(OptionCount).Append(" 行")
          .Append(" | 会话=").Append(MetaProgressService.Instance.HasSession ? "有" : "内存态")
          .Append("\n  触发数据 runs=").Append(p.runs)
          .Append(" maxClearedDepth=").Append(p.maxClearedDepth)
          .Append(" 家族树节点数=").Append(p.familyTreeNodes == null ? 0 : p.familyTreeNodes.Count)
          .Append("（").Append(p.familyTreeNodes == null ? "" : JoinIds(p.familyTreeNodes)).Append("）")
          .Append("\n  firedEvents=").Append(p.firedEvents.Count).Append(" 个：").Append(JoinIds(p.firedEvents))
          .Append("\n  本次会话已弹（可重复）=").Append(JoinIds(_poppedThisSession))
          .Append(" | 当前队列 ").Append(_queue.Count).Append(" 个");

        for (int i = 0; i < _queue.Count; i++)
            sb.Append("\n    ").Append(i + 1).Append(". ").Append(_queue[i].Index).Append("《").Append(_queue[i].TitleText).Append("》");

        //逐条列出每个事件的当前判定（便于现场看"为什么不弹"）
        for (int i = 0; i < _eventDefs.Count; i++)
        {
            EventConfig def = _eventDefs[i];
            if (def == null) continue;
            string why;
            bool ok = IsSatisfied(def, p, out why);
            bool fired = p.firedEvents.Contains(def.Index);
            sb.Append("\n  · ").Append(def.Index).Append(" type=").Append(def.TriggerType)
              .Append(" val=").Append(def.TriggerValue)
              .Append(def.Repeatable ? " 可重复" : " 一次性")
              .Append(" prio=").Append(def.Priority)
              .Append(ok ? " ✅满足" : " ❌不满足").Append("（").Append(why).Append("）")
              .Append(fired ? " [已在firedEvents]" : "");
        }
        return sb.ToString();
    }

    private static string JoinIds(ICollection<uint> ids)
    {
        if (ids == null || ids.Count == 0) return "无";
        var sb = new System.Text.StringBuilder("[");
        int i = 0;
        foreach (uint id in ids)
        {
            if (i++ > 0) sb.Append(",");
            sb.Append(id);
        }
        return sb.Append("]").ToString();
    }
}
