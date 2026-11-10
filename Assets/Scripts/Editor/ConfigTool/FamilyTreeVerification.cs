#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.MVP.Model.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;

/// <summary>隔离存档下验证技能树事务、真实界面与派遣属性；中途停留供视觉复核。</summary>
public static class FamilyTreeVerification
{
    public static string Status { get; private set; }="idle";
    public static bool ContinueRun;
    private static readonly StringBuilder Log=new StringBuilder();
    private static string _storage,_overrides;
    private static Keyboard _keyboard;
    private static LocalizationSystem.LanguageType _language;
    private static SavePayload P=>GameSession.Instance.LoadedPayload;
    private static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    private static void Check(bool ok,string text) { Log.AppendLine((ok?"PASS ":"FAIL ")+text);Flush();if(!ok)throw new Exception(text); }
    private static void Flush() { File.WriteAllText(Application.dataPath+"/QA/FamilyTree/flow.txt",Log.ToString()); }
    public static void Start()
    {
        if(!Application.isPlaying || Status=="running")throw new Exception("需要空闲Play模式");
        Status="running";ContinueRun=false;Log.Clear();
        _storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));SaveSlotService.UseEditorTestDirectory(_storage);
        _overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");_keyboard=InputSystem.AddDevice<Keyboard>("FamilyTreeQA");
        var language=LocalizationSystem.LocalizationManager.GetInstance();_language=language.CurrentLanguage;language.ChangeLanguage(LocalizationSystem.LanguageType.ChineseSimplified);
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    private static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            bool more;object value=null;
            try { more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current; }
            catch(Exception error) { Status="failed";Log.AppendLine("FAIL "+error);Cleanup();Flush();yield break; }
            if(!more) { stack.Pop();continue; } if(value is IEnumerator nested)stack.Push(nested);else yield return value;
        }
        Status="passed";Cleanup();Log.AppendLine("DONE isolated storage="+_storage);Flush();
    }
    private static void Cleanup()
    {
        InputManager.Instance.LoadOverridesJson(_overrides);
        if(_keyboard!=null && _keyboard.added)InputSystem.RemoveDevice(_keyboard);
        LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(_language);
    }
    private static IEnumerator Press(Key key)
    { InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.15f);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.3f); }
    private static void Click(string name)
    { var button=Top.GetComponentsInChildren<Button>(false).First(b=>b.name==name&&b.interactable);ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler); }
    private static void Domain()
    {
        GameSession.Instance.BeginSandbox(new SavePayload { resGoods=10000,resMoney=10000,resMaterial=10000,resBlueprint=10000 });
        for(int i=0;i<4;i++)RetainerService.Instance.RecruitRandom();
        var service=FamilyTreeService.Instance;service.Reload();var catalog=service.Catalog;
        Check(catalog.Nodes.Count==35&&catalog.Boards.Count==5&&catalog.Links.Count==35,"35 nodes, five boards and one actual effect per node");
        var before=JsonUtility.ToJson(P);var snap=EffectAggregator.Instance.Snapshot;int version=snap.Version;
        var preview=EffectAggregator.Instance.PreviewAdditionalNode(101);
        Check(preview.WorldMarketDiscount==2&&EffectAggregator.Instance.Snapshot.Version==version&&before==JsonUtility.ToJson(P),"preview is pure and shares actual aggregation");
        var root=catalog.Node(101);uint parent=root.PrereqNodeId;root.PrereqNodeId=101;
        try { Check(!service.CanUnlock(101,out _)&&before==JsonUtility.ToJson(P),"cyclic prerequisite refuses before any resource mutation"); } finally {root.PrereqNodeId=parent;}
        float x=catalog.Node(102).LayoutX;catalog.Node(102).LayoutX=root.LayoutX;
        try {Check(!service.CanUnlock(101,out _),"overlapping configured nodes reject unlock");}finally{catalog.Node(102).LayoutX=x;}
        var link=catalog.Links.First(l=>l.NodeId==101);uint effect=link.EffectId;link.EffectId=999999;
        try { Check(!service.CanUnlock(101,out _),"missing effect rejects unlock"); } finally { link.EffectId=effect; }
        Check(!service.Unlock(103,out _),"locked downstream node cannot skip prerequisite");
        var legacy=catalog.Nodes.Where(n=>n.Index%100<=3).Select(n=>n.Index).ToList();P.familyTreeNodes=legacy;
        EffectAggregator.Instance.MarkDirty();snap=EffectAggregator.Instance.Snapshot;
        Check(snap.FailureCarryCapBonus==3&&snap.TotalSlotsBonus==0&&snap.HpBonus==10&&snap.AttackBonus==5&&snap.DefenseBonus==5&&snap.SpeedBonus==2&&snap.CampRefillUnlocked,"old IDs receive refreshed effects and retain active combat bonuses");
        Check(snap.WorldMarketDiscount==10&&snap.GetBuildSecondsDelta(VillageService.WarehouseId)==-5
            &&snap.RunWoundSecondsReduction==3&&RunBagProgression.GetCapacity(RunBagSection.Engineering)==2
            &&RunBagProgression.GetCapacity(RunBagSection.Misc)==7,"legacy nodes activate current trade, warehouse, wound and bag effects");
        Check(SaveSlotService.Instance.Save(3,P),"owned legacy-format slot saved");Check(SaveSlotService.Instance.TryLoad(3,out var saved),"owned legacy-format slot loaded");GameSession.Instance.Begin(3,saved.payload,"MainScene");
        Check(P.familyTreeNodes.SequenceEqual(legacy)&&service.UnlockedCount==15,"legacy IDs preserved without refunds or extra deductions");
        P.familyTreeNodes.Clear();EffectAggregator.Instance.MarkDirty();Check(GameSession.Instance.SaveCurrent(out _),"empty owned baseline saved for rollback test");before=JsonUtility.ToJson(P);
        using(var locked=new FileStream(SaveSlotService.Instance.GetSlotPath(3),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            Check(!service.Unlock(101,out _)&&JsonUtility.ToJson(P)==before&&EffectAggregator.Instance.Snapshot.WorldMarketDiscount==0,"real save lock rolls back resources, unlock and derived effect");
        uint goods=P.resGoods;Check(service.Unlock(101,out _)&&P.resGoods==goods-50,"retry after lock release deducts once");before=JsonUtility.ToJson(P);
        Check(!service.Unlock(101,out _)&&JsonUtility.ToJson(P)==before,"duplicate unlock is idempotent");
        foreach(var node in catalog.Nodes.OrderBy(n=>n.Index))if(!service.IsUnlocked(node.Index))Check(service.Unlock(node.Index,out _),"unlock configured node "+node.Index);
        snap=EffectAggregator.Instance.Snapshot;
        Check(service.UnlockedCount==35&&snap.TotalSlotsBonus==0&&snap.HpBonus==35&&snap.AttackBonus==7&&snap.DefenseBonus==8&&snap.SpeedBonus==3,"full-tree totals use active effects");
        Check(snap.WorldResourceCycleReduction==10&&RetainerService.Instance.RunWoundSeconds==23
            &&MetaProgressService.Instance.FailureCarryCap==4,"full-tree resource cycle, wounds and failure carry");
        Check(snap.GetOutputDelta(2002)==1&&snap.GetOutputDelta(2003)==1&&snap.GetOutputDelta(2004)==1&&snap.GetBuildSecondsDelta(2002)==-2,"non-farm building effects are keyed by building ID");
        Check(MetaCharacterStats.AttackDamage(10)==17&&MetaCharacterStats.MoveSpeed(6)==9&&MetaCharacterStats.ReceivedDamage(5)==1&&MetaCharacterStats.ReceivedDamage(0)==0,"shared attack, movement and defense calculations consume family snapshot");
        var actor=new GameObject("QA_HealthLayers",typeof(Rigidbody2D));var health=actor.AddComponent<PlayerHealth>();health.InitializeSpawnedHealth(35);health.SetRunHealthBonus(15);health.SetItemHealthBonus(20);int hp=health.Hp;
        health.ApplyContactDamage(20);int injured=health.Hp;health.InitializeSpawnedHealth(35);
        Check(health.maxHp==170&&health.Hp==injured&&hp-injured==12,"family/run/item layers compose, defense acts, repeated init does not heal");UnityEngine.Object.Destroy(actor);
        P.familyTreeNodes=new List<uint>{101};P.resGoods=90;EffectAggregator.Instance.MarkDirty();Check(GameSession.Instance.SaveCurrent(out _),"four-state UI fixture saved in owned slot");
    }
    private static IEnumerator Run()
    {
        Domain();VillageSceneFlow.EnterFromSave();yield return WaitVillage();
        var facility=UnityEngine.Object.FindObjectsOfType<VillageFacility>().First(f=>f.Kind==VillageFacilityKind.FamilyMansion);
        var avatar=VillageSceneBootstrap.Current.Avatar;avatar.transform.position=facility.Entrance;avatar.GetComponent<Rigidbody2D>().position=facility.Entrance;Physics2D.SyncTransforms();yield return new WaitForSecondsRealtime(.2f);yield return Press(Key.E);
        Check(Top is FamilyTreePanel,"physical family mansion E opens new tree panel");
        var panel=(FamilyTreePanel)Top;var model=new FamilyTreeModel();var state=model.Read(1,104);
        Check(state.Nodes.Select(n=>n.Status).Distinct().Count()==4,"one real board shows all four node states");
        Check(panel.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Node_"))==7,"current board renders seven reusable node buttons");
        Check(!panel.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.gameObject.activeInHierarchy&&t.text.Contains("ft_")),"actual Chinese names replace localization keys");
        Click("Node_104");Check(P.resGoods==90,"selecting a node only opens details");
        Status="ui-ready";while(!ContinueRun)yield return null;Status="running";
        int identity=panel.transform.Find("FamilyTreeLayout/Body/TreeViewport/TreeGraph/Node_104").GetInstanceID();
        panel.Pan(new Vector2(75,25));var graph=(RectTransform)panel.transform.Find("FamilyTreeLayout/Body/TreeViewport/TreeGraph");Vector2 position=graph.anchoredPosition;
        for(uint board=2;board<=5;board++) { Click("Board_"+board);Check(panel.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Node_"))==7,"seven visible nodes in board "+board); }
        Click("Board_1");Check(Vector2.Distance(graph.anchoredPosition,position)<.01f&&panel.transform.Find("FamilyTreeLayout/Body/TreeViewport/TreeGraph/Node_104").GetInstanceID()==identity,"board viewport preserved and node object reused");
        Click("Node_104");Click("UnlockSelected");Check(P.resGoods==0&&FamilyTreeService.Instance.IsUnlocked(104),"detail unlock button deducts configured cost and persists");
        Check(!Top.GetComponentsInChildren<Button>().First(b=>b.name=="UnlockSelected").interactable,"unlocked selected node cannot purchase twice");
        for(int i=0;i<3;i++){yield return Press(Key.Escape);Check(Top==null,"Esc returns from family tree to village");UIManager.Instance.PushPanel(PanelType.FamilyTreePanel);yield return null;}
        yield return Press(Key.Escape);
        P.resGoods=P.resMoney=P.resMaterial=P.resBlueprint=10000;
        foreach(var node in FamilyTreeService.Instance.GetAll().OrderBy(n=>n.Index))if(!FamilyTreeService.Instance.IsUnlocked(node.Index))Check(FamilyTreeService.Instance.Unlock(node.Index,out _),"prepare actual expedition node "+node.Index);
        yield return Press(Key.M);Click("EnterRunBtn");yield return new WaitForSecondsRealtime(.3f);foreach(var member in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(member.id,out _);Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();
        for(int stage=1;stage<=3;stage++)
        {
            float end=Time.realtimeSinceStartup+50;while((!RunEncounter.IsSceneReady||RunEncounter.CurrentSceneIndex!=stage||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;
            Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage,"formal map ready "+stage);yield return null;
            var player=PlayerSpawner.CurrentPlayer;var health=player.GetComponent<PlayerHealth>();Check(health.maxHp==135,"family health applied once on map "+stage);
            var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var enemy in enemies)enemy.enabled=false;
            foreach(var ally in AllyController.All.Where(a=>a!=null&&RunEncounter.OwnsRuntimeObject(a.gameObject)))
            { Check(Mathf.Abs(ally.moveSpeed-(RunMap.RunBehaviorSettings.Load("Ally").moveSpeed+3))<.01f,"AI navigation uses family speed on map "+stage);ally.enabled=false; }
            if(stage==1)
            {
                int hp=health.Hp;health.ApplyContactDamage(20);Check(hp-health.Hp==12,"actual expedition player ordinary damage applies defense");health.InitializeSpawnedHealth(35);Check(health.maxHp==135&&health.Hp==hp-12,"actual repeated initialization never heals or stacks");
                var attack=player.GetComponent<PlayerAttack>();var enemy=enemies.First();enemy.transform.position=CombatQuery.Center(player.transform)+new Vector2(attack.hitBoxOffsetX,0);player.GetComponent<PlayerLocomotion>().SetFacing(1);Physics2D.SyncTransforms();int enemyHp=enemy.Hp;
                typeof(PlayerAttack).GetMethod("DoAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(attack,null);Check(enemyHp-enemy.Hp==17,"actual player hitbox attack deals base plus permanent bonus");
            }
            foreach(var enemy in enemies)if(enemy!=null&&!enemy.IsDead)enemy.TakeDamage(99999);RunEncounter.RefreshNow();
        }
        float deadline=Time.realtimeSinceStartup+40;while(!(Top is RunSettlementPanel)&&Time.realtimeSinceStartup<deadline)yield return null;Check(Top is RunSettlementPanel,"three maps reach settlement");yield return Press(Key.Escape);yield return WaitVillage();
        Check(FamilyTreeService.Instance.UnlockedCount==35,"all permanent branches survive expedition and return");
    }
    private static IEnumerator WaitVillage()
    { float end=Time.realtimeSinceStartup+40;while((!VillageSceneBootstrap.IsReady||!VillageSceneFlow.IsVillageScene)&&Time.realtimeSinceStartup<end)yield return null;Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"formal village ready"); }
}
#endif
