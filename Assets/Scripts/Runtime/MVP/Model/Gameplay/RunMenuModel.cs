using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class RunBagItem
    {
        public uint Id, Count;
    }

    /// <summary>A menu owns the pause across temporary panel covers.</summary>
    public sealed class RunMenuModel : IDisposable
    {
        private bool _paused;
        private float _previousScale;
        public bool ConfirmRetreat { get; set; }
        public bool InRun => DispatchService.Instance.IsInRun;
        public int UsedSlots => RunInventory.UsedSlots;
        public int TotalSlots => RunInventory.TotalSlots;
        public int Medicine => CampService.MedicineCount;

        public void Pause()
        {
            if (!_paused) { _previousScale = Time.timeScale; _paused = true; }
            Time.timeScale = 0;
            RunControlModel.PauseWorld(true);
        }

        public IReadOnlyList<RunBagItem> ReadBag()
        {
            var result = new List<RunBagItem>();
            foreach (var item in RunInventory.Stacks)
                result.Add(new RunBagItem { Id = item.itemId, Count = item.count });
            return result;
        }

        public bool Retreat(out string reason)
        {
            if (!InRun) { reason = "当前不在出征中"; return false; }
            // Save/transition failure keeps the menu and its paused world available for retry.
            return DispatchService.Instance.ReturnToMeta(out reason);
        }

        public void Dispose()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = _previousScale;
            RunControlModel.PauseWorld(false);
        }
    }
}
