using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RunMap;
using UnityEditor;
using UnityEngine;

namespace RunMapTool.Editor
{
    /// <summary>Editor-only pure-plan checks. Never publishes Current, assembles a scene or accesses player saves.</summary>
    public static class NaturalMapVerification
    {
        private sealed class Inputs
        {
            public NaturalMapParameters Parameters;
            public List<MapStructureTemplate> Structures;
            public MapTraversalProfile Actor;
            public IMapPopulationPass[] Passes;
        }

        [Serializable]
        private sealed class Sample
        {
            public int index;
            public uint seed;
            public bool generated, deterministic, decorationChecked, decorationIndependent, degraded;
            public int attempts, repairs, regions, structures, placements;
            public double generationMs;
            public string structuralHash, buildingLayoutHash, structuralSignature, buildingLayoutSignature;
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private sealed class BatchReport
        {
            public int generatorVersion, stage, start, count, completed, generated, passed, failures, degraded;
            public int deterministicChecks, deterministicFailures, decorationChecks, decorationFailures;
            public int distinctStructuralSignatures, distinctBuildingLayouts;
            public int firstFailureIndex = -1;
            public uint firstFailureSeed;
            public string firstFailure, configFingerprint, startedUtc, reportPath;
            public double elapsedMs, meanGenerationMs, p95GenerationMs, maxGenerationMs;
            public List<Sample> samples = new List<Sample>();
        }

        private static readonly Dictionary<int, Inputs> Cache = new Dictionary<int, Inputs>();
        private static int _scheduledStage, _scheduledStart;
        private const int ScheduledBatch = 100;
        public static string OutputDirectory => Path.Combine(Application.dataPath, "QA", "MapGeneration");

        /// <summary>Call after changing config/assets without a domain reload. Each stage loads inputs only once.</summary>
        public static void ClearCachedInputs() { Cache.Clear(); }

        private static Inputs GetInputs(int stage)
        {
            if (stage < 1 || stage > 3) throw new ArgumentOutOfRangeException(nameof(stage), "stage must be 1..3");
            Inputs inputs;
            if (Cache.TryGetValue(stage, out inputs)) return inputs;
            NaturalMapParameters parameters = NaturalMapRuntime.LoadParameters(stage, false);
            inputs = new Inputs { Parameters = parameters.Copy(), Structures = NaturalMapRuntime.LoadStructures(parameters),
                Actor = NaturalMapRuntime.CaptureActorProfile(), Passes = NaturalMapRuntime.PopulationPasses.ToArray() };
            Cache.Add(stage, inputs);
            return inputs;
        }

        private static uint SeedAt(int index)
        {
            return index == 0 ? 0u : MapRandom.Hash("natural-map-verification/seed/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static NaturalMapPlan Generate(Inputs inputs, int stage, uint seed, NaturalMapParameters parameters, out string error)
        {
            var build = new NaturalMapBuild(seed, stage, parameters, inputs.Structures, inputs.Actor, inputs.Passes);
            NaturalMapPlan plan = build.Generate();
            error = build.Error;
            return plan;
        }

        /// <summary>Bounded batch API; use start=0 to include seed 0, then continue with starts 100/200/etc.</summary>
        public static string VerifyBatch(int stage, int start, int count)
        {
            if (start < 0 || count < 1 || count > 1000 || start > int.MaxValue - count)
                throw new ArgumentOutOfRangeException(nameof(count), "start >= 0 and 1 <= count <= 1000 required");
            var report = new BatchReport { generatorVersion = NaturalMapPlan.GeneratorVersion, stage = stage, start = start,
                count = count, startedUtc = DateTime.UtcNow.ToString("o") };
            var wall = Stopwatch.StartNew();
            var timings = new List<double>();
            var structural = new HashSet<string>(StringComparer.Ordinal);
            var buildings = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                Inputs inputs = GetInputs(stage);
                report.configFingerprint = inputs.Parameters.Fingerprint;
                for (int index = start; index < start + count; index++)
                {
                    var sample = new Sample { index = index, seed = SeedAt(index) };
                    report.samples.Add(sample);
                    try
                    {
                        string error;
                        var generation = Stopwatch.StartNew();
                        NaturalMapPlan plan;
                        try { plan = Generate(inputs, stage, sample.seed, inputs.Parameters, out error); }
                        finally { generation.Stop(); sample.generationMs = generation.Elapsed.TotalMilliseconds; timings.Add(sample.generationMs); }
                        sample.generated = plan != null;
                        if (plan == null) sample.errors.Add("Generate: " + (error ?? "no plan"));
                        else
                        {
                            report.generated++;
                            sample.attempts = plan.Attempt; sample.repairs = plan.Repairs; sample.degraded = plan.Degraded;
                            sample.regions = plan.Regions.Count; sample.structures = plan.Structures.Count; sample.placements = plan.Placements.Count;
                            if (plan.Degraded) report.degraded++;
                            ValidatePlan(plan, sample.errors);
                            string physical = plan.Signature(false, false);
                            string buildingLayout = BuildingLayout(plan);
                            structural.Add(physical); buildings.Add(buildingLayout);
                            sample.structuralHash = MapRandom.Hash(physical).ToString("X8");
                            sample.buildingLayoutHash = MapRandom.Hash(buildingLayout).ToString("X8");
                            sample.structuralSignature = physical;
                            sample.buildingLayoutSignature = buildingLayout;
                            NaturalMapPlan repeated = Generate(inputs, stage, sample.seed, inputs.Parameters, out error);
                            report.deterministicChecks++;
                            sample.deterministic = repeated != null && plan.Signature() == repeated.Signature();
                            if (!sample.deterministic)
                            { report.deterministicFailures++; sample.errors.Add("Same seed/config produced a different signature: " + error); }
                            if (index % 10 == 0)
                            {
                                sample.decorationChecked = true; report.decorationChecks++;
                                NaturalMapParameters changed = inputs.Parameters.Copy();
                                changed.decorationCount = changed.decorationCount <= int.MaxValue - 7 ? changed.decorationCount + 7 : changed.decorationCount - 1;
                                NaturalMapPlan decorated = Generate(inputs, stage, sample.seed, changed, out error);
                                sample.decorationIndependent = decorated != null && physical == decorated.Signature(false, false);
                                if (!sample.decorationIndependent)
                                { report.decorationFailures++; sample.errors.Add("Decoration count changed terrain/non-decoration placements (fingerprint excluded): " + error); }
                            }
                        }
                    }
                    catch (Exception error) { sample.errors.Add(error.ToString()); }
                    report.completed++;
                    if (sample.errors.Count == 0) report.passed++;
                    else
                    {
                        report.failures++;
                        if (report.firstFailureIndex < 0)
                        { report.firstFailureIndex = index; report.firstFailureSeed = sample.seed; report.firstFailure = sample.errors[0]; }
                    }
                }
            }
            catch (Exception error)
            {
                report.firstFailureIndex = start; report.firstFailureSeed = SeedAt(start); report.firstFailure = "Input loading failed: " + error;
                report.failures = count;
            }
            wall.Stop();
            report.elapsedMs = wall.Elapsed.TotalMilliseconds;
            report.distinctStructuralSignatures = structural.Count; report.distinctBuildingLayouts = buildings.Count;
            timings.Sort();
            if (timings.Count > 0)
            {
                double total = 0; foreach (double time in timings) total += time;
                report.meanGenerationMs = total / timings.Count;
                report.p95GenerationMs = timings[Math.Max(0, (int)Math.Ceiling(timings.Count * .95) - 1)];
                report.maxGenerationMs = timings[timings.Count - 1];
            }
            Directory.CreateDirectory(OutputDirectory);
            report.reportPath = Path.Combine(OutputDirectory, "Pure-" + stage + "-" + start + ".json");
            File.WriteAllText(report.reportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            return "stage=" + stage + " start=" + start + " count=" + count + " passed=" + report.passed
                + " failed=" + report.failures + " degraded=" + report.degraded + " distinct=" + structural.Count
                + " buildings=" + buildings.Count + " P95ms=" + report.p95GenerationMs.ToString("F2")
                + (report.firstFailureIndex >= 0 ? " firstFailureSeed=" + report.firstFailureSeed + " " + report.firstFailure : "")
                + " report=" + report.reportPath;
        }

        private static bool Inside(RectInt bounds, RectInt box)
        {
            return box.width > 0 && box.height > 0 && bounds.Contains(box.min) && bounds.Contains(box.max - Vector2Int.one);
        }

        private static void ValidatePlan(NaturalMapPlan plan, List<string> errors)
        {
            if (plan.Bounds.xMin != 0 || plan.Bounds.yMin != 0 || plan.Width != plan.Parameters.widthTiles || plan.Height != plan.Parameters.heightTiles)
                errors.Add("Hard Bounds do not match configured dimensions");
            int cells = checked(plan.Width * plan.Height);
            if (plan.Solid == null || plan.Solid.Length != cells || plan.Reachable == null || plan.Reachable.Length != cells
                || plan.Protected == null || plan.Protected.Length != cells) { errors.Add("Invalid terrain grid length"); return; }
            try { errors.AddRange(NaturalMapValidation.Validate(plan)); }
            catch (Exception error) { errors.Add("NaturalMapValidation threw: " + error.Message); }
            var regionIds = new HashSet<ushort>();
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            int combatRegions = 0;
            foreach (EncounterRegion region in plan.Regions)
            {
                if (region == null) { errors.Add("Null region"); continue; }
                if (region.Required && !region.IsEntry) combatRegions++;
                if (region.Id == 0 || !regionIds.Add(region.Id) || string.IsNullOrEmpty(region.StableId) || !stableIds.Add(region.StableId))
                    errors.Add("Invalid/duplicate region ID: " + region.StableId);
                Rect b = region.Bounds;
                if (b.width <= 0 || b.height <= 0 || b.xMin < 0 || b.yMin < 0 || b.xMax > plan.Width || b.yMax > plan.Height)
                    errors.Add("Region outside hard Bounds: " + region.StableId);
            }
            EncounterRegion entry = plan.FindRegion(plan.EntryRegionId);
            if (entry == null || !entry.IsEntry || plan.FindRegion(plan.ExitRegionId) == null) errors.Add("Entry/exit region missing");
            if (combatRegions == 0) errors.Add("No required non-entry encounter region");
            int players = 0, allies = 0, exits = 0;
            stableIds.Clear();
            foreach (MapPlacement p in plan.Placements)
            {
                if (p == null) { errors.Add("Null placement"); continue; }
                if (string.IsNullOrEmpty(p.StableId) || !stableIds.Add(p.StableId)) errors.Add("Duplicate/missing placement ID: " + p.StableId);
                if (!Inside(plan.Bounds, p.Footprint)) { errors.Add("Placement outside Bounds: " + p.StableId); continue; }
                if (float.IsNaN(p.Position.x) || float.IsInfinity(p.Position.x) || float.IsNaN(p.Position.y) || float.IsInfinity(p.Position.y)
                    || p.Position.x < 0 || p.Position.y < 0 || p.Position.x >= plan.Width || p.Position.y >= plan.Height)
                    errors.Add("Invalid placement position: " + p.StableId);
                if (p.RegionId != 0 && !regionIds.Contains(p.RegionId)) errors.Add("Placement references missing region: " + p.StableId);
                if (!p.Solid && (!plan.Reachable[plan.Index(p.Footprint.x, p.Footprint.y)] || !plan.CanStand(p.Footprint.x, p.Footprint.y)))
                    errors.Add("Placement is not reachable/supported: " + p.StableId);
                if (p.Kind == MapPlacementKind.Player) players++;
                if (p.Kind == MapPlacementKind.Ally) allies++;
                if (p.Kind == MapPlacementKind.Exit) exits++;
            }
            if (players != 1 || allies != 3 || exits != 1) errors.Add("Player/ally/exit budget is not 1/3/1");
            for (int i = 0; i < plan.Placements.Count; i++) for (int j = i + 1; j < plan.Placements.Count; j++)
                if (plan.Placements[i] != null && plan.Placements[j] != null && plan.Placements[i].Footprint.Overlaps(plan.Placements[j].Footprint))
                    errors.Add("Placement footprints overlap: " + plan.Placements[i].StableId + " / " + plan.Placements[j].StableId);
        }

        private static string BuildingLayout(NaturalMapPlan plan)
        {
            var text = new StringBuilder();
            text.Append(plan.Width).Append('x').Append(plan.Height);
            foreach (MapStructure structure in plan.Structures)
                text.Append('|').Append(structure.Template.Id).Append('@').Append(structure.Bounds.x).Append(',')
                    .Append(structure.Bounds.y).Append(',').Append(structure.Bounds.width).Append(',').Append(structure.Bounds.height);
            return text.ToString();
        }

        /// <summary>Texture pixels use the same bottom-left origin as plan coordinates; no Y flip.</summary>
        public static string RenderPreview(int stage, uint seed)
        {
            Inputs inputs = GetInputs(stage);
            string error;
            NaturalMapPlan plan = Generate(inputs, stage, seed, inputs.Parameters, out error);
            if (plan == null) throw new InvalidOperationException("Preview generation failed: " + error);
            int scale = Math.Max(1, Math.Min(3, 4096 / Math.Max(plan.Width, plan.Height)));
            int width = plan.Width * scale, height = plan.Height * scale;
            var pixels = new Color32[width * height];
            for (int y = 0; y < plan.Height; y++) for (int x = 0; x < plan.Width; x++)
            {
                bool solid = plan.Solid[plan.Index(x, y)];
                MapSpaceKind space = plan.SpaceAt(x, y);
                Color32 color = space == MapSpaceKind.Building ? (solid ? new Color32(138, 103, 70, 255) : new Color32(50, 37, 31, 255))
                    : space == MapSpaceKind.Underground ? (solid ? new Color32(68, 72, 90, 255) : new Color32(20, 25, 39, 255))
                    : (solid ? new Color32(91, 112, 72, 255) : new Color32(28, 56, 85, 255));
                for (int dy = 0; dy < scale; dy++) for (int dx = 0; dx < scale; dx++) pixels[(y * scale + dy) * width + x * scale + dx] = color;
            }
            foreach (EncounterRegion region in plan.Regions)
            {
                Color32 color = region.IsEntry ? new Color32(95, 255, 140, 255) : new Color32(110, 160, 210, 255);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(region.Bounds.xMin * scale), 0, width - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(region.Bounds.xMax * scale) - 1, 0, width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(region.Bounds.yMin * scale), 0, height - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(region.Bounds.yMax * scale) - 1, 0, height - 1);
                for (int x = x0; x <= x1; x++) { pixels[y0 * width + x] = color; pixels[y1 * width + x] = color; }
                for (int y = y0; y <= y1; y++) { pixels[y * width + x0] = color; pixels[y * width + x1] = color; }
            }
            foreach (MapPlacement p in plan.Placements)
            {
                Color32 color = PlacementColor(p.Kind);
                int cx = Mathf.RoundToInt(p.Position.x * scale), cy = Mathf.RoundToInt(p.Position.y * scale);
                int radius = p.Kind == MapPlacementKind.Decoration ? 1 : Math.Max(2, scale);
                for (int y = cy - radius; y <= cy + radius; y++) for (int x = cx - radius; x <= cx + radius; x++)
                    if (x >= 0 && x < width && y >= 0 && y < height && Math.Abs(x - cx) + Math.Abs(y - cy) <= radius)
                        pixels[y * width + x] = color;
            }
            Directory.CreateDirectory(OutputDirectory);
            string path = Path.Combine(OutputDirectory, "Preview-" + stage + "-" + seed + ".png");
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try { texture.SetPixels32(pixels); texture.Apply(false, false); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            File.WriteAllText(Path.ChangeExtension(path, ".txt"), plan.Report + "\nCoordinates: world +Y is image up. Scale=" + scale
                + "\nMarkers: Player green, Ally cyan, Npc blue, Enemy red, Interactive yellow, Prop orange, Decoration grey, Exit magenta.", new UTF8Encoding(false));
            return path;
        }

        private static Color32 PlacementColor(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Player: return new Color32(80, 255, 100, 255);
                case MapPlacementKind.Ally: return new Color32(30, 240, 240, 255);
                case MapPlacementKind.Npc: return new Color32(70, 150, 255, 255);
                case MapPlacementKind.Enemy: return new Color32(255, 65, 65, 255);
                case MapPlacementKind.Interactive: return new Color32(255, 225, 40, 255);
                case MapPlacementKind.Prop: return new Color32(255, 150, 40, 255);
                case MapPlacementKind.Exit: return new Color32(255, 70, 235, 255);
                default: return new Color32(195, 190, 220, 255);
            }
        }

        [MenuItem("Tools/局内地图/自然地图/阶段1：分批验收1000", false, 210)]
        private static void VerifyStage1() { Schedule(1); }
        [MenuItem("Tools/局内地图/自然地图/阶段2：分批验收1000", false, 211)]
        private static void VerifyStage2() { Schedule(2); }
        [MenuItem("Tools/局内地图/自然地图/阶段3：分批验收1000", false, 212)]
        private static void VerifyStage3() { Schedule(3); }
        [MenuItem("Tools/局内地图/自然地图/停止分批验收", false, 213)]
        private static void CancelScheduled()
        { EditorApplication.update -= NextScheduledBatch; _scheduledStage = 0; }
        private static void Schedule(int stage)
        {
            CancelScheduled(); ClearCachedInputs(); _scheduledStage = stage; _scheduledStart = 0;
            EditorApplication.update += NextScheduledBatch;
        }
        private static void NextScheduledBatch()
        {
            if (_scheduledStage == 0 || EditorApplication.isCompiling) return;
            try
            {
                UnityEngine.Debug.Log("[NaturalMapVerification] " + VerifyBatch(_scheduledStage, _scheduledStart, ScheduledBatch));
                _scheduledStart += ScheduledBatch;
                if (_scheduledStart >= 1000) CancelScheduled();
            }
            catch (Exception error) { CancelScheduled(); UnityEngine.Debug.LogException(error); }
        }
    }
}
