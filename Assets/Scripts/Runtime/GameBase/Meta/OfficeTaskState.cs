using System;

/// <summary>办事单位列表的固定分类，不与旧事件ID或触发类型混用。</summary>
public enum OfficeTaskCategory { County = 1, Administration = 2, Preset = 3, Emergency = 4 }
public enum OfficeTaskGoal { Choice = 1, ResourceDelivery = 2, ClearRuns = 3, VillageRaid = 4 }
public enum OfficeTaskStatus { Active = 0, Completed = 1, Failed = 2, AwaitingBattle = 3, Available = 4 }

/// <summary>任务界面使用的只读快照；读取列表本身不触发任务或产生奖励。</summary>
public sealed class OfficeTaskSnapshot
{
    public uint TaskId, Cycle, SelectedOptionId, MapLocationId;
    public OfficeTaskCategory Category;
    public OfficeTaskStatus Status;
    public string Title, Body, Topic;
    public bool Timed, Repeatable;
    public double RemainingSeconds;
    public uint Progress, Required;
    public uint SupplyBuildingId, VillageSupplyProgress;
    public string SupplyBuildingName, SupplyResourceName;
    public bool VillageSupplyReady => VillageSupplyProgress >= Required && Progress >= Required;
    public OfficeTaskTable_OfficeTaskOptionConfig[] Options;
}
