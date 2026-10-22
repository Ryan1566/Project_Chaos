using System.Collections.Generic;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class EventState
    {
        public uint EventId;
        public int PendingCount;
        public string Title, Body, Result, QueueHint;
        public bool Resolved, HasEvent;
        public IReadOnlyList<TextRow> Options;
    }
    public sealed class EventPresenter : GameplayPresenter<EventState>
    {
        protected override EventState CreateState()
        {
            var svc = EventService.Instance;
            var ev = svc.Current;
            if (ev == null) return new EventState { Options = new TextRow[0] };
            var rows = new List<TextRow>();
            foreach (var opt in svc.GetOptions(ev.Index))
                rows.Add(new TextRow { Id = opt.Index, Text = GameplayText.Text("event_option_" + opt.Index, opt.OptionText) });
            return new EventState { HasEvent = true, EventId = ev.Index, PendingCount = svc.PendingCount,
                Title = GameplayText.Text("event_" + ev.Index + "_title", ev.TitleText),
                Body = GameplayText.Text("event_" + ev.Index + "_body", ev.BodyText), Resolved = svc.HasResolvedCurrent,
                Result = svc.CurrentResult ?? "", QueueHint = svc.RemainingAfterCurrent > 0
                    ? GameplayText.Text("ui_event_remaining", "还有 {0} 个事件", svc.RemainingAfterCurrent) : "", Options = rows.ToArray() };
        }
        protected override void Execute(PanelIntent intent)
        {
            var svc = EventService.Instance;
            string reason;
            if (intent.Action == PanelAction.ChooseOption)
            {
                if (svc.HasResolvedCurrent) return;
                if (!svc.TryChooseOption(intent.Id, out reason)) { Message(reason); return; }
            }
            else if (intent.Action == PanelAction.Continue)
            {
                if (!svc.TryCompleteCurrent(out reason)) { Message(reason); return; }
                if (!svc.HasPending) { View.ClosePanel(); return; }
            }
            else return;
            Refresh();
        }
    }
}
