using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    /// <summary>Shared, stage-local exploration. No scene searches, textures, saves or map UI dependencies.</summary>
    public static class RunMapExplorationService
    {
        public static event System.Action MapChanged;
        public const int DefaultRadius = 14;
        public const float RefreshSeconds = .2f;
        public static NaturalMapPlan Plan { get; private set; }
        public static RunMapFogGrid Fog { get; private set; }
        public static int Generation { get; private set; }
        private static object _context;
        private static uint _seed;
        private static int _stage, _radius = DefaultRadius;
        private static float _next;
        private static readonly List<Vector2Int> Observers = new List<Vector2Int>();
        private static readonly List<Vector2Int> Previous = new List<Vector2Int>();
        private static bool _needsRefresh;
        public static int VisionRadius
        {
            get => _radius;
            set { int radius = Mathf.Clamp(value, 0, 64); if (_radius != radius) { _radius = radius; _needsRefresh = true; } }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { Clear(); MapChanged = null; _next = 0; _radius = DefaultRadius; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var root = new GameObject("RunMapExploration");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<RunMapExplorationDriver>();
        }

        public static void Clear()
        {
            bool changed = Plan != null || Fog != null;
            if (changed) Generation++;
            Plan = null; Fog = null; _context = null;
            Observers.Clear(); Previous.Clear(); _needsRefresh = true;
            if (changed) MapChanged?.Invoke();
        }

        public static void Tick(bool force = false)
        {
            // The reference is part of identity: regenerating even the same seed starts a fresh map instance.
            var plan = NaturalMapRuntime.IsReady && !RunSceneFlow.IsTransitioning ? NaturalMapRuntime.Current : null;
            object context = GameSession.Instance.ContextId;
            if (plan == null || !GameSession.Instance.HasSession) { Clear(); return; }
            if (!ReferenceEquals(Plan, plan) || !Equals(_context, context) || _seed != plan.RunSeed || _stage != plan.SceneIndex)
            {
                Plan = plan; _context = context; _seed = plan.RunSeed; _stage = plan.SceneIndex;
                Fog = new RunMapFogGrid(plan.Width, plan.Height); Generation++;
                Previous.Clear(); _needsRefresh = true; _next = 0;
                MapChanged?.Invoke();
            }
            if (!force && Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + RefreshSeconds;
            Observers.Clear();
            var player = PlayerSpawner.CurrentPlayer;
            if (player != null && RunEncounter.OwnsRuntimeObject(player)) AddObserver(player.transform.position);
            foreach (var ally in AllyController.All)
                if (ally != null && RunEncounter.OwnsRuntimeObject(ally.gameObject) && !ally.IsDowned) AddObserver(ally.transform.position);
            Observers.Sort((a, b) => { int y = a.y.CompareTo(b.y); return y != 0 ? y : a.x.CompareTo(b.x); });
            bool changed = _needsRefresh || Observers.Count != Previous.Count;
            if (!changed) for (int i = 0; i < Observers.Count; i++) if (Observers[i] != Previous[i]) { changed = true; break; }
            if (!changed) return;
            Fog.UpdateVisibility(Observers, _radius, plan.IsSolid);
            Previous.Clear(); Previous.AddRange(Observers); _needsRefresh = false;
        }

        private static void AddObserver(Vector2 position)
        {
            var cell = Vector2Int.FloorToInt(position);
            if (Plan.Inside(cell.x, cell.y) && !Observers.Contains(cell)) Observers.Add(cell);
        }
    }

    public sealed class RunMapExplorationDriver : MonoBehaviour
    {
        private void Update() { RunMapExplorationService.Tick(); }
    }
}
