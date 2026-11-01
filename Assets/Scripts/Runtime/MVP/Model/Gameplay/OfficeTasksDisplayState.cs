using System;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>任务视图只接收展示字段，不自行查询或更改玩法服务。</summary>
    public sealed class OfficeTasksDisplayState
    {
        public string Title, Summary, BackLabel, LegacyLabel, EmptyText, Message;
        public string DetailTitle, DetailBody, DetailProgress;
        public string[] CategoryLabels;
        public OfficeTaskCategory SelectedCategory;
        public uint SelectedTaskId;
        public OfficeTaskDisplayRow[] Rows = Array.Empty<OfficeTaskDisplayRow>();
        public OfficeTaskDisplayOption[] Options = Array.Empty<OfficeTaskDisplayOption>();
        public bool ShowLegacyButton;
    }
    public sealed class OfficeTaskDisplayRow
    {
        public uint TaskId, Cycle;
        public string Title, Status, Time;
        public bool Selected;
    }
    public sealed class OfficeTaskDisplayOption
    {
        public uint TaskId, Cycle, OptionId;
        public string Text;
        public bool Enabled;
    }
}
