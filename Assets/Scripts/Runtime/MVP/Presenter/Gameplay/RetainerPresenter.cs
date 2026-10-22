using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class RetainerState
    {
        public string Summary, Hint;
        public bool CanRecruit;
        public IReadOnlyList<TextRow> Rows;
    }
    public sealed class RetainerPresenter : GameplayPresenter<RetainerState>
    {
        private readonly RetainerRecruitmentModel _model;
        public RetainerPresenter(RetainerRecruitmentModel model) { _model = model; }
        protected override RetainerState CreateState()
        {
            var svc = RetainerService.Instance;
            var all = svc.GetAll();
            int deployable = 0;
            var rows = new List<TextRow>();
            foreach (var r in all)
            {
                if (r == null) continue;
                if (svc.IsDeployable(r)) deployable++;
                var def = svc.GetDefinition(r.protoId);
                var state = svc.GetState(r);
                string status = state == RetainerService.DeployState.Wounded ? "重伤 " + r.woundedRemainSeconds + "s"
                    : state == RetainerService.DeployState.LowConfidence ? "信心不足" : "可用";
                string text = Name(r, def) + "　　品质 " + r.quality + "　　信心 " + r.confidence + "　　" + status;
                if (def != null) text += "\n　　　生命 " + def.Hp + "　　攻击 " + def.Attack + "　　防御 " + def.Defense + "　　速度 " + def.Speed;
                rows.Add(new TextRow { Id = r.id, Text = text, Tone = state == RetainerService.DeployState.Wounded ? TextTone.Warning
                    : state == RetainerService.DeployState.LowConfidence ? TextTone.Dim : TextTone.Success });
            }
            return new RetainerState { Rows = rows.ToArray(), CanRecruit = _model.CanRecruit,
                Summary = GameplayText.Text("ui_retainer_summary", "共 {0}/{1} 位　　可出战 {2} 位（信心至少 {3}）", all.Count, RetainerService.MaxRetainers, deployable, RetainerService.ConfidenceThreshold),
                Hint = all.Count == 0 ? GameplayText.Text("ui_retainer_empty", "暂无门客，点击「招募门客」组建队伍。") : "" };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action != PanelAction.Recruit) return;
            bool success = _model.TryRecruit(out uint id, out string reason);
            Refresh();
            if (success && RetainerService.Instance.TryGet(id, out var retainer))
                Message(GameplayText.Text("ui_retainer_recruited", "招募成功：{0}", Name(retainer, RetainerService.Instance.GetDefinition(retainer.protoId))), true);
            else Message(reason);
        }
        private static string Name(RetainerSave r, RetainerConfig def) => def == null ? "门客 #" + r.id : GameplayText.Text(def.NameKey, "门客 #" + r.id);
    }
}
