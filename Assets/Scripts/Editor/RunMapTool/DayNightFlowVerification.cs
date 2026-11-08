#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>只在隔离档内通过实际输入和界面验证昼夜、攻击与地图拖拽。</summary>
public static class DayNightFlowVerification
{
    private static readonly StringBuilder log = new StringBuilder();
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static string bindings, storage;
    private static int attackMode;
    public static string Status { get; private set; }
    public static bool HoldForReview;
    public static bool HoldNightForReview;
    private static BasePanel Top => UIManager.Instance.panelStack.Count > 0 ? UIManager.Instance.panelStack.Peek() : null;
    private static SavePayload Save => GameSession.Instance.LoadedPayload;
    private static InputActionAsset Actions => (InputActionAsset)typeof(InputManager)
        .GetField("_asset", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(InputManager.Instance);

    public static void Start()
    {
        log.Clear();
        Status = "running";
        storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-DayNight-" + Guid.NewGuid().ToString("N"));
        SaveSlotService.UseEditorTestDirectory(storage);
        bindings = InputManager.Instance.SaveOverridesJson();
        InputManager.Instance.LoadOverridesJson("");
        keyboard = InputSystem.AddDevice<Keyboard>("DayNightKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("DayNightMouse");
        attackMode = SettingsManager.Instance.Applied.attackTriggerMode;
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }

    private static IEnumerator Guard(IEnumerator entry)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(entry);
        while (stack.Count > 0)
        {
            object value;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                value = stack.Peek().Current;
            }
            catch (Exception error)
            {
                log.AppendLine("FAIL " + error);
                Finish("failed");
                yield break;
            }
            if (value is IEnumerator nested) stack.Push(nested);
            else yield return value;
        }
        Finish("passed");
    }

    private static void Finish(string status)
    {
        Status = status;
        if (keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse.added) InputSystem.RemoveDevice(mouse);
        InputManager.Instance.LoadOverridesJson(bindings);
        SettingsManager.Instance.Applied.attackTriggerMode = attackMode;
        log.AppendLine("DONE " + storage);
        Flush();
    }

    private static void Flush() => File.WriteAllText(Application.dataPath + "/QA/DayNight/flow.txt", log.ToString());

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        log.AppendLine("PASS " + description);
        Flush();
    }

    private static IEnumerator Until(Func<bool> condition, string description)
    {
        float limit = Time.realtimeSinceStartup + 45;
        while (!condition() && Time.realtimeSinceStartup < limit) yield return null;
        Check(condition(), description);
    }

    private static IEnumerator KeyPress(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return new WaitForSecondsRealtime(.12f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(.16f);
    }

    private static void MouseButton(bool down, Vector2 point)
        => InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = (ushort)(down ? 1 : 0) });

    private static IEnumerator LeftClick(Vector2 point, float duration)
    {
        MouseButton(true, point);
        yield return new WaitForSecondsRealtime(duration);
        MouseButton(false, point);
        yield return new WaitForSecondsRealtime(.15f);
    }

    private static Button ButtonNamed(string name) => Top?.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

    private static void Click(string name)
    {
        var button = ButtonNamed(name);
        Check(button != null && button.gameObject.activeInHierarchy && button.interactable, "clickable " + name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }

    private static void CheckWheelPointerTargets()
    {
        Canvas.ForceUpdateCanvases();
        var camp = (RunCampPanel)Top;
        var canvas = camp.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var buttons = camp.GetComponentsInChildren<Button>(false)
            .Where(button => button.name.StartsWith("Btn_TimeHour_")
                || button.name == "Btn_TimeConfirm" || button.name == "Btn_TimeCancel");
        var hits = new List<RaycastResult>();
        foreach (var button in buttons)
        {
            var rect = (RectTransform)(button.GetComponentInChildren<TextMeshProUGUI>().transform);
            Vector2 position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            if (position.x <= 0 || position.x >= Screen.width || position.y <= 0 || position.y >= Screen.height
                || hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                throw new InvalidOperationException("wheel pointer cannot hit " + button.name + " at " + position);
        }
        Check(buttons.Count() == 26, "all 24 sectors and both wheel buttons are visible and receive pointer hits");
    }

    private static void CheckClock()
    {
        Check(DayNightClock.Evaluate(240, false).Phase == DayNightPhase.Dawn
            && DayNightClock.Evaluate(360, false).Phase == DayNightPhase.Day, "village dawn spans two real minutes");
        Check(DayNightClock.Evaluate(1080, false).Phase == DayNightPhase.Dusk
            && DayNightClock.Evaluate(1200, false).Phase == DayNightPhase.Night, "village dusk spans two real minutes");
        Check(DayNightClock.Evaluate(315, true).Phase == DayNightPhase.Dawn
            && DayNightClock.Evaluate(405, true).Phase == DayNightPhase.Day, "run dawn spans ninety real seconds");
        Check(DayNightClock.Evaluate(1035, true).Phase == DayNightPhase.Dusk
            && DayNightClock.Evaluate(1125, true).Phase == DayNightPhase.Night, "run dusk spans ninety real seconds");
        Check(Mathf.Approximately(DayNightClock.Evaluate(1441, false).Daylight, DayNightClock.Evaluate(1, false).Daylight),
            "sky sampling wraps across midnight");
    }

    private static void CheckMap()
    {
        var map = (WorldMapPanel)Top;
        var navigation = map.GetComponentInChildren<WorldMapNavigation>();
        var viewport = (RectTransform)navigation.transform;
        var content = (RectTransform)viewport.Find("MapContent");
        var canvas = viewport.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var point = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(viewport.rect.center));
        navigation.SetZoom(100, point);
        Check(Mathf.Approximately(navigation.Zoom, 6), "world map clamps to six times zoom");
        var pointer = new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left, position = point, eligibleForClick = true };
        var start = RectTransformUtility.WorldToScreenPoint(camera, content.position);
        navigation.OnBeginDrag(pointer);
        pointer.position += Vector2.right * 40;
        pointer.delta = Vector2.right * 40;
        navigation.OnDrag(pointer);
        Check(Mathf.Abs(RectTransformUtility.WorldToScreenPoint(camera, content.position).x - start.x - 26) < .5f
            && !pointer.eligibleForClick, "drag maps forty pixels to twenty-six and suppresses click");
        pointer.delta = Vector2.right * 100000;
        navigation.OnDrag(pointer);
        Check(content.anchoredPosition.x <= (content.rect.width * 6 - viewport.rect.width) * .5f + .01f,
            "map remains bounded at six times zoom");
        navigation.OnEndDrag(pointer);
    }

    private static IEnumerator Run()
    {
        CheckClock();
        GameSession.Instance.BeginSandbox(new SavePayload());
        Check(DayNightClock.VillageMinutes == 480, "legacy save defaults to eight in the morning");
        RetainerService.Instance.RecruitRandom();
        var fixture = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(Save));
        fixture.hasVillageDayTime = true;
        fixture.villageDayMinutes = 1439.9f;
        Check(SaveSlotService.Instance.Save(3, fixture) && SaveSlotService.Instance.TryLoad(3, out _), "isolated save round-trip");
        GameSession.Instance.Begin(3, fixture, "MainScene");
        VillageSceneFlow.EnterFromSave();
        yield return Until(() => VillageSceneBootstrap.IsReady, "village ready");
        yield return Until(() => DayNightClock.VillageMinutes < 10, "village clock naturally crosses midnight");
        CheckSky();
        UIManager.Instance.PushPanel(PanelType.PausePanel);
        float pausedVillage = DayNightClock.VillageMinutes;
        yield return new WaitForSecondsRealtime(.3f);
        Check(DayNightClock.VillageMinutes == pausedVillage, "pause menu freezes the village clock");
        UIManager.Instance.PopPanel();
        float villageTime = DayNightClock.VillageMinutes;
        var dispatch = DispatchService.Instance;
        Check(dispatch.Toggle(dispatch.GetCandidates().First().id, out _), "prepare formal team");
        yield return KeyPress(Key.M);
        Check(Top is WorldMapPanel, "formal M map opened");
        CheckMap();
        Click("EnterRunBtn");
        yield return null;
        Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();
        yield return Until(() => RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning, "formal run ready");
        Check(DayNightClock.RunMinutes >= 480 && DayNightClock.RunMinutes < 482, "new run starts at eight");
        CheckSky();
        villageTime = DayNightClock.VillageMinutes;
        yield return Combat();
        Check(DayNightClock.VillageMinutes == villageTime, "village clock remains stopped while dispatched");

        RunEncounter.KillAllEnemiesInScene();
        Check(CampService.CanSetTime(out _), "cleared camp area enables clock selection");
        RunCampPanel.Open();
        yield return Until(() => Top is RunCampPanel && !Top.IsAnimating, "camp finishes its enter animation");
        Click("Btn_OpenTimeWheel");
        yield return null;
        Check(Top.GetComponentsInChildren<Button>(false).Count(b => b.name.StartsWith("Btn_TimeHour_")) == 24, "time wheel has 24 live buttons");
        CheckWheelPointerTargets();
        float initial = DayNightClock.RunMinutes;
        Click("Btn_TimeHour_23");
        Check(DayNightClock.RunMinutes == initial, "wheel preview does not change clock");
        yield return KeyPress(Key.Escape);
        Check(Top is RunCampPanel && !ButtonNamed("Btn_TimeConfirm").gameObject.activeInHierarchy
            && DayNightClock.RunMinutes == initial, "Escape returns to camp without changing time");
        Click("Btn_OpenTimeWheel");
        Click("Btn_TimeHour_12");
        Click("Btn_TimeCancel");
        Check(DayNightClock.RunMinutes == initial && !ButtonNamed("Btn_TimeConfirm").gameObject.activeInHierarchy,
            "cancel returns to camp without changing time");
        Click("Btn_OpenTimeWheel");
        Click("Btn_TimeHour_00");
        float playSeconds = RunEncounter.RunPlaySeconds;
        int medicine = CampService.MedicineCount;
        bool campUsed = CampService.CampUsed;
        double officeTime = Save.officeClockSeconds;
        Click("Btn_TimeConfirm");
        Check(DayNightClock.RunMinutes == 0 && CampService.CampUsed == campUsed && CampService.MedicineCount == medicine
            && RunEncounter.RunPlaySeconds == playSeconds && Save.officeClockSeconds == officeTime,
            "confirmed midnight changes only the run clock");
        Click("Btn_OpenTimeWheel");
        Status = "wheel";
        while (HoldForReview) yield return null;
        yield return KeyPress(Key.Escape);
        yield return KeyPress(Key.Escape);
        Check((Top == null || !Top.gameObject.activeInHierarchy)
            && InputManager.Instance.CanReadGameplayInput && PlayerSpawner.CurrentPlayer.GetComponent<PlayerAttack>().enabled,
            "leaving camp returns player attack control");
        Status = "night";
        while (HoldNightForReview) yield return null;
        float beforeTransition = DayNightClock.RunMinutes;
        Check(RunEncounter.TryUseExit(1), "request formal stage transition");
        yield return Until(() => RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning && RunEncounter.CurrentSceneIndex == 2, "second stage ready");
        Check(Mathf.Abs(DayNightClock.RunMinutes - beforeTransition) < 2, "night clock survives the stage change");
        Check(CountFloaters() == 0, "stage transition clears the previous damage numbers");
        DamageFloaters.Show(PlayerSpawner.CurrentPlayer.transform, 7, true);
        Check(CountFloaters() == 1, "damage pool can draw again after the stage changes");
        beforeTransition = DayNightClock.RunMinutes;
        Check(RunEncounter.TryUseExit(2), "request formal third stage transition");
        yield return Until(() => RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning && RunEncounter.CurrentSceneIndex == 3, "third stage ready");
        Check(Mathf.Abs(DayNightClock.RunMinutes - beforeTransition) < 2, "night clock survives the third stage transition");
        Check(dispatch.ReturnToMeta(out _), "request village return");
        yield return Until(() => VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene, "return to village");
        Check(DayNightClock.VillageMinutes >= villageTime && DayNightClock.VillageMinutes < villageTime + 2
            && DayNightClock.RunMinutes == DayNightClock.StartMinutes, "village clock resumes and run clock clears");
        Check(SaveSlotService.Instance.TryLoad(3, out var loaded) && loaded.payload.hasVillageDayTime,
            "village clock persisted in formal save");
    }

    private static void CheckSky()
    {
        var camera = Camera.main;
        Check(camera.GetComponent<DayNightSkyRenderer>() != null
            && camera.GetCommandBuffers(CameraEvent.BeforeForwardOpaque).Any(b => b.name.Contains("天空"))
            && camera.GetCommandBuffers(CameraEvent.AfterForwardAlpha).Any(b => b.name.Contains("调色")),
            "active camera draws sky and a separate world-only tint");
    }

    private static IEnumerator Combat()
    {
        var player = PlayerSpawner.CurrentPlayer;
        var attack = player.GetComponent<PlayerAttack>();
        attack.damage = 1;
        attack.attackCooldown = .12f;
        player.GetComponent<PlayerLocomotion>().SetFacing(1);
        player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var enemies = new List<EnemyPlaceholder>();
        RunEncounter.CopyMapEnemies(enemies);
        var enemy = enemies.First();
        enemy.enabled = false;
        var enemyBody = enemy.GetComponent<Rigidbody2D>();
        enemyBody.bodyType = RigidbodyType2D.Kinematic;
        enemyBody.constraints = RigidbodyConstraints2D.FreezeAll;
        enemy.transform.position += (Vector3)(CombatQuery.Center(player.transform) + Vector2.right * .50f - CombatQuery.Center(enemy.transform));
        Physics2D.SyncTransforms();
        Check(!CampService.TrySetTime(20, out _), "unsafe room cannot change the clock");
        var center = new Vector2(Screen.width * .55f, Screen.height * .55f);
        var action = Actions.FindAction("Player/Attack");
        Check(action.bindings[0].effectivePath == "<Mouse>/leftButton", "default attack resolves to mouse left");
        SettingsManager.Instance.Applied.attackTriggerMode = (int)MouseTriggerMode.Press;
        int hp = enemy.Hp;
        yield return LeftClick(center, .35f);
        Check(enemy.Hp == hp - 1 && attack.LastHitCount == 1, "actual left click deals one hit in press mode");
        Check(UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Any(t => t.name == "DamageNumber" && t.text == "1"),
            "visible damage number uses actual HP loss");
        SettingsManager.Instance.Applied.attackTriggerMode = (int)MouseTriggerMode.Hold;
        hp = enemy.Hp;
        yield return LeftClick(center, .42f);
        Check(enemy.Hp <= hp - 2, "holding left repeats after cooldown");
        hp = enemy.Hp;
        RunCampPanel.Open();
        yield return null;
        MouseButton(true, center);
        yield return new WaitForSecondsRealtime(.25f);
        UIManager.Instance.PopPanel();
        yield return new WaitForSecondsRealtime(.25f);
        Check(enemy.Hp == hp, "modal blocks attack and held mouse does not leak after closing");
        MouseButton(false, center);
        yield return new WaitForSecondsRealtime(.12f);
        action.ApplyBindingOverride(0, "<Keyboard>/j");
        yield return LeftClick(center, .2f);
        Check(enemy.Hp == hp, "custom attack override disables default mouse");
        yield return KeyPress(Key.J);
        Check(enemy.Hp == hp - 1, "custom keyboard attack damages the same target");
        InputManager.Instance.LoadOverridesJson("");
        hp = enemy.Hp;
        enemy.TakeRunDamage(hp + 1000);
        Check(UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Any(t => t.name == "DamageNumber" && t.text == hp.ToString()),
            "overkill displays only the remaining HP");
        var health = player.GetComponent<PlayerHealth>();
        int beforeCount = CountFloaters();
        Check(health.ApplyContactDamage(4) && !health.ApplyContactDamage(4) && CountFloaters() == beforeCount + 1,
            "an invulnerable hit produces no damage number");
        for (int i = 0; i < 56; i++) DamageFloaters.Show(player.transform, i + 100, true);
        Check(CountFloaters() == 48, "damage number pool is capped at forty-eight");
    }

    private static int CountFloaters() => UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Count(t => t.name == "DamageNumber");
}
#endif
