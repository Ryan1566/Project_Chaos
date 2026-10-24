using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RunMap;
using UnityEngine;

namespace RunMapTool.Editor
{
    /// <summary>Read-only path/config checks; independent of scenes, player saves and publication.</summary>
    public static class RunBehaviorVerification
    {
        public static NaturalMapPlan Fixture(string kind)
        {
            var p = new NaturalMapParameters { widthTiles = 32, heightTiles = 20 };
            var m = new NaturalMapPlan { Parameters = p, Actor = new MapTraversalProfile(), Solid = new bool[640],
                Protected = new bool[640], Reachable = new bool[640], SurfaceY = new int[32], CaveFloor = new int[32] };
            for (int x = 0; x < 32; x++)
            {
                int y = kind == "Drop" && x < 14 ? 6 : (kind == "Step" || kind == "Step2") && x >= 8 && x <= 12 ? (kind == "Step2" ? 5 : 4) : 3;
                m.SurfaceY[x] = y;
                for (int h = 0; h < y; h++) if (kind != "Gap" || x < 16 || x > 17) m.Solid[m.Index(x, h)] = true;
            }
            if (kind == "Wall") for (int y = 3; y < 20; y++) m.Solid[m.Index(14, y)] = true;
            for (int x = 0; x < 32; x++) for (int y = 1; y < 18; y++) if (m.CanStand(x, y)) m.StandingCells.Add(new Vector2Int(x, y));
            return m;
        }
        public static MapMotionProfile Profile(float speed = 5)
        { return new MapMotionProfile { Speed = speed, JumpForce = 11, Gravity = 29.43f, Width = .7f, Height = 1.7f }; }
        public static string Verify()
        {
            var lines = new List<string>();
            Action<bool, string> check = (ok, label) => { lines.Add((ok ? "PASS " : "FAIL ") + label); };
            RunBehaviorSettings.Load("Enemy").Validate(); RunBehaviorSettings.Load("Ally").Validate();
            check(true, "both authored behavior rows validate");
            List<MapPathStep> path;
            foreach (string kind in new[] { "Flat", "Step", "Step2", "Gap", "Drop", "Wall" })
            {
                var map = Fixture(kind); var graph = new RunMapPathfinder(map, Profile());
                var from = new Vector2(3.5f, map.SurfaceY[3] + .03f); var to = new Vector2(27.5f, map.SurfaceY[27] + .03f);
                bool found = graph.TryPath(from, to, 2, 4096, null, out path);
                check(found == (kind != "Wall"), kind + " reachability");
                if (kind == "Flat") check(path.All(s => s.Kind == MapMotionKind.Walk), "flat terrain never causes unnecessary jumping");
                if (kind == "Step" || kind == "Step2" || kind == "Gap") check(path.Any(s => s.Kind == MapMotionKind.Jump), kind + " has a real ballistic jump");
                if (kind == "Drop") check(graph.Neighbors(new Vector2Int(13, 6)).Any(s => s.Kind == MapMotionKind.Drop), "drop supports a directed walk-off edge");
                if (kind == "Gap")
                {
                    check(!new RunMapPathfinder(map, Profile(3)).TryPath(from, to, 2, 4096, null, out path), "slow enemy cannot use ally-only jump distance");
                    check(graph.TryPath(to, from, 2, 4096, null, out path), "gap is traversable in mirrored direction");
                }
                if (kind == "Drop") check(!graph.TryPath(to, from, 2, 4096, null, out path), "drop is not incorrectly reversible into an impossible climb");
                if (kind == "Flat")
                {
                    check(!graph.TryPath(from, to, 2, 4096, new Rect(0, 0, 12, 20), out path), "region leash restricts the goal");
                    check(!graph.TryPath(from, to, 2, 1, null, out path), "search budget exhaustion never publishes a partial success");
                }
            }
            var output = Path.Combine(Application.dataPath, "QA/Behavior"); Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output, "contracts.txt"), lines);
            return string.Join("\n", lines);
        }
        public static string VerifyMaps(int seedsPerStage)
        {
            var lines = new List<string>(); int failures = 0, maps = 0;
            for (int stage = 1; stage <= 3; stage++)
            {
                var p = NaturalMapRuntime.LoadParameters(stage, false); var structures = NaturalMapRuntime.LoadStructures(p);
                var actor = NaturalMapRuntime.CaptureActorProfile();
                for (int i = 0; i < seedsPerStage; i++)
                {
                    uint seed = i == 0 ? 0 : MapRandom.Hash("behavior/" + i);
                    var m = new NaturalMapBuild(seed, stage, p, structures, actor).Generate(); maps++;
                    if (m == null) { failures++; lines.Add("FAIL generated stage=" + stage + " seed=" + seed); continue; }
                    var graph = new RunMapPathfinder(m, Profile()); List<MapPathStep> path;
                    var start = m.FindPlacement(MapPlacementKind.Player).Position; var end = m.FindPlacement(MapPlacementKind.Exit).Position;
                    bool ok = graph.TryPath(start, end, 2, 4096, null, out path);
                    if (!ok) { failures++; lines.Add("FAIL actual ally capability entry-exit stage=" + stage + " seed=" + seed); }
                }
            }
            lines.Insert(0, "maps=" + maps + " failures=" + failures);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "QA/Behavior"));
            File.WriteAllLines(Path.Combine(Application.dataPath, "QA/Behavior/maps.txt"), lines); return string.Join("\n", lines);
        }
    }
}
