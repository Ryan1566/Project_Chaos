using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

public enum RunFunctionTab { Team, Inventory, Map }

namespace Chaos.MVP.Presenter.Gameplay
{
    public readonly struct RunBagMove
    {
        public readonly RunBagSection From, To;
        public readonly int FromIndex, ToIndex;
        public RunBagMove(RunBagSection from, int fromIndex, RunBagSection to, int toIndex)
        { From = from; FromIndex = fromIndex; To = to; ToIndex = toIndex; }
    }
    public sealed class RunBagSlotState
    {
        public RunBagSection Section;
        public int Index;
        public uint Id, Count;
        public bool Unlocked;
        public string Name;
    }
    public sealed class RunMenuState
    {
        public string Summary, Hint, RetreatLabel, Placeholder;
        public IReadOnlyList<RunBagSlotState> Slots;
        public IReadOnlyList<uint> Uncarried;
        public RunFunctionTab Tab;
        public bool ConfirmRetreat;
    }
    public interface IRunBagView
    {
        event Action<RunBagMove> MoveRequested;
        event Action<uint> CarryRequested;
    }
    public sealed class RunMenuPresenter : GameplayPresenter<RunMenuState>
    {
        private readonly RunMenuModel _model;
        private RunFunctionTab _tab = RunFunctionTab.Inventory;
        private IRunBagView _bagView;
        public RunMenuPresenter(RunMenuModel model) { _model = model; }
        protected override void OnAttach()
        {
            _model.Pause();
            _bagView = View as IRunBagView;
            if (_bagView != null) { _bagView.MoveRequested += Move; _bagView.CarryRequested += Carry; }
            RunInventory.Changed += Refresh;
            MetaRuntimeDriver.Changed += Refresh;
            base.OnAttach();
        }
        protected override void OnDetach()
        {
            RunInventory.Changed -= Refresh;
            MetaRuntimeDriver.Changed -= Refresh;
            if (_bagView != null) { _bagView.MoveRequested -= Move; _bagView.CarryRequested -= Carry; }
            _bagView = null;
            base.OnDetach();
        }
        public void SelectTab(RunFunctionTab tab)
        { if (!Enum.IsDefined(typeof(RunFunctionTab), tab)) return; _tab = tab; _model.ConfirmRetreat = false; Refresh(); }
        private void Move(RunBagMove move)
        {
            if (!_model.InRun) { Message("当前背包会话已结束"); return; }
            bool ok = RunInventory.TryMove(move.From, move.FromIndex, move.To, move.ToIndex, out string reason);
            Refresh();
            if (!ok) Message(reason);
        }
        private void Carry(uint id)
        {
            if (!_model.InRun) return;
            bool ok = RunInventory.TryCarryImportant(id, out string reason);
            Refresh();
            if (!ok) Message(reason);
        }
        protected override RunMenuState CreateState()
        {
            var slots = new List<RunBagSlotState>();
            foreach (RunBagSection section in Enum.GetValues(typeof(RunBagSection)))
                for (int i = 0; i < RunInventory.GetMaximum(section); i++)
                {
                    var item = RunInventory.GetSlot(section, i);
                    slots.Add(new RunBagSlotState { Section = section, Index = i,
                        Id = item == null ? 0 : item.itemId, Count = item == null ? 0 : item.count,
                        Unlocked = i < RunInventory.GetCapacity(section),
                        Name = item == null ? "" : GameplayText.ItemName(item.itemId) });
                }
            return new RunMenuState { Slots = slots, Tab = _tab, Uncarried = RunInventory.GetUncarriedImportant(),
                ConfirmRetreat = _model.ConfirmRetreat,
                Summary = _tab == RunFunctionTab.Team ? "派遣队伍 · 本局成长" : _tab == RunFunctionTab.Map ? "局内地图" : "局内背包  " + _model.UsedSlots + "/" + _model.TotalSlots + " 格",
                Hint = _model.ConfirmRetreat ? "撤离将放弃普通行囊；已获得的重要道具永久保留。"
                    : _tab == RunFunctionTab.Team ? "击杀经验由全队共享 · 每级 1 属性点 · 生命加点不治疗 · 成长仅本局有效。"
                    : _tab == RunFunctionTab.Map ? "滚轮缩放 · 拖动平移 · 队伍共享视野；敌人仅在当前视野显示。"
                    : "拖拽物品到对应专用行或杂物格；已有物品时交换。专用行快捷选用待开放。",
                Placeholder = _tab == RunFunctionTab.Team ? "队伍状态页签预留，后续接入。"
                    : "",
                RetreatLabel = _model.ConfirmRetreat ? "确认撤离" : "撤离" };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Toggle) { SelectTab((RunFunctionTab)intent.Id); return; }
            if (intent.Action == PanelAction.Settings) { UIManager.Instance.PushPanel(PanelType.SettingPanel); return; }
            if (intent.Action == PanelAction.Cancel) { _model.ConfirmRetreat = false; Refresh(); return; }
            if (intent.Action == PanelAction.Heal) { UIManager.Instance.PushPanel(PanelType.RunCampPanel); return; }
            if (intent.Action != PanelAction.Return) return;
            if (!_model.ConfirmRetreat) { _model.ConfirmRetreat = true; Refresh(); return; }
            if (!_model.Retreat(out string reason)) { Message(reason); return; }
            _model.Dispose();
            if (UIManager.Instance.panelStack.Count > 0 && ReferenceEquals(UIManager.Instance.panelStack.Peek(), View)) View.ClosePanel();
        }
    }
}
