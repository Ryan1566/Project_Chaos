using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class RunMenuState
    {
        public string Summary, Hint, RetreatLabel;
        public IReadOnlyList<string> Items;
        public bool ConfirmRetreat;
    }

    public sealed class RunMenuPresenter : GameplayPresenter<RunMenuState>
    {
        private readonly RunMenuModel _model;
        public RunMenuPresenter(RunMenuModel model) { _model = model; }
        protected override void OnAttach() { _model.Pause(); base.OnAttach(); }
        protected override RunMenuState CreateState()
        {
            var items = new List<string>();
            foreach (var item in _model.ReadBag()) items.Add(GameplayText.ItemName(item.Id) + " × " + item.Count);
            return new RunMenuState { Items = items, ConfirmRetreat = _model.ConfirmRetreat,
                Summary = "行囊 " + _model.UsedSlots + "/" + _model.TotalSlots + " 格　　药品 " + _model.Medicine,
                Hint = _model.ConfirmRetreat ? "撤离会放弃本次行囊，不计通关；倒地门客按现有规则进入重伤。" : "战斗已暂停。查看行囊，或前往扎营修整。",
                RetreatLabel = _model.ConfirmRetreat ? "确认撤离" : "撤离返回局外" };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Cancel) { _model.ConfirmRetreat = false; Refresh(); return; }
            if (intent.Action == PanelAction.Heal)
            {
                UIManager.Instance.PushPanel(PanelType.RunCampPanel);
                return;
            }
            if (intent.Action != PanelAction.Return) return;
            if (!_model.ConfirmRetreat) { _model.ConfirmRetreat = true; Refresh(); return; }
            if (!_model.Retreat(out string reason)) { Message(reason); return; }
            // Return callbacks may already replace the root or push a story event.
            // Pop only when this overlay still owns the top of the stack.
            _model.Dispose();
            if (UIManager.Instance.panelStack.Count > 0 && ReferenceEquals(UIManager.Instance.panelStack.Peek(), View))
                View.ClosePanel();
        }
    }
}
