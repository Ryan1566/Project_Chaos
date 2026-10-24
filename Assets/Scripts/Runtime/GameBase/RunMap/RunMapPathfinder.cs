using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    public enum MapMotionKind { Walk, Jump, Drop }
    public struct MapPathStep
    {
        public Vector2Int From, To;
        public MapMotionKind Kind;
        public float FlightTime;
        public float HorizontalSpeed;
        public Vector2 Destination => new Vector2(To.x + .5f, To.y + .03f);
    }
    public struct MapMotionProfile
    {
        public float Speed, JumpForce, Gravity, Width, Height;
        public string Key => Speed.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "/"
            + JumpForce.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "/"
            + Gravity.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "/"
            + Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "/"
            + Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Directed walking/jumping graph. Edges use the actor's actual ballistic capability.</summary>
    public sealed class RunMapPathfinder
    {
        private readonly NaturalMapPlan map;
        private readonly MapMotionProfile actor;
        private readonly List<Vector2Int>[] columns;
        private readonly Dictionary<int, List<MapPathStep>> edges = new Dictionary<int, List<MapPathStep>>();
        private static NaturalMapPlan cachedMap;
        private static readonly Dictionary<string, RunMapPathfinder> Cache = new Dictionary<string, RunMapPathfinder>();
        public int LastExpanded { get; private set; }

        public static RunMapPathfinder For(NaturalMapPlan map, MapMotionProfile actor)
        {
            if (!ReferenceEquals(cachedMap, map)) { Cache.Clear(); cachedMap = map; }
            RunMapPathfinder result;
            if (!Cache.TryGetValue(actor.Key, out result)) Cache.Add(actor.Key, result = new RunMapPathfinder(map, actor));
            return result;
        }
        public RunMapPathfinder(NaturalMapPlan map, MapMotionProfile actor)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map)); this.actor = actor;
            if (actor.Gravity <= 0 || actor.Speed <= 0 || actor.JumpForce <= 0 || actor.Width <= 0 || actor.Height <= 0)
                throw new ArgumentException("Invalid motion profile");
            foreach (float value in new[] { actor.Gravity, actor.Speed, actor.JumpForce, actor.Width, actor.Height })
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Non-finite motion profile");
            columns = new List<Vector2Int>[map.Width];
            for (int x = 0; x < columns.Length; x++) columns[x] = new List<Vector2Int>();
            foreach (var cell in map.StandingCells) if (map.CanStand(cell.x, cell.y)) columns[cell.x].Add(cell);
        }
        public bool TryNearest(Vector2 feet, int radius, Rect? bounds, out Vector2Int cell)
        {
            cell = default; float best = float.PositiveInfinity;
            int left = Mathf.Max(0, Mathf.FloorToInt(feet.x) - radius), right = Mathf.Min(map.Width - 1, Mathf.FloorToInt(feet.x) + radius);
            for (int x = left; x <= right; x++) foreach (var c in columns[x])
            {
                var p = new Vector2(c.x + .5f, c.y + .03f);
                if (Mathf.Abs(p.y - feet.y) > radius || (bounds.HasValue && !bounds.Value.Contains(p)) || !Clear(p.x, p.y)) continue;
                float distance = (p - feet).sqrMagnitude;
                if (distance < best) { best = distance; cell = c; }
            }
            return !float.IsPositiveInfinity(best);
        }
        public bool TryPath(Vector2 feet, Vector2 destination, int radius, int budget, Rect? allowedBounds,
            out List<MapPathStep> path)
        {
            path = new List<MapPathStep>(); LastExpanded = 0;
            Vector2Int start, goal;
            if (!TryNearest(feet, radius, allowedBounds, out start) || !TryNearest(destination, radius, allowedBounds, out goal)) return false;
            int s = map.Index(start.x, start.y), g = map.Index(goal.x, goal.y);
            if (s == g) return true;
            var queue = new Queue<int>(); queue.Enqueue(s);
            var parents = new Dictionary<int, MapPathStep>(); var seen = new HashSet<int> { s };
            while (queue.Count > 0 && LastExpanded++ < budget)
            {
                int index = queue.Dequeue();
                foreach (var edge in Neighbors(new Vector2Int(index % map.Width, index / map.Width)))
                {
                    int next = map.Index(edge.To.x, edge.To.y);
                    if (seen.Contains(next) || (allowedBounds.HasValue && !allowedBounds.Value.Contains(edge.Destination))) continue;
                    seen.Add(next); parents.Add(next, edge);
                    if (next == g)
                    {
                        for (int cursor = g; cursor != s;)
                        { var previous = parents[cursor]; path.Add(previous); cursor = map.Index(previous.From.x, previous.From.y); }
                        path.Reverse(); return true;
                    }
                    queue.Enqueue(next);
                }
            }
            return false;
        }
        public IReadOnlyList<MapPathStep> Neighbors(Vector2Int from)
        {
            int index = map.Index(from.x, from.y); List<MapPathStep> result;
            if (edges.TryGetValue(index, out result)) return result;
            result = new List<MapPathStep>(); edges.Add(index, result);
            int reach = Mathf.Min(map.Actor.MaxJumpDistance, Mathf.CeilToInt(actor.Speed * actor.JumpForce * 2 / actor.Gravity));
            for (int distance = 1; distance <= Mathf.Max(1, reach); distance++) for (int sign = -1; sign <= 1; sign += 2)
            {
                int x = from.x + sign * distance;
                if (x < 0 || x >= map.Width) continue;
                foreach (var to in columns[x])
                {
                    if (to.y - from.y > map.Actor.MaxJumpHeight || from.y - to.y > map.Actor.MaxDrop) continue;
                    MapPathStep edge;
                    if (TryEdge(from, to, out edge)) result.Add(edge);
                }
            }
            return result;
        }
        private bool TryEdge(Vector2Int from, Vector2Int to, out MapPathStep edge)
        {
            edge = new MapPathStep { From = from, To = to };
            float dx = to.x - from.x, dy = to.y - from.y;
            if (dy > 1 && Mathf.Abs(dx) > 1)
            {
                // Do not skip a usable intermediate stair. A capsule can settle on its
                // rounded corner before reaching the higher destination of a two-step jump.
                for (int x = Math.Min(from.x, to.x) + 1; x < Math.Max(from.x, to.x); x++)
                    if (map.CanStand(x, from.y + 1)) return false;
            }
            if (dy == 0)
            {
                bool walk = true;
                int count = Mathf.CeilToInt(Mathf.Abs(dx) * 4);
                for (int i = 0; i <= count; i++)
                {
                    float x = from.x + .5f + dx * i / count;
                    if (!Clear(x, from.y + .03f) || !map.IsSolid(Mathf.FloorToInt(x), from.y - 1)) walk = false;
                }
                if (walk) { edge.Kind = MapMotionKind.Walk; return true; }
            }
            if (dy < 0 && Mathf.Abs(dx) <= 2)
            {
                bool clear = true;
                for (int i = 0; i <= 8; i++)
                {
                    float t = i / 8f;
                    if (!Clear(from.x + .5f + dx * t, from.y + .03f)) clear = false;
                    if (!Clear(to.x + .5f, Mathf.Lerp(from.y + .03f, to.y + .03f, t))) clear = false;
                }
                if (clear) { edge.Kind = MapMotionKind.Drop; return true; }
            }
            float discriminant = actor.JumpForce * actor.JumpForce - 2 * actor.Gravity * dy;
            if (discriminant < 0) return false;
            float time = (actor.JumpForce + Mathf.Sqrt(discriminant)) / actor.Gravity;
            if (Mathf.Abs(dx) / time > actor.Speed + .01f) return false;
            int samples = Mathf.Max(16, Mathf.CeilToInt(time / .025f));
            for (int i = 0; i <= samples; i++)
            {
                float t = time * i / samples;
                if (!Clear(from.x + .5f + dx * t / time, from.y + .03f + actor.JumpForce * t - .5f * actor.Gravity * t * t))
                    return TryWallAscent(from, to, out edge);
            }
            edge.Kind = MapMotionKind.Jump; edge.FlightTime = time; return true;
        }
        private bool TryWallAscent(Vector2Int from, Vector2Int to, out MapPathStep edge)
        {
            edge = new MapPathStep { From = from, To = to };
            if (to.y <= from.y || Mathf.Abs(to.x - from.x) > 2) return false;
            // Horizontal collision holds the body against the face during ascent; vertical motion continues.
            // This is a physical jump onto a ledge, not a straight parabolic sweep through the wall.
            float x = from.x + .5f, y = from.y + .03f, t = 0;
            for (int i = 1; i <= 140; i++)
            {
                t = i * .01f;
                float nx = Mathf.MoveTowards(x, to.x + .5f, actor.Speed * .01f);
                if (Clear(nx, y)) x = nx;
                float ny = from.y + .03f + actor.JumpForce * t - .5f * actor.Gravity * t * t;
                if (!Clear(x, ny))
                {
                    if (actor.JumpForce - actor.Gravity * t < 0 && Mathf.Abs(y - to.y) < .2f
                        && Mathf.Abs(x - (to.x + .5f)) < .7f && Clear(x, to.y + .03f))
                    { edge.Kind = MapMotionKind.Jump; edge.FlightTime = t; edge.HorizontalSpeed = actor.Speed; return true; }
                    return false;
                }
                y = ny;
                if (y < to.y - .15f && actor.JumpForce - actor.Gravity * t < 0) return false;
            }
            return false;
        }
        private bool Clear(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x - actor.Width * .5f + .025f), x1 = Mathf.FloorToInt(x + actor.Width * .5f - .025f);
            int y0 = Mathf.FloorToInt(y + .025f), y1 = Mathf.FloorToInt(y + actor.Height - .04f);
            for (int cx = x0; cx <= x1; cx++) for (int cy = y0; cy <= y1; cy++) if (map.IsSolid(cx, cy)) return false;
            return true;
        }
    }
}
