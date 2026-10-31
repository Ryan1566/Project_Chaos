#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.Gameplay.Items;
using Chaos.MVP.Model.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Explicit integration check through the formal village/run flow and owned temporary saves.</summary>
public static class GameplayPolishFlowVerification
{
    public static string Status { get; private set; } = "idle";
    public static bool Resume;
    static Keyboard _keyboard; static Mouse _mouse;
    static string _overrides, _storage; static int _mode;
    static readonly StringBuilder Report = new StringBuilder();
    static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    static RunItemWheelDriver Wheel => UnityEngine.Object.FindObjectOfType<RunItemWheelDriver>();
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/GameplayPolish"); File.WriteAllText(Application.dataPath + "/QA/GameplayPolish/flow.txt", Report.ToString()); }
    static void Key(params Key[] keys) => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
    static IEnumerator Press(Key key)
    { Key(key); yield return new WaitForSecondsRealtime(.2f); Key(); yield return new WaitForSecondsRealtime(.3f); }
    static void Click(string name)
    {
        var b = Top.GetComponentsInChildren<Button>(false).Single(x => x.name == name && x.interactable);
        ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    static void Equip(uint id, RunBagSection row, int slot)
    {
        for (int r = 0; r < 4; r++) for (int i = 0; i < RunInventory.GetCapacity((RunBagSection)r); i++)
            if (RunInventory.GetSlot((RunBagSection)r, i)?.itemId == id)
            { Check(RunInventory.TryMove((RunBagSection)r, i, row, slot, out _), "equip " + id + " slot " + slot); return; }
        throw new Exception("missing item " + id);
    }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running" || Status == "visual") throw new Exception("Requires idle Play mode");
        Status = "running"; Resume = false; Report.Clear();
        _storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storage); SaveSlotService.UseEditorTestDirectory(_storage);
        _overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        _mode = SettingsManager.Instance.Applied.itemWheelMode;
        _keyboard = InputSystem.AddDevice<Keyboard>("ChaosPolishKeyboard"); _mouse = InputSystem.AddDevice<Mouse>("ChaosPolishMouse");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        // Flatten nested routines so every assertion is covered by the same cleanup/error handler.
        var stack = new Stack<IEnumerator>(); stack.Push(run);
        while (stack.Count > 0)
        {
            object value = null; bool more;
            try { more = stack.Peek().MoveNext(); if (more) value = stack.Peek().Current; }
            catch (Exception e) { Status = "failed"; Report.AppendLine("FAIL " + e); Cleanup(); Flush(); yield break; }
            if (!more) { stack.Pop(); continue; }
            if (value is IEnumerator child) { stack.Push(child); continue; }
            yield return value;
        }
        Status = "passed"; Cleanup(); Report.AppendLine("DONE " + _storage); Flush();
    }
    static void Cleanup()
    {
        Wheel?.Close(); InputManager.Instance.LoadOverridesJson(_overrides); SettingsManager.Instance.Applied.itemWheelMode = _mode;
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
    }
    static IEnumerator Run()
    {
        int count = UIManager.Instance.panelStack.Count;
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(Top is MainMenuPanel && UIManager.Instance.panelStack.Count == count, "root main menu survives Esc");
        UIManager.Instance.PushPanel(PanelType.SLPanel); yield return new WaitForSecondsRealtime(.4f);
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(Top is MainMenuPanel, "Esc returns from save slots without starting or deleting a save");
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, payload), "owned isolated slot saved");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        float deadline = Time.realtimeSinceStartup + 40;
        while (!VillageSceneBootstrap.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && SaveSlotService.SavesDirectory == _storage, "formal village ready with isolated save");
        yield return Press(UnityEngine.InputSystem.Key.M); Check(Top is WorldMapPanel, "village M opens world map");
        var labels = Top.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false).Where(t => t.transform.parent.name == "FactionNames").ToArray();
        Check(labels.Length == WorldFactionLabelCatalog.Read(1, WorldMapService.Instance.Areas(1)).Count && labels.All(t => !t.raycastTarget), "runtime country labels rendered without blocking clicks");
        Status = "visual"; Flush(); while (!Resume) yield return null; Status = "running";
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(!(Top is WorldMapPanel) && InputManager.Instance.CanReadGameplayInput, "Esc closes village world map and restores gameplay");
        yield return Press(UnityEngine.InputSystem.Key.M);
        foreach (PanelType type in new[] { PanelType.RetainerPanel, PanelType.InventoryPanel, PanelType.MarketPanel, PanelType.VillagePanel, PanelType.FamilyTreePanel, PanelType.EraProgressPanel })
        {
            var parent = Top; UIManager.Instance.PushPanel(type); yield return new WaitForSecondsRealtime(.4f);
            Check(Top != parent, "open meta panel " + type); yield return Press(UnityEngine.InputSystem.Key.Escape);
            Check(Top == parent, "Esc returns one level from " + type);
        }
        UIManager.Instance.PushPanel(PanelType.SettingPanel); yield return new WaitForSecondsRealtime(.4f);
        var settings = (SettingPanel)Top;
        Click("Btn_Keybind");
        settings.GetComponentsInChildren<SettingRow_Button>(true).Single(r => r.settingId == SettingIds.OpenBindings).GetComponentInChildren<Button>().onClick.Invoke();
        yield return null; Check(settings.IsSubPageOpen, "key bindings subpage open");
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(Top == settings && !settings.IsSubPageOpen, "Esc closes settings subpage first");
        SettingsManager.Instance.Pending.itemWheelMode = 1 - SettingsManager.Instance.Applied.itemWheelMode; SettingsManager.Instance.NotifyPendingChanged();
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        var confirm = settings.transform.Find("ConfirmGroup").gameObject;
        Check(Top == settings && confirm.activeSelf, "Esc asks about unapplied changes");
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(Top == settings && !confirm.activeSelf && SettingsManager.Instance.HasPendingChanges, "Esc cancels settings confirmation without losing draft");
        yield return Press(UnityEngine.InputSystem.Key.Escape); Click("ConfirmDiscardBtn"); yield return new WaitForSecondsRealtime(.4f);
        Check(Top is WorldMapPanel, "discard returns to parent world map");
        Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.4f); string reason;
        foreach (var member in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(member.id, out reason);
        Click("Btn_Depart"); deadline = Time.realtimeSinceStartup + 40;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunEncounter.IsSceneReady && RunItemUseService.IsReady, "formal run ready"); RunItemUseService.Actor.invulnSeconds = 100000;
        yield return GameplayWallPlayVerification.Run(Check);
        yield return Press(UnityEngine.InputSystem.Key.M); Check(Top is RunMenuPanel, "run M opens collection map");
        var menu = (RunMenuPanel)Top; Click("MapLegend"); yield return null;
        var map = menu.GetComponentInChildren<RunMapView>(true); Check(map.LegendVisible, "map legend open");
        yield return Press(UnityEngine.InputSystem.Key.Escape); Check(Top == menu && !map.LegendVisible, "Esc closes legend first");
        Click("Retreat"); yield return null; yield return Press(UnityEngine.InputSystem.Key.Escape);
        Check(Top == menu && !menu.GetComponentsInChildren<Button>(false).Any(b => b.name == "CancelRetreat"), "Esc cancels retreat without closing collection");
        yield return Press(UnityEngine.InputSystem.Key.Escape); Check(!InputManager.Instance.GameplayInputBlocked, "Esc closes collection and releases input");
        Check(RunInventory.GrantSlots(RunBagSection.Recovery, 3, "polish-test", out reason), "grow dedicated recovery row");
        Check(RunInventory.AddItem(1, 4, out reason) && RunInventory.AddItem(5, 2, out reason), "grant occupied stacks");
        Equip(1, RunBagSection.Recovery, 0); Equip(5, RunBagSection.Recovery, 4);
        var options = new InventoryRadialProvider().BuildOptions();
        Check(!options[1].Enabled && options[1].Children.Count == 0, "empty engineering category cannot expand");
        Check(options[2].Children.Count == 2 && options[2].Children[1].ShortcutNumber == 5 && !options[2].Children[0].Enabled, "occupied-only recovery slots preserve 1 and 5 including full-health disabled item");
        SettingsManager.Instance.Applied.itemWheelMode = 0;
        yield return Press(UnityEngine.InputSystem.Key.LeftAlt); Check(Wheel.IsOpen, "toggle radial opens");
        InputSystem.QueueDeltaStateEvent(_mouse.delta, new Vector2(-150, -90)); yield return new WaitForSecondsRealtime(.4f);
        var presenter = (Chaos.MVP.Presenter.Gameplay.RadialMenuPresenter)typeof(RunItemWheelDriver).GetField("_presenter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Wheel);
        Check(presenter.Model.Level == 2 && presenter.Model.CurrentOptions.Count == 2, "actual radial expands only two occupied sectors");
        yield return Press(UnityEngine.InputSystem.Key.Digit5); Check(presenter.Model.SelectedIndex == 1, "digit 5 selects original physical slot 4");
        yield return Press(UnityEngine.InputSystem.Key.Escape); Check(Wheel.IsOpen && presenter.Model.Level == 1 && presenter.Model.SelectedIndex == -1, "Esc returns secondary wheel to neutral primary");
        yield return Press(UnityEngine.InputSystem.Key.Escape); Check(!Wheel.IsOpen && !InputManager.Instance.GameplayInputBlocked, "Esc closes primary wheel without reopening collection");
        var enemies = new List<EnemyPlaceholder>();
        for (int stage = 1; stage <= 3; stage++)
        {
            RunEncounter.CopyMapEnemies(enemies); foreach (var enemy in enemies) if (enemy != null && !enemy.IsDead) enemy.TakeDamage(99999);
            RunEncounter.RefreshNow(); deadline = Time.realtimeSinceStartup + 40;
            if (stage < 3)
            { while ((RunEncounter.CurrentSceneIndex != stage + 1 || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null; Check(RunEncounter.CurrentSceneIndex == stage + 1 && RunEncounter.IsSceneReady, "map ready " + (stage + 1)); }
            else
            { while (!(Top is RunSettlementPanel) && Time.realtimeSinceStartup < deadline) yield return null; Check(Top is RunSettlementPanel, "three-map settlement reached"); }
        }
        yield return Press(UnityEngine.InputSystem.Key.Escape); deadline = Time.realtimeSinceStartup + 40;
        while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene && SaveSlotService.SavesDirectory == _storage, "settlement Esc confirms through normal flow and returns village");
    }
}
#endif
