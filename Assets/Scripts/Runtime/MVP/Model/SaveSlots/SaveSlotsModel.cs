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
        public int SlotCount => GlobalPath.save_SlotCount;
        public void Open() { Confirmation = 0; SelectedSlot = SaveSlotService.Instance.FindFirstNonEmptySlot(); Refresh(); }
        public void Refresh() { Slots = SaveSlotService.Instance.ReadAllSlots(); }
        public SaveSlotInfo GetInfo(int slot) { return Slots.Find(info => info.Slot == slot); }
        public bool CanDelete => SaveSlotService.HasSaveFile(GetInfo(SelectedSlot));
        public bool Select(int slot)
        {
            if (!SaveSlotService.IsValidSlot(slot)) return false;
            SelectedSlot = slot;
            if (GetInfo(slot) == null) Refresh();
            return true;
        }
        public void ConfirmStart() { Confirmation = 1; }
        public void ConfirmDelete() { if (CanDelete) Confirmation = 2; }
        public void Cancel() { Confirmation = 0; }
        public bool Execute()
        {
            int purpose = Confirmation;
            Confirmation = 0;
            if (purpose == 1)
            {
                if (SaveSlotService.Instance.CreateNew(SelectedSlot)) return Enter(SelectedSlot);
                else ChaosLog.Error(LogChannel.Save, "新建存档失败，已取消进入游戏");
            }
            else if (purpose == 2)
            {
                SaveSlotService.Instance.Delete(SelectedSlot);
                SelectedSlot = SaveSlotService.Instance.FindFirstNonEmptySlot();
            }
            Refresh();
            return false;
        }
        public bool Enter(int slot)
        {
            SaveSlotFile file;
            if (!SaveSlotService.Instance.TryLoad(slot, out file)) { Refresh(); return false; }
            GameSession.Instance.Begin(slot, file.payload, null);
            return true;
        }
        public void Dispose() { Cancel(); }
    }
}
