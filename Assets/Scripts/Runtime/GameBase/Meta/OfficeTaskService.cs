using System;
using System.Collections.Generic;
using ChaosDebug;

/// <summary>
/// 办事单位任务领域服务：负责激活、计时、选择、结算与时代门槛判定。
/// 所有改动存档的入口都走 MetaCommandRunner 一次命令一次保存；读取列表只返回快照，不产生副作用。
/// 时钟只来自主控传入的可计时秒数（Tick），不读系统时间，离线不推进。
/// </summary>
public sealed class OfficeTaskService : SingletonBase<OfficeTaskService>
{
    /// <summary>三项村庄指标的合法上限。</summary>
    public const int StatMax = int.MaxValue;

    /// <summary>重新读取任务配置。只影响后续读取，不改动任何存档状态。</summary>
    public void Reload()
    {
        OfficeTaskCatalog.Reload();
    }

    /// <summary>配置是否可用；不可用时给出明确原因。</summary>
    public bool TryGetConfig(out OfficeTaskCatalogSet set, out string reason)
    {
        set = OfficeTaskCatalog.EnsureLoaded();
        reason = set == null ? (OfficeTaskCatalog.LastError ?? "任务配置缺失或不可用") : null;
        return set != null;
    }

    /// <summary>
    /// 只读列表：不激活、不计时、不发奖，也不补写存档里的空列表。
    /// 活动、已失败、已完成、等待战斗的记录都会返回，由界面按 Status 显示；选项为配置的副本。
    /// </summary>
    public OfficeTaskSnapshot[] GetTasks(OfficeTaskCategory category)
    {
        var result = new List<OfficeTaskSnapshot>();
        SavePayload payload;
        if (!TryGetPayload(out payload)) return result.ToArray();
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) return result.ToArray();
        List<OfficeTaskSave> records = payload.officeTasks;
        if (records == null) return result.ToArray();
        for (int i = 0; i < records.Count; i++)
        {
            OfficeTaskSave record = records[i];
            if (record == null) continue;
            OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(record.taskId);
            if (entry == null || entry.Config.Category != (uint)category) continue;
            result.Add(BuildSnapshot(entry, record, payload));
        }
        return result.ToArray();
    }

    /// <summary>取一条任务快照；没有记录或配置缺失时返回 null，同样只读。</summary>
    public OfficeTaskSnapshot GetTask(uint taskId)
    {
        SavePayload payload;
        if (!TryGetPayload(out payload) || payload.officeTasks == null) return null;
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) return null;
        OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(taskId);
        if (entry == null) return null;
        for (int i = 0; i < payload.officeTasks.Count; i++)
        {
            OfficeTaskSave record = payload.officeTasks[i];
            if (record != null && record.taskId == taskId) return BuildSnapshot(entry, record, payload);
        }
        return null;
    }

    /// <summary>取当前存档的会话数据；没有活动会话时返回 false。</summary>
    private static bool TryGetPayload(out SavePayload payload)
    {
        payload = null;
        GameSession session = GameSession.Instance;
        if (!session.HasSession || session.LoadedPayload == null) return false;
        payload = session.LoadedPayload;
        return true;
    }

    /// <summary>
    /// 构造只读快照：进度、剩余时间、当前持有量全部按存档现算。
    /// 资源类任务显示“当前持有 / 需求”，交付类同一资源还要加上选项代价；
    /// 通关类显示本轮增量；已完成 / 已失败轮次按满进度显示，保证界面能表达结果。
    /// </summary>
    private static OfficeTaskSnapshot BuildSnapshot(OfficeTaskCatalogSet.TaskEntry entry, OfficeTaskSave record, SavePayload payload)
    {
        OfficeTaskTable_OfficeTaskConfig config = entry.Config;
        OfficeTaskGoal goal = (OfficeTaskGoal)config.GoalType;
        bool settled = record.status == OfficeTaskStatus.Completed || record.status == OfficeTaskStatus.Failed;
        var snapshot = new OfficeTaskSnapshot
        {
            TaskId = config.Index,
            MapLocationId = config.MapLocationId,
            Cycle = record.cycle == 0u ? 1u : record.cycle,
            Category = (OfficeTaskCategory)config.Category,
            Status = record.status,
            Title = config.TitleText,
            Body = config.BodyText,
            Topic = config.Topic,
            SelectedOptionId = record.selectedOptionId,
            Timed = config.DurationSeconds > 0u,
            Repeatable = config.Repeatable,
            Options = CopyOptions(entry.OptionList())
        };
        if (config.DurationSeconds > 0u && record.status == OfficeTaskStatus.Active)
        {
            double remain = record.deadlineClock - payload.officeClockSeconds;
            snapshot.RemainingSeconds = remain > 0.0 ? remain : 0.0;
        }

        switch (goal)
        {
            case OfficeTaskGoal.ResourceDelivery:
            {
                // 显示“当前持有量”，界面据此判断能否提交；需求量为任务要求 + 同资源选项代价的合计。
                ulong demand = config.RequiredAmount;
                OfficeTaskTable_OfficeTaskOptionConfig chosen = entry.FindOption(record.selectedOptionId);
                if (chosen != null && chosen.CostResourceType == config.ResourceType) demand += chosen.CostAmount;
                snapshot.Required = demand > uint.MaxValue ? uint.MaxValue : (uint)demand;
                uint held;
                snapshot.Progress = TryReadResource(config.ResourceType, out held) ? held : 0u;
                break;
            }
            case OfficeTaskGoal.ClearRuns:
                snapshot.Required = config.RequiredClears;
                snapshot.Progress = settled ? config.RequiredClears : ClearsSinceStart(payload, record);
                break;
            default:
                snapshot.Required = config.RequiredAmount;
                snapshot.Progress = settled ? snapshot.Required : 0u;
                break;
        }
        return snapshot;
    }

    /// <summary>本轮开始后完成的通关数；clears 被回退时按 0 计，不出现负数下溢。</summary>
    private static uint ClearsSinceStart(SavePayload payload, OfficeTaskSave record)
    {
        if (payload == null || record == null) return 0u;
        uint now = payload.clears;
        return now > record.startedClears ? now - record.startedClears : 0u;
    }

    /// <summary>展示快照不共享可写配置对象，避免外部界面修改领域目录。</summary>
    static OfficeTaskTable_OfficeTaskOptionConfig[] CopyOptions(OfficeTaskTable_OfficeTaskOptionConfig[] source)
    {
        var copy=new OfficeTaskTable_OfficeTaskOptionConfig[source.Length];
        for(int i=0;i<source.Length;i++)
        {
            var item=source[i];copy[i]=new OfficeTaskTable_OfficeTaskOptionConfig {Index=item.Index,TaskId=item.TaskId,OptionText=item.OptionText,
                CostResourceType=item.CostResourceType,CostAmount=item.CostAmount,RewardResourceType=item.RewardResourceType,RewardAmount=item.RewardAmount,
                ReputationDelta=item.ReputationDelta,InfluenceDelta=item.InfluenceDelta,DevelopmentDelta=item.DevelopmentDelta,Enable=item.Enable};
        }
        return copy;
    }

    /// <summary>按任务资源码读当前持有量（不写存档），码非法返回 false。</summary>
    private static bool TryReadResource(uint code, out uint amount)
    {
        amount = 0u;
        InventoryService.ResourceKind kind;
        if (!OfficeTaskCatalog.TryGetResourceKind(code, out kind)) return false;
        amount = InventoryService.Instance.GetResource(kind);
        return true;
    }

    /// <summary>按任务资源码读当前持有量；与 TryReadResource 同义，保留给结算流程统一使用。</summary>
    private static bool TryGetResource(uint code, out uint amount)
    {
        return TryReadResource(code, out amount);
    }

    /// <summary>按任务资源码增加资源，返回新增后的持有量；码非法返回 0 并给出原因。</summary>
    private static bool TryAddResource(uint code, uint amount, out uint result, out string reason)
    {
        reason = null;
        result = 0u;
        if(amount==0u)return true;
        InventoryService.ResourceKind kind;
        if (!OfficeTaskCatalog.TryGetResourceKind(code, out kind))
        {
            reason = "资源码非法：" + code;
            return false;
        }
        if (amount == 0u)
        {
            result = InventoryService.Instance.GetResource(kind);
            return true;
        }
        result = InventoryService.Instance.AddResource(kind, amount);
        return true;
    }

    /// <summary>按任务资源码扣减资源；不足时返回 false 且不改变持有量。</summary>
    private static bool TrySpendResource(uint code, uint amount, out string reason)
    {
        reason = null;
        if (amount == 0u) return true;
        InventoryService.ResourceKind kind;
        if (!OfficeTaskCatalog.TryGetResourceKind(code, out kind))
        {
            reason = "资源码非法：" + code;
            return false;
        }
        return InventoryService.Instance.SpendResource(kind, amount, out reason);
    }

    /// <summary>找到某任务的保存记录；没有则返回 null。允许 payload.officeTasks 为 null。</summary>
    private static OfficeTaskSave FindRecord(SavePayload payload, uint taskId)
    {
        if (payload == null || payload.officeTasks == null) return null;
        for (int i = 0; i < payload.officeTasks.Count; i++)
        {
            OfficeTaskSave record = payload.officeTasks[i];
            if (record != null && record.taskId == taskId) return record;
        }
        return null;
    }

    /// <summary>确保列表存在；只在事务内部调用，读列表路径不会走到这里。</summary>
    private static void EnsureLists(SavePayload payload)
    {
        if (payload != null && payload.officeTasks == null) payload.officeTasks = new List<OfficeTaskSave>();
    }

    /// <summary>三项指标按 long 中间值累加后限 0..int.MaxValue，不接受溢出。</summary>
    private static int ClampStat(int current, int delta)
    {
        long next = (long)current + delta;
        if (next < 0L) return 0;
        if (next > StatMax) return StatMax;
        return (int)next;
    }

    /// <summary>三项村庄指标的增量与失败惩罚；奖励与惩罚都走这里，避免两处规则漂移。</summary>
    private static void ApplyStats(SavePayload payload, int reputation, int influence, int development)
    {
        payload.reputation = ClampStat(payload.reputation, reputation);
        payload.influence = ClampStat(payload.influence, influence);
        payload.villageDevelopment = ClampStat(payload.villageDevelopment, development);
    }

    /// <summary>结算选项奖励：资源只在配置了正数量时入账，三项指标按配置增减；调用前必须先通过预检。</summary>
    private static bool TryApplyReward(SavePayload payload, OfficeTaskTable_OfficeTaskOptionConfig option, out string reason)
    {
        reason = null;
        if (option == null) return true;
        if(option.RewardAmount>0u)
        {
            if(!TryReadResource(option.RewardResourceType,out var have)||(ulong)have+option.RewardAmount>uint.MaxValue)
            {reason="奖励资源不存在或将超过上限";return false;}
        }
        uint added;
        if (!TryAddResource(option.RewardResourceType, option.RewardAmount, out added, out reason)) return false;
        ApplyStats(payload, option.ReputationDelta, option.InfluenceDelta, option.DevelopmentDelta);
        return true;
    }

    /// <summary>失败惩罚：资源按 min(持有, 惩罚) 扣减，三项指标按配置扣并限 0。</summary>
    private static void ApplyFailurePenalty(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config)
    {
        if (payload == null || config == null) return;
        uint amount = config.FailureResourceAmount;
        if (amount > 0u)
        {
            uint have;
            if (TryGetResource(config.FailureResourceType, out have))
            {
                uint cost = have < amount ? have : amount;
                string ignored;
                TrySpendResource(config.FailureResourceType, cost, out ignored);
            }
        }
        ApplyStats(payload, config.FailureReputation, config.FailureInfluence, config.FailureDevelopment);
    }

    /// <summary>
    /// 主控在“有条件变化”时调用：只激活满足条件、尚无记录或已冷却完的任务。
    /// 没有可激活项时不发起事务（避免每帧空保存）；返回是否发生了保存。
    /// 只对已启用任务建记录，被禁用的任务不写存档。
    /// </summary>
    public bool RefreshTriggers()
    {
        SavePayload payload;
        if (!TryGetPayload(out payload) || MetaCommandRunner.IsExecuting) return false;
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) return false;
        var pending = new List<OfficeTaskTable_OfficeTaskConfig>();
        for (int i = 0; i < set.Tasks.Count; i++)
            if (CanActivate(set, payload, set.Tasks[i], payload.officeClockSeconds)) pending.Add(set.Tasks[i]);
        if (pending.Count == 0) return false;
        return RunAtomic("办事任务刷新触发", () =>
        {
            int activated = 0;
            for (int i = 0; i < pending.Count; i++)
                if (Activate(payload, set, pending[i])) activated++;
            return activated > 0;
        }, out _);
    }

    /// <summary>
    /// 推进可计时时间：累计到 Payload.officeClockSeconds，然后最多处理一轮过期与冷却重开。
    /// 负数 / NaN / 无穷大明确拒绝；无会话或事务执行中不推进；seconds=0 只做评估。
    /// </summary>
    public bool Tick(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0.0)
        {
            LogWarn("[办事任务] 计时参数非法：" + seconds);
            return false;
        }
        SavePayload payload;
        if (!TryGetPayload(out payload) || MetaCommandRunner.IsExecuting) return false;
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) return false;
        double clock=payload.officeClockSeconds+seconds;
        if(double.IsNaN(clock)||double.IsInfinity(clock))return false;
        // 普通走时交给运行驱动周期保存，不创建会因无到期事件而回滚时钟的空事务。
        payload.officeClockSeconds=clock;
        bool committed=false;
        if(HasDueWork(set,payload))committed=RunAtomic("办事任务到期处理",()=>{
            bool fired=ProcessDue(set,payload);
            bool reopened=ReopenDue(set,payload);
            return fired||reopened;
        },out _);
        // 进度或前置任务改变时也能激活新事务，不仅限于首次进入村庄。
        return RefreshTriggers()||committed;
    }

    /// <summary>日志统一走配置通道，LogChannel 没有独立玩法通道。</summary>
    private static void LogWarn(string message)
    {
        ChaosLog.Warn(LogChannel.Config, message);
    }

    /// <summary>存在需要处理的过期或冷却到期记录（用于避免空事务）。</summary>
    private static bool HasDueWork(OfficeTaskCatalogSet set, SavePayload payload)
    {
        if (payload == null || payload.officeTasks == null) return false;
        double clock = payload.officeClockSeconds;
        for (int i = 0; i < payload.officeTasks.Count; i++)
        {
            OfficeTaskSave record = payload.officeTasks[i];
            if (record == null) continue;
            OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(record.taskId);
            if (entry == null) continue;
            if (record.status == OfficeTaskStatus.Active && entry.Config.DurationSeconds > 0u
                && clock >= record.deadlineClock) return true;
            if (record.status != OfficeTaskStatus.Active && entry.Config.Repeatable
                && clock >= record.nextActivationClock && CanActivate(set, payload, entry.Config, clock)) return true;
        }
        return false;
    }

    /// <summary>
    /// 一轮到期处理：限时任务超时按目标类型转失败或待战斗；成功处理时返回 true。
    /// 本函数不再改写 cycle 等无关字段，避免把“没有实际变化”也算成需要保存。
    /// </summary>
    private static bool ProcessDue(OfficeTaskCatalogSet set, SavePayload payload)
    {
        if (payload == null || payload.officeTasks == null) return false;
        bool changed = false;
        double clock = payload.officeClockSeconds;
        for (int i = 0; i < payload.officeTasks.Count; i++)
        {
            OfficeTaskSave record = payload.officeTasks[i];
            if (record == null) continue;
            OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(record.taskId);
            if (entry == null) continue;
            if (record.status != OfficeTaskStatus.Active) continue;
            if (entry.Config.DurationSeconds == 0u || clock < record.deadlineClock) continue;
            if (entry.Config.GoalType == (uint)OfficeTaskGoal.VillageRaid)
            {
                // 超过时限的遭遇袭击转待战斗接入：不直接判胜利、不发奖、不判输赢
                record.status = OfficeTaskStatus.AwaitingBattle;
                record.deadlineClock = 0.0;
            }
            else
            {
                record.status = OfficeTaskStatus.Failed;
                ApplyFailurePenalty(payload, entry.Config);
                ScheduleReopen(entry.Config, clock, record);
            }
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// 冷却到期的一轮重开：每条可重复任务在一次 Tick 内最多重开一次，
    /// 因此大时间增量不会循环刷奖励；刚失败的任务也走这里而不是立即重开。
    /// </summary>
    private static bool ReopenDue(OfficeTaskCatalogSet set, SavePayload payload)
    {
        if (payload == null) return false;
        bool changed = false;
        double clock = payload.officeClockSeconds;
        for (int i = 0; i < set.Tasks.Count; i++)
        {
            OfficeTaskTable_OfficeTaskConfig config = set.Tasks[i];
            if (config == null || !config.Repeatable) continue;
            OfficeTaskSave record = FindRecord(payload, config.Index);
            if (record == null || record.status == OfficeTaskStatus.Active || record.status == OfficeTaskStatus.AwaitingBattle) continue;
            if (!CanActivate(set, payload, config, clock)) continue;
            Activate(payload, set, config);
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// 激活判定：时代区间、进入次数、最深通关、前置完成，以及该任务当前状态是否允许重新开始。
    /// 郡县指派事务写入记录后永不重开；等待战斗的轮次也不重开。
    /// </summary>
    private static bool CanActivate(OfficeTaskCatalogSet set, SavePayload payload, OfficeTaskTable_OfficeTaskConfig config, double clock)
    {
        if (config == null || !config.Enable) return false;
        if (payload.era < config.MinEra || payload.era > config.MaxEra) return false;
        if (payload.runs < config.MinRuns || payload.maxClearedDepth < config.MinClearedDepth) return false;
        if (config.PrerequisiteTaskId != 0u && !IsPrerequisiteCompleted(payload, config.PrerequisiteTaskId)) return false;
        OfficeTaskSave record = FindRecord(payload, config.Index);
        if (record == null) return true;
        if (record.status == OfficeTaskStatus.Active || record.status == OfficeTaskStatus.AwaitingBattle
            || record.status == OfficeTaskStatus.Available) return false;
        if (!config.Repeatable) return false;                 // 郡县指派等一次性任务只触发一次
        return clock >= record.nextActivationClock;           // 冷却未到不重开
    }

    /// <summary>前置任务是否已完成过至少一轮（cycle 从 1 开始，completedCycles 永久保留）。</summary>
    private static bool IsPrerequisiteCompleted(SavePayload payload, uint prerequisite)
    {
        OfficeTaskSave record = FindRecord(payload, prerequisite);
        return record != null && record.completedCycles > 0u;
    }

    /// <summary>写入新的一轮：cycle 从 1 开始，有时限才写 deadlineClock；等待战斗的轮次不会被覆盖。</summary>
    private static bool Activate(SavePayload payload, OfficeTaskCatalogSet set, OfficeTaskTable_OfficeTaskConfig config)
    {
        if (payload == null || config == null) return false;
        EnsureLists(payload);
        OfficeTaskSave record = FindRecord(payload, config.Index);
        if (record == null)
        {
            record = new OfficeTaskSave { taskId = config.Index, cycle = 1u };
            payload.officeTasks.Add(record);
        }
        else
        {
            record.cycle = record.cycle == 0u ? 1u : record.cycle + 1u;
        }
        record.status = config.RequiresAcceptance ? OfficeTaskStatus.Available : OfficeTaskStatus.Active;
        record.selectedOptionId = 0u;
        record.startedClears = payload.clears;
        record.deadlineClock = !config.RequiresAcceptance && config.DurationSeconds > 0u
            ? payload.officeClockSeconds + config.DurationSeconds : 0.0;
        record.nextActivationClock = 0.0;
        return true;
    }

    /// <summary>结束后安排下一次可重开时刻；不可重复任务不安排冷却。</summary>
    private static void ScheduleReopen(OfficeTaskTable_OfficeTaskConfig config, double clock, OfficeTaskSave record)
    {
        if (config == null || record == null) return;
        record.deadlineClock = 0.0;
        record.selectedOptionId = 0u;
        record.nextActivationClock = config.Repeatable ? clock + config.CooldownSeconds : 0.0;
    }

    /// <summary>完成一轮：completedCycles 永久累加（用于前置与时代门槛），并按冷却安排下一次可重开时刻。</summary>
    private static void FinishCycle(OfficeTaskTable_OfficeTaskConfig config, OfficeTaskSave record, OfficeTaskStatus status, double clock)
    {
        if (record == null) return;
        record.status = status;
        if (status == OfficeTaskStatus.Completed && record.completedCycles < uint.MaxValue)
            record.completedCycles += 1u;
        record.deadlineClock = 0.0;
        record.selectedOptionId = 0u;
        record.nextActivationClock = config != null && config.Repeatable ? clock + config.CooldownSeconds : 0.0;
    }

    /// <summary>显式接取只作用于新配置；倒计时与通关基线从成功接取时开始。</summary>
    public bool TryAccept(uint taskId, uint cycle, int contextId, out string reason)
    {
        if (!TryGetPayload(out var payload) || contextId != GameSession.Instance.ContextId)
        { reason = "当前任务会话已失效"; return false; }
        var entry = OfficeTaskCatalog.EnsureLoaded()?.GetEntry(taskId);
        var record = FindRecord(payload, taskId);
        if (entry == null || !entry.Config.RequiresAcceptance || record == null
            || record.cycle != cycle || record.status != OfficeTaskStatus.Available)
        { reason = "该轮事务当前不可接取"; return false; }
        if (payload.era < entry.Config.MinEra || payload.era > entry.Config.MaxEra)
        { reason = "该事务不属于当前时代"; return false; }
        return RunAtomic("接取地图事务", (out string why) => {
            why = null;
            record.status = OfficeTaskStatus.Active;
            record.startedClears = payload.clears;
            record.deadlineClock = entry.Config.DurationSeconds == 0 ? 0
                : payload.officeClockSeconds + entry.Config.DurationSeconds;
            return true;
        }, out reason);
    }

    /// <summary>只接受本存档活动轮次的选项；交付、奖励和完成记录由一次命令提交。</summary>
    public bool TryChoose(uint taskId, uint cycle, uint optionId, int contextId, out string reason)
    {
        SavePayload payload;
        if (!TryGetPayload(out payload)) { reason = "没有活动会话，请先读档"; return false; }
        if (MetaCommandRunner.IsExecuting) { reason = "另一玩法命令正在执行，请稍候"; return false; }
        if (contextId != GameSession.Instance.ContextId) { reason = "面板上下文已失效，请重新打开"; return false; }
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) { reason = OfficeTaskCatalog.LastError ?? "任务配置缺失"; return false; }
        return RunAtomic("办事任务选择", (out string inner) =>
            ChooseInternal(payload, set, taskId, cycle, optionId, out inner), out reason);
    }

    /// <summary>选择的具体规则；任何一步失败都返回 false，由外层命令整体回滚。</summary>
    private static bool ChooseInternal(SavePayload payload, OfficeTaskCatalogSet set,
        uint taskId, uint cycle, uint optionId, out string reason)
    {
        reason = null;
        OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(taskId);
        if (entry == null) { reason = "任务不存在或已禁用：" + taskId; return false; }
        OfficeTaskTable_OfficeTaskConfig config = entry.Config;
        OfficeTaskSave record = FindRecord(payload, taskId);
        if (record == null) { reason = "任务尚未触发：" + taskId; return false; }
        if (record.cycle != cycle) { reason = "任务轮次已变化，请刷新列表"; return false; }
        if (record.status != OfficeTaskStatus.Active)
        {
            reason = record.status == OfficeTaskStatus.AwaitingBattle ? "任务正在等待战斗接入，不能重复选择"
                : "任务当前状态不允许选择：" + record.status;
            return false;
        }
        if(config.DurationSeconds>0u&&payload.officeClockSeconds>=record.deadlineClock)
        {reason="任务时限已结束，不能再提交选项";return false;}
        if(config.GoalType==(uint)OfficeTaskGoal.VillageRaid&&record.selectedOptionId!=0u)
        {reason="本轮应对方案已经记录，不能重复支付或更换";return false;}
        OfficeTaskTable_OfficeTaskOptionConfig option = entry.FindOption(optionId);
        if (option == null) { reason = "选项不属于该任务或已禁用：" + optionId; return false; }
        if (record.selectedOptionId == optionId) { reason = "已提交过相同选项，不重复结算"; return false; }

        double clock = payload.officeClockSeconds;
        switch ((OfficeTaskGoal)config.GoalType)
        {
            case OfficeTaskGoal.Choice:
                return CompleteWithOption(payload, config, record, option, clock, out reason);
            case OfficeTaskGoal.ResourceDelivery:
                return Deliver(payload, config, record, option, clock, out reason);
            case OfficeTaskGoal.ClearRuns:
                return FinishClears(payload, config, record, option, clock, out reason);
            case OfficeTaskGoal.VillageRaid:
                return PrepareRaid(payload, config, record, option, out reason);
            default:
                reason = "未知任务目标类型：" + config.GoalType;
                return false;
        }
    }

    /// <summary>选项类任务：选合法选项即完成，选项代价与奖励在同一事务内结算。</summary>
    private static bool CompleteWithOption(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskSave record, OfficeTaskTable_OfficeTaskOptionConfig option, double clock, out string reason)
    {
        if (!PrecheckSettlement(payload, config, option, out reason)) return false;
        if (!Settle(payload, config, option, out reason)) return false;
        FinishCycle(config, record, OfficeTaskStatus.Completed, clock);
        return true;
    }

    /// <summary>资源交付：任务要求量与选项额外代价合计预检后一次扣除，再结算奖励。</summary>
    private static bool Deliver(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskSave record, OfficeTaskTable_OfficeTaskOptionConfig option, double clock, out string reason)
    {
        if (!PrecheckSettlement(payload, config, option, out reason)) return false;
        if (!Settle(payload, config, option, out reason)) return false;
        FinishCycle(config, record, OfficeTaskStatus.Completed, clock);
        return true;
    }

    /// <summary>通关类：当前 clears 相对本轮开始时的增量达到要求才允许结算。</summary>
    private static bool FinishClears(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskSave record, OfficeTaskTable_OfficeTaskOptionConfig option, double clock, out string reason)
    {
        if (ClearsSinceStart(payload, record) < config.RequiredClears)
        {
            reason = "本轮通关次数尚未达标（" + ClearsSinceStart(payload, record) + "/" + config.RequiredClears + "）";
            return false;
        }
        if (!PrecheckSettlement(payload, config, option, out reason)) return false;
        if (!Settle(payload, config, option, out reason)) return false;
        FinishCycle(config, record, OfficeTaskStatus.Completed, clock);
        return true;
    }

    /// <summary>遭遇袭击：只记录策略并扣选项成本，倒计时结束后转待战斗，不提前发奖。</summary>
    private static bool PrepareRaid(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskSave record, OfficeTaskTable_OfficeTaskOptionConfig option, out string reason)
    {
        if (payload.era < config.MinEra || payload.era > config.MaxEra) { reason = "当前时代不适用该任务"; return false; }
        // 准备阶段只支付策略成本；战斗奖励必须等真实结果回报后再发放。
        if(!TrySpendResource(option.CostResourceType,option.CostAmount,out reason))return false;
        record.selectedOptionId = option.Index;
        return true;
    }

    /// <summary>
    /// 任务要求量与选项代价按资源码合并后统一预检：
    /// 同类资源先合计再与持有量比较，不足整体拒绝；奖励入账后若超过 uint 上限也整体拒绝。
    /// </summary>
    private static bool PrecheckSettlement(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskTable_OfficeTaskOptionConfig option, out string reason)
    {
        reason = null;
        var demand = new Dictionary<uint, ulong>();
        if (config.GoalType == (uint)OfficeTaskGoal.ResourceDelivery && config.RequiredAmount > 0u)
            AddDemand(demand, config.ResourceType, config.RequiredAmount);
        if (option != null && option.CostAmount > 0u)
            AddDemand(demand, option.CostResourceType, option.CostAmount);
        foreach (KeyValuePair<uint, ulong> pair in demand)
        {
            uint have;
            if (!TryGetResource(pair.Key, out have))
            {
                reason = "代价资源码非法：" + pair.Key + "（只允许 1 物资 / 2 材料 / 3 图纸 / 4 钱财）";
                return false;
            }
            if ((ulong)have < pair.Value)
            {
                InventoryService.ResourceKind kind;
                string name = OfficeTaskCatalog.TryGetResourceKind(pair.Key, out kind)
                    ? InventoryService.ResourceDisplayName(kind) : pair.Key.ToString();
                reason = name + "不足（需要 " + pair.Value + "，持有 " + have + "）";
                return false;
            }
        }
        if (option == null || option.RewardAmount == 0u) return true;
        uint rewardHave;
        if (!TryGetResource(option.RewardResourceType, out rewardHave))
        {
            reason = "奖励资源码非法：" + option.RewardResourceType;
            return false;
        }
        ulong afterSpend = rewardHave;
        ulong spendSame;
        if (demand.TryGetValue(option.RewardResourceType, out spendSame)) afterSpend = rewardHave - spendSame;
        if (afterSpend + option.RewardAmount > uint.MaxValue)
        {
            reason = "结算后资源会超过上限，本次操作已拒绝";
            return false;
        }
        return true;
    }

    /// <summary>累计同一资源码的需求量，超出 ulong 时按 ulong 上限饱和（预检必然判为不足）。</summary>
    private static void AddDemand(Dictionary<uint, ulong> demand, uint code, uint amount)
    {
        ulong current;
        demand.TryGetValue(code, out current);
        ulong next = current + amount;
        demand[code] = next < current ? ulong.MaxValue : next;
    }

    /// <summary>
    /// 按预检通过的组合执行扣减与奖励：先扣任务要求量，再扣选项代价，最后结算奖励。
    /// 任一步失败立即返回 false（外层事务会整体回滚），不做“部分成功”。
    /// </summary>
    private static bool Settle(SavePayload payload, OfficeTaskTable_OfficeTaskConfig config,
        OfficeTaskTable_OfficeTaskOptionConfig option, out string reason)
    {
        reason = null;
        if (config.GoalType == (uint)OfficeTaskGoal.ResourceDelivery && config.RequiredAmount > 0u)
        {
            if (!TrySpendResource(config.ResourceType, config.RequiredAmount, out reason)) return false;
        }
        if (option != null && option.CostAmount > 0u)
        {
            if (!TrySpendResource(option.CostResourceType, option.CostAmount, out reason)) return false;
        }
        return TryApplyReward(payload, option, out reason);
    }

    /// <summary>
    /// 统一的原子提交入口：命令名 + 变更体，全部由 MetaCommandRunner 负责保存与回滚。
    /// 真实签名是 Execute(string, Mutation, out string, Action, Action)；
    /// 变更体自身的失败原因经局部变量取回，不依赖 out 参数在闭包中传递。
    /// </summary>
    private static bool RunAtomic(string command, Func<bool> mutation, out string reason)
    {return RunAtomic(command,(out string why)=>{why=null;return mutation();},out reason);}

    private static bool RunAtomic(string command, MetaCommandRunner.Mutation mutation, out string reason)
    {
        string failure = null;
        bool committed = MetaCommandRunner.Execute(command, InternalMutation, out string runnerReason);
        if (committed) { reason = null; return true; }
        if (string.IsNullOrEmpty(failure)) failure = runnerReason;
        reason = string.IsNullOrEmpty(failure) ? "操作未完成" : failure;
        return false;

        // 局部函数：只把变更体结果与原因转写到外部变量，不捕获 out 参数。
        bool InternalMutation(out string internalReason)
        {
            string local;
            bool accepted;
            try { accepted = mutation(out local); }
            catch (Exception error)
            {
                failure = "操作未完成：" + error.Message;
                internalReason = failure;
                return false;
            }
            if (accepted)
            {
                internalReason = null;
                return true;
            }
            failure = string.IsNullOrEmpty(local) ? "操作未完成" : local;
            internalReason = failure;
            return false;
        }
    }

    /// <summary>
    /// 战斗结果回报（仅未来桥接调用）：AWAITING_BATTLE 且 ctx/cycle 一致才受理；
    /// 胜利按已选策略发奖（未选策略则无奖励），失败只做配置惩罚，重复回报拒绝。
    /// 本服务不伪造战斗结果，现有界面不得直接调用。
    /// </summary>
    public bool ReportBattleResult(uint taskId, uint cycle, bool won, int contextId, out string reason)
    {
        SavePayload payload;
        if (!TryGetPayload(out payload)) { reason = "没有活动会话，请先读档"; return false; }
        if (contextId != GameSession.Instance.ContextId) { reason = "面板上下文已失效，请重新打开"; return false; }
        if (MetaCommandRunner.IsExecuting) { reason = "另一玩法命令正在执行，请稍候"; return false; }
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) { reason = OfficeTaskCatalog.LastError ?? "任务配置缺失"; return false; }
        return RunAtomic(won ? "办事任务战斗胜利" : "办事任务战斗失败", (out string inner) =>
            ReportInternal(payload, set, taskId, cycle, won, out inner), out reason);
    }

    private static bool ReportInternal(SavePayload payload, OfficeTaskCatalogSet set,
        uint taskId, uint cycle, bool won, out string reason)
    {
        reason = null;
        OfficeTaskCatalogSet.TaskEntry entry = set.GetEntry(taskId);
        if (entry == null) { reason = "任务不存在或已禁用：" + taskId; return false; }
        if (entry.Config.GoalType != (uint)OfficeTaskGoal.VillageRaid) { reason = "该任务不是遭遇袭击任务"; return false; }
        OfficeTaskSave record = FindRecord(payload, taskId);
        if (record == null) { reason = "任务尚未触发：" + taskId; return false; }
        if (record.cycle != cycle) { reason = "战斗回报的轮次与当前任务不一致"; return false; }
        if (record.status != OfficeTaskStatus.AwaitingBattle)
        {
            reason = record.status == OfficeTaskStatus.Completed || record.status == OfficeTaskStatus.Failed
                ? "该轮已结算，重复回报被拒绝" : "任务尚未进入等待战斗状态";
            return false;
        }
        // 外层入口已经持有事务，此处只修改结果，不能再嵌套命令。
        {
            double clock=payload.officeClockSeconds;
            if(won)
            {
                var option=entry.FindOption(record.selectedOptionId);
                if(option!=null&&!TryApplyReward(payload,option,out reason))return false;
                FinishCycle(entry.Config,record,OfficeTaskStatus.Completed,clock);
            }
            else {ApplyFailurePenalty(payload,entry.Config);FinishCycle(entry.Config,record,OfficeTaskStatus.Failed,clock);}
            return true;
        }
    }

    /// <summary>
    /// 时代推进门槛（只判定，不写存档）。参数 era 与 EraGate.Era 同义：当前待推进时代，允许 1..4，
    /// 1 表示 1→2。该时代所有已启用的门槛任务都完成过才放行；同一时代多条门槛按 AND 处理，
    /// 不替代既有时代条件。
    /// </summary>
    public bool CanAdvanceEra(uint era, out string reason)
    {
        reason = null;
        SavePayload payload;
        if (!TryGetPayload(out payload)) { reason = "没有活动会话，请先读档"; return false; }
        if (era < 1u || era >= OfficeTaskCatalog.MaxEra)
        {
            reason = "待推进时代必须在 1.." + (OfficeTaskCatalog.MaxEra - 1u) + " 之间（1 表示 1 到 2）";
            return false;
        }
        OfficeTaskCatalogSet set = OfficeTaskCatalog.EnsureLoaded();
        if (set == null) { reason = OfficeTaskCatalog.LastError ?? "任务配置缺失"; return false; }
        List<OfficeTaskTable_OfficeEraGateConfig> gates;
        if (!set.EraGatesByEra.TryGetValue(era, out gates) || gates == null || gates.Count == 0) return true;
        for (int i = 0; i < gates.Count; i++)
        {
            OfficeTaskTable_OfficeEraGateConfig gate = gates[i];
            if (gate == null || !gate.Enable) continue;
            if (!IsTaskCompleted(payload, gate.TaskId))
            {
                OfficeTaskTable_OfficeTaskConfig task = set.GetEntry(gate.TaskId)?.Config;
                string title = task == null || string.IsNullOrEmpty(task.TitleText)
                    ? gate.TaskId.ToString() : task.TitleText;
                reason = "推进到时代 " + (era + 1u) + " 需要先完成郡县任务：" + title;
                return false;
            }
        }
        return true;
    }

    /// <summary>当前待推进时代的第一道未完成门槛；没有门槛时返回 null。只读，供界面提示。</summary>
    public OfficeTaskTable_OfficeEraGateConfig GetPendingEraGate(out uint era)
    {
        era = 0u;
        SavePayload payload;
        if (!TryGetPayload(out payload)) return null;
        OfficeTaskTable_OfficeEraGateConfig gate = OfficeTaskCatalog.FirstPendingGate(payload.era,
            taskId => IsTaskCompleted(payload, taskId));
        if (gate != null) era = gate.Era;
        return gate;
    }

    /// <summary>该任务是否已完成过至少一轮；无记录视为未完成。</summary>
    private static bool IsTaskCompleted(SavePayload payload, uint taskId)
    {
        OfficeTaskSave record = FindRecord(payload, taskId);
        return record != null && record.completedCycles > 0u;
    }
}
