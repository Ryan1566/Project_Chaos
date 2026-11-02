#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;
using Chaos.MVP.Model.Gameplay;

/// <summary>仅使用新建隔离目录验证编队、保存失败与真实场景入口，不读取玩家存档。</summary>
public static class DispatchUiVerification
{
    public static string Status { get; private set; }="idle";
    public static bool Continue;
    public static uint[] MemberIds;
    private static string _storage,_overrides;
    private static Keyboard _keyboard;
    private static LocalizationSystem.LanguageType _language;
    private static readonly StringBuilder Log=new StringBuilder();
    private static SavePayload P=>GameSession.Instance.LoadedPayload;
    private static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    public static void Check(bool value,string text)
    { Log.AppendLine((value?"PASS ":"FAIL ")+text);Flush();if(!value)throw new Exception(text); }
    private static void Flush() { File.WriteAllText(Application.dataPath+"/QA/DispatchUI/flow.txt",Log.ToString()); }
    public static void Start()
    {
        if(!Application.isPlaying||Status=="running")throw new Exception("需要空闲Play模式");
        Log.Clear();Status="running";Continue=false;
        _storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));SaveSlotService.UseEditorTestDirectory(_storage);
        _overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");_keyboard=InputSystem.AddDevice<Keyboard>("DispatchUiQA");
        var manager=LocalizationSystem.LocalizationManager.GetInstance();_language=manager.CurrentLanguage;manager.ChangeLanguage(LocalizationSystem.LanguageType.ChineseSimplified);
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    private static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            bool more;object current=null;
            try { more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current; }
            catch(Exception error) { Status="failed";Log.AppendLine("FAIL "+error);Cleanup();Flush();yield break; }
            if(!more){stack.Pop();continue;}if(current is IEnumerator child)stack.Push(child);else yield return current;
        }
        Status="passed";Cleanup();Log.AppendLine("DONE isolated storage="+_storage);Flush();
    }
    private static void Cleanup()
    { InputManager.Instance.LoadOverridesJson(_overrides);if(_keyboard!=null&&_keyboard.added)InputSystem.RemoveDevice(_keyboard);LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(_language); }
    private static IEnumerator Press(Key key)
    { InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.15f);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.3f); }
    private static void Click(string name)
    { var button=Top.GetComponentsInChildren<Button>(false).First(x=>x.name==name&&x.interactable);ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler); }
    private static IEnumerator OpenDispatch()
    { while(Top is EventPanel)yield return Press(Key.Escape);if(Top is DispatchPanel)yield break;if(!(Top is WorldMapPanel))yield return Press(Key.M);Check(Top is WorldMapPanel,"village M opens world map");Click("EnterRunBtn");yield return new WaitForSecondsRealtime(.3f);Check(Top is DispatchPanel,"world map opens new dispatch interface"); }
    private static IEnumerator WaitVillage()
    { float end=Time.realtimeSinceStartup+45;while((!VillageSceneBootstrap.IsReady||!VillageSceneFlow.IsVillageScene)&&Time.realtimeSinceStartup<end)yield return null;Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"formal village ready"); }
    private static IEnumerator WaitRun(int stage)
    { float end=Time.realtimeSinceStartup+50;while((!RunEncounter.IsSceneReady||RunEncounter.CurrentSceneIndex!=stage||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage,"formal world map ready "+stage); }
    private static void DraftChecks()
    {
        var service=DispatchService.Instance;
        Check(!service.CanDepartWithReason(out _)&&!service.CanDepart,"empty team is consistently disabled");
        string file=SaveSlotService.Instance.GetSlotPath(3),saved=File.ReadAllText(file);
        Check(!service.Toggle(MemberIds[6],out _)&&!service.Toggle(MemberIds[7],out _),"wounded and low-confidence recruits cannot join");
        foreach(uint id in MemberIds.Take(4))Check(service.Toggle(id,out _),"join healthy member "+id);
        var before=service.SelectedIds.ToArray();Check(!service.Toggle(MemberIds[4],out _)&&service.SelectedIds.SequenceEqual(before),"fifth member rejected without replacing existing member");
        Check(service.SetControlled(MemberIds[2],out _)&&service.SelectedIds.SequenceEqual(new[]{MemberIds[2],MemberIds[0],MemberIds[1],MemberIds[3]}),"controlled member moves first, other order stable");
        before=service.SelectedIds.ToArray();Check(service.SetControlled(MemberIds[2],out _)&&service.SelectedIds.SequenceEqual(before),"repeated controlled selection does not reorder twice");
        Check(!service.SetControlled(MemberIds[5],out _)&&service.SelectedIds.SequenceEqual(before),"non-team member cannot become controlled implicitly");
        var injured=RetainerService.Instance.GetAll().First(x=>x.id==MemberIds[0]);injured.wounded=true;injured.woundedRemainSeconds=5000;
        Check(!service.CanDepartWithReason(out _)&&!service.CanDepart,"selected member losing eligibility blocks departure immediately");
        Check(service.Toggle(injured.id,out _),"ineligible selected member remains removable");injured.wounded=false;injured.woundedRemainSeconds=0;
        service.ResetSelection();service.SelectedIds.Add(MemberIds[0]);service.SelectedIds.Add(MemberIds[0]);Check(!service.CanDepartWithReason(out _),"duplicate draft ID cannot depart");
        service.ResetSelection();Check(service.Toggle(MemberIds[0],out _)&&service.CanDepartWithReason(out _)&&service.CanDepart,"one healthy member can depart without extra penalty gate");
        Check(File.ReadAllText(file)==saved,"draft selection and controlled ordering never write the owned save");service.ResetSelection();
    }
    private static void UiChecks()
    {
        var service=DispatchService.Instance;service.ResetSelection();MetaRuntimeDriver.NotifyChanged();
        var model=new DispatchModel();var all=model.Read(DispatchFilter.All,"",MemberIds[0]);
        Check(all.Cards.Length==8&&all.AvailableCount==6&&all.Slots.Length==4,"model adapts eight cards, six available and exactly four slots");
        Check(model.Read(DispatchFilter.Available,"",MemberIds[0]).Cards.Length==6,"available filter excludes both eligibility failures");
        Check(model.Read(DispatchFilter.Team,"",MemberIds[0]).Cards.Length==0,"empty draft team filter has no cards");
        Click("Card_"+MemberIds[1]);Check(service.SelectedCount==0,"clicking a candidate views details without joining");
        Click("Btn_Join");Check(service.SelectedIds.SequenceEqual(new[]{MemberIds[1]}),"detail join button adds first controlled member");
        Click("Card_"+MemberIds[0]);Click("Btn_Join");Check(service.SelectedCount==2,"detail joins second member without replacing first");
        Click("Btn_SetControlled");Check(service.SelectedIds.SequenceEqual(new[]{MemberIds[0],MemberIds[1]}),"actual set-controlled button reorders existing team");
        var controlled=model.Read(DispatchFilter.All,"",MemberIds[0]).Detail;
        Check(controlled.Hp==110&&controlled.Attack==15&&controlled.DefenseReduction==5,"controlled preview reflects actual family-buffed spawn stats");
        var ai=model.Read(DispatchFilter.All,"",MemberIds[1]).Detail;
        Check(ai.Attack==11&&Mathf.Abs(ai.Speed-(RunMap.RunBehaviorSettings.Load("Ally").moveSpeed+2))<.01f,"AI preview uses AI attack and navigation base rather than unused template attributes");
        Click("Card_"+MemberIds[6]);Check(!Top.GetComponentsInChildren<Button>().First(x=>x.name=="Btn_Join").interactable,"wounded member remains viewable but cannot join through UI");
        Click("Card_"+MemberIds[7]);Check(!Top.GetComponentsInChildren<Button>().First(x=>x.name=="Btn_Join").interactable,"low confidence member is visible with disabled join");
        Click("Filter_Team");Check(model.Read(DispatchFilter.Team,"",MemberIds[0]).Cards.Length==2,"selected team filter tracks draft membership");Click("Filter_All");
        var input=Top.GetComponentInChildren<TMP_InputField>();
        if(input!=null){input.text="__no_such_retainer__";Check(model.Read(DispatchFilter.All,input.text,MemberIds[0]).Cards.Length==0,"search can represent a readable no-results state");input.text="";}
        var scroll=Top.GetComponentsInChildren<ScrollRect>().FirstOrDefault();
        if(scroll!=null){scroll.verticalNormalizedPosition=.2f;float before=scroll.verticalNormalizedPosition;MetaRuntimeDriver.NotifyChanged();Check(Mathf.Abs(scroll.verticalNormalizedPosition-before)<.01f,"same-roster timer refresh preserves scroll position");}
        var eventField=typeof(MetaRuntimeDriver).GetField("Changed",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        int count=((Delegate)eventField.GetValue(null))?.GetInvocationList().Length??0;
        for(int i=0;i<3;i++){UIManager.Instance.PopPanel();UIManager.Instance.PushPanel(PanelType.DispatchPanel);}
        Check((((Delegate)eventField.GetValue(null))?.GetInvocationList().Length??0)==count,"cached panel reopens do not accumulate meta subscriptions");
        service.ResetSelection();MetaRuntimeDriver.NotifyChanged();
    }
    private static IEnumerator Run()
    {
        GameSession.Instance.BeginSandbox(new SavePayload{resGoods=10000,resMoney=10000,resMaterial=10000,resBlueprint=10000});
        for(int i=0;i<8;i++)RetainerService.Instance.RecruitRandom();var fixture=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(P));
        MemberIds=fixture.retainers.Select(x=>x.id).ToArray();Check(MemberIds.Length==8,"owned eight-member fixture created");
        var empty=new SavePayload{resGoods=10000,resMoney=10000,resMaterial=10000,resBlueprint=10000};Check(SaveSlotService.Instance.Save(3,empty),"empty roster owned save created");GameSession.Instance.Begin(3,empty,"MainScene");VillageSceneFlow.EnterFromSave();yield return WaitVillage();yield return OpenDispatch();
        Check(!Top.GetComponentsInChildren<Button>().First(x=>x.name=="Btn_Depart").interactable&&P.retainers.Count==0,"empty roster disables departure without automatic recruitment");
        P.retainers=fixture.retainers;P.retainers[6].wounded=true;P.retainers[6].woundedRemainSeconds=5000;P.retainers[7].confidence=10;
        P.familyTreeNodes=new List<uint>{301,401,402,501};EffectAggregator.Instance.MarkDirty();Check(GameSession.Instance.SaveCurrent(out _),"owned mixed-status roster fixture saved");
        DraftChecks();MetaRuntimeDriver.NotifyChanged();yield return null;Status="ui-ready";while(!Continue)yield return null;Status="running";UiChecks();
        var service=DispatchService.Instance;service.ResetSelection();Check(service.Toggle(MemberIds[0],out _),"single-person formal team prepared");MetaRuntimeDriver.NotifyChanged();yield return null;
        string payload=JsonUtility.ToJson(P);
        using(var locked=new FileStream(SaveSlotService.Instance.GetSlotPath(3),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        { Click("Btn_Depart");Check(!service.IsInRun&&!RunSceneFlow.IsTransitioning&&JsonUtility.ToJson(P)==payload,"actual departure button save failure keeps scene and payload unchanged"); }
        Click("Btn_Depart");yield return WaitRun(1);Check(service.RunTeam.Count==1,"one-person team reaches actual world scene");
        UIManager.Instance.PushPanel(PanelType.DispatchPanel);yield return null;Check(!Top.GetComponentsInChildren<Button>(true).First(x=>x.name=="Btn_Depart").gameObject.activeInHierarchy,"in-run dispatch hides departure and cannot depart a second time");Click("Btn_Return");yield return WaitVillage();
        yield return OpenDispatch();service.ResetSelection();foreach(uint id in MemberIds.Take(4))service.Toggle(id,out _);Check(service.SetControlled(MemberIds[2],out _),"four-person controlled order prepared");var order=service.SelectedIds.ToArray();MetaRuntimeDriver.NotifyChanged();yield return null;Click("Btn_Depart");
        for(int stage=1;stage<=3;stage++)
        {
            yield return WaitRun(stage);
            Check(service.RunTeam.SequenceEqual(order)&&Chaos.Gameplay.RunTeam.RunTeamProgress.MemberIds.SequenceEqual(order),"saved and spawned team order agrees on map "+stage);
            Check(PlayerSpawner.CurrentPlayer!=null&&AllyController.All.Count(a=>a!=null&&RunEncounter.OwnsRuntimeObject(a.gameObject))==3,"one controlled plus three AI spawned on map "+stage);
            var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var enemy in enemies){enemy.enabled=false;if(!enemy.IsDead)enemy.TakeDamage(99999);}RunEncounter.RefreshNow();
        }
        float end=Time.realtimeSinceStartup+45;while(!(Top is RunSettlementPanel)&&Time.realtimeSinceStartup<end)yield return null;Check(Top is RunSettlementPanel,"four-person expedition reaches settlement");yield return Press(Key.Escape);yield return WaitVillage();
        Check(P.runs==2,"failed departure not counted, one-person and four-person expeditions counted once each");
    }
}
#endif
