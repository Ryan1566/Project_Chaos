using System;
using System.Collections.Generic;
using Chaos.Gameplay.Interaction;
using RunMap;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class RunMapMarkerState
    {
        public string Id, Name;
        public RunMapMarkerKind Kind;
        public Vector2 Position;
        public Sprite Portrait;
        public bool Dimmed;
    }

    /// <summary>Converts the current map and registered runtime actors into a fog-filtered presentation snapshot.</summary>
    public sealed class RunMapModel : IDisposable
    {
        public Texture2D Terrain { get; private set; }
        public NaturalMapPlan Plan { get; private set; }
        public readonly List<RunMapMarkerState> Markers = new List<RunMapMarkerState>();
        public Vector2 Focus { get; private set; }
        public string Status { get; private set; }
        private readonly List<EnemyPlaceholder> _enemies = new List<EnemyPlaceholder>();
        private readonly Dictionary<string, RunMapMarkerState> _remembered = new Dictionary<string, RunMapMarkerState>();
        private readonly HashSet<string> _activeInteractions = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _remove = new List<string>();
        private Color32[] _base, _pixels;
        private int _revision = -1;

        public RunMapModel() { RunMapExplorationService.MapChanged += Reset; }
        public void Dispose() { RunMapExplorationService.MapChanged -= Reset; Reset(); }
        private void Reset()
        {
            if (Terrain != null)
            { if (Application.isPlaying) UnityEngine.Object.Destroy(Terrain); else UnityEngine.Object.DestroyImmediate(Terrain); }
            Terrain = null; Plan = null; _base = _pixels = null; _revision = -1;
            Markers.Clear(); _remembered.Clear(); _activeInteractions.Clear(); _enemies.Clear();
        }

        public bool Refresh()
        {
            RunMapExplorationService.Tick();
            var plan = RunMapExplorationService.Plan;
            var fog = RunMapExplorationService.Fog;
            if (plan == null || fog == null) { Reset(); Status = "地图正在准备"; return false; }
            if (!ReferenceEquals(Plan, plan))
            {
                Reset(); Plan = plan;
                _base = new Color32[plan.Width * plan.Height]; _pixels = new Color32[_base.Length];
                for (int y = 0; y < plan.Height; y++) for (int x = 0; x < plan.Width; x++)
                    _base[plan.Index(x, y)] = TerrainColor(plan, x, y);
                Terrain = new Texture2D(plan.Width, plan.Height, TextureFormat.RGBA32, false) {
                    name = "RunMapTerrain", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave };
            }
            if (_revision != fog.Revision)
            {
                if (_revision < 0 || _revision != fog.Revision - 1)
                    for (int i = 0; i < _pixels.Length; i++) UpdatePixel(fog, i);
                else foreach (int i in fog.DirtyCells) UpdatePixel(fog, i);
                Terrain.SetPixels32(_pixels); Terrain.Apply(false, false); _revision = fog.Revision;
            }
            RefreshMarkers(fog);
            Status = "第 " + plan.SceneIndex + "/3 图 · " + plan.Width + "×" + plan.Height + " · 已探索 "
                + (100f * fog.ExploredCount / _pixels.Length).ToString("F1") + "%";
            return true;
        }

        public static Color32 TerrainColor(NaturalMapPlan plan, int x, int y)
        {
            var space = plan.SpaceAt(x, y);
            if (!plan.IsSolid(x, y)) return space == MapSpaceKind.Underground ? new Color32(35, 42, 52, 255) : new Color32(71, 96, 110, 255);
            bool surface = y + 1 < plan.Height && !plan.IsSolid(x, y + 1);
            if (space == MapSpaceKind.Building) return surface ? new Color32(224, 195, 136, 255) : new Color32(145, 118, 84, 255);
            if (space == MapSpaceKind.Underground) return surface ? new Color32(148, 150, 164, 255) : new Color32(77, 82, 95, 255);
            return surface ? new Color32(146, 193, 120, 255) : new Color32(95, 115, 81, 255);
        }

        private void UpdatePixel(RunMapFogGrid fog, int i)
        {
            int x = i % Plan.Width, y = i / Plan.Width;
            if (!fog.IsExplored(x, y)) { _pixels[i] = new Color32(7, 10, 16, 255); return; }
            var c = _base[i];
            if (!fog.IsVisible(x, y)) c = new Color32((byte)(c.r * .32f), (byte)(c.g * .32f), (byte)(c.b * .32f), 255);
            _pixels[i] = c;
        }

        private void RefreshMarkers(RunMapFogGrid fog)
        {
            Markers.Clear();
            var player = PlayerSpawner.CurrentPlayer;
            Focus = new Vector2(Plan.Width, Plan.Height) * .5f;
            if (player != null && RunEncounter.OwnsRuntimeObject(player))
            {
                Focus = player.transform.position;
                var health = player.GetComponent<PlayerHealth>();
                AddActor(player, "player", RunMapMarkerKind.Player, health != null && health.IsDowned);
            }
            foreach (var ally in AllyController.All)
                if (ally != null && RunEncounter.OwnsRuntimeObject(ally.gameObject))
                    AddActor(ally.gameObject, "ally/" + ally.memberId + "/" + ally.GetInstanceID(), RunMapMarkerKind.Ally, ally.IsDowned);
            RunEncounter.CopyMapEnemies(_enemies);
            foreach (var enemy in _enemies)
                if (Visible(fog, enemy.transform.position)) AddActor(enemy.gameObject, "enemy/" + enemy.GetInstanceID(), RunMapMarkerKind.Enemy, false);

            _activeInteractions.Clear();
            foreach (var target in RunInteractable.Active)
            {
                if (target == null || !target.IsRegistered || !OwnsContent(target.gameObject)) continue;
                string id = "interactive/" + target.StableId;
                _activeInteractions.Add(id);
                var portrait = target.GetComponent<RunMapMarkerPortrait>();
                var inspectable = target as MapInspectable;
                bool npc = portrait != null ? portrait.Kind == RunMapMarkerKind.Npc : inspectable != null && inspectable.Kind == MapPlacementKind.Npc;
                bool remember = portrait != null ? portrait.RememberWhenHidden : !npc;
                bool visible = Visible(fog, target.InteractionPoint);
                if (visible)
                {
                    var marker = new RunMapMarkerState { Id = id, Name = target.DisplayName, Position = target.InteractionPoint,
                        Kind = npc ? RunMapMarkerKind.Npc : RunMapMarkerKind.Interactive, Portrait = PortraitFor(target.gameObject),
                        Dimmed = inspectable != null && inspectable.Inspected };
                    Markers.Add(marker);
                    if (remember) _remembered[id] = new RunMapMarkerState { Id = marker.Id, Name = marker.Name, Position = marker.Position,
                        Kind = marker.Kind, Portrait = marker.Portrait, Dimmed = true };
                    else _remembered.Remove(id);
                }
                else if (remember && _remembered.TryGetValue(id, out var known)) Markers.Add(known);
                else _remembered.Remove(id);
            }
            _remove.Clear();
            foreach (var entry in _remembered) if (!_activeInteractions.Contains(entry.Key)) _remove.Add(entry.Key);
            foreach (var id in _remove) _remembered.Remove(id);
        }

        public static bool Visible(RunMapFogGrid fog, Vector2 position)
        { var cell = Vector2Int.FloorToInt(position); return fog.IsVisible(cell.x, cell.y); }
        private static bool OwnsContent(GameObject target)
        {
            var root = RunMapBootstrap.CurrentMapRoot;
            return RunEncounter.OwnsRuntimeObject(target) || root != null && root.activeInHierarchy && target.transform.IsChildOf(root.transform);
        }
        private void AddActor(GameObject actor, string id, RunMapMarkerKind kind, bool downed)
        {
            if (!Plan.Inside(Mathf.FloorToInt(actor.transform.position.x), Mathf.FloorToInt(actor.transform.position.y))) return;
            Markers.Add(new RunMapMarkerState { Id = id, Name = actor.name, Kind = kind, Position = actor.transform.position,
                Portrait = kind == RunMapMarkerKind.Player ? PortraitFor(actor) : null, Dimmed = downed });
        }
        private static Sprite PortraitFor(GameObject actor)
        {
            var authored = actor.GetComponent<RunMapMarkerPortrait>();
            if (authored != null && authored.Portrait != null) return authored.Portrait;
            // Existing generated rectangles are not portraits; the view provides explicit person/chest silhouettes.
            if (actor.GetComponent<MapPopulationPlaceholder>() != null) return null;
            var sprite = actor.GetComponentInChildren<SpriteRenderer>();
            return sprite != null ? sprite.sprite : null;
        }
    }
}
