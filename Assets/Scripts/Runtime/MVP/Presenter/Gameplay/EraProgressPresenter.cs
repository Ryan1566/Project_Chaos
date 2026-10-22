using System.Text;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class EraProgressState
    {
        public string Lines, Hint, AdvanceLabel, CancelLabel;
        public bool CanAdvance;
    }
    public sealed class EraProgressPresenter : GameplayPresenter<EraProgressState>
    {
        private bool _confirming;
        private string Reason()
        {
            var meta = MetaProgressService.Instance;
            uint threshold = EraConfigService.Instance.GetMilestoneValue(meta.Era);
            if (meta.Era >= MetaProgressService.MaxEra) return "已是最后时代";
            if (!meta.BossClearedThisRun) return "需先击败当前时代 boss（本次通关未满足）";
            return threshold != 0 && meta.Payload.clears < threshold ? "需累计通关 " + threshold + " 次（当前 " + meta.Payload.clears + "）" : null;
        }
        protected override EraProgressState CreateState()
        {
            var meta = MetaProgressService.Instance;
            var era = meta.Era;
            uint threshold = EraConfigService.Instance.GetMilestoneValue(era), clears = meta.Payload.clears;
            bool boss = meta.BossClearedThisRun, milestone = threshold == 0 || clears >= threshold;
            var text = new StringBuilder();
            text.Append("时代　").Append(era).Append("/5　（").Append(EraConfigService.Instance.GetNameKey(era)).Append("）\n")
                .Append("深度　").Append(meta.Depth).Append('/').Append(meta.DepthCap).Append(meta.Depth >= meta.DepthCap ? "　已满\n" : "\n")
                .Append("累计通关　").Append(clears).Append(" 次\n")
                .Append("里程碑　").Append(threshold == 0 ? "无需里程碑" : clears + "/" + threshold).Append("（类型 ").Append(EraConfigService.Instance.GetMilestoneType(era)).Append("）\n")
                .Append("门槛① 本次通关已满足　").Append(boss ? "✔\n" : "✘\n")
                .Append("门槛② 累计通关 ≥ ").Append(threshold).Append("（当前 ").Append(clears).Append("）　").Append(milestone ? "✔\n" : "✘\n")
                .Append("历史最深　").Append(meta.Payload.maxClearedDepth).Append(" 层\n");
            string reason = Reason();
            return new EraProgressState { Lines = text.ToString(), CanAdvance = reason == null,
                AdvanceLabel = _confirming ? "确认推进到 时代" + (era + 1) + "（不可回退）" : "推进时代", CancelLabel = _confirming ? "取消" : "关闭",
                Hint = _confirming ? "再次点击确认 → 时代 " + era + " → " + (era + 1) + "（不可回退）"
                    : reason ?? "条件已满足，可推进时代（难度 " + EraConfigService.Instance.GetBaseDifficulty(era) + " → " + EraConfigService.Instance.GetBaseDifficulty(era + 1) + "）" };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Cancel)
            {
                if (_confirming) { _confirming = false; Refresh(); } else View.ClosePanel();
                return;
            }
            if (intent.Action != PanelAction.Advance) return;
            if (Reason() != null) { _confirming = false; Refresh(); return; }
            if (!_confirming) { _confirming = true; Refresh(); return; }
            MetaProgressService.Instance.TryAdvanceEra();
            _confirming = false;
            Refresh();
        }
    }
}
