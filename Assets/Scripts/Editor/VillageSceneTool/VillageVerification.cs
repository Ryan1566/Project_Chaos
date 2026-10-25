using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using VillageAI;

/// <summary>Read-only configuration, saved scene, job arbitration and tree-preemption contracts. No player saves.</summary>
public static class VillageVerification
{
    public static string Verify()
    {
        var lines = new List<string>(); Action<bool, string> check = (ok, text) => lines.Add((ok ? "PASS " : "FAIL ") + text);
        var p = VillageSceneSettings.Load(); check(p.widthTiles == 96 && p.workerCount == 4, "standard village dimensions and roles");
        var texts = ConfigLoader.Instance.LoadData<VillageTextConfig>().datas;
        check(texts.Count == 30 && texts.Select(t => t.key).Distinct().Count() == 30, "thirty distinct authored UI and role text keys");
        check(texts.All(t => !string.IsNullOrEmpty(t.zhCN) && !string.IsNullOrEmpty(t.zhTW) && !string.IsNullOrEmpty(t.enUS) && !string.IsNullOrEmpty(t.jaJP) && !string.IsNullOrEmpty(t.koKR)), "all five languages populated");
        Action<Action<VillageSceneSettings>, string> reject = (edit, name) => {
            var copy = JsonUtility.FromJson<VillageSceneSettings>(JsonUtility.ToJson(p)); edit(copy); bool rejected = false;
            try { copy.Validate(); } catch (InvalidOperationException) { rejected = true; } check(rejected, "reject " + name);
        };
        reject(c => c.widthTiles = 47, "small bounds"); reject(c => c.widthTiles = 95, "asymmetric tile bounds");
        reject(c => c.groundY = .5f, "non-grid ground height"); reject(c => c.workerCount = 0, "empty workforce");
        reject(c => c.workSeconds = float.NaN, "non-finite work period"); reject(c => c.plotSpacing = 2, "overlapping plots");
        reject(c => c.firstPlotX = -100, "out-of-bounds plots"); reject(c => c.wanderMinSeconds = 20, "reversed wander range");
        var legacy = JsonUtility.FromJson<BuildingSave>("{\"buildingType\":2001,\"level\":1,\"completed\":true}");
        check(legacy.productionSeconds == 0 && legacy.pendingOutput == 0, "old building JSON gains empty production state without invented output");
        object owner = new object(), other = new object(); VillageWorkforce.Clear();
        check(VillageWorkforce.TryAcquire(2001, owner) && !VillageWorkforce.TryAcquire(2001, other), "a job has one owner");
        VillageWorkforce.Release(2001, other); check(!VillageWorkforce.TryAcquire(2001, other), "another NPC cannot release the existing owner's job");
        VillageWorkforce.Working(2001, owner, true); check(!VillageWorkforce.CanProduce(2001, owner), "no production outside a ready village");
        VillageWorkforce.Release(2001, owner); check(VillageWorkforce.TryAcquire(2001, other), "job can be reacquired after release"); VillageWorkforce.Clear();
        int firstCalls = 0, runningCalls = 0;
        var sequence = new Sequence(new ActionNode(_ => { firstCalls++; return Status.Success; }), new ActionNode(_ => ++runningCalls < 3 ? Status.Running : Status.Success));
        sequence.Tick(.1f); sequence.Tick(.1f); sequence.Tick(.1f);
        check(firstCalls == 1 && runningCalls == 3, "running sequence does not reacquire its job every frame");
        bool priority = false; int interrupted = 0;
        var selector = new ReactiveSelector(new Guard(() => priority, new ActionNode(_ => Status.Running)), new ActionNode(_ => Status.Running, () => interrupted++));
        selector.Tick(.1f); priority = true; selector.Tick(.1f);
        check(interrupted == 1, "work availability preempts and resets idle behavior"); selector.Reset();
        check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/MainScene.unity"), "MainScene enabled in build settings");
        var original = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
        try
        {
            var root = scene.GetRootGameObjects().Single(g => g.name == "VillageRoot"); var plots = root.GetComponentsInChildren<VillagePlot>();
            check(root.GetComponent<VillageSceneBootstrap>() != null && plots.Length == 4 && plots.Select(t => t.BuildingId).Distinct().Count() == 4, "saved scene owns four plot anchors and village bootstrap");
            var ground = root.GetComponentInChildren<Tilemap>();
            check(ground.cellBounds.size.x == p.widthTiles && ground.cellBounds.size.y == p.groundDepth && ground.GetComponent<CompositeCollider2D>() != null, "saved fixed terrain and composite collision match configuration");
            VillageSceneBootstrap.ValidateSceneLayout(p, ground, plots);
            Action<Action<VillageSceneSettings>, string> rejectStaleScene = (edit, name) => {
                var changed = JsonUtility.FromJson<VillageSceneSettings>(JsonUtility.ToJson(p)); edit(changed); changed.Validate();
                bool rejected = false;
                try { VillageSceneBootstrap.ValidateSceneLayout(changed, ground, plots); } catch (InvalidOperationException) { rejected = true; }
                check(rejected, "reject stale scene after legal " + name + " change");
            };
            rejectStaleScene(c => c.groundY += 1, "ground height");
            rejectStaleScene(c => c.firstPlotX += 1, "first plot position");
            rejectStaleScene(c => c.plotSpacing += 1, "plot spacing");
            check(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).All(c => c != null), "saved scene has no missing scripts");
        }
        finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); if (opened) EditorSceneManager.CloseScene(scene, true); }
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "QA/Village")); File.WriteAllLines(Path.Combine(Application.dataPath, "QA/Village/contracts.txt"), lines); return string.Join("\n", lines);
    }
}
