using System.Collections.Generic;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class DispatchState
    {
        public string Summary, Hint;
        public IReadOnlyList<TextRow> Rows;
    }
    public sealed class DispatchPresenter : GameplayPresenter<DispatchState>
    {
        protected override DispatchState CreateState()
        {
            var svc = DispatchService.Instance;
            var rs = RetainerService.Instance;
            var rows = new List<TextRow>();
            foreach (var r in svc.GetCandidates())
            {
                if (r == null) continue;
                var def = rs.GetDefinition(r.protoId);
                string name = def == null ? "门客 #" + r.id : GameplayText.Text(def.NameKey, "门客 #" + r.id);
                string unavailable = svc.GetUnavailableReason(r);
                bool selected = svc.SelectedIds.Contains(r.id), enabled = string.IsNullOrEmpty(unavailable);
                rows.Add(new TextRow { Id = r.id, Selected = selected, Enabled = selected || enabled,
                    Text = (selected ? "【已选】" : "　　") + name + "　品质 " + r.quality + "　信心 " + r.confidence
                        + (enabled ? "" : "　（不可出战：" + unavailable + "）"),
                    Tone = enabled ? (selected ? TextTone.Success : TextTone.Normal)
                        : rs.GetState(r) == RetainerService.DeployState.Wounded ? TextTone.Warning : TextTone.Dim });
            }
            return new DispatchState { Rows = rows.ToArray(), Summary = "已选 " + svc.SelectedCount + "/" + DispatchService.MaxTeamSize + (svc.IsInRun ? "　　（局内）" : ""),
                Hint = svc.IsInRun ? GameplayText.Text("ui_dispatch_inrun", "队伍正在探索，点「撤离」返回局外。")
                    : svc.SelectedCount == 0 ? GameplayText.Text("ui_dispatch_empty", "点门客名字选择（1~4 位）；不可出战的门客会置灰并显示原因") : "" };
        }
        protected override void Execute(PanelIntent intent)
        {
            string reason;
            bool ok;
            switch (intent.Action)
            {
                case PanelAction.Toggle: ok = DispatchService.Instance.Toggle(intent.Id, out reason); Refresh(); break;
                case PanelAction.Depart: ok = DispatchService.Instance.Depart(out reason); break;
                case PanelAction.Return: ok = DispatchService.Instance.ReturnToMeta(out reason); break;
                default: return;
            }
            // A successful scene transition owns UI visibility; do not redraw and reveal the old panel.
            if (!ok) { Refresh(); Message(reason); }
        }
    }
}
