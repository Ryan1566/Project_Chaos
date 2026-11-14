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
using RunMap;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class RunContentFlowVerification
{
    public static string Status {get;private set;}="idle";
    public static bool Resume;
    static string storage,overrides;
    static Keyboard keyboard;
    static readonly StringBuilder Log=new StringBuilder();
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    static void Check(bool value,string label){if(!value)throw new Exception(label);Log.AppendLine("PASS "+label);Flush();}
    static void Flush(){Directory.CreateDirectory(Application.dataPath+"/QA/RunContent");File.WriteAllText(Application.dataPath+"/QA/RunContent/flow.txt",Log.ToString());}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.2f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.2f);}
    static void Click(string name)
    {var button=Top.GetComponentsInChildren<Button>(false).Single(b=>b.name==name&&b.interactable);ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
    public static void Start()
    {
        if(!Application.isPlaying||Status=="running"||Status=="visual")throw new Exception("requires idle Play");
        Status="running";Resume=false;Log.Clear();
        storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(storage);SaveSlotService.UseEditorTestDirectory(storage);
        overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");keyboard=InputSystem.AddDevice<Keyboard>("ChaosContentKeyboard");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            object value=null;bool more;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}
            catch(Exception e){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Status="failed";Log.AppendLine("FAIL "+e);Cleanup();Flush();yield break;}
            if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Status="passed";Cleanup();Log.AppendLine("DONE owned storage="+storage);Flush();
    }
    static void Cleanup(){InputManager.Instance.LoadOverridesJson(overrides);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);}
    static void MoveNear(RunContentInteractable target)
    {
        var player=PlayerSpawner.CurrentPlayer;var shape=player.GetComponent<Collider2D>();var body=player.GetComponent<Rigidbody2D>();
        var placement=NaturalMapRuntime.Current.Placements.First(p=>p.StableId==target.StableId);Vector2 feet=placement.Position;
        foreach(float x in new[]{.8f,-.8f,0f,1.3f,-1.3f})
        {
            var candidate=feet+Vector2.right*x;if(!RunMapPhysics.CanOccupy(candidate,shape.bounds.size))continue;
            var previous=body.position;body.position=candidate+((Vector2)player.transform.position-RunMapPhysics.Feet(shape));body.velocity=Vector2.zero;
            player.GetComponent<PlayerLocomotion>().InjectInput(0,false,2);Physics2D.SyncTransforms();GameplayCameraRig.NotifyTargetWarped(player.transform,body.position-previous);return;
        }
        throw new Exception("no safe content approach");
    }
    static IEnumerator Open(RunContentInteractable target)
    {
        MoveNear(target);yield return new WaitForSeconds(.2f);yield return Press(Key.E);
        Check(Top is RunContentPanel&&RunContentService.Current?.Target==target&&Time.timeScale==0,"Interact opens paused content panel "+target.StableId);
    }
    static IEnumerator Close(){yield return Press(Key.Escape);Check(!(Top is RunContentPanel)&&Time.timeScale==1,"Esc returns gameplay");}
    static RunContentInteractable Content(string id)=>RunInteractable.Active.OfType<RunContentInteractable>().First(x=>x.Definition.id==id);
    static IEnumerator Run()
    {
        GameSession.Instance.BeginSandbox(new SavePayload{resMoney=1000,resGoods=1000,resMaterial=1000,resBlueprint=1000});
        for(int i=0;i<4;i++)RetainerService.Instance.RecruitRandom();
        var payload=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));GameSession.Instance.Clear();
        Check(SaveSlotService.Instance.Save(3,payload),"owned isolated slot created");GameSession.Instance.Begin(3,payload,"MainScene");VillageSceneFlow.EnterFromSave();
        float deadline=Time.realtimeSinceStartup+40;while(!VillageSceneBootstrap.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady,"formal village ready");yield return Press(Key.M);Click("EnterRunBtn");yield return new WaitForSecondsRealtime(.3f);
        string reason;foreach(var member in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(member.id,out reason);Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();
        deadline=Time.realtimeSinceStartup+40;while((!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(RunEncounter.IsSceneReady,"formal expedition ready");var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var e in enemies)e.enabled=false;
        var health=PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>();health.invulnSeconds=100000;health.ApplyContactDamage(1);
        Check(RunInteractable.Active.OfType<RunContentInteractable>().Count()==4,"NPC, chest and two gathering points generated");
        var npc=Content("traveller");var chest=Content("chest");var herbs=Content("herbs");var ore=Content("ore");
        var npcSpine = npc.GetComponentInChildren<SkeletonAnimation>();
        var npcIdle = CharacterSpineClips.Find(npcSpine?.Skeleton?.Data, CharacterSpineClips.Idle);
        Check(npcIdle != null && npcSpine.AnimationState.GetCurrent(0)?.Animation == npcIdle, "NPC uses the current Spine idle clip");
        Check(npc.GetComponent<AllyController>()==null&&npc.GetComponent<Rigidbody2D>()==null&&npc.GetComponents<Collider2D>().All(x=>x.isTrigger),"NPC has no fighter AI or blocking body");
        var actor=PlayerSpawner.CurrentPlayer;var hud=actor.GetComponent<RunInteractionHud>();MoveNear(npc);yield return new WaitForSeconds(.2f);
        hud.enabled=false;var service=new RunInteractionService(2.5f);
        Check(!service.TryExecute(actor,npc,out reason)&&RunContentService.Current==null,"missing UI listener does not open a fake session");hud.enabled=true;yield return null;yield return null;
        yield return Guards(npc);
        yield return Open(npc);var session=RunContentService.Current;var npcReward=session.Rewards[0];uint before=RunInventory.GetCount(npcReward.ItemId);
        Click("Claim0");yield return null;
        Check(RunInventory.GetCount(npcReward.ItemId)==before+npcReward.Count&&npc.Completed,"NPC grants exactly one chosen supply");
        Check(!session.TryClaim(0,out reason)&&!session.TryClaim(1,out reason),"NPC repeat and alternate choices cannot duplicate rewards");
        npc.ConfigureContent(npc.StableId,RunContentCatalog.Load().Get("traveller"),NaturalMapRuntime.Current);Check(npc.Completed,"reconfigure cannot reset claimed NPC");
        yield return Close();yield return Open(npc);Check(RunContentService.Current.Completed,"NPC can be revisited as dialogue without new supplies");
        Status="visual";Log.AppendLine("VISUAL npc dialogue");Flush();while(!Resume)yield return null;Status="running";
        yield return Close();
        int medicineSlot=Enumerable.Range(0,RunInventory.GetCapacity(RunBagSection.Misc)).First(i=>RunInventory.GetSlot(RunBagSection.Misc,i)?.itemId==1);
        Check(RunInventory.TryMove(RunBagSection.Misc,medicineSlot,RunBagSection.Recovery,0,out reason),"NPC medicine moves into recovery quick-use slot");
        int hpBefore=health.Hp;
        Check(Chaos.Gameplay.Items.RunItemUseService.TryUse(RunBagSection.Recovery,0,1,out reason)&&health.Hp>hpBefore,"NPC supply uses existing item effect and consumes quantity");
        yield return new WaitForSeconds(.6f);
        foreach(var def in InventoryService.Instance.GetAllDefinitions().Where(x=>x.Enable&&x.RunCategory!=1&&x.Index!=7))
        {
            if(Enumerable.Range(0,RunInventory.GetCapacity(RunBagSection.Misc)).All(i=>RunInventory.GetSlot(RunBagSection.Misc,i)!=null))break;
            if(RunInventory.GetCount(def.Index)==0)RunInventory.AddItem(def.Index,1,out reason);
        }
        Check(Enumerable.Range(0,5).All(i=>RunInventory.GetSlot(RunBagSection.Misc,i)!=null)&&RunInventory.GetCount(7)==0,"misc row filled without ore for capacity test");
        var closedSprite=chest.GetComponentInChildren<SpriteRenderer>().sprite;
        yield return Open(chest);session=RunContentService.Current;var counts=session.Rewards.Select(x=>x.Count).ToArray();
        Check(chest.GetComponentInChildren<SpriteRenderer>().sprite!=closedSprite,"opening chest changes its visual before all items claimed");
        Check(!session.TryClaim(1,out reason)&&!session.Rewards[1].Claimed&&!chest.Completed,"full bag leaves unclaimed chest ore intact");
        uint countBefore=RunInventory.GetCount(session.Rewards[0].ItemId);uint grant=session.Rewards[0].Count;bool reentered=false;
        Action reentry=()=>{if(session.TryClaim(0,out var ignored))reentered=true;};RunInventory.Changed+=reentry;
        try{Click("Claim0");}finally{RunInventory.Changed-=reentry;}
        yield return null;Check(!reentered&&RunInventory.GetCount(session.Rewards[0].ItemId)==countBefore+grant,"Changed reentry cannot duplicate a reward");
        Check(session.Rewards[0].Claimed&&!chest.Completed,"chest records partial claim independently");yield return Close();yield return Open(chest);session=RunContentService.Current;
        Check(session.Rewards.Select(x=>x.Count).SequenceEqual(counts)&&session.Rewards[0].Claimed,"reopening chest preserves rolled contents and claimed rows");
        var removable=Enumerable.Range(0,5).Select(i=>RunInventory.GetSlot(RunBagSection.Misc,i)).First(s=>s!=null&&s.itemId!=1&&s.itemId!=7&&s.itemId!=5);
        Check(RunInventory.RemoveItem(removable.itemId,removable.count,out reason),"free a misc slot");
        Click("Claim1");yield return null;Click("Claim2");yield return null;
        Check(chest.Completed&&!session.TryClaim(1,out reason),"remaining chest rewards claim once after freeing space");
        yield return Close();
        foreach(var node in new[]{herbs,ore})
        {
            var fresh=node.GetComponentInChildren<SpriteRenderer>().sprite;yield return Open(node);session=RunContentService.Current;var reward=session.Rewards[0];before=RunInventory.GetCount(reward.ItemId);
            Click("Claim0");yield return null;
            Check(node.Completed&&RunInventory.GetCount(reward.ItemId)==before+reward.Count,"gathering adds configured material "+node.Definition.id);
            Check(node.GetComponentInChildren<SpriteRenderer>().sprite!=fresh&&!session.TryClaim(0,out reason),"gathering depletes and cannot repeat "+node.Definition.id);yield return Close();
        }
        using(var map=new RunMapModel())
        {Check(map.Refresh()&&map.Markers.Any(m=>m.Id=="interactive/"+ore.StableId&&m.Dimmed),"depleted gathering marker is dimmed on explored map");}
        yield return ImportantSaveFailure(npc);
        var oldTargets=RunInteractable.Active.OfType<RunContentInteractable>().ToArray();var oldSession=RunContentService.Current;
        for(int stage=1;stage<=3;stage++)
        {
            RunEncounter.CopyMapEnemies(enemies);foreach(var e in enemies)if(e!=null&&!e.IsDead)e.TakeDamage(99999);RunEncounter.RefreshNow();deadline=Time.realtimeSinceStartup+40;
            if(stage<3)
            {
                while((RunEncounter.CurrentSceneIndex!=stage+1||!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
                Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage+1,"next stage ready "+(stage+1));
                Check(RunInteractable.Active.OfType<RunContentInteractable>().Count()==4&&RunContentService.Current==null,"new map content replaces registry and sessions");
                if(stage==1)Check(oldTargets.All(x=>x==null),"all first-map content and Spine instances destroyed");
                RunEncounter.CopyMapEnemies(enemies);foreach(var e in enemies)e.enabled=false;
            }
            else{while(!(Top is RunSettlementPanel)&&Time.realtimeSinceStartup<deadline)yield return null;Check(Top is RunSettlementPanel,"three-map settlement reached");}
        }
        yield return Press(Key.Escape);deadline=Time.realtimeSinceStartup+40;
        while((!VillageSceneBootstrap.IsReady||!VillageSceneFlow.IsVillageScene)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"returned to physical village");yield return null;
        Check(!RunInteractable.Active.Any()&&RunContentService.Current==null,"run end clears content registry and ticket");
        Check(SaveSlotService.SavesDirectory==storage,"all saves remain in owned test directory");
    }
    static IEnumerator Guards(RunContentInteractable npc)
    {
        string reason;yield return Open(npc);var ticket=RunContentService.Current;var body=PlayerSpawner.CurrentPlayer.GetComponent<Rigidbody2D>();var original=body.position;
        body.position+=Vector2.right*10;Physics2D.SyncTransforms();Check(!ticket.TryClaim(0,out reason),"out-of-range ticket cannot claim");body.position=original;Physics2D.SyncTransforms();
        var blocker=new GameObject("OwnedInteractionLosBlocker");
        blocker.transform.position=(CombatQuery.Center(ticket.Actor.transform)+npc.InteractionPoint)*.5f;
        blocker.AddComponent<BoxCollider2D>().size=new Vector2(.10f,2);Physics2D.SyncTransforms();
        try{Check(!ticket.TryClaim(0,out reason),"new solid obstruction prevents a paused-panel claim");}finally{UnityEngine.Object.Destroy(blocker);}
        var current=typeof(PlayerSpawner).GetField("<CurrentPlayer>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        var controlled=PlayerSpawner.CurrentPlayer;
        try{current.SetValue(null,AllyController.All[0].gameObject);Check(!ticket.IsValid&&!ticket.TryClaim(0,out reason),"changing controlled actor invalidates old ticket");}
        finally{current.SetValue(null,controlled);}
        yield return null;
        yield return Close();Check(!ticket.IsValid&&!ticket.TryClaim(0,out reason),"closed ticket cannot claim");
        yield return Open(npc);var newer=RunContentService.Current;ticket.Close();Check(ReferenceEquals(RunContentService.Current,newer),"closing old ticket preserves newer interaction");
        npc.gameObject.SetActive(false);yield return null;yield return null;
        Check(RunContentService.Current==null&&!(Top is RunContentPanel)&&Time.timeScale==1,"deactivating target closes modal and restores time");npc.gameObject.SetActive(true);yield return null;
    }
    static IEnumerator ImportantSaveFailure(RunContentInteractable npc)
    {
        MoveNear(npc);yield return null;yield return null;
        var go=new GameObject("OwnedImportantRewardProbe");go.transform.SetParent(RunMapBootstrap.CurrentMapRoot.transform,false);go.transform.position=npc.transform.position;
        var content=go.AddComponent<RunContentInteractable>();
        content.ConfigureContent("test/important",new RunContentDefinition{id="important-test",kind=RunContentKind.Chest,
            name=new RunContentText{zhCN="重要道具验收",enUS="Important reward probe"},body=new RunContentText{zhCN="仅测试目录中的保存失败检查",enUS="Owned test-storage failure check"},
            rewards=new[]{new RunContentReward{itemId=11,minCount=1,maxCount=1}}},NaturalMapRuntime.Current);
        try
        {
            Check(new RunInteractionService(2.5f).TryExecute(PlayerSpawner.CurrentPlayer,content,out var opening),"important reward opens owned test ticket");
            var ticket=RunContentService.Current;
            string slot=Directory.GetFiles(storage,"Slot3.json").Single();
            using(var held=new FileStream(slot,FileMode.Open,FileAccess.Read,FileShare.None))
            {Check(!ticket.TryClaim(0,out var failed)&&!content.Completed&&!ticket.Rewards[0].Claimed&&!RunBagProgression.Owns(11),"locked owned slot: important reward/save failure does not consume content");}
            yield return null;
            Check(ticket.TryClaim(0,out var retried)&&content.Completed&&RunBagProgression.Owns(11),"important reward retries after save failure and gains permanent ownership");
            yield return Close();
        }
        finally{UnityEngine.Object.Destroy(go);}
        yield return null;
    }
}
#endif
