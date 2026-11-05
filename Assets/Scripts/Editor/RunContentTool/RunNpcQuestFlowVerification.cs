#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Chaos.Gameplay.Interaction;
using RunMap;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>隔离存档的委托定向验收；辅助接近和关闭敌人AI不作为自然游玩证据。</summary>
public static class RunNpcQuestFlowVerification
{
    static readonly StringBuilder log = new StringBuilder();
    static Keyboard keyboard;
    static string overrides;
    public static string Status { get; private set; } = "idle";
    public static string Storage { get; private set; }
    static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();

    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new InvalidOperationException("需要空闲Play环境");
        log.Clear();
        Storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-NpcQuest-" + Guid.NewGuid().ToString("N"));
        SaveSlotService.UseEditorTestDirectory(Storage);
        overrides = InputManager.Instance.SaveOverridesJson();
        InputManager.Instance.LoadOverridesJson("");
        keyboard = InputSystem.AddDevice<Keyboard>("NpcQuestKeyboard");
        Run(Setup());
    }

    static void Run(IEnumerator routine)
    {
        Status = "running";
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(routine));
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        while (stack.Count > 0)
        {
            bool more; object current = null;
            try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
            catch (Exception e) { Status = "failed"; log.AppendLine("FAIL " + e); Flush(); yield break; }
            if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) stack.Push(nested); else yield return current;
        }
        Status = "ready";
    }
    static IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return new WaitForSecondsRealtime(.18f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(.2f);
    }
    public static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        log.AppendLine("PASS " + label); Flush();
    }
    static void Flush()
    {
        Directory.CreateDirectory(Application.dataPath + "/QA/RunNpcQuest");
        File.WriteAllText(Application.dataPath + "/QA/RunNpcQuest/flow.txt", log.ToString());
    }
    public static void Click(string objectName)
    {
        var button = Top.GetComponentsInChildren<Button>(false).Single(x => x.name == objectName && x.interactable);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
    public static string[] Texts() => Top.GetComponentsInChildren<TextMeshProUGUI>(false).Select(x => x.text).ToArray();
    public static RunContentInteractable Content(string id) => RunInteractable.Active.OfType<RunContentInteractable>().First(x => x.Definition.id == id);

    static IEnumerator Setup()
    {
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000 });
        RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear();
        Check(SaveSlotService.Instance.Save(3, payload), "创建独立临时槽3");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        float end = Time.realtimeSinceStartup + 45;
        while (!VillageSceneBootstrap.IsReady && Time.realtimeSinceStartup < end) yield return null;
        Check(VillageSceneBootstrap.IsReady, "村庄正式入口Ready");
        yield return Press(Key.M); Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.3f);
        foreach (var member in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(member.id, out _);
        end = Time.realtimeSinceStartup + 5;
        while (!Top.GetComponentsInChildren<Button>(false).Any(b => b.name == "Btn_Depart" && b.interactable)
            && Time.realtimeSinceStartup < end) yield return null;
        Click("Btn_Depart");
        yield return WaitForMap(1);
        Check(Content("traveller").GetComponentInChildren<Spine.Unity.SkeletonAnimation>() != null, "医者仍使用Spine占位");
    }
    static IEnumerator WaitForMap(int stage)
    {
        float end = Time.realtimeSinceStartup + 50;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning || RunEncounter.CurrentSceneIndex != stage) && Time.realtimeSinceStartup < end) yield return null;
        Check(RunEncounter.IsSceneReady && RunEncounter.CurrentSceneIndex == stage && !RunSceneFlow.IsTransitioning, "正式派遣地图Ready " + stage);
        var enemies = new List<EnemyPlaceholder>(); RunEncounter.CopyMapEnemies(enemies);
        foreach (var enemy in enemies) enemy.enabled = false;
    }
    public static void Open(string id) { Run(OpenRoutine(Content(id))); }
    static IEnumerator OpenRoutine(RunContentInteractable target)
    {
        var player = PlayerSpawner.CurrentPlayer; var shape = player.GetComponent<Collider2D>(); var body = player.GetComponent<Rigidbody2D>();
        Vector2 feet = NaturalMapRuntime.Current.Placements.First(p => p.StableId == target.StableId).Position;
        bool moved = false;
        foreach (float offset in new[] { .8f, -.8f, 0f, 1.3f, -1.3f })
        {
            var position = feet + Vector2.right * offset;
            if (!RunMapPhysics.CanOccupy(position, shape.bounds.size)) continue;
            var previous = body.position; body.position = position + ((Vector2)player.transform.position - RunMapPhysics.Feet(shape)); body.velocity = Vector2.zero;
            player.GetComponent<PlayerLocomotion>().InjectInput(0, false, 2);
            Physics2D.SyncTransforms(); GameplayCameraRig.NotifyTargetWarped(player.transform, body.position - previous); moved = true; break;
        }
        Check(moved, "辅助安全接近 " + target.Definition.id);
        yield return new WaitForSeconds(.2f); yield return Press(Key.E);
        Check(Top is RunContentPanel && RunContentService.Current?.Target == target && Time.timeScale == 0, "实际E打开交互面板 " + target.Definition.id);
    }
    public static void Close() { Run(CloseRoutine()); }
    static IEnumerator CloseRoutine()
    {
        yield return Press(Key.Escape);
        Check(!(Top is RunContentPanel) && Time.timeScale == 1, "Esc返回游玩并恢复时间");
    }
    public static void NextMap() { Run(NextRoutine()); }
    static IEnumerator NextRoutine()
    {
        int next = RunEncounter.CurrentSceneIndex + 1;
        Check(RunEncounter.TryUseExit(RunEncounter.CurrentSceneIndex), "测试入口请求正式切图");
        yield return WaitForMap(next);
    }
    public static void Finish()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        InputManager.Instance.LoadOverridesJson(overrides);
        Status = "finished"; log.AppendLine("DONE isolated=" + Storage); Flush();
    }
}
#endif
