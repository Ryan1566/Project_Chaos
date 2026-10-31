using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chaos.Gameplay.RunTeam
{
    /// <summary>One run owns growth across stages. It never serializes or reads player saves.</summary>
    public static class RunTeamProgress
    {
        private static readonly Dictionary<uint, RunCharacterGrowth> Members = new Dictionary<uint, RunCharacterGrowth>();
        private static readonly List<uint> Order = new List<uint>();
        private static readonly IReadOnlyList<uint> ReadOnlyOrder = Order.AsReadOnly();
        private static int _context = -1;
        public static int RunId { get; private set; }
        public static event Action Changed;
        public static IReadOnlyList<uint> MemberIds { get { EnsureContext(); return ReadOnlyOrder; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { Clear(); Changed = null; }

        public static void BeginRun(IEnumerable<uint> team)
        {
            Clear();
            _context = GameSession.Instance.ContextId;
            if (team != null) foreach (uint id in team)
            {
                if (id == 0 || Members.ContainsKey(id) || Order.Count >= DispatchService.MaxTeamSize) continue;
                Members.Add(id, new RunCharacterGrowth(id)); Order.Add(id);
            }
            GameSession.Instance.ContextChanged += Clear;
            Changed?.Invoke();
        }

        public static void Clear()
        {
            GameSession.Instance.ContextChanged -= Clear;
            Members.Clear(); Order.Clear(); _context = -1; RunId++;
            Changed?.Invoke();
        }

        private static void EnsureContext()
        { if (_context >= 0 && _context != GameSession.Instance.ContextId) Clear(); }

        internal static bool TryRead(uint id, out RunCharacterGrowth growth)
        { EnsureContext(); return Members.TryGetValue(id, out growth); }

        /// <summary>Called once from the encounter's owned-enemy removal transaction.</summary>
        public static void AwardNormalKill()
        {
            EnsureContext();
            foreach (var member in Members.Values)
                member.TryAddExperience(RunCharacterGrowthRules.KillExperience, out _);
            Changed?.Invoke();
        }

        public static int HealthBonus(uint id)
        { return TryRead(id, out var growth) ? growth.HealthBonus : 0; }

        public static bool TrySpend(uint id, RunTeamAttribute attribute, out string reason)
        {
            if (!RunEncounter.IsSceneReady || RunEncounter.RunCleared || RunEncounter.Defeated
                || RunSceneFlow.IsTransitioning || !DispatchService.Instance.IsInRun)
            { reason = "当前不能加点"; return false; }
            if (!TryRead(id, out var growth)) { reason = "角色不在当前派遣队伍中"; return false; }
            // Initialize existing current health before changing the maximum, including lazy AI health.
            ApplyHealthBonus(id);
            if (!growth.TrySpendPoint(attribute, out reason)) return false;
            ApplyHealthBonus(id);
            Changed?.Invoke(); return true;
        }

        public static void ApplyHealthBonus(uint id)
        {
            int bonus = HealthBonus(id);
            var player = PlayerSpawner.CurrentPlayer;
            if (Order.Count > 0 && Order[0] == id && player != null && RunEncounter.OwnsRuntimeObject(player))
                player.GetComponent<PlayerHealth>()?.SetRunHealthBonus(bonus);
            foreach (var ally in AllyController.All)
                if (ally != null && ally.memberId == id && RunEncounter.OwnsRuntimeObject(ally.gameObject))
                    ally.SetRunHealthBonus(bonus);
        }

        public static void ApplyToSpawnedTeam()
        { foreach (uint id in Order) ApplyHealthBonus(id); }
    }
}
