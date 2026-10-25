using System;
using System.Collections.Generic;
using ChaosDebug;

namespace Chaos.MVP.Model.SaveSlots
{
    public sealed class SaveSlotsModel : IDisposable
    {
        public int SelectedSlot { get; private set; } = 1;
        public List<SaveSlotInfo> Slots { get; private set; } = new List<SaveSlotInfo>();
        public int Confirmation { get; private set; }
        public string Error { get; private set; }
        public int SlotCount => GlobalPath.save_SlotCount;
        public void Open()
        {
            Confirmation = 0; Error = null;
            SelectedSlot = GameSession.Instance.HasSession && SaveSlotService.IsValidSlot(GameSession.Instance.SelectedSlot)
                ? GameSession.Instance.SelectedSlot : SaveSlotService.Instance.FindFirstNonEmptySlot();
            Refresh();
        }
        public void Refresh() { Slots = SaveSlotService.Instance.ReadAllSlots(); }
        public SaveSlotInfo GetInfo(int slot) { return Slots.Find(info => info.Slot == slot); }
        public bool CanDelete => SaveSlotService.HasSaveFile(GetInfo(SelectedSlot));
        public bool Select(int slot)
        {
            if (!SaveSlotService.IsValidSlot(slot)) return false;
            SelectedSlot = slot;
            Error = null;
            Refresh();
            return true;
        }
        public void ConfirmStart() { Error = null; Confirmation = 1; }
        public void ConfirmDelete() { if (CanDelete) Confirmation = 2; }
        public void Cancel() { Confirmation = 0; }
        public bool Execute()
        {
            int purpose = Confirmation;
            Confirmation = 0;
            if (purpose == 1)
            {
                if (SaveSlotService.Instance.CreateNew(SelectedSlot)) return Enter(SelectedSlot);
                else ReportEntryFailure("新建槽 " + SelectedSlot + " 失败，请检查存档目录后重试。");
            }
            else if (purpose == 2)
            {
                if (SaveSlotService.Instance.Delete(SelectedSlot))
                    SelectedSlot = SaveSlotService.Instance.FindFirstNonEmptySlot();
                else ReportEntryFailure("删除槽 " + SelectedSlot + " 失败，可重试。");
            }
            Refresh();
            return false;
        }
        public bool Enter(int slot)
        {
            SaveSlotFile file;
            if (!SaveSlotService.Instance.TryLoad(slot, out file))
            {
                ReportEntryFailure("读取槽 " + slot + " 失败，请检查存档后重试。");
                Refresh(); return false;
            }
            GameSession.Instance.Begin(slot, file.payload, null);
            Error = null;
            return true;
        }
        public void ReportEntryFailure(string reason)
        {
            Error = reason;
            ChaosLog.Error(LogChannel.Save, reason);
        }
        public void Dispose() { Cancel(); }
    }
}
