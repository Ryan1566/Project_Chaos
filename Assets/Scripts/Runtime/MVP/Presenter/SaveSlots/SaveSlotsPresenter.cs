using System;
using System.Collections.Generic;
using Chaos.MVP.Model.SaveSlots;

namespace Chaos.MVP.Presenter.SaveSlots
{
    public enum SaveSlotsAction { Select, Delete, Return, Confirm, Cancel, Refresh }
    public readonly struct SaveSlotsIntent
    {
        public readonly SaveSlotsAction Action;
        public readonly int Slot;
        public SaveSlotsIntent(SaveSlotsAction action, int slot = 0) { Action = action; Slot = slot; }
    }
    public sealed class SaveSlotsState
    {
        public List<SaveSlotCellState> Cells;
        public int SelectedSlot;
        public bool CanDelete;
        public string ConfirmationKey;
    }
    public sealed class SaveSlotCellState
    {
        public int Slot;
        public string TitleKey;
        public string Time;
    }
    public static class SaveSlotPresentation
    {
        public static SaveSlotCellState FromInfo(SaveSlotInfo info, int fallbackSlot = 0)
        {
            bool empty = info == null || info.IsEmpty;
            return new SaveSlotCellState { Slot = info != null && info.Slot != 0 ? info.Slot : fallbackSlot,
                TitleKey = empty ? "ui_slpanel_cell_empty" : "ui_recordcell",
                Time = empty ? "--" : FormatLocalTime(info.LastSaveUtcTicks) };
        }
        public static string FormatLocalTime(long ticks)
        {
            if (ticks <= 0) return "--";
            try { return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy/MM/dd HH:mm"); }
            catch (ArgumentOutOfRangeException) { return "--"; }
        }
    }
    public interface ISaveSlotsView : IPanelView<SaveSlotsState, SaveSlotsIntent> { bool CanShowConfirmation { get; } }
    public sealed class SaveSlotsPresenter : PresenterBase<ISaveSlotsView>
    {
        private readonly SaveSlotsModel _model;
        public SaveSlotsPresenter(SaveSlotsModel model) { _model = model ?? throw new ArgumentNullException(nameof(model)); }
        protected override void OnAttach() { View.Intent += Handle; _model.Open(); Render(); }
        protected override void OnDetach() { if (View != null) View.Intent -= Handle; _model.Cancel(); }
        private void Handle(SaveSlotsIntent intent)
        {
            bool entered = false;
            switch (intent.Action)
            {
                case SaveSlotsAction.Select:
                    if (!_model.Select(intent.Slot)) return;
                    if (_model.GetInfo(intent.Slot)?.HasUsableSave == true) entered = _model.Enter(intent.Slot);
                    else _model.ConfirmStart();
                    break;
                case SaveSlotsAction.Delete: _model.ConfirmDelete(); break;
                case SaveSlotsAction.Return: _model.Cancel(); UIManager.Instance.PopPanel(); return;
                case SaveSlotsAction.Confirm: entered = _model.Execute(); break;
                case SaveSlotsAction.Cancel: _model.Cancel(); break;
                case SaveSlotsAction.Refresh: _model.Refresh(); break;
            }
            if (IsDisposed) return;
            if (_model.Confirmation != 0 && !View.CanShowConfirmation) entered = _model.Execute();
            if (entered)
            {
                if (UIManager.Instance != null) UIManager.Instance.SetRootPanel(PanelType.WorldMapPanel);
                return;
            }
            if (!IsDisposed) Render();
        }
        private void Render()
        {
            var cells = new List<SaveSlotCellState>();
            foreach (SaveSlotInfo slot in _model.Slots) cells.Add(SaveSlotPresentation.FromInfo(slot));
            View.Render(new SaveSlotsState { Cells = cells, SelectedSlot = _model.SelectedSlot,
                CanDelete = _model.CanDelete, ConfirmationKey = _model.Confirmation == 1 ? "ui_slpanel_confirm_start"
                    : _model.Confirmation == 2 ? "ui_slpanel_confirm_delete" : null });
        }
    }
}
