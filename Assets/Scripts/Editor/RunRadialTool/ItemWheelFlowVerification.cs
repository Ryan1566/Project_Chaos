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

/// <summary>Explicit formal-flow integration checks, exclusively in an owned temporary save directory.</summary>
public static class ItemWheelFlowVerification
{
    public static string Status { get; private set; } = "idle";
    public static bool Resume;
    static Keyboard _keys; static Mouse _mouse;
    static string _storage, _overrides;
    static int _mode;
    static readonly StringBuilder Report = new StringBuilder();
    static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    static RunItemWheelDriver Wheel => UnityEngine.Object.FindObjectOfType<RunItemWheelDriver>();
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    static void Flush() { Directory.CreateDirectory(Application.dataPath + "/QA/ItemWheel"); File.WriteAllText(Application.dataPath + "/QA/ItemWheel/flow.txt", Report.ToString()); }
    static void Key(params Key[] keys) => InputSystem.QueueStateEvent(_keys, new KeyboardState(keys));
    static void MouseButton(bool down) => InputSystem.QueueStateEvent(_mouse, new MouseState { buttons = (ushort)(down ? 1 : 0) });
    static void Click(string name)
    {
        var button = Top.GetComponentsInChildren<Button>(false).Single(b => b.name == name && b.interactable);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    static void Equip(uint id, RunBagSection row, int slot)
    {
        for (int r = 0; r < 4; r++) for (int i = 0; i < RunInventory.GetCapacity((RunBagSection)r); i++)
            if (RunInventory.GetSlot((RunBagSection)r, i)?.itemId == id)
            { Check(RunInventory.TryMove((RunBagSection)r, i, row, slot, out _), "equip " + id); return; }
        throw new Exception("missing item " + id);
    }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running" || Status == "visual") throw new Exception("Requires idle Play mode");
        Report.Clear(); Status = "running"; Resume = false;
        _storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storage); SaveSlotService.UseEditorTestDirectory(_storage);
        _overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        _mode = SettingsManager.Instance.Applied.itemWheelMode;
        _keys = InputSystem.AddDevice<Keyboard>("ChaosItemVerificationKeyboard"); _mouse = InputSystem.AddDevice<Mouse>("ChaosItemVerificationMouse");
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, payload), "isolated slot created");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        while (true)
        {
            object value;
            try { if (!source.MoveNext()) { Status = "passed"; Cleanup(); Report.AppendLine("DONE " + _storage); Flush(); yield break; } value = source.Current; }
            catch (Exception e) { Status = "failed"; Report.AppendLine("FAIL " + e); Cleanup(); Flush(); yield break; }
            yield return value;
        }
    }
    static void Cleanup()
    {
        Wheel?.Close(); InputManager.Instance.LoadOverridesJson(_overrides);
        SettingsManager.Instance.Applied.itemWheelMode = _mode;
        if (_keys != null && _keys.added) InputSystem.RemoveDevice(_keys);
        if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
    }
    static IEnumerator Run()
    {
        string reason;
        float deadline = Time.realtimeSinceStartup + 40;
        while (!VillageSceneBootstrap.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && SaveSlotService.SavesDirectory == _storage, "formal village with isolated storage");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.4f); Key(); yield return null;
        Check(Top is WorldMapPanel, "outside M opens world map"); Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.3f);
        foreach (var member in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(member.id, out reason);
        Click("Btn_Depart"); deadline = Time.realtimeSinceStartup + 40;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunItemUseService.IsReady && RunEncounter.CurrentSceneIndex == 1, "formal loading to WorldScene ready");
        var actor = RunItemUseService.Actor; actor.invulnSeconds = 100000;
        foreach (uint id in new uint[] { 1, 5, 6, 10 }) Check(RunInventory.AddItem(id, 4, out reason), "grant test stack " + id);
        Check(RunInventory.AddItem(11, 1, out reason), "important ownership acquired");
        Check(RunInventory.GetCapacity(RunBagSection.Important) == 0, "important initially no slots");
        Check(!new InventoryRadialProvider().BuildOptions()[0].Enabled, "locked category disabled");
        Check(RunInventory.GrantSlots(RunBagSection.Important, 1, "item-test", out reason), "unlock important slot isolated");
        Equip(1, RunBagSection.Recovery, 0); Equip(5, RunBagSection.Recovery, 1); Equip(10, RunBagSection.Engineering, 0); Equip(11, RunBagSection.Important, 0);
        var all = new InventoryRadialProvider().BuildOptions();
        Check(all.Count == 3 && all[2].Children.Count == 2 && all[2].Children[1].ShortcutNumber == 2, "categories and capacity-index shortcuts");
        Check(!RunItemUseService.TryUse(RunBagSection.Recovery, 0, 1, out reason) && RunInventory.GetCount(1) == 4, "full health refuses without consuming");
        actor.ApplyContactDamage(40); int hp = actor.Hp;
        Check(RunItemUseService.TryUse(RunBagSection.Recovery, 0, 1, out reason) && actor.Hp == hp + 35 && RunInventory.GetCount(1) == 3, "heal commits exact quantity and HP");
        Check(!RunItemUseService.TryUse(RunBagSection.Recovery, 0, 1, out reason) && RunInventory.GetCount(1) == 3, "cooldown prevents duplicate use");
        Check(!RunItemUseService.TryUse(RunBagSection.Recovery, 0, 6, out reason) && RunInventory.GetCount(6) == 4, "stale expected ID cannot consume another item");
        Check(!RunItemUseService.TryUse(RunBagSection.Misc, 0, 6, out reason), "misc not quick usable");
        yield return new WaitForSecondsRealtime(.7f);
        int oldMax = actor.maxHp; hp = actor.Hp;
        Check(RunItemUseService.TryUse(RunBagSection.Recovery, 1, 5, out reason) && actor.maxHp == oldMax + 20 && actor.Hp == hp, "buff changes maximum without healing");
        yield return new WaitForSecondsRealtime(.7f);
        Check(!RunItemUseService.TryUse(RunBagSection.Important, 0, 11, out reason) && RunInventory.GetCount(11) == 1, "passive important item cannot be consumed");

        yield return new WaitForSecondsRealtime(1.2f); // Cooldowns intentionally do not advance inside paused menus.
        Key(UnityEngine.InputSystem.Key.I); yield return new WaitForSecondsRealtime(.3f); Key(); yield return null;
        Check(Top is RunMenuPanel bag && bag.SelectedTab == RunFunctionTab.Inventory, "I opens bag directly");
        Click("Settings"); yield return new WaitForSecondsRealtime(.2f);
        Check(Top is SettingPanel, "settings opens over collection");
        var setting = (SettingPanel)Top;
        var triggerRow = setting.GetComponentsInChildren<SettingRow_Selector>(true).Single(r => r.settingId == SettingIds.ItemWheelMode);
        Check(triggerRow.transform.Find("Options").childCount == 2 && triggerRow.transform.Find("Options").GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text == "长按显示", "two real translated settings options");
        int appliedMode = SettingsManager.Instance.Applied.itemWheelMode;
        triggerRow.transform.Find("Prev").GetComponent<Button>().onClick.Invoke();
        Check(SettingsManager.Instance.Pending.itemWheelMode == 0 && SettingsManager.Instance.Applied.itemWheelMode == appliedMode, "selector changes draft only");
        SettingsManager.Instance.ResetPendingSection(SettingCategory.Gameplay);
        Check(SettingsManager.Instance.Pending.itemWheelMode == 1, "reset gameplay restores hold mode");
        Click("Btn_Keybind");
        setting.GetComponentsInChildren<SettingRow_Button>(true).Single(r => r.settingId == SettingIds.OpenBindings).GetComponentInChildren<Button>().onClick.Invoke();
        yield return null;
        Check(new[] { SettingIds.ItemWheel, SettingIds.Team, SettingIds.Inventory }.All(id => setting.GetComponentsInChildren<SettingRow_Keybind>(true).Any(r => r.settingId == id)), "all three single-key actions present in bindings UI");
        SettingsManager.Instance.RevertEdit(); UIManager.Instance.PopPanel(); yield return null; yield return null;
        Key(UnityEngine.InputSystem.Key.T); yield return new WaitForSecondsRealtime(.3f); Key(); yield return null;
        Check(Top is RunMenuPanel team && team.SelectedTab == RunFunctionTab.Team, "T switches collection to team");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.3f); Key(); yield return null;
        Check(Top is RunMenuPanel map && map.SelectedTab == RunFunctionTab.Map, "inside M opens local map"); Click("Continue"); yield return null; yield return null;

        // Hold: release never executes. Toggle: Alt release keeps open. Both require left click.
        SettingsManager.Instance.Applied.itemWheelMode = 1;
        uint before = RunInventory.GetCount(5);
        Key(UnityEngine.InputSystem.Key.LeftAlt); yield return new WaitForSecondsRealtime(.2f);
        Check(Wheel.IsOpen && Time.timeScale == 0, "hold Alt opens paused radial");
        InputSystem.QueueDeltaStateEvent(_mouse.delta, new Vector2(-150, -90)); yield return new WaitForSecondsRealtime(.4f);
        var presenter = (Chaos.MVP.Presenter.Gameplay.RadialMenuPresenter)typeof(RunItemWheelDriver).GetField("_presenter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Wheel);
        Check(presenter.Model.Level == 2 && presenter.Model.SelectedIndex == -1, "hover recovery expands with no implicit child");
        Key(UnityEngine.InputSystem.Key.LeftAlt, UnityEngine.InputSystem.Key.Digit2); yield return null; yield return null;
        Check(presenter.Model.SelectedIndex == 1 && RunInventory.GetCount(5) == before, "number selects only");
        Key(); yield return new WaitForSecondsRealtime(.2f);
        Check(!Wheel.IsOpen && RunInventory.GetCount(5) == before && Time.timeScale == 1, "hold release cancels without consuming");
        SettingsManager.Instance.Applied.itemWheelMode = 0;
        Key(UnityEngine.InputSystem.Key.LeftAlt); yield return new WaitForSecondsRealtime(.2f); Key(); yield return new WaitForSecondsRealtime(.2f);
        Check(Wheel.IsOpen, "toggle stays open after Alt release");
        InputSystem.QueueDeltaStateEvent(_mouse.delta, new Vector2(-150, -90)); yield return new WaitForSecondsRealtime(.4f);
        Key(UnityEngine.InputSystem.Key.Digit2); yield return null; yield return null; Key(); yield return null;
        Status = "visual"; Flush(); while (!Resume) yield return null; Status = "running";
        MouseButton(true); yield return null; yield return null;
        Check(!Wheel.IsOpen && RunInventory.GetCount(5) == before - 1 && actor.maxHp == oldMax + 20, "left click uses once and refreshes buff without stacking");
        yield return new WaitForSecondsRealtime(.1f);
        Check(!InputManager.Instance.CanReadGameplayInput, "use animation blocks attack leakage"); MouseButton(false);
        yield return new WaitForSecondsRealtime(.6f);
        Check(InputManager.Instance.CanReadGameplayInput, "animation releases own input block");
        before = RunInventory.GetCount(5); var previousPanel = Top;
        Key(UnityEngine.InputSystem.Key.LeftAlt); yield return new WaitForSecondsRealtime(.2f); Key(); yield return null;
        Key(UnityEngine.InputSystem.Key.Escape); yield return new WaitForSecondsRealtime(.2f); Key(); yield return null;
        Check(!Wheel.IsOpen && Top == previousPanel && RunInventory.GetCount(5) == before, "Esc cancels without opening another menu or using item");

        bool rebound = false;
        var step = InputManager.Instance.GetRebindSteps(InputManager.ActionItemWheel, InputManager.BindingKind.Keyboard)[0];
        InputManager.Instance.BeginRebind(InputManager.ActionItemWheel, step.BindingIndex, InputManager.BindingKind.Keyboard, result => rebound = true);
        yield return null; Key(UnityEngine.InputSystem.Key.F2); yield return new WaitForSecondsRealtime(.3f); Key(); yield return null;
        Check(rebound && InputManager.Instance.GetBindingPath(InputManager.ActionItemWheel, InputManager.BindingKind.Keyboard) == "<Keyboard>/f2", "wheel remaps one binding to F2");
        Key(UnityEngine.InputSystem.Key.LeftAlt); yield return new WaitForSecondsRealtime(.2f); Key(); yield return null;
        Check(!Wheel.IsOpen, "old Alt binding no longer opens");
        Key(UnityEngine.InputSystem.Key.F2); yield return new WaitForSecondsRealtime(.2f); Key(); yield return null;
        Check(Wheel.IsOpen, "new F2 binding opens");
        InputSystem.QueueStateEvent(_mouse, new MouseState { buttons = 2 }); yield return null; yield return null;
        Check(!Wheel.IsOpen && RunInventory.GetCount(5) == before, "right click cancels without using");
        MouseButton(false); InputManager.Instance.LoadOverridesJson(""); yield return null;

        // Use engineering damage against an owned encounter enemy.
        var enemies = new List<EnemyPlaceholder>(); RunEncounter.CopyMapEnemies(enemies); var target = enemies[0];
        actor.transform.position = target.transform.position + Vector3.left; int enemyHp = target.Hp;
        Check(RunItemUseService.TryUse(RunBagSection.Engineering, 0, 10, out reason) && target.Hp == Mathf.Max(0, enemyHp - 20) && RunInventory.GetCount(10) == 3, "engineering area damage and quantity");
        yield return new WaitForSecondsRealtime(.6f);
        for (int stage = 1; stage <= 3; stage++)
        {
            RunEncounter.CopyMapEnemies(enemies); foreach (var enemy in enemies) if (enemy != null && !enemy.IsDead) enemy.TakeDamage(99999);
            RunEncounter.RefreshNow(); deadline = Time.realtimeSinceStartup + 40;
            if (stage < 3)
            {
                while ((RunEncounter.CurrentSceneIndex != stage + 1 || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(RunEncounter.IsSceneReady && RunEncounter.CurrentSceneIndex == stage + 1, "next map ready " + (stage + 1));
                yield return null;
                Check(RunInventory.GetCount(5) == before && RunItemUseService.Actor.maxHp >= oldMax + 20, "item/buff retained across maps " + (stage + 1));
            }
            else { while (!(Top is RunSettlementPanel) && Time.realtimeSinceStartup < deadline) yield return null; Check(Top is RunSettlementPanel, "three-map settlement"); }
        }
        Check(RunSettlementModel.Current.Confirm(true, out reason), "isolated settlement confirm");
        Check(RunSettlementModel.Current.ReturnToMeta(out reason), "return village requested");
        deadline = Time.realtimeSinceStartup + 40;
        while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && RunInventory.TotalCount == 0 && RunBagProgression.Owns(11), "return clears run bag but retains unique ownership");
        Check(SaveSlotService.SavesDirectory == _storage, "storage isolation retained through complete flow");
    }
}
#endif
