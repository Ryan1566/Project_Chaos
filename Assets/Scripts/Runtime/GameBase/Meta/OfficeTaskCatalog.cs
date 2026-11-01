using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 办事单位任务配置目录：一次性读取三张任务相关表并完成全量校验。
/// 只做“读配置 + 校验”，不读写存档、不推进任何玩法；校验失败时整批作废并明确报告缺失或非法条目。
/// 不定义额外辅助类型（时代规则、资源码规则都放在本类内部），避免出现未交付的依赖。
/// </summary>
public static class OfficeTaskCatalog
{
    /// <summary>三张表的类型名（由 ConfigTableAssets 按类型名解析资源索引）。</summary>
    public const string TaskClassName = "OfficeTaskTable_OfficeTaskConfig";
    public const string OptionClassName = "OfficeTaskTable_OfficeTaskOptionConfig";
    public const string EraGateClassName = "OfficeTaskTable_OfficeEraGateConfig";

    /// <summary>时代上限，与 SavePayload.era 的取值范围 1..5 一致。</summary>
    public const uint MaxEra = 5u;

    /// <summary>任务时限的合理上限（约 30 天），避免把 uint 极值写入存档。</summary>
    public const uint MaxDurationSeconds = 2592000u;

    /// <summary>被禁用的任务要保留记录，否则读档后会把它当成“从未出现”而再次触发。</summary>
    public const OfficeTaskStatus DisabledTaskStatus = OfficeTaskStatus.Failed;

    /// <summary>最后一次加载得到的配置聚合；尚未成功加载时为 null。</summary>
    public static OfficeTaskCatalogSet Current { get; private set; }

    /// <summary>上一次加载失败的原因；成功加载后为 null。</summary>
    public static string LastError { get; private set; }

    private static bool _loaded;

    /// <summary>确保已加载；只在首次需要时读表。加载失败返回 null，不抛异常。</summary>
    public static OfficeTaskCatalogSet EnsureLoaded()
    {
        if (_loaded) return Current;
        _loaded = true;
        Reload();
        return Current;
    }

    /// <summary>强制重新读表并校验；只有新目录校验通过才替换旧目录。</summary>
    public static void Reload()
    {
        _loaded = true;
        LastError = null;
        OfficeTaskCatalogSet next = LoadAll(out string error);
        if (next == null)
        {
            Current = null;
            LastError = error;
            return;
        }
        Current = next;
    }

    /// <summary>返回全部有效任务定义（按表顺序）；未加载成功时为空数组。</summary>
    public static OfficeTaskTable_OfficeTaskConfig[] GetTasks()
    {
        OfficeTaskCatalogSet set = EnsureLoaded();
        return set == null ? new OfficeTaskTable_OfficeTaskConfig[0] : set.Tasks.ToArray();
    }

    /// <summary>按任务 ID 取定义；配置未加载或不存在时返回 null。</summary>
    public static OfficeTaskTable_OfficeTaskConfig GetTask(uint taskId)
    {
        OfficeTaskCatalogSet set = EnsureLoaded();
        OfficeTaskCatalogSet.TaskEntry entry = set == null ? null : set.GetEntry(taskId);
        return entry == null ? null : entry.Config;
    }

    /// <summary>取某任务的全部有效选项（按表顺序）；不存在时为空数组。</summary>
    public static OfficeTaskTable_OfficeTaskOptionConfig[] GetOptions(uint taskId)
    {
        OfficeTaskCatalogSet set = EnsureLoaded();
        OfficeTaskCatalogSet.TaskEntry entry = set == null ? null : set.GetEntry(taskId);
        return entry == null ? new OfficeTaskTable_OfficeTaskOptionConfig[0] : entry.OptionList();
    }

    /// <summary>取某任务的全部时代门槛记录；无门槛时为空数组。</summary>
    public static OfficeTaskTable_OfficeEraGateConfig[] GetEraGates(uint taskId)
    {
        OfficeTaskCatalogSet set = EnsureLoaded();
        OfficeTaskCatalogSet.TaskEntry entry = set == null ? null : set.GetEntry(taskId);
        return entry == null || entry.EraGates.Count == 0
            ? new OfficeTaskTable_OfficeEraGateConfig[0] : entry.EraGates.ToArray();
    }

    /// <summary>
    /// 按“当前待推进时代”取门槛记录：EraGate.Era = 1 表示 1→2，允许 1..4。
    /// 该参数与 CanAdvanceEra 的当前时代参数同义，不是目标时代。
    /// </summary>
    public static OfficeTaskTable_OfficeEraGateConfig[] GetEraGatesByEra(uint currentEra)
    {
        OfficeTaskCatalogSet set = EnsureLoaded();
        if (set == null) return new OfficeTaskTable_OfficeEraGateConfig[0];
        List<OfficeTaskTable_OfficeEraGateConfig> list;
        if (!set.EraGatesByEra.TryGetValue(currentEra, out list) || list == null || list.Count == 0)
            return new OfficeTaskTable_OfficeEraGateConfig[0];
        return list.ToArray();
    }

    /// <summary>
    /// 从当前待推进时代出发，逐级检查后续每个时代门槛是否都已完成；
    /// 用于给调用端显示“下一道门槛”，不修改任何状态。返回 null 表示没有后续门槛。
    /// </summary>
    public static OfficeTaskTable_OfficeEraGateConfig FirstPendingGate(uint currentEra, Func<uint, bool> isTaskCompleted)
    {
        if (isTaskCompleted == null) return null;
        for (uint era = currentEra < 1u ? 1u : currentEra; era < MaxEra; era++)
        {
            List<OfficeTaskTable_OfficeEraGateConfig> gates;
            OfficeTaskCatalogSet set = EnsureLoaded();
            if (set == null || !set.EraGatesByEra.TryGetValue(era, out gates) || gates == null) continue;
            for (int i = 0; i < gates.Count; i++)
            {
                OfficeTaskTable_OfficeEraGateConfig gate = gates[i];
                if (gate != null && gate.Enable && !isTaskCompleted(gate.TaskId)) return gate;
            }
        }
        return null;
    }

    /// <summary>
    /// 资源码规则：1 物资 / 2 材料 / 3 图纸 / 4 钱财；数量为 0 的选项允许用 0 表示“无资源”。
    /// 配置校验与业务消费都必须走这一处，避免两边判定不一致。
    /// </summary>
    public static bool IsUsableResourceCode(uint code)
    {
        return code >= 1u && code <= 4u;
    }

    /// <summary>数量与资源码组合是否合法：有数量必须带 1..4 的资源码；数量为 0 时资源码允许为 0。</summary>
    public static bool IsUsableResourcePair(uint code, uint amount)
    {
        if (amount > 0u) return IsUsableResourceCode(code);
        return code == 0u || IsUsableResourceCode(code);
    }

    /// <summary>把资源码翻译成 InventoryService 的资源种类；非法码返回 false。</summary>
    public static bool TryGetResourceKind(uint code, out InventoryService.ResourceKind kind)
    {
        switch (code)
        {
            case 1u: kind = InventoryService.ResourceKind.Goods; return true;
            case 2u: kind = InventoryService.ResourceKind.Material; return true;
            case 3u: kind = InventoryService.ResourceKind.Blueprint; return true;
            case 4u: kind = InventoryService.ResourceKind.Money; return true;
            default: kind = InventoryService.ResourceKind.Goods; return false;
        }
    }

    /// <summary>依次读取三张表并校验；任何一步失败都返回 null 并给出可直接定位的原因。</summary>
    private static OfficeTaskCatalogSet LoadAll(out string error)
    {
        error = null;
        List<OfficeTaskTable_OfficeTaskConfig> tasks = ReadTable<OfficeTaskTable_OfficeTaskConfig>(TaskClassName, out error);
        if (tasks == null) return null;
        List<OfficeTaskTable_OfficeTaskOptionConfig> options = ReadTable<OfficeTaskTable_OfficeTaskOptionConfig>(OptionClassName, out error);
        if (options == null) return null;
        List<OfficeTaskTable_OfficeEraGateConfig> gates = ReadTable<OfficeTaskTable_OfficeEraGateConfig>(EraGateClassName, out error);
        if (gates == null) return null;

        var set = new OfficeTaskCatalogSet();
        string problem = OfficeTaskCatalogSet.Build(set, tasks, options, gates);
        if (problem != null)
        {
            error = problem;
            return null;
        }
        return set;
    }

    /// <summary>按类型名读一张表；缺表、索引缺类型、Json 损坏都转成文字原因返回。</summary>
    private static List<T> ReadTable<T>(string className, out string error) where T : class
    {
        error = null;
        TextAsset asset;
        try { asset = ConfigTableAssets.Load(className); }
        catch (Exception e)
        {
            error = "配置表 " + className + " 未能解析为资源：" + e.Message;
            return null;
        }
        if (asset == null)
        {
            error = "配置缺失：找不到 " + className + "（表未发布或索引未包含该类型）";
            return null;
        }
        DataList<T> list;
        try { list = JsonUtility.FromJson<DataList<T>>(asset.text); }
        catch (Exception e)
        {
            error = "配置表 " + className + " 格式错误：" + e.Message;
            return null;
        }
        if (list == null || list.datas == null)
        {
            error = "配置表 " + className + " 缺少 datas 容器";
            return null;
        }
        return list.datas;
    }
}

/// <summary>校验通过后的任务配置聚合；任务 → 选项 / 时代门槛已建索引。</summary>
public sealed class OfficeTaskCatalogSet
{
    /// <summary>一个任务连同它的选项、时代门槛。</summary>
    public sealed class TaskEntry
    {
        public OfficeTaskTable_OfficeTaskConfig Config;
        public readonly List<OfficeTaskTable_OfficeTaskOptionConfig> Options = new List<OfficeTaskTable_OfficeTaskOptionConfig>();
        public readonly List<OfficeTaskTable_OfficeEraGateConfig> EraGates = new List<OfficeTaskTable_OfficeEraGateConfig>();

        /// <summary>该任务全部可用选项的副本；调用方不能改动目录内部列表。</summary>
        public OfficeTaskTable_OfficeTaskOptionConfig[] OptionList()
        {
            var result = new List<OfficeTaskTable_OfficeTaskOptionConfig>();
            for (int i = 0; i < Options.Count; i++)
                if (Options[i] != null) result.Add(Options[i]);
            return result.ToArray();
        }

        /// <summary>按选项 ID 查选项；不存在返回 null。</summary>
        public OfficeTaskTable_OfficeTaskOptionConfig FindOption(uint optionId)
        {
            for (int i = 0; i < Options.Count; i++)
                if (Options[i] != null && Options[i].Index == optionId) return Options[i];
            return null;
        }
    }

    public readonly List<OfficeTaskTable_OfficeTaskConfig> Tasks = new List<OfficeTaskTable_OfficeTaskConfig>();
    public readonly Dictionary<uint, TaskEntry> TaskById = new Dictionary<uint, TaskEntry>();
    public readonly List<OfficeTaskTable_OfficeEraGateConfig> EraGates = new List<OfficeTaskTable_OfficeEraGateConfig>();
    public readonly Dictionary<uint, List<OfficeTaskTable_OfficeEraGateConfig>> EraGatesByEra = new Dictionary<uint, List<OfficeTaskTable_OfficeEraGateConfig>>();
    /// <summary>表里出现过的全部任务 ID（含被禁用的），用于区分“前置不存在”和“前置被禁用”。</summary>
    internal readonly HashSet<uint> SeenTaskId = new HashSet<uint>();

    /// <summary>按 ID 取任务条目；不存在返回 null。</summary>
    public TaskEntry GetEntry(uint taskId)
    {
        TaskEntry entry;
        return TaskById.TryGetValue(taskId, out entry) ? entry : null;
    }

    /// <summary>校验并填充聚合；返回 null 表示通过，否则返回第一个问题描述。</summary>
    internal static string Build(OfficeTaskCatalogSet set, List<OfficeTaskTable_OfficeTaskConfig> rawTasks,
        List<OfficeTaskTable_OfficeTaskOptionConfig> rawOptions, List<OfficeTaskTable_OfficeEraGateConfig> rawGates)
    {
        if (rawTasks == null || rawTasks.Count == 0)
            return "配置缺失：" + OfficeTaskCatalog.TaskClassName + " 没有任何任务条目";
        string problem = ValidateTasks(set, rawTasks);
        if (problem != null) return problem;
        problem = ValidateOptions(set, rawOptions);
        if (problem != null) return problem;
        return ValidateGates(set, rawGates);
    }

    /// <summary>任务表校验：ID、类别、目标、时代、资源码、时限、前置关系。</summary>
    private static string ValidateTasks(OfficeTaskCatalogSet set, List<OfficeTaskTable_OfficeTaskConfig> rawTasks)
    {
        var seen = new HashSet<uint>();
        set.SeenTaskId.Clear();
        for (int i = 0; i < rawTasks.Count; i++)
        {
            OfficeTaskTable_OfficeTaskConfig config = rawTasks[i];
            if (config == null) continue; // 空行由导表跳过，运行时不参与
            if (config.Index == 0u) return "任务表第 " + (i + 1) + " 条 ID 为 0";
            if (!seen.Add(config.Index)) return "任务表任务 ID 重复：" + config.Index;
            set.SeenTaskId.Add(config.Index);
            if (!config.Enable) continue;
            if (config.Index > 0x7FFFFFFFu) return "任务 " + config.Index + " ID 超出 int 安全范围";
            string problem = ValidateTask(config);
            if (problem != null) return problem;
            var entry = new TaskEntry { Config = config };
            set.Tasks.Add(config);
            set.TaskById.Add(config.Index, entry);
        }
        return ValidatePrerequisites(set);
    }

    /// <summary>前置任务必须存在且已启用；沿前置链回走超过任务总数即判定成环。</summary>
    private static string ValidatePrerequisites(OfficeTaskCatalogSet set)
    {
        for (int i = 0; i < set.Tasks.Count; i++)
        {
            OfficeTaskTable_OfficeTaskConfig config = set.Tasks[i];
            uint prerequisite = config.PrerequisiteTaskId;
            if (prerequisite == 0u) continue;
            if (!set.SeenTaskId.Contains(prerequisite))
                return "任务 " + config.Index + " 前置任务不存在：" + prerequisite;
            TaskEntry entry;
            if (!set.TaskById.TryGetValue(prerequisite, out entry))
                return "任务 " + config.Index + " 前置任务已被禁用：" + prerequisite;
            uint cursor = prerequisite;
            for (int step = 0; step <= set.Tasks.Count; step++)
            {
                if (cursor == config.Index) return "任务 " + config.Index + " 的前置链形成循环";
                TaskEntry next;
                if (!set.TaskById.TryGetValue(cursor, out next) || next.Config.PrerequisiteTaskId == 0u) break;
                cursor = next.Config.PrerequisiteTaskId;
            }
        }
        return null;
    }

    /// <summary>单个任务的定义校验；只校验任务自身字段，不依赖选项表。</summary>
    private static string ValidateTask(OfficeTaskTable_OfficeTaskConfig config)
    {
        string id = "任务 " + config.Index + " ";
        if (config.Category < (uint)OfficeTaskCategory.County || config.Category > (uint)OfficeTaskCategory.Emergency)
            return id + "类别非法：" + config.Category;
        bool isCounty = config.Category == (uint)OfficeTaskCategory.County;
        // 郡县指派每档仅一次，其余分类按配置决定是否复用。
        if (isCounty && config.Repeatable) return id + "郡县指派事务不能配置为可重复";

        if (config.MinEra < 1u || config.MaxEra > OfficeTaskCatalog.MaxEra || config.MinEra > config.MaxEra)
            return id + "时代区间非法：" + config.MinEra + "~" + config.MaxEra;
        if (!OfficeTaskCatalog.IsUsableResourcePair(config.ResourceType, config.RequiredAmount))
            return id + "资源码非法（只允许 1 物资 / 2 材料 / 3 图纸 / 4 钱财）：" + config.ResourceType;
        if (config.FailureResourceAmount > 0u && !OfficeTaskCatalog.IsUsableResourceCode(config.FailureResourceType))
            return id + "失败惩罚资源码非法：" + config.FailureResourceType;
        if (config.FailureResourceAmount == 0u && config.FailureResourceType != 0u
            && !OfficeTaskCatalog.IsUsableResourceCode(config.FailureResourceType))
            return id + "失败惩罚资源码非法：" + config.FailureResourceType;
        if (config.DurationSeconds > OfficeTaskCatalog.MaxDurationSeconds)
            return id + "时限参数超出上限 " + OfficeTaskCatalog.MaxDurationSeconds;

        // 无时限任务（DurationSeconds=0）不写 deadlineClock；有时限任务才按 DurationSeconds 计时
        switch ((OfficeTaskGoal)config.GoalType)
        {
            case OfficeTaskGoal.Choice:
                return null;
            case OfficeTaskGoal.ResourceDelivery:
                if (!OfficeTaskCatalog.IsUsableResourceCode(config.ResourceType)) return id + "资源交付缺少资源码";
                if (config.RequiredAmount == 0u) return id + "资源交付数量必须大于 0";
                return null;
            case OfficeTaskGoal.ClearRuns:
                if (config.RequiredClears == 0u) return id + "通关目标次数必须大于 0";
                return null;
            case OfficeTaskGoal.VillageRaid:
                if(config.Category!=(uint)OfficeTaskCategory.Emergency)return id+"遭遇袭击应归入突发任务";
                return config.DurationSeconds == 0u
                    ? id + "遭遇袭击任务必须配置正数时限（无时限的遭遇袭击已被明确拒绝）" : null;
            default:
                return id + "目标类型非法：" + config.GoalType;
        }
    }

    /// <summary>选项表校验：唯一 ID、任务存在且目标允许选择、资源码与代价/奖励合法。</summary>
    private static string ValidateOptions(OfficeTaskCatalogSet set, List<OfficeTaskTable_OfficeTaskOptionConfig> rawOptions)
    {
        var seen = new HashSet<uint>();
        if (rawOptions != null)
        {
            for (int i = 0; i < rawOptions.Count; i++)
            {
                OfficeTaskTable_OfficeTaskOptionConfig option = rawOptions[i];
                if (option == null) continue;
                if (option.Index == 0u) return "选项表第 " + (i + 1) + " 条 ID 为 0";
                if (!seen.Add(option.Index)) return "选项 ID 重复：" + option.Index;
                if (!option.Enable) continue;
                if (option.Index > 0x7FFFFFFFu) return "选项 " + option.Index + " ID 超出 int 安全范围";
                string id = "选项 " + option.Index + " ";
                TaskEntry entry = set.GetEntry(option.TaskId);
                if (entry == null) return id + "所属任务不存在或已被禁用：" + option.TaskId;
                if (!OfficeTaskCatalog.IsUsableResourcePair(option.CostResourceType, option.CostAmount))
                    return id + "代价资源码非法：" + option.CostResourceType;
                if (!OfficeTaskCatalog.IsUsableResourcePair(option.RewardResourceType, option.RewardAmount))
                    return id + "奖励资源码非法：" + option.RewardResourceType;
                // 任务要求量与选项代价若落在同一资源，合计必须仍在 uint 内，业务层按合并后一次扣除。
                if (option.CostAmount > 0u
                    && entry.Config.GoalType == (uint)OfficeTaskGoal.ResourceDelivery
                    && entry.Config.ResourceType == option.CostResourceType
                    && (ulong)option.CostAmount + entry.Config.RequiredAmount > uint.MaxValue)
                    return id + "与任务要求量合计超出资源上限";
                entry.Options.Add(option);
            }
        }
        foreach (TaskEntry task in set.TaskById.Values)
        {
            uint goal = task.Config.GoalType;
            bool needsOption = goal == (uint)OfficeTaskGoal.Choice
                || goal == (uint)OfficeTaskGoal.ResourceDelivery
                || goal == (uint)OfficeTaskGoal.ClearRuns
                || goal == (uint)OfficeTaskGoal.VillageRaid;
            if (needsOption && task.Options.Count == 0)
                return "任务 " + task.Config.Index + " 至少需要一个可用选项";
        }
        return null;
    }

    /// <summary>
    /// 时代门槛表校验：唯一 ID、Era 表示“当前待推进时代”（1 表示 1→2），允许 1..4，
    /// 目标必须是已启用的郡县指派事务；同一时代的多条记录按 AND 处理。
    /// </summary>
    private static string ValidateGates(OfficeTaskCatalogSet set, List<OfficeTaskTable_OfficeEraGateConfig> rawGates)
    {
        var seen = new HashSet<uint>();
        if (rawGates == null) return null;
        for (int i = 0; i < rawGates.Count; i++)
        {
            OfficeTaskTable_OfficeEraGateConfig gate = rawGates[i];
            if (gate == null) continue;
            if (gate.Index == 0u) return "时代门槛表第 " + (i + 1) + " 条 ID 为 0";
            if (!seen.Add(gate.Index)) return "时代门槛 ID 重复：" + gate.Index;
            if (!gate.Enable) continue;
            string id = "时代门槛 " + gate.Index + " ";
            if (gate.Era < 1u || gate.Era >= OfficeTaskCatalog.MaxEra)
                return id + "待推进时代非法（允许 1.." + (OfficeTaskCatalog.MaxEra - 1u) + "）：" + gate.Era;
            TaskEntry entry = set.GetEntry(gate.TaskId);
            if (entry == null) return id + "依赖的任务不存在或已被禁用：" + gate.TaskId;
            if (entry.Config.Category != (uint)OfficeTaskCategory.County)
                return id + "只允许依赖郡县指派事务，实际任务 " + gate.TaskId + " 类别为 " + entry.Config.Category;
            List<OfficeTaskTable_OfficeEraGateConfig> list;
            if (!set.EraGatesByEra.TryGetValue(gate.Era, out list))
            {
                list = new List<OfficeTaskTable_OfficeEraGateConfig>();
                set.EraGatesByEra.Add(gate.Era, list);
            }
            list.Add(gate);
            entry.EraGates.Add(gate);
            set.EraGates.Add(gate);
        }
        return null;
    }
}
