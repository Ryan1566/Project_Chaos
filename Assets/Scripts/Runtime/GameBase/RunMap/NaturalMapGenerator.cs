using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    /// <summary>Pure planning: no scene objects, physics queries, Resources or global random state.</summary>
    public sealed class NaturalMapBuild
    {
        public NaturalMapPlan Plan { get; private set; }
        public NaturalMapPlan LastAttempt { get; private set; }
        public string Error { get; private set; }
        private readonly NaturalMapParameters settings;
        private readonly MapTraversalProfile actor;
        private readonly IList<MapStructureTemplate> templates;
        private readonly IList<IMapPopulationPass> extensions;
        private readonly uint seed;
        private readonly int stage;
        private readonly string fingerprint;
        private bool populationOk;

        public NaturalMapBuild(uint runSeed, int sceneIndex, NaturalMapParameters parameters,
            IList<MapStructureTemplate> structures, MapTraversalProfile profile = null, IList<IMapPopulationPass> population = null)
        {
            seed = runSeed; stage = sceneIndex; settings = parameters.Copy(); templates = structures;
            var sourceActor = profile ?? new MapTraversalProfile();
            actor = new MapTraversalProfile { Height = sourceActor.Height, BodyWidth = sourceActor.BodyWidth, MaxJumpHeight = sourceActor.MaxJumpHeight,
                MaxJumpDistance = sourceActor.MaxJumpDistance, MaxDrop = Math.Min(sourceActor.MaxDrop, settings.maxDropHeight) };
            extensions = population;
            var identity = new System.Text.StringBuilder(settings.Fingerprint).Append('/').Append(JsonUtility.ToJson(actor));
            if (structures != null) foreach (var t in structures)
            {
                uint hash = 2166136261;
                if (t.Solid != null) foreach (bool value in t.Solid) hash = unchecked((hash ^ (value ? 1u : 0u)) * 16777619);
                identity.Append('/').Append(t.Id).Append(':').Append(t.ResourcePath).Append(':').Append(t.Width).Append('x').Append(t.Height).Append(':').Append(hash)
                    .Append(':').Append(t.Weight).Append(':').Append(t.MaxCount).Append(':').Append(t.EntranceFloor).Append(':').Append(t.ShaftColumn);
            }
            fingerprint = MapRandom.Hash(identity.ToString()).ToString("X8");
        }

        public NaturalMapPlan Generate()
        {
            foreach (var unused in Steps()) { }
            return Plan;
        }

        public IEnumerable<string> Steps()
        {
            Plan = null; Error = null;
            var errors = settings.Validate(actor);
            if (stage < 1 || stage > 3) errors.Add("关卡编号必须在1..3");
            if (settings.buildingCountMin > 0 && (templates == null || templates.Count == 0)) errors.Add("手工结构资源池为空");
            if (errors.Count > 0) { Error = string.Join("；", errors); yield break; }
            string previousError = null;
            for (int attempt = 0; attempt <= settings.retryMax; attempt++)
            {
                bool fallback = attempt == settings.retryMax;
                var m = new NaturalMapPlan { Parameters = settings.Copy(), Actor = actor, RunSeed = seed, SceneIndex = stage,
                    ConfigFingerprint = fingerprint, Attempt = attempt + 1, Degraded = fallback };
                LastAttempt = m;
                m.Solid = new bool[m.Width * m.Height]; m.Protected = new bool[m.Solid.Length];
                m.Reachable = new bool[m.Solid.Length]; m.SurfaceY = new int[m.Width]; m.CaveFloor = new int[m.Width];
                yield return "规划区域与边界";
                Surface(m, fallback);
                if (!ChooseStructures(m, fallback)) { previousError = "结构数量、间距与地图宽度无法兼容"; continue; }
                yield return "生成连续地表";
                for (int x = 0; x < m.Width; x++)
                {
                    for (int y = 0; y < m.SurfaceY[x]; y++) m.Solid[m.Index(x, y)] = true;
                    if ((x + 1) % settings.workColumnsPerFrame == 0) yield return null;
                }
                yield return "雕刻地下与连接通路";
                Caves(m, fallback);
                yield return "嵌入手工结构";
                StampStructures(m);
                Regions(m);
                yield return "校验角色通行与生成锚点";
                foreach (var unused in NavigationSteps(m)) yield return null;
                if (!HasRequiredAccess(m)) { previousError = "入口、出口或战斗区域缺少可达支撑面"; continue; }
                yield return "规划NPC、交互物与杂物";
                foreach (var unused in Populate(m)) yield return null;
                if (!populationOk) { previousError = "必需内容点或对象预算无法满足"; continue; }
                if (Random(m, "layout/direction").Chance(settings.startOnRightChance)) Mirror(m);
                foreach (var unused in NavigationSteps(m)) yield return null;
                var exit = m.FindPlacement(MapPlacementKind.Exit);
                var route = m.RouteTo(new Vector2Int(exit.Footprint.x, exit.Footprint.y));
                for (int i = 1; i < route.Count; i++) m.MainRouteLength += Vector2.Distance(route[i - 1], route[i]);
                errors = NaturalMapValidation.Validate(m);
                if (errors.Count != 0) { previousError = string.Join("；", errors); continue; }
                if (fallback) m.Diagnostics.Add("使用有界平缓地形保底：" + previousError);
                else if (attempt > 0) m.Diagnostics.Add("前次失败：" + previousError);
                Plan = m;
                yield return "地图计划校验完成";
                yield break;
            }
            Error = "生成及有界保底失败：" + previousError;
        }

        private RunMapRng Random(NaturalMapPlan m, string stream)
        { return MapRandom.Stream(seed, stage, stream, m.Attempt - 1); }

        private static void Mirror(NaturalMapPlan m)
        {
            m.StartsOnRight = true;
            for (int x = 0; x < m.Width / 2; x++)
            {
                int other = m.Width - 1 - x;
                int sy = m.SurfaceY[x]; m.SurfaceY[x] = m.SurfaceY[other]; m.SurfaceY[other] = sy;
                int cy = m.CaveFloor[x]; m.CaveFloor[x] = m.CaveFloor[other]; m.CaveFloor[other] = cy;
                for (int y = 0; y < m.Height; y++)
                {
                    int a = m.Index(x, y), b = m.Index(other, y);
                    bool solid = m.Solid[a]; m.Solid[a] = m.Solid[b]; m.Solid[b] = solid;
                    bool protect = m.Protected[a]; m.Protected[a] = m.Protected[b]; m.Protected[b] = protect;
                }
            }
            foreach (var r in m.Regions) r.Bounds = new Rect(m.Width - r.Bounds.xMax, r.Bounds.yMin, r.Bounds.width, r.Bounds.height);
            foreach (var s in m.Structures) { s.Bounds = new RectInt(m.Width - s.Bounds.xMax, s.Bounds.y, s.Bounds.width, s.Bounds.height); s.MirrorX = true; }
            foreach (var p in m.Placements)
            {
                p.Position = new Vector2(m.Width - p.Position.x, p.Position.y); p.Facing = -p.Facing;
                p.Footprint = new RectInt(m.Width - p.Footprint.xMax, p.Footprint.y, p.Footprint.width, p.Footprint.height);
            }
            for (int i = 0; i < m.Entrances.Count; i++) m.Entrances[i] = new Vector2Int(m.Width - 1 - m.Entrances[i].x, m.Entrances[i].y);
            foreach (var c in m.Connections) c.Position = new Vector2(m.Width - c.Position.x, c.Position.y);
        }

        private void Surface(NaturalMapPlan m, bool fallback)
        {
            var rng = Random(m, "terrain/surface");
            int current = settings.surfaceBaseY, target = current;
            for (int x = 0; x < m.Width; x++)
            {
                if (x % settings.surfaceStepLength == 0) target = settings.surfaceBaseY + (fallback ? 0 : rng.Range(-settings.surfaceAmplitude, settings.surfaceAmplitude));
                if (x % settings.platformMinWidth == 0 && x > settings.edgeMargin + settings.entrySafetyRadius * 2
                    && x < m.Width - settings.edgeMargin - 12) current += Math.Sign(target - current) * Math.Min(settings.maxStepHeight, Math.Abs(target - current));
                m.SurfaceY[x] = current;
            }
        }

        private bool ChooseStructures(NaturalMapPlan m, bool fallback)
        {
            if (templates == null || templates.Count == 0) return settings.buildingCountMin == 0;
            var rng = Random(m, "structures");
            int count = fallback ? settings.buildingCountMin : rng.Range(settings.buildingCountMin, settings.buildingCountMax);
            var use = new Dictionary<uint, int>();
            int left = settings.edgeMargin + settings.entrySafetyRadius * 2 + 20;
            int right = m.Width - settings.edgeMargin - 16;
            for (int n = 0; n < count; n++)
            {
                var pool = new List<MapStructureTemplate>(); int weight = 0;
                foreach (var t in templates)
                {
                    int used; use.TryGetValue(t.Id, out used);
                    if (used >= t.MaxCount || t.Width < 4 || t.Height < 4 || t.Solid == null || t.Solid.Length != t.Width * t.Height) continue;
                    pool.Add(t); weight += Math.Max(1, t.Weight);
                }
                if (pool.Count == 0) return false;
                int roll = rng.Range(weight); MapStructureTemplate selected = pool[0];
                foreach (var t in pool) { roll -= Math.Max(1, t.Weight); if (roll < 0) { selected = t; break; } }
                int band = (right - left) / Math.Max(1, count), start = left + n * band;
                int x = start + rng.Range(0, Math.Max(0, band - selected.Width));
                int y = 0;
                for (int i = x - 3; i < x + selected.Width + 3; i++) y = Math.Max(y, m.SurfaceY[Mathf.Clamp(i, 0, m.Width - 1)]);
                var bounds = new RectInt(x, y - selected.EntranceFloor, selected.Width, selected.Height);
                if (bounds.xMax > right || bounds.yMax > m.Height - 4) return false;
                foreach (var other in m.Structures)
                    if (bounds.xMin < other.Bounds.xMax + settings.buildingMinSpacing) return false;
                // Foundation blending is part of planning, before any tiles are materialised.
                for (int i = x - 3; i < x + selected.Width + 3; i++) m.SurfaceY[i] = y;
                for (int i = x - 4; i >= settings.edgeMargin; i--)
                {
                    if (Math.Abs(m.SurfaceY[i] - m.SurfaceY[i + 1]) <= settings.maxStepHeight) break;
                    m.SurfaceY[i] = m.SurfaceY[i + 1] + Math.Sign(m.SurfaceY[i] - m.SurfaceY[i + 1]) * settings.maxStepHeight;
                }
                for (int i = x + selected.Width + 3; i < m.Width - settings.edgeMargin; i++)
                {
                    if (Math.Abs(m.SurfaceY[i] - m.SurfaceY[i - 1]) <= settings.maxStepHeight) break;
                    m.SurfaceY[i] = m.SurfaceY[i - 1] + Math.Sign(m.SurfaceY[i] - m.SurfaceY[i - 1]) * settings.maxStepHeight;
                }
                m.Structures.Add(new MapStructure { StableId = "structure/" + n, Template = selected, Bounds = bounds });
                int c; use.TryGetValue(selected.Id, out c); use[selected.Id] = c + 1;
            }
            return true;
        }

        private void Caves(NaturalMapPlan m, bool fallback)
        {
            var rng = Random(m, "terrain/caves");
            int leftTop = settings.edgeMargin + settings.entrySafetyRadius * 2 + 4;
            int rightTop = m.Width - settings.edgeMargin - 5;
            int ramp = settings.caveDepth + settings.surfaceAmplitude + 8;
            int a = leftTop + ramp, b = rightTop - ramp;
            int floor = settings.surfaceBaseY - settings.caveDepth;
            for (int x = 0; x < m.Width; x++)
            {
                if (!fallback && x % settings.caveWidth == 0) floor = Mathf.Clamp(floor + rng.Range(-1, 1), settings.surfaceBaseY - settings.caveDepth - 3, settings.surfaceBaseY - settings.caveDepth + 3);
                m.CaveFloor[x] = floor;
            }
            for (int pass = 0; pass < settings.caveSmoothPasses; pass++)
                for (int x = 1; x < m.Width - 1; x++) m.CaveFloor[x] = (m.CaveFloor[x - 1] + m.CaveFloor[x] * 2 + m.CaveFloor[x + 1]) / 4;
            var roofRandom = Random(m, "terrain/cave-roof");
            int roofHeight = settings.caveHeight, roofTarget = roofHeight;
            for (int x = a; x <= b; x++)
            {
                if (x % settings.caveWidth == 0) roofTarget = settings.caveHeight + (fallback ? 0 : roofRandom.Range(0, settings.chamberRadius));
                if (x % 2 == 0) roofHeight += Math.Sign(roofTarget - roofHeight);
                int height = Math.Max(settings.routeHeadroom, Math.Min(roofHeight, m.SurfaceY[x] - m.CaveFloor[x] - 4));
                CarveColumn(m, x, m.CaveFloor[x], height, true);
            }
            var leftEntrance = new Vector2Int(leftTop, m.SurfaceY[leftTop]);
            var rightEntrance = new Vector2Int(rightTop, m.SurfaceY[rightTop]);
            Stair(m, leftEntrance, new Vector2Int(a, m.CaveFloor[a]), true);
            Stair(m, new Vector2Int(b, m.CaveFloor[b]), rightEntrance, true);
            m.Entrances.Add(leftEntrance);
            m.Entrances.Add(rightEntrance);
            int entrances = fallback ? settings.caveEntranceMin : rng.Range(settings.caveEntranceMin, settings.caveEntranceMax);
            for (int i = 2; i < entrances; i++)
            {
                int x = a + (b - a) * (i - 1) / (entrances - 1);
                int end = Math.Min(b, x + ramp);
                if (InStructure(m, x, m.SurfaceY[x])) continue;
                var entrance = new Vector2Int(x, m.SurfaceY[x]);
                Stair(m, entrance, new Vector2Int(end, m.CaveFloor[end]), true);
                m.Entrances.Add(entrance);
            }
            int branches = fallback ? settings.branchCountMin : rng.Range(settings.branchCountMin, settings.branchCountMax);
            var branchRng = Random(m, "layout/branches");
            var branchPaths = new List<Vector2Int[]>();
            for (int n = 0; n < branches; n++)
            {
                int start = branchRng.Range(a + 4, b - 4);
                int dir = branchRng.Chance(50) ? -1 : 1;
                int length = branchRng.Range(settings.branchLengthMin, settings.branchLengthMax);
                int end = Mathf.Clamp(start + length * dir, a + 3, b - 3);
                int depth = Math.Min(settings.chamberRadius + 4, Math.Max(4, Math.Abs(end - start) / 2));
                int y = Math.Max(settings.undergroundMinY + 2, m.CaveFloor[end] - depth);
                Stair(m, new Vector2Int(start, m.CaveFloor[start]), new Vector2Int(end, y), false);
                branchPaths.Add(new[] { new Vector2Int(start, m.CaveFloor[start]), new Vector2Int(end, y) });
                int radius = settings.chamberRadius;
                for (int x = Math.Max(a, end - radius); x <= Math.Min(b, end + radius); x++)
                {
                    int h = settings.caveHeight + (int)Math.Sqrt(Math.Max(0, radius * radius - (x - end) * (x - end)));
                    CarveColumn(m, x, y, h, false);
                }
                if (branchRng.Chance(settings.loopChance))
                {
                    int rejoin = Mathf.Clamp(end + length * dir / 2, a, b);
                    if (Math.Abs(rejoin - end) >= depth)
                        Stair(m, new Vector2Int(end, y), new Vector2Int(rejoin, m.CaveFloor[rejoin]), false);
                }
            }
            // Restore the main route after all branch/chamber operations.
            for (int x = a; x <= b; x++)
            {
                Set(m, x, m.CaveFloor[x] - 1, true);
                CarveColumn(m, x, m.CaveFloor[x], settings.routeHeadroom, true);
            }
            foreach (var path in branchPaths)
            {
                int length = Math.Abs(path[1].x - path[0].x), dir = Math.Sign(path[1].x - path[0].x), portal = -1;
                for (int i = 0; i <= length; i++)
                {
                    int x = path[0].x + i * dir;
                    int y = path[0].y + (path[1].y - path[0].y) * i / Math.Max(1, length);
                    if (y > m.CaveFloor[x] - 3) continue;
                    if (portal < 0) portal = x;
                    CarveColumn(m, x, y, Math.Min(settings.caveHeight, m.CaveFloor[x] - y - 1), false);
                    Set(m, x, y - 1, true);
                }
                if (portal < a || portal >= b) continue;
                for (int n = 0; n < 2; n++)
                {
                    int x = portal + n * dir, y = m.CaveFloor[x] - 2 - n;
                    CarveColumn(m, x, y, m.CaveFloor[x] + settings.routeHeadroom - y, true);
                    Set(m, x, y - 1, true);
                }
            }
            // Open hatches after all bridge foundations; later bridge smoothing must not reseal them.
            for (int i = 0; i < m.Entrances.Count; i++)
            {
                var entrance = m.Entrances[i];
                int inward = i == 1 ? -1 : 1;
                for (int n = 0; n < 2; n++)
                {
                    int x = entrance.x + n * inward, level = entrance.y + (n == 0 ? 2 : 0);
                    int top = m.SurfaceY[x];
                    for (int y = level; y <= top + actor.Height; y++) Set(m, x, y, false);
                    Set(m, x, level - 1, true); m.SurfaceY[x] = level;
                    Protect(m, x, level, settings.routeHeadroom);
                }
                m.Entrances[i] = new Vector2Int(entrance.x, m.SurfaceY[entrance.x]);
            }
            // Optional surface ledges are outside the protected main route. They never sever it.
            var ledges = Random(m, "terrain/ledges");
            if (!fallback && settings.maxGapWidth > 0)
                for (int n = 0; n < branches; n++)
                {
                    int x0 = ledges.Range(settings.edgeMargin + 12, m.Width - settings.edgeMargin - settings.platformMinWidth - 12);
                    int y = m.SurfaceY[x0] + settings.maxDropHeight + 1;
                    for (int x = x0; x < x0 + settings.platformMinWidth; x++)
                        if (x - x0 >= settings.maxGapWidth && !InStructure(m, x, y)) Set(m, x, y, true);
                }
            for (int x = settings.edgeMargin; x < m.Width - settings.edgeMargin; x++)
                Protect(m, x, m.SurfaceY[x], settings.routeHeadroom);
        }

        private static bool InStructure(NaturalMapPlan m, int x, int y)
        { foreach (var s in m.Structures) if (s.Bounds.Contains(new Vector2Int(x, y))) return true; return false; }
        private static void Set(NaturalMapPlan m, int x, int y, bool solid)
        { if (m.Inside(x, y)) m.Solid[m.Index(x, y)] = solid; }
        private static void Protect(NaturalMapPlan m, int x, int y, int height)
        { for (int h = 0; h < height; h++) if (m.Inside(x, y + h)) m.Protected[m.Index(x, y + h)] = true; }
        private void CarveColumn(NaturalMapPlan m, int x, int y, int height, bool protect)
        {
            if (x < settings.edgeMargin || x >= m.Width - settings.edgeMargin) return;
            for (int h = 0; h < height; h++) Set(m, x, y + h, false);
            if (protect) Protect(m, x, y, settings.routeHeadroom);
        }
        private void Stair(NaturalMapPlan m, Vector2Int from, Vector2Int to, bool protect)
        {
            int length = Math.Abs(to.x - from.x), dir = Math.Sign(to.x - from.x);
            if (length < Math.Abs(to.y - from.y)) return;
            for (int i = 0; i <= length; i++)
            {
                int x = from.x + i * dir;
                int y = from.y + (to.y - from.y) * i / Math.Max(1, length);
                CarveColumn(m, x, y, settings.caveHeight, protect);
                Set(m, x, y - 1, true);
            }
            bool fromSurface = from.y == m.SurfaceY[from.x], toSurface = to.y == m.SurfaceY[to.x];
            if (protect && (fromSurface || toSurface))
            {
                for (int i = 0; i <= length; i++)
                {
                    int x = from.x + i * dir;
                    int y = from.y + (to.y - from.y) * i / Math.Max(1, length);
                    m.SurfaceY[x] = Math.Max(m.SurfaceY[x], y + settings.routeHeadroom + 1);
                }
                for (int x = 1; x < m.Width; x++) m.SurfaceY[x] = Math.Max(m.SurfaceY[x], m.SurfaceY[x - 1] - settings.maxStepHeight);
                for (int x = m.Width - 2; x >= 0; x--) m.SurfaceY[x] = Math.Max(m.SurfaceY[x], m.SurfaceY[x + 1] - settings.maxStepHeight);
                for (int x = 0; x < m.Width; x++) Set(m, x, m.SurfaceY[x] - 1, true);
            }
        }

        private void StampStructures(NaturalMapPlan m)
        {
            foreach (var s in m.Structures)
            {
                for (int x = 0; x < s.Bounds.width; x++)
                    for (int y = 0; y < s.Bounds.height; y++)
                    {
                        int wx = s.Bounds.x + x, wy = s.Bounds.y + y;
                        bool solid = s.Template.Solid[y * s.Bounds.width + x];
                        // Seal unused vertical sockets. Left/right retain the authored doorway profile.
                        if (Math.Abs(x - s.Template.ShaftColumn) <= 1 && (y < s.Template.EntranceFloor || y >= s.Bounds.height - 2)) solid = true;
                        Set(m, wx, wy, solid);
                    }
                // The prototype blocks have high ledges. Add a bounded internal stair rather
                // than claiming graph connectivity is enough for a two-tile jump character.
                if (m.Repairs < settings.repairMax)
                {
                    int floor = s.Bounds.y + s.Template.EntranceFloor;
                    int end = s.Bounds.width - 11;
                    for (int x = 5; x <= end; x++)
                    {
                        int rise = Math.Min(Math.Min(5, s.Bounds.height - s.Template.EntranceFloor - settings.routeHeadroom - 2), Math.Min((x - 5) / 2 + 1, (end - x) / 2 + 1));
                        for (int y = 0; y < rise; y++) Set(m, s.Bounds.x + x, floor + y, true);
                        for (int y = rise; y < rise + settings.routeHeadroom; y++) Set(m, s.Bounds.x + x, floor + y, false);
                    }
                    m.Repairs++;
                }
            }
        }

        private void Regions(NaturalMapPlan m)
        {
            var rng = Random(m, "layout/regions");
            int margin = settings.edgeMargin, entryEnd = margin + settings.entrySafetyRadius * 2 + 3;
            int splitY = settings.surfaceBaseY - settings.surfaceAmplitude - 3;
            m.Regions.Add(new EncounterRegion { Id = 1, StableId = "entry", Space = MapSpaceKind.Outdoor,
                Bounds = new Rect(margin, splitY, entryEnd - margin, m.Height - splitY), IsEntry = true });
            ushort id = 2;
            int cursor = entryEnd;
            foreach (int width in Partition(m.Width - margin - entryEnd, rng))
            {
                int x = cursor;
                var space = MapSpaceKind.Outdoor;
                foreach (var s in m.Structures) if (s.Bounds.center.x >= x && s.Bounds.center.x < x + width) space = MapSpaceKind.Building;
                m.Regions.Add(new EncounterRegion { Id = id++, StableId = "surface/" + x, Space = space, Required = stage != 2 || m.Regions.Count % 2 == 0,
                    Bounds = new Rect(x, splitY, width, m.Height - splitY) });
                cursor += width;
            }
            m.ExitRegionId = (ushort)(id - 1);
            int a = m.Width, b = 0;
            for (int x = margin; x < m.Width - margin; x++)
                for (int y = settings.undergroundMinY; y < splitY; y++)
                    if (m.CanStand(x, y)) { a = Math.Min(a, x); b = Math.Max(b, x + 1); break; }
            cursor = a;
            foreach (int width in Partition(b - a, rng))
            {
                int x = cursor;
                m.Regions.Add(new EncounterRegion { Id = id++, StableId = "underground/" + x, Space = MapSpaceKind.Underground, Required = stage != 3 || x == a,
                    Bounds = new Rect(x, settings.undergroundMinY, width, splitY - settings.undergroundMinY) });
                cursor += width;
            }
            foreach (var entrance in m.Entrances)
            {
                var above = m.Regions.Find(r => r.Space != MapSpaceKind.Underground && r.Bounds.xMin <= entrance.x && r.Bounds.xMax > entrance.x);
                EncounterRegion below = null; float distance = float.MaxValue;
                foreach (var r in m.Regions)
                    if (r.Space == MapSpaceKind.Underground)
                    {
                        float d = Math.Abs(entrance.x - Mathf.Clamp(entrance.x, r.Bounds.xMin, r.Bounds.xMax));
                        if (d < distance) { below = r; distance = d; }
                    }
                if (above != null && below != null) m.Connections.Add(new RegionConnection { From = above.Id, To = below.Id, Position = entrance, IsLoop = m.Connections.Count > 0 });
            }
            for (int i = 1; i < m.Regions.Count; i++)
            {
                var left = m.Regions[i - 1]; var right = m.Regions[i];
                if (Math.Abs(left.Bounds.xMax - right.Bounds.xMin) < .01f && Math.Abs(left.Bounds.yMin - right.Bounds.yMin) < .01f)
                    m.Connections.Add(new RegionConnection { From = left.Id, To = right.Id, Position = new Vector2(right.Bounds.xMin,
                        right.Space == MapSpaceKind.Underground ? m.CaveFloor[Mathf.Clamp((int)right.Bounds.xMin, 0, m.Width - 1)] : m.SurfaceY[(int)right.Bounds.xMin]) });
            }
        }

        private IEnumerable<int> Partition(int span, RunMapRng random)
        {
            int minCount = (span + settings.regionWidthMax - 1) / settings.regionWidthMax;
            int maxCount = span / settings.regionWidthMin;
            if (span <= 0 || minCount > maxCount) yield break;
            int count = random.Range(minCount, maxCount), remaining = span;
            for (int i = 0; i < count; i++)
            {
                int low = Math.Max(settings.regionWidthMin, remaining - (count - i - 1) * settings.regionWidthMax);
                int high = Math.Min(settings.regionWidthMax, remaining - (count - i - 1) * settings.regionWidthMin);
                int width = random.Range(low, high); remaining -= width; yield return width;
            }
        }

        private static bool HasRequiredAccess(NaturalMapPlan m)
        {
            foreach (var r in m.Regions)
                if (!m.StandingCells.Exists(c => m.Reachable[m.Index(c.x, c.y)] && r.Bounds.Contains(new Vector2(c.x + .5f, c.y + .05f)))) return false;
            var exit = m.FindPlacement(MapPlacementKind.Exit);
            int exitX = exit != null ? exit.Footprint.x : m.Width - m.Parameters.edgeMargin - 3;
            int exitY = exit != null ? exit.Footprint.y : m.SurfaceY[exitX];
            return m.Reachable[m.Index(exitX, exitY)];
        }

        public static IEnumerable<int> NavigationSteps(NaturalMapPlan m)
        {
            Array.Clear(m.Reachable, 0, m.Reachable.Length); m.StandingCells.Clear();
            m.RouteParents = new int[m.Solid.Length];
            for (int i = 0; i < m.RouteParents.Length; i++) m.RouteParents[i] = -1;
            var columns = new List<int>[m.Width];
            for (int x = 0; x < m.Width; x++)
            {
                columns[x] = new List<int>();
                for (int y = 1; y < m.Height - m.Actor.Height; y++)
                    if (m.CanStand(x, y)) { columns[x].Add(y); m.StandingCells.Add(new Vector2Int(x, y)); }
                if ((x + 1) % m.Parameters.workColumnsPerFrame == 0) yield return x;
            }
            var player = m.FindPlacement(MapPlacementKind.Player);
            int sx = player != null ? player.Footprint.x : m.Parameters.edgeMargin + m.Parameters.entrySafetyRadius;
            int sy = player != null ? player.Footprint.y : m.SurfaceY[sx];
            if (!m.CanStand(sx, sy)) yield break;
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(sx, sy)); m.Reachable[m.Index(sx, sy)] = true;
            int work = 0;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (int dx = -m.Actor.MaxJumpDistance; dx <= m.Actor.MaxJumpDistance; dx++)
                {
                    int x = cell.x + (m.StartsOnRight ? -dx : dx);
                    if (x < 0 || x >= m.Width || dx == 0) continue;
                    foreach (int y in columns[x])
                    {
                        if (m.Reachable[m.Index(x, y)] || y - cell.y > m.Actor.MaxJumpHeight || cell.y - y > m.Actor.MaxDrop) continue;
                        if (!TransitionClear(m, cell, new Vector2Int(x, y))) continue;
                        m.Reachable[m.Index(x, y)] = true; queue.Enqueue(new Vector2Int(x, y));
                        m.RouteParents[m.Index(x, y)] = m.Index(cell.x, cell.y);
                    }
                }
                if (++work % 128 == 0) yield return work;
            }
        }

        private static bool TransitionClear(NaturalMapPlan m, Vector2Int a, Vector2Int b)
        {
            int distance = Math.Abs(a.x - b.x);
            if (b.y < a.y && distance <= 2)
            {
                // A walk off a ledge followed by a controlled vertical drop. A jump-only
                // parabola incorrectly rejects descending through a low cave entrance.
                bool clear = true;
                for (int x = Math.Min(a.x, b.x); x <= Math.Max(a.x, b.x); x++)
                    for (int h = 0; h < m.Actor.Height; h++) if (m.IsSolid(x, a.y + h)) clear = false;
                for (int y = b.y; y <= a.y + m.Actor.Height - 1; y++) if (m.IsSolid(b.x, y)) clear = false;
                if (clear) return true;
            }
            // A short swept jump, including head clearance, rather than graph-only adjacency.
            int samples = distance * 4;
            float rise = Math.Max(a.y, b.y) > Math.Min(a.y, b.y) || distance > 1 ? m.Actor.MaxJumpHeight - .15f : 0f;
            for (int i = 1; i < samples; i++)
            {
                float t = (float)i / samples;
                float x = Mathf.Lerp(a.x + .5f, b.x + .5f, t);
                float y = Mathf.Lerp(a.y, b.y, t) + 4f * rise * t * (1f - t);
                int x0 = Mathf.FloorToInt(x - m.Actor.BodyWidth * .5f), x1 = Mathf.FloorToInt(x + m.Actor.BodyWidth * .5f - .01f);
                int y0 = Mathf.FloorToInt(y + .05f), y1 = Mathf.FloorToInt(y + m.Actor.Height - .15f);
                for (int cx = x0; cx <= x1; cx++) for (int cy = y0; cy <= y1; cy++) if (m.IsSolid(cx, cy)) return false;
            }
            return true;
        }

        private IEnumerable<string> Populate(NaturalMapPlan m)
        {
            populationOk = false;
            var context = new MapPlacementContext(m);
            int x = settings.edgeMargin + settings.entrySafetyRadius;
            MapPlacement placed;
            if (!context.TryReserve(new MapPlacementRequest { StableId = "player", Kind = MapPlacementKind.Player, RegionId = 1, Cell = new Vector2Int(x, m.SurfaceY[x]), AllowProtected = true }, out placed)) yield break;
            int[] allyOffsets = { -4, 4, 8 };
            for (int i = 0; i < 3; i++)
            {
                int ax = x + allyOffsets[i];
                if (!context.TryReserve(new MapPlacementRequest { StableId = "ally/" + i, Kind = MapPlacementKind.Ally, RegionId = 1, Cell = new Vector2Int(ax, m.SurfaceY[ax]), AllowProtected = true }, out placed)) yield break;
            }
            x = m.Width - settings.edgeMargin - 3;
            if (!context.TryReserve(new MapPlacementRequest { StableId = "exit", Kind = MapPlacementKind.Exit, RegionId = m.ExitRegionId,
                Cell = new Vector2Int(x, m.SurfaceY[x]), AllowProtected = true }, out placed)) yield break;
            foreach (var r in m.Regions)
            {
                if (!r.Required || r.IsEntry) continue;
                var rng = Random(m, "enemies/" + r.StableId);
                var candidates = context.Candidates(r.Id);
                for (int i = 0; i < settings.enemiesPerRegion; i++)
                    if (!ReserveRandom(context, candidates, rng, MapPlacementKind.Enemy, r.Id, "enemy/" + r.StableId + "/" + i, false)) yield break;
                yield return null;
            }
            var passes = new List<IMapPopulationPass> {
                new CountPopulationPass(MapPlacementKind.Npc, settings.npcCount, 20),
                new CountPopulationPass(MapPlacementKind.Interactive, settings.interactiveCount, 30),
                new CountPopulationPass(MapPlacementKind.Prop, settings.propCount, 40),
                new CountPopulationPass(MapPlacementKind.Decoration, settings.decorationCount, 50) };
            if (extensions != null) passes.AddRange(extensions);
            passes.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : string.CompareOrdinal(a.StreamKey, b.StreamKey));
            foreach (var pass in passes)
            {
                var random = Random(m, "population/" + pass.StreamKey);
                var builtin = pass as CountPopulationPass;
                if (builtin != null) foreach (var unused in builtin.Steps(context, random)) yield return null;
                else { pass.BuildPlan(context, random); yield return null; }
            }
            populationOk = true;
        }

        private static bool ReserveRandom(MapPlacementContext context, List<Vector2Int> candidates, RunMapRng rng,
            MapPlacementKind kind, ushort region, string key, bool solid)
        {
            int offset = rng.Range(candidates.Count);
            for (int n = 0; n < candidates.Count; n++)
            {
                var c = candidates[(offset + n) % candidates.Count]; MapPlacement placement;
                ushort id = region;
                if (id == 0)
                {
                    var r = context.Plan.Regions.Find(t => t.Bounds.Contains(new Vector2(c.x + .5f, c.y + .05f)));
                    if (r != null) id = r.Id;
                }
                if (context.TryReserve(new MapPlacementRequest { StableId = key, Kind = kind, RegionId = id,
                    Cell = c, Solid = solid, Height = solid || kind == MapPlacementKind.Decoration ? 1 : 2, AllowProtected = !solid }, out placement))
                {
                    if (solid)
                    {
                        foreach (var unused in NavigationSteps(context.Plan)) { }
                        bool valid = HasRequiredAccess(context.Plan);
                        foreach (var existing in context.Plan.Placements)
                            if (!existing.Solid && !context.Plan.Reachable[context.Plan.Index(existing.Footprint.x, existing.Footprint.y)]) valid = false;
                        if (!valid)
                        {
                            for (int x = placement.Footprint.xMin; x < placement.Footprint.xMax; x++)
                                for (int y = placement.Footprint.yMin; y < placement.Footprint.yMax; y++) context.Plan.Solid[context.Plan.Index(x, y)] = false;
                            context.Plan.Placements.Remove(placement);
                            foreach (var unused in NavigationSteps(context.Plan)) { }
                            if (context.Plan.Repairs >= context.Plan.Parameters.repairMax) return false;
                            context.Plan.Repairs++;
                            continue;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private sealed class CountPopulationPass : IMapPopulationPass
        {
            private readonly MapPlacementKind kind;
            private readonly int count;
            public string StreamKey { get { return kind.ToString(); } }
            public int Priority { get; private set; }
            public CountPopulationPass(MapPlacementKind type, int number, int priority) { kind = type; count = number; Priority = priority; }
            public void BuildPlan(MapPlacementContext context, RunMapRng random)
            { foreach (var unused in Steps(context, random)) { } }
            public IEnumerable<string> Steps(MapPlacementContext context, RunMapRng random)
            {
                var candidates = context.Candidates(0, kind == MapPlacementKind.Prop);
                for (int i = 0; i < count; i++)
                {
                    if (!ReserveRandom(context, candidates, random, kind, 0, StreamKey + "/" + i, kind == MapPlacementKind.Prop))
                    { context.Plan.Diagnostics.Add(StreamKey + "：可用位置不足，放置" + i + "/" + count); break; }
                    yield return null;
                }
            }
        }
    }

    public static class NaturalMapValidation
    {
        public static List<string> Validate(NaturalMapPlan m)
        {
            var e = new List<string>();
            if (m == null) { e.Add("无地图计划"); return e; }
            var ids = new HashSet<string>();
            foreach (var p in m.Placements)
            {
                if (!ids.Add(p.StableId)) e.Add("重复放置ID " + p.StableId);
                if (!m.Bounds.Contains(p.Footprint.min) || !m.Bounds.Contains(p.Footprint.max - Vector2Int.one)) e.Add("内容越界 " + p.StableId);
                if (!p.Solid && !m.Reachable[m.Index(p.Footprint.x, p.Footprint.y)]) e.Add("内容不可达 " + p.StableId);
                if (!p.Solid && !m.CanStand(p.Footprint.x, p.Footprint.y)) e.Add("内容缺少净空/支撑 " + p.StableId);
            }
            for (int i = 0; i < m.Placements.Count; i++) for (int j = i + 1; j < m.Placements.Count; j++)
                if (m.Placements[i].Footprint.Overlaps(m.Placements[j].Footprint)) e.Add("放置重叠");
            foreach (var b in m.Structures)
                if (!m.Bounds.Contains(b.Bounds.min) || !m.Bounds.Contains(b.Bounds.max - Vector2Int.one)) e.Add("结构越界");
            foreach (var r in m.Regions)
            {
                if (r.Bounds.xMin < 0 || r.Bounds.yMin < 0 || r.Bounds.xMax > m.Width || r.Bounds.yMax > m.Height) e.Add("区域越界");
                if (r.Required && m.Placements.FindAll(p => p.RegionId == r.Id && p.Kind == MapPlacementKind.Enemy).Count != m.Parameters.enemiesPerRegion) e.Add("战斗区域敌人预算不匹配");
                if (!r.IsEntry && (r.Bounds.width < m.Parameters.regionWidthMin || r.Bounds.width > m.Parameters.regionWidthMax)) e.Add("区域宽度超出参数范围");
                if (!m.StandingCells.Exists(c => m.Reachable[m.Index(c.x, c.y)] && r.Bounds.Contains(new Vector2(c.x + .5f, c.y + .05f)))) e.Add("存在孤立区域 " + r.StableId);
            }
            if (m.Placements.Count > m.Parameters.maxContentObjects) e.Add("对象预算超出");
            if (m.MainRouteLength < m.Parameters.mainRouteMinLength || m.MainRouteLength > m.Parameters.mainRouteMaxLength) e.Add("主路线长度超出参数范围");
            return e;
        }
    }
}
