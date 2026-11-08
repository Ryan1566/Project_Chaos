#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.Gameplay.Interaction;
using Chaos.MVP.Model.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>使用隔离存档验证出口键、结算按钮和仓库事务。</summary>
public static class RunCompletionVerification
{
    private static readonly StringBuilder Log = new StringBuilder();
    private static Keyboard keyboard;
    private static string storage, originalBindings;
    public static string Status { get; private set; } = "idle";
    public static bool HoldForReview;
    private static SavePayload Payload => GameSession.Instance.LoadedPayload;
    private static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    private static string ReportPath => Application.dataPath + "/QA/RunCompletion/flow.txt";

    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new InvalidOperationException("需在空闲 Play 模式启动");
        Log.Clear();
        Status = "running";
        storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-Completion-" + Guid.NewGuid().ToString("N"));
        SaveSlotService.UseEditorTestDirectory(storage);
        originalBindings = InputManager.Instance.SaveOverridesJson();
        InputManager.Instance.LoadOverridesJson("");
        keyboard = InputSystem.AddDevice<Keyboard>("RunCompletionQA");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }

    private static IEnumerator Guard(IEnumerator entry)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(entry);
        while (stack.Count > 0)
        {
            object current;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                current = stack.Peek().Current;
            }
            catch (Exception error)
            {
                Status = "failed";
                Log.AppendLine("FAIL " + error);
                Cleanup();
                yield break;
            }
            if (current is IEnumerator nested) stack.Push(nested);
            else yield return current;
        }
        Status = "passed";
        Log.AppendLine("DONE " + storage);
        Cleanup();
    }

    private static void Cleanup()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        InputManager.Instance.LoadOverridesJson(originalBindings);
        File.WriteAllText(ReportPath, Log.ToString());
    }

    private static void Check(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(label);
        Log.AppendLine("PASS " + label);
        File.WriteAllText(ReportPath, Log.ToString());
    }

    private static IEnumerator Until(Func<bool> condition, string label)
    {
        float deadline = Time.realtimeSinceStartup + 45;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Check(condition(), label);
    }

    private static IEnumerator Press(Key key, float hold = .15f)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return new WaitForSecondsRealtime(hold);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(.2f);
    }

    private static Button FindButton(string name)
        => Top?.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == name);

    private static void Click(string name)
    {
        var button = FindButton(name);
        Check(button != null && button.gameObject.activeInHierarchy && button.interactable, "clickable " + name);
        ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
    }

    private static void WarehouseChecks()
    {
        var legacy = new SavePayload { resMaterial = 700, resMoney = 500, resWood = 495 };
        legacy.items.Add(new ItemStackSave { itemId = 9101, count = 10 });
        legacy.items.Add(new ItemStackSave { itemId = 9104, count = 2 });
        GameSession.Instance.BeginSandbox(legacy);
        var inventory = InventoryService.Instance;
        var village = VillageService.Instance;
        var ordinary = inventory.GetAllDefinitions().Where(d => d.Enable && !d.IsUnique && d.RunCategory != 1
            && !InventoryService.TryGetResourceCommodity(d.Index, out _)).ToArray();
        Check(Payload.resWood == 505 && Payload.resRawSalt == 2 && Payload.items.Count == 0, "old commodity rows migrate without clamping");
        var roundTrip = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(Payload));
        GameSession.Instance.BeginSandbox(roundTrip);
        Check(Payload.resWood == 505 && Payload.resRawSalt == 2, "save reload does not migrate resources twice");
        Check(village.ResourceCapacity == 500 && inventory.ResourceRoom(InventoryService.ResourceKind.Wood) == 0, "default capacity and old excess are preserved");
        Check(!inventory.AddItem(9101, 1, out _) && Payload.resWood == 505, "resource commodity respects cap");
        Check(ordinary.Length > InventoryService.BaseSlots, "fixture has more than the old item slot limit");
        foreach (var item in ordinary)
            if (!inventory.AddItem(item.Index, 1000, out var reason)) throw new InvalidOperationException(reason);
        Check(inventory.UsedSlots == ordinary.Length && inventory.GetItemCount(ordinary[0].Index) == 1000, "warehouse stores more kinds and quantities than the old limits");
        uint money = Payload.resMoney;
        Check(!MarketService.Instance.TryBuy(9101, out _) && Payload.resMoney == money, "full resource purchase does not debit money");
        Check(!MarketService.Instance.TrySell(9104, out _) && Payload.resRawSalt == 2, "full money balance prevents sale without removing stock");
        Check(village.StartUpgrade(VillageService.WarehouseId, out _) && Payload.resMaterial == 650 && village.ResourceCapacity == 500,
            "warehouse upgrade charges once and old capacity holds during construction");
        Check(!village.StartUpgrade(VillageService.OfficeId, out _), "fixed facilities share the existing construction queue");
        village.Tick(30);
        Check(village.ResourceCapacity == 1000 && village.EffectiveLevel(VillageService.WarehouseId) == 2, "warehouse completion unlocks level two capacity");
        Check(village.StartUpgrade(VillageService.OfficeId, out _), "office can upgrade from implicit old-save level one");
        village.Tick(40);
        Check(village.StartUpgrade(VillageService.FamilyMansionId, out _), "family mansion can upgrade");
        village.Tick(40);
        roundTrip = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(Payload));
        GameSession.Instance.BeginSandbox(roundTrip);
        Check(village.ResourceCapacity == 1000 && village.EffectiveLevel(VillageService.OfficeId) == 2
            && village.EffectiveLevel(VillageService.FamilyMansionId) == 2, "three upgraded facilities survive reload");
        uint[] capacities = { 500, 1000, 1500, 2500, 3500, 5000, 6500, 8000, 9999 };
        Check(capacities.Select((cap, i) => village.GetDef(VillageService.WarehouseId, (uint)i + 1)?.ResourceCapacity == cap).All(x => x),
            "all nine exported warehouse tiers match approved capacities");

        var site = WorldLocationService.Instance.GetLocations(1, WorldLocationIdentity.Family)
            .First(p => p.Kind == WorldLocationKind.Resource && p.CanBuild);
        Check(WorldResourceService.Instance.Build(site.Id, out _), "build resource site using capped resource ledger");
        WorldResourceService.Instance.Tick(600);
        Check(InventoryService.TryGetResourceCommodity(site.OutputItemId, out var kind), "site output is a village resource");
        inventory.AddResource(kind, village.ResourceCapacity - inventory.GetResource(kind) - 1);
        Check(WorldResourceService.Instance.Harvest(site.Id, out uint amount, out _) && amount == 1, "site fills exactly the remaining resource capacity");
        Check(Payload.worldLocations.Find(s => s.locationId == site.Id).pendingOutput == 9
            && !WorldResourceService.Instance.Harvest(site.Id, out _, out _), "remaining yield stays at the site");

        var tree = FamilyTreeService.Instance;
        uint[] bonusNodes = tree.GetAll().Where(n => n.Enable).Select(n => n.Index).ToArray();
        var outside = bonusNodes.First(id => tree.GetDef(id).Board != 5
            && EffectAggregator.Instance.PreviewNodes(new[] { id }).DeployCapBonus > 0);
        var inside = bonusNodes.First(id => tree.GetDef(id).Board == 5
            && EffectAggregator.Instance.PreviewNodes(new[] { id }).FailureCarryCapBonus > 0);
        Payload.familyTreeNodes.Add(outside);
        EffectAggregator.Instance.MarkDirty();
        Check(MetaProgressService.Instance.FailureCarryCap == 1, "outside branch bonus does not affect defeat carry");
        Payload.familyTreeNodes.Add(inside);
        EffectAggregator.Instance.MarkDirty();
        Check(MetaProgressService.Instance.FailureCarryCap > 1, "run branch bonus affects defeat carry");
        RunSettlementModel.Clear();
        RunSettlementModel.Begin(false);
        var empty = RunSettlementModel.Current;
        empty.MovePage(1);
        Check(empty.GetItems().Count == 0 && empty.SelectedCount == 0, "empty defeat inventory needs no selection");
        empty.MovePage(1);
        Check(empty.Confirm(false, out _), "empty defeat settlement can confirm");
        RunSettlementModel.Clear();
    }

    private static IEnumerator Run()
    {
        WarehouseChecks();
        var icons = new[] { "capital", "market_town", "tribe", "forest", "iron_mine", "quarry", "saltworks", "cave", "ruins", "easter_egg", "rumor", "task" };
        Check(icons.All(key => Resources.Load<Sprite>("UI/WorldLocations/" + key) != null), "all twelve configured map sprites resolve");
        GameSession.Instance.BeginSandbox(new SavePayload { resMaterial = 300, resMoney = 300, resGoods = 300, resBlueprint = 300 });
        RetainerService.Instance.RecruitRandom();
        RetainerService.Instance.RecruitRandom();
        var fixture = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(Payload));
        GameSession.Instance.Clear();
        Check(SaveSlotService.Instance.Save(3, fixture), "isolated slot saved");
        GameSession.Instance.Begin(3, fixture, "MainScene");
        VillageSceneFlow.EnterFromSave();
        yield return Until(() => VillageSceneBootstrap.IsReady, "formal village ready");
        UIManager.Instance.PushPanel(PanelType.InventoryPanel);
        yield return null;
        Click("WarehouseManagement");
        yield return null;
        Click("Btn_2103");
        Check(VillageService.Instance.Find(2103)?.level == 2 && VillageService.Instance.ResourceCapacity == 500,
            "warehouse upgrade starts from actual facility UI");
        while (Top != null) UIManager.Instance.PopPanel();

        yield return Depart();
        Check(!RunInventory.AddItem(9101, 1, out _) && RunInventory.GetCount(9101) == 0,
            "village resource cannot enter an active run bag");
        float pausedAt = RunEncounter.RunPlaySeconds;
        RunControlModel.PauseWorld(true);
        yield return new WaitForSecondsRealtime(.3f);
        Check(RunEncounter.RunPlaySeconds == pausedAt, "paused world time is excluded from the report");
        RunControlModel.PauseWorld(false);
        int damage = 0;
        var enemies = new List<EnemyPlaceholder>();
        RunEncounter.CopyMapEnemies(enemies);
        Check(enemies.Count > 0, "first scene populated with live enemies");
        damage = enemies[0].Hp;
        enemies[0].TakeRunDamage(damage + 99);
        Check(RunEncounter.RunKills == 1 && RunEncounter.HighestRunDamage == damage, "actual damage excludes overkill and records death once");
        enemies[0].TakeRunDamage(damage + 99);
        Check(RunEncounter.RunKills == 1, "repeat damage on corpse does not increase kill count");
        if (enemies.Count > 1)
        {
            enemies[1].Die("掉落死亡");
            Check(RunEncounter.RunKills == 1, "enemy falling does not count as a kill");
        }
        yield return Press(Key.E);
        Check(RunEncounter.CurrentSceneIndex == 1 && !RunSceneFlow.IsTransitioning, "interact away from exit cannot progress");
        Check(RunInventory.AddItem(1, 1, out _), "success fixture bag stocked");
        uint bagId = RunInventory.Stacks.First().itemId;
        uint bagCount = RunInventory.GetCount(bagId);
        yield return PassDoor(1);
        Check(RunEncounter.RunKills == 1 && RunInventory.GetCount(bagId) == bagCount, "loot and stats survive first door");
        yield return PassDoor(2);
        Check(!RunInteractable.Active.OfType<RunExitDoor>().Any(), "third scene has no progression door");
        RunEncounter.KillAllEnemiesInScene();
        yield return Until(() => Top is RunSettlementPanel, "final combat opens success settlement");
        var success = RunSettlementModel.Current;
        Check(success.Win && success.Report.StagesPassed == 3 && success.Report.HighestDamage == damage
            && success.Report.PlaySeconds > 0 && success.Report.Members.Count > 0, "success report freezes three stages, time, real damage and roster");
        Check(success.Page == 0 && !FindButton("Btn_Confirm").gameObject.activeSelf
            && !success.Confirm(false, out _), "first page cannot submit");
        yield return Press(Key.Escape);
        Check(Top is RunSettlementPanel && success.Page == 0, "Escape cannot bypass settlement");
        while (HoldForReview) yield return null;
        Click("Btn_Next");
        Check(success.Page == 1 && success.GetItems().Any(i => i.Id == bagId && i.Retained)
            && !success.Toggle(bagId, out _), "success loot is retained and read only");
        Click("Btn_Next");
        Check(success.Page == 2 && success.Report.Events.Count == 0, "final event page is initially empty");
        uint wins = Payload.clears;
        uint prior = InventoryService.Instance.GetItemCount(bagId);
        using (var locked = new FileStream(SaveSlotService.Instance.GetSlotPath(3), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Click("Btn_Confirm");
            Check(success.PendingSave && !success.Completed && Payload.clears == wins
                && InventoryService.Instance.GetItemCount(bagId) == prior, "settlement rolls back on actual save failure");
        }
        Click("Btn_Confirm");
        yield return Until(() => VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene, "success returns to village");
        Check(Payload.clears == wins + 1 && InventoryService.Instance.GetItemCount(bagId) >= prior + bagCount
            && !success.Confirm(false, out _), "successful retry grants once");
        while (Top != null) UIManager.Instance.PopPanel();

        yield return Depart();
        var ordinary = InventoryService.Instance.GetAllDefinitions().Where(d => d.Enable && !d.IsUnique
            && d.RunCategory == 0 && !InventoryService.TryGetResourceCommodity(d.Index, out _)).Take(2).ToArray();
        Check(ordinary.Length == 2 && RunInventory.AddItem(ordinary[0].Index, 2, out _)
            && RunInventory.AddItem(ordinary[1].Index, 1, out _), "failure fixture bag stocked");
        uint important = InventoryService.Instance.GetAllDefinitions().First(d => d.Enable && d.RunCategory == 1).Index;
        Check(RunInventory.AddItem(important, 1, out _) && RunBagProgression.Owns(important),
            "new important item receives permanent ownership immediately");
        uint kept = ordinary[0].Index, lost = ordinary[1].Index;
        uint keptBefore = InventoryService.Instance.GetItemCount(kept), lostBefore = InventoryService.Instance.GetItemCount(lost);
        PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>().ApplyContactDamage(99999);
        yield return Until(() => Top is RunSettlementPanel, "actual team wipe opens failure settlement");
        var failure = RunSettlementModel.Current;
        Check(!failure.Win && failure.Page == 0 && failure.Report.Members.All(member => member.Downed), "failure starts at team report");
        Click("Btn_Next");
        Check(failure.SelectedCount == 0 && failure.FailureCarryCap == 1, "failure initially has no selected loot");
        Check(failure.GetItems().Single(i => i.Id == important).Important && !failure.Toggle(important, out _),
            "important loot is visible but consumes no failure selection");
        Click("Sel_" + kept);
        Click("Sel_" + lost);
        Check(failure.SelectedCount == 1 && failure.GetItems().Single(i => i.Id == kept).Selected, "failure cap rejects second distinct type");
        Click("Btn_Prev");
        Click("Btn_Next");
        Check(failure.GetItems().Single(i => i.Id == kept).Selected, "going back preserves failure choice");
        UIManager.Instance.PopPanel();
        RunSettlementPanel.Open(false);
        Check(Top is RunSettlementPanel && failure.Page == 1 && failure.SelectedCount == 1,
            "cached settlement reopening preserves page and selected item");
        Click("Btn_Next");
        bool rejectedReturn = false;
        void FailReturnOnce(Scene scene, LoadSceneMode mode)
        {
            var request = SceneTransitionCoordinator.Active;
            if (request?.Kind != SceneTransitionCoordinator.TransitionKind.ReturnVillage || scene.name != "MainScene") return;
            SceneManager.sceneLoaded -= FailReturnOnce;
            rejectedReturn = true;
            SceneTransitionCoordinator.Fail(request.Id, "隔离验收：模拟回村就绪失败");
        }
        Click("Btn_Confirm");
        SceneManager.sceneLoaded += FailReturnOnce;
        try
        {
            yield return Until(() => rejectedReturn && Top is RunSettlementPanel && !RunSceneFlow.IsTransitioning,
                "failed return restores the committed settlement");
        }
        finally { SceneManager.sceneLoaded -= FailReturnOnce; }
        Check(failure.Completed && failure.Page == 2 && InventoryService.Instance.GetItemCount(kept) == keptBefore + 2,
            "return failure retains committed items and final page");
        Click("Btn_Confirm");
        yield return Until(() => VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene, "failure returns to village");
        Check(Payload.clears == wins + 1 && InventoryService.Instance.GetItemCount(kept) == keptBefore + 2
            && InventoryService.Instance.GetItemCount(lost) == lostBefore && RunBagProgression.Owns(important),
            "return retry preserves selected stack and important ownership with no duplicate reward");
    }

    private static IEnumerator Depart()
    {
        var dispatch = DispatchService.Instance;
        dispatch.ResetSelection();
        var member = dispatch.GetCandidates().First(candidate => dispatch.GetUnavailableReason(candidate) == null);
        Check(dispatch.Toggle(member.id, out _), "ready single-member draft");
        yield return Press(Key.M);
        Check(Top is WorldMapPanel, "M opens map for formal dispatch");
        Click("EnterRunBtn");
        yield return null;
        Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();
        yield return Until(() => RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning
            && RunEncounter.CurrentSceneIndex == 1, "formal first scene ready");
    }

    private static IEnumerator PassDoor(int stage)
    {
        var door = RunInteractable.Active.OfType<RunExitDoor>().Single();
        var enemies = new List<EnemyPlaceholder>();
        RunEncounter.CopyMapEnemies(enemies);
        Check(enemies.Count > 0 && door.IsRegistered, "exit active with enemies remaining in stage " + stage);
        var player = PlayerSpawner.CurrentPlayer;
        var body = player.GetComponent<Rigidbody2D>();
        var position = (Vector2)door.transform.position + Vector2.up * .1f;
        body.position = position;
        player.transform.position = position;
        body.velocity = Vector2.zero;
        Physics2D.SyncTransforms();
        if (stage == 2)
        {
            var asset = (InputActionAsset)typeof(InputManager).GetField("_asset", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(InputManager.Instance);
            asset.FindAction("Player/Interact").ApplyBindingOverride(0, "<Keyboard>/q");
            yield return Press(Key.E);
            Check(RunEncounter.CurrentSceneIndex == 2 && !RunSceneFlow.IsTransitioning, "old E no longer triggers a rebound exit");
            yield return Press(Key.Q, .55f);
            InputManager.Instance.LoadOverridesJson("");
        }
        else yield return Press(Key.E, .55f);
        yield return Until(() => RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning
            && RunEncounter.CurrentSceneIndex == stage + 1, "bound input advances exactly one stage from " + stage);
        Check(RunEncounter.StagesPassed == stage, "pass counter records only the used door");
    }
}
#endif
