#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RunMap;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Compare rendered solid tiles, the final plan and actual collider geometry in a disposable scene.</summary>
public static class MapTileCollisionVerification
{
    [Serializable] public sealed class Result
    {
        public int stage; public uint seed; public bool mirrored;
        public int solidCells, missingPhysics, extraPhysics, visibleEmpty, missingTiles, numericalSeams;
        public List<string> examples = new List<string>();
        public List<string> colliders = new List<string>();
    }
    public static string Capture(int stage, uint seed, bool requireAligned = false)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run this geometry check in Edit mode");
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        NaturalMapAssembler assembler = null;
        try
        {
            var settings = NaturalMapRuntime.LoadParameters(stage, false);
            var build = new NaturalMapBuild(seed, stage, settings, NaturalMapRuntime.LoadStructures(settings), NaturalMapRuntime.CaptureActorProfile());
            var plan = build.Generate(); if (plan == null) throw new Exception(build.Error);
            assembler = new NaturalMapAssembler(plan); foreach (var unused in assembler.Steps(null)) { }
            if (assembler.Root == null || assembler.Error != null) throw new Exception(assembler.Error ?? "assembly failed");
            Physics2D.SyncTransforms();
            var root = assembler.Root;
            var ground = root.transform.Find("Tilemap_Ground").GetComponent<Tilemap>();
            var colliders = new List<Collider2D>();
            var hits = new Collider2D[32];
            var filter = new ContactFilter2D { useTriggers = false };
            var result = new Result { stage = stage, seed = seed, mirrored = plan.StartsOnRight };
            foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
                if (c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && !c.usedByComposite)
                {
                    colliders.Add(c); var composite = c as CompositeCollider2D;
                    result.colliders.Add(c.transform.parent.name + "/" + c.name + " " + c.GetType().Name + " simulated=" + (c.attachedRigidbody == null || c.attachedRigidbody.simulated) + " paths=" + (composite == null ? -1 : composite.pathCount));
                }
            for (int y = 0; y < plan.Height; y++) for (int x = 0; x < plan.Width; x++)
            {
                bool expected = plan.IsSolid(x, y), actual = false;
                Vector2 point = root.transform.TransformPoint(new Vector3(x + .5f, y + .5f));
                int count = Physics2D.OverlapPoint(point, filter, hits);
                if (count == hits.Length) throw new Exception("Physics query buffer full");
                for (int i = 0; i < count; i++) if (colliders.Contains(hits[i])) { actual = true; break; }
                // Composite triangulation can reject an exact point on an internal seam.
                // Confirm coverage within 0.001 tile, rather than treating that numerical point as a missing tile.
                if (expected && !actual)
                    foreach (var hit in Physics2D.OverlapCircleAll(point, .001f))
                        if (colliders.Contains(hit)) { actual = true; result.numericalSeams++; break; }
                if (expected) result.solidCells++;
                if (expected && !actual)
                {
                    result.missingPhysics++; string area = "";
                    for (int yy = y + 2; yy >= y - 2; yy--) { area += "/"; for (int xx = x - 2; xx <= x + 2; xx++) area += plan.IsSolid(xx, yy) ? "#" : "."; }
                    string probes = ""; var gc = ground.GetComponent<CompositeCollider2D>();
                    foreach (var delta in new[] {new Vector2(.03f,0),new Vector2(-.03f,0),new Vector2(0,.03f),new Vector2(0,-.03f),new Vector2(.03f,.03f)})
                        probes += gc.OverlapPoint(point + delta) ? "1" : "0";
                    Example(result, "missing collider " + x + "," + y + " neighbors=" + area + " nearby=" + probes);
                }
                if (!expected && actual) { result.extraPhysics++; Example(result, "extra collider " + x + "," + y); }
                if (expected && !ground.HasTile(new Vector3Int(x, y, 0))) { result.missingTiles++; Example(result, "missing tile " + x + "," + y); }
            }
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.name != "Tilemap_Ground" && map.name != "Tilemap_Deco") continue;
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    if (!map.HasTile(cell) || (map.name == "Tilemap_Deco" && map.GetColliderType(cell) == Tile.ColliderType.None)) continue;
                    var local = root.transform.InverseTransformPoint(map.GetCellCenterWorld(cell));
                    int x = Mathf.FloorToInt(local.x), y = Mathf.FloorToInt(local.y);
                    if (!plan.Inside(x, y) || !plan.IsSolid(x, y))
                    { result.visibleEmpty++; Example(result, "visible tile on empty plan " + map.transform.parent.name + "/" + map.name + " " + x + "," + y); }
                }
            }
            string json = JsonUtility.ToJson(result, true);
            string directory = Application.dataPath + "/QA/ActorMapBugs"; Directory.CreateDirectory(directory);
            File.WriteAllText(directory + "/map-" + stage + "-" + seed + ".json", json);
            if (requireAligned && (result.missingPhysics + result.extraPhysics + result.visibleEmpty + result.missingTiles > 0)) throw new Exception(json);
            return json;
        }
        finally
        {
            assembler?.Dispose(); SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene, true);
        }
    }
    static void Example(Result result, string message) { if (result.examples.Count < 12) result.examples.Add(message); }
}
#endif
