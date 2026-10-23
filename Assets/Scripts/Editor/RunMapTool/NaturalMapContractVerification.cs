using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RunMap;
using UnityEngine;

namespace RunMapTool.Editor
{
    public static class NaturalMapContractVerification
    {
        private sealed class TestPopulation : IMapPopulationPass
        {
            public string StreamKey => "qa/interactive-extension";
            public int Priority => 35;
            public void BuildPlan(MapPlacementContext context, RunMapRng random)
            {
                var cells = context.Candidates(0, false, MapCellTags.Supported | MapCellTags.Reachable,
                    MapCellTags.EntrySafety | MapCellTags.Reserved);
                int offset = random.Range(cells.Count);
                for (int i = 0; i < cells.Count; i++)
                {
                    MapPlacement placement;
                    if (context.TryReserve(new MapPlacementRequest { StableId = "qa/interactive", Kind = MapPlacementKind.Interactive,
                        Cell = cells[(i + offset) % cells.Count], AllowProtected = true,
                        ForbiddenTags = MapCellTags.EntrySafety | MapCellTags.Reserved }, out placement)) return;
                }
                throw new InvalidOperationException("Extension cannot reserve a safe interactive anchor");
            }
        }

        public static string Verify()
        {
            var results = new List<string>();
            var settings = NaturalMapRuntime.LoadParameters(1, false);
            var templates = NaturalMapRuntime.LoadStructures(settings);
            var actor = NaturalMapRuntime.CaptureActorProfile();
            Action<bool, string> check = (ok, text) => { results.Add((ok ? "PASS " : "FAIL ") + text); };
            Action<string, Action<NaturalMapParameters>> rejects = (name, mutate) =>
            {
                var p = settings.Copy(); mutate(p);
                var b = new NaturalMapBuild(12345, 1, p, templates, actor);
                check(b.Generate() == null && !string.IsNullOrEmpty(b.Error), name + " explicitly rejected");
            };
            rejects("invalid dimensions", p => p.widthTiles = 32);
            rejects("tile allocation budget", p => p.maxTileCells = 10);
            rejects("unsupported step height", p => p.maxStepHeight = 100);
            rejects("invalid frame budget", p => p.frameBudgetMs = float.NaN);
            rejects("invalid retry budget", p => p.retryMax = 0);
            rejects("insufficient object budget", p => p.maxContentObjects = 1);
            rejects("incompatible main route limits", p => { p.mainRouteMinLength = 900; p.mainRouteMaxLength = 1000; });
            rejects("invalid direction weight", p => p.startOnRightChance = 101);

            var leftSettings = settings.Copy(); leftSettings.startOnRightChance = 0;
            var rightSettings = settings.Copy(); rightSettings.startOnRightChance = 100;
            var left = new NaturalMapBuild(12345, 1, leftSettings, templates, actor).Generate();
            var right = new NaturalMapBuild(12345, 1, rightSettings, templates, actor).Generate();
            check(left != null && right != null && !left.StartsOnRight && right.StartsOnRight, "direction endpoints 0/100");
            bool mirror = left != null && right != null && left.Attempt == right.Attempt;
            if (mirror)
            {
                for (int x = 0; x < left.Width; x++) for (int y = 0; y < left.Height; y++)
                    if (left.IsSolid(x, y) != right.IsSolid(left.Width - 1 - x, y)) mirror = false;
                foreach (var p in left.Placements)
                {
                    var other = right.Placements.Find(t => t.StableId == p.StableId);
                    if (other == null || Math.Abs(p.Position.x + other.Position.x - left.Width) > .001f || p.Facing != -other.Facing) mirror = false;
                }
            }
            check(mirror, "terrain, structures, anchors and facing mirror together");
            check(left != null && right != null && Math.Abs(left.MainRouteLength - right.MainRouteLength) < .001f, "mirrored logical main route preserves length");
            var chunked = settings.Copy(); chunked.workColumnsPerFrame = 1; chunked.frameBudgetMs = 1;
            var baseline = new NaturalMapBuild(12345, 1, settings, templates, actor).Generate();
            var budgetVariant = new NaturalMapBuild(12345, 1, chunked, templates, actor).Generate();
            check(baseline != null && budgetVariant != null && baseline.Signature(false) == budgetVariant.Signature(false), "frame scheduling budgets never alter generated geometry or placements");

            string snapshot = settings.Fingerprint;
            var extended = new NaturalMapBuild(12345, 1, settings, templates, actor, new[] { new TestPopulation() }).Generate();
            check(extended != null && extended.Placements.Any(p => p.StableId == "qa/interactive"), "custom population pass uses shared reservation interface");
            check(settings.Fingerprint == snapshot, "generator never mutates authored parameter snapshot");
            if (extended != null)
            {
                var exit = extended.FindPlacement(MapPlacementKind.Exit);
                var route = extended.RouteTo(exit.Footprint.min);
                check(route.Count > 1 && route[route.Count - 1] == exit.Footprint.min, "queryable traversal route reaches exit");
                check(extended.MainRouteLength >= settings.mainRouteMinLength && extended.MainRouteLength <= settings.mainRouteMaxLength, "actual traversal length respects configured range");
                var context = new MapPlacementContext(extended); MapPlacement unused;
                check(context.Candidates(ushort.MaxValue).Count == 0, "unknown region does not silently query the whole map");
                check(!context.TryReserve(new MapPlacementRequest { StableId = "qa/outside", Kind = MapPlacementKind.Interactive, Cell = new Vector2Int(-1, 0) }, out unused), "out-of-bounds reservation rejected");
                var existing = extended.Placements[0];
                check(!context.TryReserve(new MapPlacementRequest { StableId = existing.StableId, Cell = existing.Footprint.min }, out unused), "duplicate stable anchor ID rejected");
                check(extended.Connections.All(c => extended.FindRegion(c.From) != null && extended.FindRegion(c.To) != null), "region connections reference valid regions");
            }
            var folder = NaturalMapVerification.OutputDirectory; Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "contracts.txt"), results);
            return string.Join("\n", results);
        }
    }
}
