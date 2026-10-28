using System.Collections.Generic;

namespace Chaos.MVP.Model.Gameplay
{
    public readonly struct SettlementItem
    {
        public uint Id { get; }
        public uint Count { get; }
        public bool Selected { get; }
        public bool Retained { get; }
        public SettlementItem(uint id, uint count, bool selected, bool retained)
        { Id = id; Count = count; Selected = selected; Retained = retained; }
    }

    /// <summary>A run owns this transaction; hiding or rebuilding its presenter must never reset it.</summary>
    public sealed class RunSettlementModel
    {
        public static RunSettlementModel Current { get; private set; }
        private readonly List<uint> _ids = new List<uint>();
        private readonly List<uint> _counts = new List<uint>();
        private readonly HashSet<uint> _selected = new HashSet<uint>();
        private readonly HashSet<uint> _retained = new HashSet<uint>();
        private SavePayload _owner;
        private bool _applied;
        public bool Win { get; private set; }
        public bool PendingSave { get; private set; }
        public bool Completed { get; private set; }
        public uint Seed { get; private set; }
        public uint RunCount { get; private set; }
        public int SelectedCount => _selected.Count;
        public int RetainedCount => _retained.Count;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime() { Current = null; }

        public static void Begin(bool win)
        {
            var owner = MetaProgressService.Instance.Payload;
            if (Current != null && ReferenceEquals(Current._owner, owner) && Current.Seed == RunEncounter.RunSeed && Current.RunCount == owner.runs && Current.Win == win)
                return;
            var model = new RunSettlementModel { Win = win, Seed = RunEncounter.RunSeed, RunCount = owner.runs, _owner = owner };
            foreach (var stack in RunInventory.Stacks) { model._ids.Add(stack.itemId); model._counts.Add(stack.count); }
            if (!win)
            {
                var indices = new List<int>();
                for (int i = 0; i < model._ids.Count; i++) indices.Add(i);
                var random = new System.Random(20261018);
                for (int i = 0; i < model._ids.Count / 2; i++)
                {
                    int pick = random.Next(indices.Count);
                    model._retained.Add(model._ids[indices[pick]]); indices.RemoveAt(pick);
                }
            }
            Current = model;
        }

        public IReadOnlyList<SettlementItem> GetItems()
        {
            var result = new SettlementItem[_ids.Count];
            for (int i = 0; i < result.Length; i++) result[i] = new SettlementItem(_ids[i], _counts[i], _selected.Contains(_ids[i]), _retained.Contains(_ids[i]));
            return result;
        }
        public bool Toggle(uint id, out string reason)
        {
            reason = null;
            if (!ReferenceEquals(_owner, MetaProgressService.Instance.Payload)) { reason = "存档会话已切换，请重新打开结算"; return false; }
            if (_applied || !Win || !_ids.Contains(id)) return false;
            if (_selected.Remove(id)) return true;
            if (_selected.Count >= MetaProgressService.Instance.DeployCap) { reason = "最多带出 " + MetaProgressService.Instance.DeployCap + " 件"; return false; }
            _selected.Add(id); return true;
        }
        public bool Confirm(bool discard, out string reason)
        {
            reason = null;
            if (!ReferenceEquals(_owner, MetaProgressService.Instance.Payload)) { reason = "存档会话已切换，请重新打开结算"; return false; }
            if (Completed) return false;
            bool committed = MetaCommandRunner.Execute("Run.Settle", (out string why) => {
                for (int i = 0; i < _ids.Count; i++)
                    if (Win ? (!discard && _selected.Contains(_ids[i])) : _retained.Contains(_ids[i]))
                        InventoryService.Instance.AddItem(_ids[i], _counts[i], out string unused);
                if (Win)
                {
                    var meta = MetaProgressService.Instance;
                    var payload = meta.Payload;
                    meta.MarkBossCleared(); meta.MarkSceneCleared(3); payload.clears++;
                    meta.TryAdvanceDepth();
                    if (payload.depth > payload.maxClearedDepth) payload.maxClearedDepth = payload.depth;
                    meta.TryAdvanceEra();
                }
                why = null; return true;
            }, out reason, () => { _applied = true; PendingSave = false; Completed = true; if (discard) _selected.Clear(); }, () => PendingSave = true);
            if (!committed) return false;
            return true;
        }
        public bool ReturnToMeta(out string reason)
        {
            if (!DispatchService.Instance.ReturnToMeta(out reason)) return false;
            RunControlModel.PauseWorld(false);
            RunInventory.Clear();
            return true;
        }
    }
}
