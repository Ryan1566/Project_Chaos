using System.Collections.Generic;
using UnityEngine;

namespace Chaos.Gameplay.Items
{
    /// <summary>Single synchronous commit, run-owned cooldowns/buffs. No state is serialized into the village save.</summary>
    public static class RunItemUseService
    {
        sealed class Buff { public uint CharacterId; public int Value; public float Left; }
        static readonly Dictionary<uint, float> Cooldowns = new Dictionary<uint, float>();
        static readonly List<uint> Keys = new List<uint>();
        static readonly List<EnemyPlaceholder> Enemies = new List<EnemyPlaceholder>();
        static readonly List<Buff> Buffs = new List<Buff>();
        static int _context = -1;
        static bool _committing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Cooldowns.Clear(); Buffs.Clear(); _context = -1; _committing = false; }
        static void EnsureContext()
        {
            int context = GameSession.Instance.ContextId;
            if (_context == context && DispatchService.Instance.IsInRun) return;
            Clear(); _context = context;
        }
        public static void Clear()
        {
            Cooldowns.Clear(); Buffs.Clear();
            if (PlayerSpawner.CurrentPlayer != null) PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>()?.SetItemHealthBonus(0);
        }
        public static bool IsReady => DispatchService.Instance.IsInRun && RunMap.NaturalMapRuntime.IsReady
            && RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning && !RunEncounter.Defeated && !RunEncounter.RunCleared;
        public static PlayerHealth Actor => PlayerSpawner.CurrentPlayer == null ? null : PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>();
        public static bool CanUse(ItemBase item, out string reason)
        {
            EnsureContext(); reason = null;
            if (_committing || !IsReady) { reason = "当前不能使用局内道具"; return false; }
            if (item == null || item.HeldQuantity == 0) { reason = "道具已不存在"; return false; }
            if (Cooldowns.TryGetValue(item.Id, out float remaining) && remaining > 0) { reason = "道具冷却中"; return false; }
            return item.CanUse(Actor, out reason);
        }
        public static bool TryUse(RunBagSection section, int index, uint expectedId, out string reason)
        {
            if (section == RunBagSection.Misc) { reason = "请先拖入对应专用行"; return false; }
            var stack = RunInventory.GetSlot(section, index);
            if (stack == null || stack.itemId != expectedId) { reason = "格子内容已经改变，请重新选择"; return false; }
            var item = ItemBase.Create(expectedId);
            if (!CanUse(item, out reason)) return false;
            var actor = Actor;
            _committing = true;
            try
            {
                if (item.Consumes && !RunInventory.TryConsumeExact(section, index, expectedId, out reason)) return false;
                item.Apply(actor);
                Cooldowns[expectedId] = Mathf.Max(.1f, item.Profile.cooldown);
                // Presentation failure cannot turn an already committed effect into a second charge.
                try { item.PlayUseAnimation(actor); } catch (System.Exception error) { Debug.LogException(error); }
            }
            finally { _committing = false; }
            RunInventory.NotifyUseCommitted();
            reason = "已使用：" + item.Name; return true;
        }
        public static void Tick(float deltaTime)
        {
            EnsureContext();
            if (!IsReady || deltaTime <= 0) return;
            Keys.Clear(); Keys.AddRange(Cooldowns.Keys);
            foreach (uint id in Keys) Cooldowns[id] = Mathf.Max(0, Cooldowns[id] - deltaTime);
            for (int i = Buffs.Count - 1; i >= 0; i--) { Buffs[i].Left -= deltaTime; if (Buffs[i].Left <= 0) Buffs.RemoveAt(i); }
            ApplyCurrentBuff();
        }
        // Growth's controlled-character identity remains stable across map swaps and control changes.
        static uint CurrentCharacterId()
        { var ids = RunTeam.RunTeamProgress.MemberIds; return ids.Count == 0 ? 0 : ids[0]; }
        static void ApplyCurrentBuff()
        {
            int value = 0; uint character = CurrentCharacterId();
            foreach (var buff in Buffs) if (buff.CharacterId == character) value = Mathf.Max(value, buff.Value);
            Actor?.SetItemHealthBonus(value);
        }
        internal static void BoostHealth(PlayerHealth actor, int value, float duration)
        {
            uint character = CurrentCharacterId();
            var buff = Buffs.Find(x => x.CharacterId == character);
            if (buff == null) { buff = new Buff { CharacterId = character }; Buffs.Add(buff); }
            buff.Value = value; buff.Left = duration; actor.SetItemHealthBonus(value);
        }
        public static bool HasEnemyInRange(Vector2 origin, float radius)
        {
            RunEncounter.CopyMapEnemies(Enemies);
            foreach (var enemy in Enemies) if (enemy != null && !enemy.IsDead && ((Vector2)enemy.transform.position - origin).sqrMagnitude <= radius * radius) return true;
            return false;
        }
        internal static void DamageArea(Vector2 origin, float radius, int damage)
        {
            RunEncounter.CopyMapEnemies(Enemies);
            foreach (var enemy in Enemies) if (enemy != null && !enemy.IsDead && ((Vector2)enemy.transform.position - origin).sqrMagnitude <= radius * radius) enemy.TakeDamage(damage);
        }
    }
}
