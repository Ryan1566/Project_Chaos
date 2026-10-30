#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.MVP.Model.Gameplay;
using Chaos.Gameplay.Interaction;
using RunMap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Formal village/dispatch/three-map flow with synthetic input and an owned temporary save.</summary>
public static class RunMapFlowVerification
{
    public static string Status { get; private set; } = "idle";
    private static readonly StringBuilder Report = new StringBuilder();
    private static Keyboard _keyboard;
    private static string _storage, _overrides;
    private static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    private static void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    private static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/RunMapUI"); File.WriteAllText(Application.dataPath + "/QA/RunMapUI/flow.txt", Report.ToString()); }
    private static void Isolation()
    { if (string.IsNullOrEmpty(_storage) || SaveSlotService.SavesDirectory != _storage) throw new Exception("Save isolation lost"); }
    private static void Key(params Key[] keys) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new Exception("Requires idle Play");
        Report.Clear(); Status = "running";
        _storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-Map-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storage); SaveSlotService.UseEditorTestDirectory(_storage); Isolation();
        _overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        _keyboard = InputSystem.AddDevice<Keyboard>("ChaosMapFlowKeyboard");
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        for (int i = 0; i < 4; i++) RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, payload), "isolated slot created");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    private static IEnumerator Guard(IEnumerator routine)
    {
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext()) { Status = "passed"; Cleanup(); Report.AppendLine("DONE isolated=" + _storage); Flush(); yield break; }
                current = routine.Current;
            }
            catch (Exception error) { Status = "failed"; Report.AppendLine("FAIL " + error); Cleanup(); Flush(); yield break; }
            yield return current;
        }
    }
    private static void Cleanup()
    {
        InputManager.Instance.LoadOverridesJson(_overrides);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        SettingsManager.Instance.RevertEdit();
    }
    private static Button Button(string name)
    { return Top.GetComponentsInChildren<Button>().Single(b => b.name == name); }
    private static void Click(string name)
    {
        var button = Button(name); Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform; var canvas = button.GetComponentInParent<Canvas>();
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        Check(hits.Count > 0 && (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform)), "top raycast " + name);
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
    }
    private static IEnumerator Run()
    {
        string reason;
        yield return new WaitForSecondsRealtime(4);
        Check(VillageSceneFlow.IsVillageScene && !VillageSceneFlow.IsLoading, "formal MainScene ready");
        Check(InputManager.Instance.ActiveScope == InputManager.BindingScope.Outside, "village outside scope");
        var actions = (InputActionAsset)typeof(InputManager).GetField("_asset", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InputManager.Instance);
        actions.FindAction("Player/VillageMap").ApplyBindingOverride(0, "<Keyboard>/q");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(!(Top is WorldMapPanel), "outside remap leaves run M inactive in village");
        Key(UnityEngine.InputSystem.Key.Q); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(Top is WorldMapPanel, "outside uses independent Q override");
        Key(UnityEngine.InputSystem.Key.Q); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(!(Top is WorldMapPanel), "outside Q closes own world map");
        InputManager.Instance.LoadOverridesJson(""); yield return null;
        actions.FindAction("Player/Menu").ApplyBindingOverride(0, "<Keyboard>/f2");
        Key(UnityEngine.InputSystem.Key.F2); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(Top is SettingPanel, "rebound global menu opens village settings");
        Key(UnityEngine.InputSystem.Key.F2); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(!(Top is SettingPanel), "rebound global menu returns from village settings");
        InputManager.Instance.LoadOverridesJson(""); yield return null;
        Key(UnityEngine.InputSystem.Key.Tab); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(!(Top is RunMenuPanel), "outside Tab does not open run collection");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
        Check(Top is WorldMapPanel, "outside M opens world map");
        Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.6f);
        Isolation(); foreach (var member in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(member.id, out reason);
        Click("Btn_Depart");
        float deadline = Time.realtimeSinceStartup + 30;
        while ((RunEncounter.CurrentSceneIndex != 1 || !NaturalMapRuntime.IsReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunEncounter.CurrentSceneIndex == 1 && NaturalMapRuntime.IsReady, "formal WorldScene ready");
        Check(InputManager.Instance.ActiveScope == InputManager.BindingScope.InRun, "WorldScene in-run scope");
        yield return new WaitForSecondsRealtime(.3f);
        Check(RunMapExplorationService.Fog != null && RunMapExplorationService.Fog.ExploredCount > 0, "exploration updates with map closed");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
        var panel = Top as RunMenuPanel;
        Check(panel != null && panel.SelectedTab == RunFunctionTab.Map && Time.timeScale == 0, "in-run M opens map and pauses");
        var view = panel.GetComponentInChildren<RunMapView>();
        Check(view != null && !UnityEngine.Object.FindObjectsOfType<WorldMapPanel>().Any(w => w.IsShowing), "real local map only");
        using (var model = new RunMapModel())
        {
            Check(model.Refresh() && model.Terrain.width == 320 && model.Terrain.height == 144, "terrain matches map tile dimensions");
            Check(model.Markers.Count(m => m.Kind == RunMapMarkerKind.Player) == 1
                && model.Markers.Count(m => m.Kind == RunMapMarkerKind.Ally) == 3, "one yellow and three blue squad markers");
            var p = model.Plan; var fog = RunMapExplorationService.Fog;
            int unknown = Enumerable.Range(0, p.Solid.Length).First(i => !fog.IsExplored(i % p.Width, i / p.Width));
            Check(model.Terrain.GetPixels32()[unknown].Equals(new Color32(7, 10, 16, 255)), "unknown terrain is opaque");
            Check(view.WorldToViewport(new Vector2(p.Width, p.Height)).x > view.WorldToViewport(Vector2.zero).x
                && view.WorldToViewport(new Vector2(p.Width, p.Height)).y > view.WorldToViewport(Vector2.zero).y, "map axes increase right and up");
            var playerMarker = model.Markers.Single(m => m.Kind == RunMapMarkerKind.Player);
            var pooled = panel.GetComponentsInChildren<RunMapMarkerView>(true).Single(m => m.MarkerId == playerMarker.Id);
            Check(pooled.transform.Find("Portrait/DiamondFrame").gameObject.activeInHierarchy
                && pooled.transform.Find("Portrait").localPosition.y > 0, "controlled portrait diamond above yellow point");
            Check(pooled.transform.GetSiblingIndex() > panel.GetComponentsInChildren<RunMapMarkerView>()
                .Where(m => m.MarkerId != playerMarker.Id).Select(m => m.transform.GetSiblingIndex()).DefaultIfEmpty(-1).Max(), "controlled yellow point drawn above overlapping allies");
            float markerSize = ((RectTransform)pooled.transform.Find("Portrait")).rect.width;
            Click("ZoomIn"); Check(view.Zoom > 1, "zoom button changes scale");
            Check(((RectTransform)pooled.transform.Find("Portrait")).rect.width == markerSize, "portrait stays constant size under zoom");
            view.ZoomAt(100, Vector2.zero); Check(view.Zoom == 6, "zoom clamps upper limit");
            view.DragBy(new Vector2(99999, -99999));
            Check(view.Pan.magnitude < 10000, "pan clamped to map bounds");
            Click("FitMap"); Check(view.Zoom == 1 && view.Pan == Vector2.zero, "fit resets pan and zoom");
            var canvas = view.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, view.Viewport.TransformPoint(view.Viewport.rect.center)), scrollDelta = Vector2.up };
            ExecuteEvents.Execute(view.Viewport.gameObject, pointer, ExecuteEvents.scrollHandler);
            Check(view.Zoom > 1, "viewport wheel event zooms");
            view.ZoomAt(3, Vector2.zero); var beforePan = view.Pan;
            ExecuteEvents.Execute(view.Viewport.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position += Vector2.right * 80;
            ExecuteEvents.Execute(view.Viewport.gameObject, pointer, ExecuteEvents.dragHandler);
            Check(view.Pan.x > beforePan.x, "viewport pointer drag pans");
            view.Fit();
            Click("MapLegend"); Check(view.LegendVisible && Time.timeScale == 0, "secondary legend opens with pause");
            float zoom = view.Zoom; var pan = view.Pan; view.ZoomAt(6, Vector2.zero); view.DragBy(Vector2.one * 200);
            Check(view.Zoom == zoom && view.Pan == pan, "legend blocks underlying gestures");
            Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
            Check(Top == panel && !view.LegendVisible && Time.timeScale == 0, "M closes legend before collection");
            Click("MapLegend"); Click("CloseMapLegend"); Check(!view.LegendVisible, "legend close button");
            Click("MapLegend"); Key(UnityEngine.InputSystem.Key.Escape); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
            Check(Top == panel && !view.LegendVisible && Time.timeScale == 0, "Esc closes legend before collection");
            Click("Settings"); yield return new WaitForSecondsRealtime(.6f);
            Click("Btn_Keybind"); yield return new WaitForSecondsRealtime(.6f);
            var setting = (SettingPanel)Top;
            typeof(KeybindSettingsPage).GetMethod("OpenBindingsPage", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(setting.GetComponentsInChildren<KeybindSettingsPage>(true).Single(), null);
            yield return null;
            var bindings = setting.GetComponentsInChildren<KeybindBindingsPage>(true).Single();
            Check(bindings.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Scope_")) == 3, "three visible keybind scope sections");
            Check(bindings.GetComponentsInChildren<SettingRow_Keybind>().Any(r => r.settingId == SettingIds.VillageMap)
                && bindings.GetComponentsInChildren<SettingRow_Keybind>().Any(r => r.settingId == SettingIds.Map), "separate map rows exposed");
            Check(!bindings.GetComponentsInChildren<Transform>().Any(t => t.name == "MouseButton"), "single binding button per row");
            ScreenCapture.CaptureScreenshot(Application.dataPath + "/QA/RunMapUI/input-sections.png");
            yield return new WaitForSecondsRealtime(.3f);
            Key(UnityEngine.InputSystem.Key.Escape); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
            Check(Top == setting && !setting.IsSubPageOpen, "global menu closes settings subpage first");
            Key(UnityEngine.InputSystem.Key.Escape); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
            Check(Top == panel && panel.SelectedTab == RunFunctionTab.Map && Time.timeScale == 0 && !view.LegendVisible, "settings restores map without stale modal");
            int iconsBefore = panel.GetComponentsInChildren<RunMapMarkerView>(true).Length;
            int texturesBefore = Resources.FindObjectsOfTypeAll<Texture2D>().Count(t => t.name == "RunMapTerrain");
            for (int i = 0; i < 20; i++) { panel.SelectTab(RunFunctionTab.Inventory); panel.SelectTab(RunFunctionTab.Map); }
            Check(iconsBefore == panel.GetComponentsInChildren<RunMapMarkerView>(true).Length
                && texturesBefore == Resources.FindObjectsOfTypeAll<Texture2D>().Count(t => t.name == "RunMapTerrain"), "20 page roundtrips reuse icons and terrain textures");
            VerifyMarkers(model);
            view.Fit(); ScreenCapture.CaptureScreenshot(Application.dataPath + "/QA/RunMapUI/map.png");
            yield return new WaitForSecondsRealtime(.3f);
            int explored = RunMapExplorationService.Fog.ExploredCount;
            Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
            Check(!(Top is RunMenuPanel) && Time.timeScale == 1, "M closes map and resumes world");
            Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
            Check(RunMapExplorationService.Fog.ExploredCount >= explored, "close reopen retains exploration");
            Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.6f); Key(); yield return null;
        }
        var oldFog = RunMapExplorationService.Fog;
        for (int stage = 1; stage <= 3; stage++)
        {
            Check(RunEncounter.CurrentSceneIndex == stage, "stage index " + stage);
            RunEncounter.KillAllEnemiesInScene(); RunEncounter.RefreshNow();
            if (stage < 3)
            {
                RunEncounter.ProcessPendingScene();
                deadline = Time.realtimeSinceStartup + 30;
                while ((RunEncounter.CurrentSceneIndex != stage + 1 || !NaturalMapRuntime.IsReady || RunSceneFlow.IsTransitioning
                    || RunMapExplorationService.Fog == null) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(RunMapExplorationService.Fog != null && !ReferenceEquals(oldFog, RunMapExplorationService.Fog), "stage clears fog " + stage);
                oldFog = RunMapExplorationService.Fog;
            }
        }
        yield return new WaitForSecondsRealtime(.3f);
        Check(RunEncounter.RunCleared && Top is RunSettlementPanel, "formal three-map settlement");
        Isolation(); Check(RunSettlementModel.Current.Confirm(true, out reason) && RunSettlementModel.Current.ReturnToMeta(out reason), "isolated settlement and return");
        yield return new WaitForSecondsRealtime(4);
        Check(VillageSceneFlow.IsVillageScene && Time.timeScale == 1 && RunMapExplorationService.Fog == null, "village return clears map exploration");
        Check(InputManager.Instance.ActiveScope == InputManager.BindingScope.Outside, "outside input scope restored");
    }
    private static void VerifyMarkers(RunMapModel model)
    {
        var player = PlayerSpawner.CurrentPlayer; var actors = new List<Transform> { player.transform };
        actors.AddRange(AllyController.All.Where(a => a != null && RunEncounter.OwnsRuntimeObject(a.gameObject)).Select(a => a.transform));
        var positions = actors.Select(t => t.position).ToArray();
        var target = RunInteractable.Active.First(t => t is MapInspectable && ((MapInspectable)t).Kind == MapPlacementKind.Interactive);
        var npc = RunInteractable.Active.First(t => t is MapInspectable && ((MapInspectable)t).Kind == MapPlacementKind.Npc);
        var enemies = new List<EnemyPlaceholder>(); RunEncounter.CopyMapEnemies(enemies); var enemy = enemies.First();
        Vector3 enemyPosition = enemy.transform.position;
        try
        {
            foreach (var actor in actors) actor.position = target.InteractionPoint + Vector2.up;
            enemy.transform.position = target.InteractionPoint + Vector2.up;
            RunMapExplorationService.Tick(true); model.Refresh();
            string targetId = "interactive/" + target.StableId;
            Check(model.Markers.Any(m => m.Id == targetId && m.Kind == RunMapMarkerKind.Interactive), "visible chest diamond marker");
            Check(model.Markers.Any(m => m.Id == "enemy/" + enemy.GetInstanceID()), "enemy appears in shared vision");
            foreach (var actor in actors) actor.position = npc.InteractionPoint + Vector2.up;
            RunMapExplorationService.Tick(true); model.Refresh();
            Check(model.Markers.Any(m => m.Id == "interactive/" + npc.StableId && m.Kind == RunMapMarkerKind.Npc), "visible NPC portrait marker");
            var far = model.Plan.StandingCells.OrderByDescending(c => Vector2.Distance(c, target.InteractionPoint) + Vector2.Distance(c, npc.InteractionPoint)).First();
            foreach (var actor in actors) actor.position = new Vector3(far.x + .5f, far.y + 1, 0);
            RunMapExplorationService.Tick(true); model.Refresh();
            Check(!model.Markers.Any(m => m.Id == "enemy/" + enemy.GetInstanceID()), "enemy hidden outside current vision");
            Check(!model.Markers.Any(m => m.Id == "interactive/" + npc.StableId), "dynamic NPC hidden outside vision");
            Check(model.Markers.Any(m => m.Id == targetId && m.Dimmed), "discovered static chest remembered dimmed");
            var cached = model.Terrain; model.Refresh(); Check(cached == model.Terrain, "terrain texture reused on unchanged map");
        }
        finally
        {
            for (int i = 0; i < actors.Count; i++) actors[i].position = positions[i];
            enemy.transform.position = enemyPosition;
            RunMapExplorationService.Tick(true); model.Refresh();
        }
    }
}
#endif
