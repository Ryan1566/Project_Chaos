#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Independent camera acceptance through owned temporary save, village, dispatch and all three maps.</summary>
public static class CinemachineFlowVerification
{
    public static string Status { get; private set; } = "idle";
    static string _overrides, _storage; static Keyboard _keys;
    static readonly StringBuilder Report = new StringBuilder();
    static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    static Camera Output => Camera.main;
    static CinemachineVirtualCamera Live => Output.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineVirtualCamera;
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception(message); Report.AppendLine("PASS " + message); Flush(); }
    static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/3C"); File.WriteAllText(Application.dataPath + "/QA/3C/formal-flow.txt", Report.ToString()); }
    static IEnumerator Press(Key key)
    { InputSystem.QueueStateEvent(_keys, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.25f); InputSystem.QueueStateEvent(_keys, new KeyboardState()); yield return new WaitForSecondsRealtime(.25f); }
    static void Click(string name)
    {
        var b = Top.GetComponentsInChildren<Button>(false).Single(x => x.name == name && x.interactable);
        ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    static void CheckOutput(string context)
    {
        Check(UnityEngine.Object.FindObjectsOfType<Camera>().Count(c => c.enabled && c.CompareTag("MainCamera")) == 1, context + " one enabled main output");
        Check(UnityEngine.Object.FindObjectsOfType<AudioListener>().Count(a => a.enabled) == 1, context + " one audio listener");
        Check(UnityEngine.Object.FindObjectsOfType<CinemachineBrain>().Count(b => b.isActiveAndEnabled) == 1 && Live != null, context + " one live Brain and virtual camera");
        Check(Live.GetComponent<CinemachineFramingTransposer>() != null || Live.GetCinemachineComponent<CinemachineFramingTransposer>() != null, context + " framing transposer active");
    }
    static int ActiveImpulses()
    {
        var shake = Output.GetComponent<CombatCameraShake>();
        return shake == null ? 0 : ((ICollection)typeof(CombatCameraShake).GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shake)).Count;
    }
    static void CheckMapView(string context)
    {
        Check(RunBounds.TryGetMapBounds(out var b), context + " real map bounds available");
        float hy = Output.orthographicSize, hx = hy * Output.aspect; var p = Output.transform.position;
        Check((b.width < hx * 2 || (p.x >= b.xMin + hx - .05f && p.x <= b.xMax - hx + .05f))
            && (b.height < hy * 2 || (p.y >= b.yMin + hy - .05f && p.y <= b.yMax - hy + .05f)), context + " viewport confined to map");
        var v = Output.WorldToViewportPoint(PlayerSpawner.CurrentPlayer.transform.position);
        Check(v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1, context + " controlled player visible");
    }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new Exception("Requires idle Play mode");
        Status = "running"; Report.Clear();
        _storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storage); SaveSlotService.UseEditorTestDirectory(_storage);
        _overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        _keys = InputSystem.AddDevice<Keyboard>("ChaosCinemachineKeyboard");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        var routines = new Stack<IEnumerator>(); routines.Push(source);
        while (routines.Count > 0)
        {
            object value = null; bool more;
            try { more = routines.Peek().MoveNext(); if (more) value = routines.Peek().Current; }
            catch (Exception error) { while (routines.Count > 0) (routines.Pop() as IDisposable)?.Dispose(); Status = "failed"; Report.AppendLine("FAIL " + error); Cleanup(); Flush(); yield break; }
            if (!more) { routines.Pop(); continue; }
            if (value is IEnumerator nested) { routines.Push(nested); continue; }
            yield return value;
        }
        Status = "passed"; Cleanup(); Report.AppendLine("DONE " + _storage); Flush();
    }
    static void Cleanup()
    { InputManager.Instance.LoadOverridesJson(_overrides); if (_keys != null && _keys.added) InputSystem.RemoveDevice(_keys); }
    static IEnumerator Run()
    {
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        RetainerService.Instance.RecruitRandom();
        var data = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, data), "owned isolated save created");
        GameSession.Instance.Begin(3, data, "MainScene"); VillageSceneFlow.EnterFromSave();
        float deadline = Time.realtimeSinceStartup + 40;
        while (!VillageSceneBootstrap.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady, "formal village ready"); yield return new WaitForSecondsRealtime(.5f); CheckOutput("village");
        Check(Mathf.Abs(Output.orthographicSize - 8) < .01f, "village size remains 8");
        var avatar = VillageSceneBootstrap.Current.Avatar; var body = avatar.GetComponent<Rigidbody2D>();
        body.position = new Vector2(-5, VillageSceneBootstrap.Current.Settings.groundY + .05f); body.velocity = Vector2.zero;
        Physics2D.SyncTransforms(); yield return new WaitForSecondsRealtime(1.5f);
        float x = Output.transform.position.x, y = Output.transform.position.y;
        avatar.GetComponent<PlayerLocomotion>().InjectInput(1, false, 2); yield return new WaitForSecondsRealtime(1.6f);
        avatar.GetComponent<PlayerLocomotion>().InjectInput(0, false, .1f);
        Check(Output.transform.position.x > x + 1 && Mathf.Abs(Output.transform.position.y - y) < .05f, "village follows horizontal motion with stable height");
        yield return Press(UnityEngine.InputSystem.Key.M); Check(Top is WorldMapPanel, "village M world map remains usable");
        Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.3f); string reason;
        foreach (var member in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(member.id, out reason);
        Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation(); deadline = Time.realtimeSinceStartup + 40;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunEncounter.IsSceneReady, "formal expedition ready"); yield return new WaitForSecondsRealtime(.4f);
        CheckOutput("run1"); CheckMapView("run1"); Check(Mathf.Abs(Output.orthographicSize - 6) < .01f, "run size remains 6");
        var player = PlayerSpawner.CurrentPlayer; var health = player.GetComponent<PlayerHealth>(); health.invulnSeconds = .1f;
        Check(Output.GetComponent<CombatCameraShake>()?.enabled == true, "run binds combat feedback");
        Check(health.ApplyContactDamage(1), "real controlled-player damage accepted");
        Check(ActiveImpulses() > 0, "real damage produces owned impulse");
        Check(!health.ApplyContactDamage(1), "invulnerable duplicate damage rejected");
        yield return Press(UnityEngine.InputSystem.Key.I);
        Check(Top is RunMenuPanel && Time.timeScale == 0, "inventory pauses world"); var stopped = Output.transform.position;
        yield return new WaitForSecondsRealtime(.2f);
        Check(ActiveImpulses() == 0 && Vector3.Distance(stopped, Output.transform.position) < .002f, "pause cancels impulse and camera stays still");
        yield return Press(UnityEngine.InputSystem.Key.Escape); health.invulnSeconds = 100000; health.ApplyContactDamage(1);
        var enemies = new List<EnemyPlaceholder>(); object oldScope = null;
        for (int stage = 1; stage <= 3; stage++)
        {
            if (stage == 3) oldScope = typeof(PlayerSpawner).GetField("_spawnScope", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            RunEncounter.CopyMapEnemies(enemies); foreach (var enemy in enemies) if (enemy != null && !enemy.IsDead) enemy.TakeDamage(99999);
            RunEncounter.RefreshNow(); deadline = Time.realtimeSinceStartup + 40;
            if (stage < 3)
            {
                while ((RunEncounter.CurrentSceneIndex != stage + 1 || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(RunEncounter.IsSceneReady && RunEncounter.CurrentSceneIndex == stage + 1, "map ready " + (stage + 1));
                yield return new WaitForSecondsRealtime(.3f); CheckOutput("run" + (stage + 1)); CheckMapView("run" + (stage + 1));
            }
            else { while (!(Top is RunSettlementPanel) && Time.realtimeSinceStartup < deadline) yield return null; Check(Top is RunSettlementPanel, "three-map settlement"); }
        }
        yield return Press(UnityEngine.InputSystem.Key.Escape); deadline = Time.realtimeSinceStartup + 40;
        while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene, "settlement returns physical village");
        yield return new WaitForSecondsRealtime(.4f); CheckOutput("returned village");
        Check(Output.GetComponent<CombatCameraShake>()?.enabled != true && Mathf.Abs(Output.orthographicSize - 8) < .01f, "return restores village framing without combat shake");
        typeof(PlayerSpawner).GetMethod("ClearForScope", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { oldScope, (object)false });
        yield return null; Check(Live != null && Output.GetComponent<CinemachineBrain>().enabled, "stale run cleanup preserves newer village camera");
        Check(SaveSlotService.SavesDirectory == _storage, "all transitions retain isolated storage");
    }
}
#endif
