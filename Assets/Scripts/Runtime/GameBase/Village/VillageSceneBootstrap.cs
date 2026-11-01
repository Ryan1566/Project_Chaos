using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>MainScene owns the fixed ground and plots; save data controls its runtime occupants and buildings.</summary>
public sealed class VillageSceneBootstrap : MonoBehaviour
{
    public static VillageSceneBootstrap Current { get; private set; }
    public static bool IsReady { get; private set; }
    public static string LastError { get; private set; }
    public VillageSceneSettings Settings { get; private set; }
    public GameObject Avatar { get; private set; }
    public VillagePlot[] Plots { get; private set; }
    public VillageProductionNpc[] Workers { get; private set; }
    public VillageFacility[] Facilities { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Current = null; IsReady = false; LastError = null; }
    private void Start()
    {
        if (!GameSession.Instance.HasSession) { SceneManager.LoadScene("EnterScene"); return; }
        try
        {
            if (Current != null && Current != this) throw new InvalidOperationException("Duplicate village runtime");
            Current = this; IsReady = false; LastError = null; Settings = VillageSceneSettings.Load();
            Plots = GetComponentsInChildren<VillagePlot>().OrderBy(p => p.BuildingId).ToArray();
            var terrain = GetComponentInChildren<Tilemap>();
            ValidateSceneLayout(Settings, terrain, Plots);
            var facilitySettings = VillageFacilitySettings.Load(); facilitySettings.Validate(Settings, Plots);
            Facilities = GetComponentsInChildren<VillageFacility>(true);
            if (Facilities.Length != facilitySettings.facilities.Length
                || Facilities.Select(f => f.Id).Distinct().Count() != Facilities.Length)
                throw new InvalidOperationException("MainScene needs three unique civic facility anchors");
            foreach (var definition in facilitySettings.facilities)
            {
                var facility = Array.Find(Facilities, f => f.Id == definition.id);
                if (facility == null || facility.Kind != definition.kind || !facility.gameObject.activeInHierarchy
                    || Vector3.Distance(facility.transform.position,new Vector3(definition.x,Settings.groundY+.03f,0))>.001f)
                    throw new InvalidOperationException("MainScene facility differs from configuration: "+definition.id);
                facility.Initialize(definition);
            }
            VillageService.Instance.EnsureLoaded();
            for (int i = 0; i < Plots.Length; i++)
                if (Plots[i].BuildingId != 2001u + i || VillageService.Instance.GetDef(Plots[i].BuildingId, 1) == null)
                    throw new InvalidOperationException("Village plot/Building configuration mismatch");
            VillageWorkforce.Clear();
            GameObject appearance;
            Avatar = VillageVisuals.CreateActor(transform, "VillagePlayer", new Vector2(Settings.playerSpawnX, Settings.groundY + .05f), out appearance);
            Avatar.AddComponent<PlayerLocomotion>(); Avatar.SetActive(true);
            Workers = new VillageProductionNpc[Settings.workerCount];
            for (int i = 0; i < Workers.Length; i++)
            {
                uint building = 2001u + (uint)(i % 4);
                var plot = FindPlot(building);
                var actor = VillageVisuals.CreateActor(transform, "ProductionNpc_" + i, plot.Entrance + Vector2.right * (i % 2 == 0 ? -3 : 3), out appearance);
                var npc = actor.AddComponent<VillageProductionNpc>(); npc.Initialize(building, i, Settings, appearance);
                Workers[i] = npc; actor.SetActive(true);
                var shape = actor.GetComponent<Collider2D>(); Physics2D.IgnoreCollision(shape, Avatar.GetComponent<Collider2D>());
                for (int n = 0; n < i; n++) Physics2D.IgnoreCollision(shape, Workers[n].GetComponent<Collider2D>());
            }
            if (UnityEngine.Object.FindObjectsOfType<EventSystem>().Length == 0)
            {
                var events = new GameObject("VillageEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            // Reuse the project's persistent world/UI camera stack. The saved scene camera is an editor preview.
            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            var camera = cameras.FirstOrDefault(c => c.CompareTag("MainCamera") && c.gameObject.scene.name == "DontDestroyOnLoad") ?? Camera.main;
            if (camera == null) throw new InvalidOperationException("MainScene camera missing");
            foreach (var other in cameras)
                if (other != camera && other.CompareTag("MainCamera"))
                { other.enabled = false; var listener = other.GetComponent<AudioListener>(); if (listener != null) listener.enabled = false; }
            // Lens/projection are owned by GameplayCameraSettings and Cinemachine.
            camera.backgroundColor = new Color(.48f, .65f, .8f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var follow = camera.GetComponent<VillageCamera>(); if (follow == null) follow = camera.gameObject.AddComponent<VillageCamera>();
            follow.enabled = true; follow.Initialize(Avatar.transform, Settings);
            gameObject.AddComponent<VillageHud>(); Physics2D.SyncTransforms(); IsReady = true;
            var request = SceneTransitionCoordinator.Active;
            if (request != null && request.Target == VillageSceneFlow.SceneName)
                SceneTransitionCoordinator.ReportReady(request.Id, gameObject.scene);
        }
        catch (Exception error)
        {
            LastError = error.Message; IsReady = false;
            Debug.LogError("[Village] " + error);
            VillageSceneFlow.ReportEntryFailure(error.Message);
        }
    }
    /// <summary>Reject stale authored geometry before any actors can become ready.</summary>
    public static void ValidateSceneLayout(VillageSceneSettings settings, Tilemap terrain, VillagePlot[] plots)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Validate();
        if (terrain == null) throw new InvalidOperationException("MainScene village ground is missing");
        var min = new Vector3Int(-settings.widthTiles / 2, Mathf.RoundToInt(settings.groundY) - settings.groundDepth, 0);
        var max = new Vector3Int(settings.widthTiles / 2, Mathf.RoundToInt(settings.groundY), 1);
        var bounds = terrain.cellBounds;
        // Cell bounds alone cannot detect a translated/scaled Grid or Tilemap.
        if (bounds.min != min || bounds.max != max
            || Vector3.Distance(terrain.CellToWorld(min), new Vector3(min.x, min.y, 0)) > .001f
            || Vector3.Distance(terrain.CellToWorld(new Vector3Int(max.x, min.y, 0)), new Vector3(max.x, min.y, 0)) > .001f
            || Vector3.Distance(terrain.CellToWorld(new Vector3Int(min.x, max.y, 0)), new Vector3(min.x, max.y, 0)) > .001f)
            throw new InvalidOperationException("MainScene ground position or bounds differ from VillageScene config; rebuild the fixed scene");
        if (plots == null || plots.Length != 4 || plots.Any(p => p == null)
            || plots.Select(p => p.BuildingId).Distinct().Count() != 4)
            throw new InvalidOperationException("MainScene needs exactly four unique village plots");
        for (int i = 0; i < 4; i++)
        {
            var plot = Array.Find(plots, p => p.BuildingId == 2001u + i);
            var expected = new Vector3(settings.firstPlotX + i * settings.plotSpacing, settings.groundY + .03f, 0);
            if (plot == null || Vector3.Distance(plot.transform.position, expected) > .001f)
                throw new InvalidOperationException("MainScene plot position differs from VillageScene config; rebuild the fixed scene");
        }
    }
    public static VillagePlot FindPlot(uint id)
    { return Current == null || Current.Plots == null ? null : Array.Find(Current.Plots, p => p.BuildingId == id); }
    public static string BindingText(string action)
    {
        var device = (InputDeviceType)SettingsManager.Instance.Applied.inputDevice;
        return InputManager.Instance.GetBindingDisplay(action, InputManager.KindOf(device), device);
    }
    private void Update()
    {
        if (!IsReady || Avatar == null) return;
        if (Avatar.transform.position.y < Settings.groundY - Settings.groundDepth)
        {
            Vector3 before = Avatar.transform.position;
            Avatar.transform.position = new Vector2(Settings.playerSpawnX, Settings.groundY + .05f);
            GameplayCameraRig.NotifyTargetWarped(Avatar.transform, Avatar.transform.position - before);
            var body = Avatar.GetComponent<Rigidbody2D>(); body.position = Avatar.transform.position; body.velocity = Vector2.zero;
        }
    }
    private void OnDestroy()
    {
        if (Current != this) return;
        var camera = Camera.main; var follow = camera == null ? null : camera.GetComponent<VillageCamera>(); if (follow != null) follow.enabled = false;
        Current = null; IsReady = false; VillageWorkforce.Clear();
    }
}
