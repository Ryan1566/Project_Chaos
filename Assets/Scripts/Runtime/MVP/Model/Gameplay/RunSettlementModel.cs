using System.Collections.Generic;
using System;

namespace Chaos.MVP.Model.Gameplay
{
    public readonly struct SettlementItem
    {
        public uint Id { get; }
        public uint Count { get; }
        public bool Selected { get; }
        public bool Retained { get; }
        public bool Important { get; }
        public SettlementItem(uint id, uint count, bool selected, bool retained, bool important = false)
        { Id = id; Count = count; Selected = selected; Retained = retained; Important = important; }
    }

    public sealed class RunSettlementReport
    {
        public int StagesPassed { get; } = RunEncounter.StagesPassed;
        public int Kills { get; } = RunEncounter.RunKills;
        public int HighestDamage { get; } = RunEncounter.HighestRunDamage;
        public float PlaySeconds { get; } = RunEncounter.RunPlaySeconds;
        public IReadOnlyList<RunTeamMemberState> Members { get; } = Array.AsReadOnly(new RunTeamModel().Read().Members);
        public IReadOnlyList<string> Events { get; } = Array.Empty<string>();
    }

    /// <summary>结算由本局持有，面板重建不能重置结果、页码或勾选。</summary>
    public sealed class RunSettlementModel
    {
        public static RunSettlementModel Current { get; private set; }
        private readonly List<uint> _ids = new List<uint>();
        private readonly List<uint> _counts = new List<uint>();
        private readonly HashSet<uint> _selected = new HashSet<uint>();
        private readonly List<uint> _important = new List<uint>();
        private SavePayload _owner;
        private int _ownerContext;
        private bool _applied;
        public bool Win { get; private set; }
        public bool PendingSave { get; private set; }
        public bool Completed { get; private set; }
        public uint Seed { get; private set; }
        public uint RunCount { get; private set; }
        public int SelectedCount => _selected.Count;
        public int RetainedCount => Win ? _ids.Count : _selected.Count;
        public int Page { get; private set; }
        public int FailureCarryCap { get; private set; }
        public RunSettlementReport Report { get; private set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime() { Current = null; }
        public static void Clear() { Current = null; }

        public void MovePage(int delta)
        {
            if (!Completed && (delta == -1 || delta == 1))
                Page = UnityEngine.Mathf.Clamp(Page + delta, 0, 2);
        }

        internal void CaptureChoices(ActiveRunSave save)
        {
            save.settlementPage = Page;
            save.retained.AddRange(_selected);
        }

        internal void RestoreChoices(ActiveRunSave save)
        {
            Page = save.settlementPage;
            _selected.Clear();
            if (!Win) foreach (uint id in save.retained)
                if (_selected.Count < FailureCarryCap && _ids.Contains(id)) _selected.Add(id);
        }

        public static void Begin(bool win)
        {
            var owner = MetaProgressService.Instance.Payload;
            int context = GameSession.Instance.ContextId;
            if (Current != null && Current._ownerContext == context && ReferenceEquals(Current._owner, owner) && Current.Seed == RunEncounter.RunSeed && Current.RunCount == owner.runs)
                return;
            var model = new RunSettlementModel { Win = win, Seed = RunEncounter.RunSeed, RunCount = owner.runs, _owner = owner, _ownerContext = context,
                Report = new RunSettlementReport(), FailureCarryCap = MetaProgressService.Instance.FailureCarryCap };
            foreach (var stack in RunInventory.Stacks)
            {
                if (RunInventory.IsImportant(stack.itemId)) continue;
                model._ids.Add(stack.itemId); model._counts.Add(stack.count);
            }
            foreach (uint id in RunBagProgression.OwnedItems())
                if (RunEncounter.IsNewRunImportant(id)) model._important.Add(id);
            Current = model;
        }

        public IReadOnlyList<SettlementItem> GetItems()
        {
            var result = new SettlementItem[_ids.Count + _important.Count];
            for (int i = 0; i < _ids.Count; i++) result[i] = new SettlementItem(_ids[i], _counts[i], _selected.Contains(_ids[i]), Win);
            for (int i = 0; i < _important.Count; i++) result[_ids.Count + i] = new SettlementItem(_important[i], 1, false, true, true);
            return result;
        }
        public bool Toggle(uint id, out string reason)
        {
            reason = null;
            if (_ownerContext != GameSession.Instance.ContextId || !ReferenceEquals(_owner, MetaProgressService.Instance.Payload)) { reason = "存档会话已切换，请重新打开结算"; return false; }
            if (_applied || Win || Page != 1 || !_ids.Contains(id)) return false;
            if (_selected.Remove(id)) return true;
            if (_selected.Count >= FailureCarryCap) { reason = $"最多带出 {FailureCarryCap} 种物品"; return false; }
            _selected.Add(id); return true;
        }
        public bool Confirm(bool discard, out string reason)
        {
            reason = null;
            if (_ownerContext != GameSession.Instance.ContextId || !ReferenceEquals(_owner, MetaProgressService.Instance.Payload)) { reason = "存档会话已切换，请重新打开结算"; return false; }
            if (Completed || Page != 2 || discard) { reason = "请在事件页确认本局结算"; return false; }
            var wounds = RunEncounter.CaptureWounds();
            bool committed = MetaCommandRunner.Execute("Run.Settle", (out string why) => {
                for (int i = 0; i < _ids.Count; i++)
                    if ((Win || _selected.Contains(_ids[i]))
                        && !InventoryService.Instance.AddItem(_ids[i], _counts[i], out why)) return false;
                RunEncounter.ApplyRunWounds(!Win);
                _owner.activeRun = null;
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
            }, out reason, () => { _applied = true; PendingSave = false; Completed = true; },
                () => { RunEncounter.RestoreWounds(wounds); PendingSave = true; });
            if (!committed) return false;
            Chaos.Gameplay.RunTeam.RunTeamProgress.Clear();
            return true;
        }
        public bool ReturnToMeta(out string reason)
        {
            if (_ownerContext != GameSession.Instance.ContextId || !ReferenceEquals(_owner, MetaProgressService.Instance.Payload))
            { reason = "存档会话已切换，请重新打开结算"; return false; }
            if (!Completed) { reason = "请先完成结算"; return false; }
            if (!DispatchService.Instance.ReturnAfterSettlement(out reason)) return false;
            RunControlModel.PauseWorld(false);
            RunInventory.Clear();
            return true;
        }
    }
}
