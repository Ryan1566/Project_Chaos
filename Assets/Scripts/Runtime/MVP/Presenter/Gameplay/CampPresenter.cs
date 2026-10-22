using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class CampState
    {
        public string State, Hint;
        public bool CanHeal, CanRefill;
        public bool InRun;
    }
    public sealed class CampPresenter : GameplayPresenter<CampState>
    {
        protected override void OnAttach() { RunControlModel.SetPlayerInput(false); base.OnAttach(); }
        protected override void CloseView() { RunControlModel.SetPlayerInput(true); base.CloseView(); }
        public void ReleaseControl() { RunControlModel.SetPlayerInput(true); }
        protected override CampState CreateState()
        {
            bool heal = CampService.CanHeal(out string healReason), refill = CampService.CanRefill(out string refillReason);
            return new CampState { CanHeal = heal, CanRefill = refill, InRun = DispatchService.Instance.IsInRun,
                State = "药品：" + CampService.MedicineCount + "　　" + (CampService.IsInSafeRoom ? "安全房间（可扎营）" : "战斗中无法扎营")
                    + (CampService.CampUsed ? "　　本局已扎营" : ""),
                Hint = heal || refill ? "选择一项修整（扎营每局 1 次）" : !CampService.IsInSafeRoom ? "战斗中无法扎营" : healReason ?? refillReason };
        }
        protected override void Execute(PanelIntent intent)
        {
            string reason;
            bool ok;
            if (intent.Action == PanelAction.Heal) ok = CampService.TryHeal(out reason);
            else if (intent.Action == PanelAction.Refill) ok = CampService.TryRefill(out reason);
            else return;
            Refresh();
            Message(ok ? (intent.Action == PanelAction.Heal ? "完全治疗完成：hp 已回满，消耗 1 药" : "倒地门客已半血复活，消耗 2 药") : reason, ok);
        }
    }
}
