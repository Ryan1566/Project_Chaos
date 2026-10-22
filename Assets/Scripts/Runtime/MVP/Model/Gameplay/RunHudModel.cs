using System.Collections.Generic;

namespace Chaos.MVP.Model.Gameplay
{
    public readonly struct CombatantSnapshot
    {
        public readonly string Name;
        public readonly int Hp, MaxHp;
        public readonly bool Downed;
        public CombatantSnapshot(string name, int hp, int maxHp, bool downed) { Name = name; Hp = hp; MaxHp = maxHp; Downed = downed; }
    }
    public sealed class RunHudModel
    {
        public IReadOnlyList<CombatantSnapshot> ReadTeam()
        {
            var rows = new List<CombatantSnapshot>();
            var player = PlayerSpawner.CurrentPlayer;
            if (player != null)
            {
                var health = player.GetComponent<PlayerHealth>();
                rows.Add(new CombatantSnapshot("受控角色", health == null ? 0 : health.Hp, health == null ? 0 : health.maxHp, health != null && health.IsDowned));
            }
            foreach (var ally in AllyController.All)
            {
                if (rows.Count >= 4) break;
                if (ally != null) rows.Add(new CombatantSnapshot(ally.AllyName, ally.Hp, ally.MaxHp, ally.IsDowned));
            }
            return rows.ToArray();
        }
        public uint Depth => MetaProgressService.Instance.Depth;
        public uint DepthCap => MetaProgressService.Instance.DepthCap;
        public int DeployCap => MetaProgressService.Instance.DeployCap;
        public int SceneIndex => RunEncounter.CurrentSceneIndex;
    }
}
