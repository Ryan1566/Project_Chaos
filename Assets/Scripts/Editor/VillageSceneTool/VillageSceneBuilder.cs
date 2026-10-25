using System;
using System.Linq;
using RunMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Explicit editor authoring command. MainScene owns its saved fixed terrain and plot anchors.</summary>
public static class VillageSceneBuilder
{
    [MenuItem("Tools/Village/Rebuild MainScene Fixed Village")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing MainScene");
        var p = VillageSceneSettings.Load();
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("MainScene has unsaved edits; save or review them before rebuilding");
        SceneManager.SetActiveScene(scene);
        try
        {
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "VillageRoot" || g.name == "Canvas")) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("VillageRoot", typeof(VillageSceneBootstrap));
            var grid = new GameObject("FixedVillageGround", typeof(Grid)); grid.transform.SetParent(root.transform, false);
            var go = new GameObject("VillageGround", typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(grid.transform, false);
            var ground = go.GetComponent<Tilemap>();
            var palette = Resources.Load<GameObject>("Prefabs/RunMap/Kenney/" + p.groundTilePool + "/Chunk_" + p.groundTilePool + "_Combat").GetComponent<RunMapChunkTiles>();
            if (palette == null || palette.fill == null) throw new InvalidOperationException("Village ground tile palette missing");
            int left = -p.widthTiles / 2, floor = Mathf.RoundToInt(p.groundY);
            for (int x = left; x < left + p.widthTiles; x++) for (int y = floor - p.groundDepth; y < floor; y++)
            { var cell = new Vector3Int(x, y); ground.SetTile(cell, y == floor - 1 && palette.top != null ? palette.top : palette.fill); ground.SetColliderType(cell, Tile.ColliderType.Grid); }
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>().usedByComposite = true; go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
            for (int i = 0; i < 4; i++)
            {
                var plot = new GameObject("Plot_" + (2001 + i), typeof(VillagePlot)); plot.transform.SetParent(root.transform, false);
                plot.transform.position = new Vector2(p.firstPlotX + i * p.plotSpacing, p.groundY + .03f); plot.GetComponent<VillagePlot>().BuildingId = (uint)(2001 + i);
            }
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var edge = new GameObject("VillageBoundary", typeof(BoxCollider2D)); edge.transform.SetParent(root.transform, false);
                edge.transform.position = new Vector2(sign * (p.widthTiles * .5f + .5f), p.groundY + 4); edge.GetComponent<BoxCollider2D>().size = new Vector2(1, 24);
            }
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).FirstOrDefault();
            if (camera == null) camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 8; camera.transform.position = new Vector3(-28, p.groundY + 4.5f, -10);
            camera.backgroundColor = new Color(.48f, .65f, .8f); camera.clearFlags = CameraClearFlags.SolidColor;
            var previewAudio = camera.GetComponent<AudioListener>(); if (previewAudio != null) previewAudio.enabled = false;
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = canvas.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == scene.path);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(scene.path, true)); else scenes[index] = new EditorBuildSettingsScene(scene.path, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
