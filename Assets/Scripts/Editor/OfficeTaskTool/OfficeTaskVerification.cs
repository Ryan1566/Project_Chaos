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

/// <summary>使用独立临时存档验证领域事务、真实界面和三图配置加载，不读取真实玩家存档。</summary>
public static class OfficeTaskVerification
{
    public static string Status {get;private set;}="idle";
    static readonly StringBuilder Log=new StringBuilder();
    static string storage,overrides;
    static Keyboard keyboard;
    static LocalizationSystem.LanguageType language;
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    static SavePayload Payload=>GameSession.Instance.LoadedPayload;
    static void Check(bool value,string text){Log.AppendLine((value?"PASS ":"FAIL ")+text);Flush();if(!value)throw new Exception(text);}
    static void Flush(){Directory.CreateDirectory(Application.dataPath+"/QA/OfficeTasks");File.WriteAllText(Application.dataPath+"/QA/OfficeTasks/flow.txt",Log.ToString());}
    public static void Start()
    {
        if(!Application.isPlaying||Status=="running")throw new Exception("需要空闲的运行模式");
        Status="running";Log.Clear();storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(storage);SaveSlotService.UseEditorTestDirectory(storage);
        overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");keyboard=InputSystem.AddDevice<Keyboard>("OfficeTaskTestKeyboard");
        language=LocalizationSystem.LocalizationManager.GetInstance().CurrentLanguage;LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(LocalizationSystem.LanguageType.ChineseSimplified);
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            bool more;object value=null;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}
            catch(Exception error){Status="failed";Log.AppendLine("FAIL "+error);Cleanup();Flush();yield break;}
            if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Status="passed";Cleanup();Log.AppendLine("DONE isolated storage="+storage);Flush();
    }
    static void Cleanup(){InputManager.Instance.LoadOverridesJson(overrides);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(language);}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.2f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.3f);}
    static void ClickName(string name){Click(Top.GetComponentsInChildren<Button>(false).First(b=>b.name==name&&b.interactable));}
    static void ClickText(string prefix){Click(Top.GetComponentsInChildren<Button>(false).First(b=>b.interactable&&b.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.StartsWith(prefix,StringComparison.Ordinal))));}
    static void Click(Button button){ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
    static OfficeTaskSave Record(uint id)=>Payload.officeTasks.Single(r=>r.taskId==id);
    static string SlotPath()=>Directory.GetFiles(storage,"*.json").Single();
    static OfficeTaskTable_OfficeTaskConfig Definition(uint id,OfficeTaskCategory category,OfficeTaskGoal goal=OfficeTaskGoal.Choice)
    {return new OfficeTaskTable_OfficeTaskConfig{Index=id,Category=(uint)category,GoalType=(uint)goal,TitleText="隔离样例"+id,BodyText="仅用于独立验收",MinEra=1,MaxEra=5,Enable=true};}
    static OfficeTaskTable_OfficeTaskOptionConfig Option(uint id,uint task)=>new OfficeTaskTable_OfficeTaskOptionConfig{Index=id,TaskId=task,OptionText="隔离选项",Enable=true};
    static string BuildFixture(OfficeTaskCatalogSet set,List<OfficeTaskTable_OfficeTaskConfig> tasks,List<OfficeTaskTable_OfficeTaskOptionConfig> options,List<OfficeTaskTable_OfficeEraGateConfig> gates)
    {return (string)typeof(OfficeTaskCatalogSet).GetMethod("Build",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{set,tasks,options,gates});}
    static void Fixtures()
    {
        var county=Definition(70001,OfficeTaskCategory.County);
        var admin=Definition(70002,OfficeTaskCategory.Administration);admin.Repeatable=true;admin.CooldownSeconds=3;
        var delivery=Definition(70003,OfficeTaskCategory.Emergency,OfficeTaskGoal.ResourceDelivery);delivery.DurationSeconds=20;delivery.ResourceType=1;delivery.RequiredAmount=20;
        var clears=Definition(70004,OfficeTaskCategory.Preset,OfficeTaskGoal.ClearRuns);clears.RequiredClears=1;clears.Repeatable=true;clears.CooldownSeconds=2;
        var raid=Definition(70005,OfficeTaskCategory.Emergency,OfficeTaskGoal.VillageRaid);raid.DurationSeconds=4;
        var timeout=Definition(70006,OfficeTaskCategory.Emergency);timeout.DurationSeconds=2;timeout.FailureResourceType=4;timeout.FailureResourceAmount=3;timeout.FailureReputation=-2;
        var overflow=Definition(70007,OfficeTaskCategory.Preset);
        var pending=Definition(70008,OfficeTaskCategory.Preset);
        var tasks=new List<OfficeTaskTable_OfficeTaskConfig>{county,admin,delivery,clears,raid,timeout,overflow,pending};
        var options=tasks.Select((t,i)=>Option((uint)(71001+i),t.Index)).ToList();
        options[0].ReputationDelta=2;options[1].ReputationDelta=10;
        options[2].CostResourceType=1;options[2].CostAmount=3;options[2].RewardResourceType=4;options[2].RewardAmount=5;
        options[4].CostResourceType=1;options[4].CostAmount=2;options[4].RewardResourceType=4;options[4].RewardAmount=11;
        options[6].RewardResourceType=4;options[6].RewardAmount=uint.MaxValue;options[6].CostResourceType=1;options[6].CostAmount=1;
        var gates=new List<OfficeTaskTable_OfficeEraGateConfig>{new OfficeTaskTable_OfficeEraGateConfig{Index=1,Era=1,TaskId=county.Index,Enable=true}};
        var set=new OfficeTaskCatalogSet();string catalogError=BuildFixture(set,tasks,options,gates);Check(catalogError==null,"independent fixture catalog accepts current-era gate and emergency raid: "+catalogError);
        typeof(OfficeTaskCatalog).GetProperty("Current").SetValue(null,set);
        typeof(OfficeTaskCatalog).GetField("_loaded",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
        var payload=new SavePayload{resGoods=100,resMoney=100};Check(SaveSlotService.Instance.Save(3,payload),"owned fixture slot created");GameSession.Instance.Begin(3,payload,"MainScene");
        var service=OfficeTaskService.Instance;int context=GameSession.Instance.ContextId;string reason;
        Check(service.RefreshTriggers()&&Payload.officeTasks.Count==8,"four categories and all goal types activate once");
        Check(!service.CanAdvanceEra(1,out reason),"unfinished county assignment blocks configured current-era transition");
        Check(service.TryChoose(70001,1,71001,context,out reason),"county task settles through existing transaction");
        Check(service.CanAdvanceEra(1,out reason)&&Payload.reputation==2,"completed county task releases only task gate");
        string snapshot=JsonUtility.ToJson(Payload);Check(!service.TryChoose(70001,1,71001,context,out reason)&&snapshot==JsonUtility.ToJson(Payload),"county reward cannot be claimed twice");
        Check(!service.TryChoose(70008,1,71008,context-1,out reason),"stale context cannot settle task");
        Payload.resGoods=22;snapshot=JsonUtility.ToJson(Payload);Check(!service.TryChoose(70003,1,71003,context,out reason)&&snapshot==JsonUtility.ToJson(Payload),"delivery and same-resource option cost are prechecked together");
        Payload.resGoods=30;uint money=Payload.resMoney;Check(service.TryChoose(70003,1,71003,context,out reason)&&Payload.resGoods==7&&Payload.resMoney==money+5,"delivery deducts combined cost once and grants configured reward");
        Payload.reputation=int.MaxValue-1;Check(service.TryChoose(70002,1,71002,context,out reason)&&Payload.reputation==int.MaxValue,"reputation arithmetic saturates without signed overflow");
        snapshot=JsonUtility.ToJson(Payload);Check(!service.TryChoose(70007,1,71007,context,out reason)&&snapshot==JsonUtility.ToJson(Payload),"resource reward overflow rejects entire command before deduction");
        var copy=service.GetTasks(OfficeTaskCategory.Preset).First(t=>t.TaskId==70008);copy.Options[0].OptionText="更改快照";Check(OfficeTaskCatalog.GetOptions(70008)[0].OptionText=="隔离选项","snapshot option changes do not alter catalog");
        snapshot=JsonUtility.ToJson(Payload);
        using(var locked=new FileStream(SlotPath(),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            Check(!service.TryChoose(70008,1,71008,context,out reason)&&snapshot==JsonUtility.ToJson(Payload),"real owned-slot write failure rolls back task and payload");
        Check(service.TryChoose(70008,1,71008,context,out reason),"retry after releasing slot lock succeeds once");
        service.Tick(.5);Check(Payload.officeClockSeconds==.5,"ordinary clock advance is retained without due work");
        long reputation=Payload.reputation;money=Payload.resMoney;service.Tick(1.5);
        Check(Record(70006).status==OfficeTaskStatus.Failed&&Payload.resMoney==money-3&&Payload.reputation==reputation-2,"deadline applies configured signed penalty exactly once");
        money=Payload.resMoney;service.Tick(0);Check(Payload.resMoney==money,"expired task does not apply penalty again");
        Check(service.TryChoose(70005,1,71005,context,out reason),"raid preparation records choice without resolving battle");money=Payload.resMoney;service.Tick(2);
        Check(Record(70005).status==OfficeTaskStatus.AwaitingBattle&&Payload.resMoney==money,"raid deadline enters explicit pending battle without reward");
        Check(service.ReportBattleResult(70005,1,true,context,out reason)&&Payload.resMoney==money+11&&!service.ReportBattleResult(70005,1,true,context,out reason),"future battle bridge requires matching pending cycle and is idempotent");
        Check(Record(70002).cycle==2&&Record(70001).cycle==1&&Record(70001).completedCycles==1,"repeat cooldown opens one new cycle while county stays unique");
        Payload.clears=1;Check(service.TryChoose(70004,1,71004,context,out reason),"clear-run goal uses progress since activation");service.Tick(3);
        Check(Record(70004).cycle==2&&Record(70004).startedClears==1&&!service.TryChoose(70004,2,71004,context,out reason),"repeated clear task captures new progress baseline");
        double clock=Payload.officeClockSeconds;Check(!service.Tick(double.NaN)&&Payload.officeClockSeconds==clock,"invalid timer delta is rejected");
        Check(GameSession.Instance.SaveCurrent(out reason),"fixture clock and cycle saved");var saved=JsonUtility.FromJson<SaveSlotFile>(File.ReadAllText(SlotPath()));GameSession.Instance.Begin(3,saved.payload,"MainScene");
        Check(Payload.officeClockSeconds==clock&&Record(70001).completedCycles==1,"reading owned save restores clock and one-time receipt without offline advance");
        var old=Payload.officeTasks;Payload.officeTasks=null;snapshot=JsonUtility.ToJson(Payload);service.GetTasks(OfficeTaskCategory.County);Check(snapshot==JsonUtility.ToJson(Payload),"reading null legacy task list has no mutation");Payload.officeTasks=old;
        OfficeTaskCatalog.Reload();
    }
    static IEnumerator Run()
    {
        Fixtures();
        GameSession.Instance.BeginSandbox(new SavePayload{resMoney=1000,resGoods=1000,resMaterial=1000,resBlueprint=1000});for(int i=0;i<4;i++)RetainerService.Instance.RecruitRandom();
        var payload=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(Payload));GameSession.Instance.Clear();Check(SaveSlotService.Instance.Save(3,payload),"formal-flow isolated slot created");GameSession.Instance.Begin(3,payload,"MainScene");VillageSceneFlow.EnterFromSave();
        yield return WaitVillage();yield return new WaitForSecondsRealtime(.3f);
        Check(Payload.officeTasks.Any(t=>t.taskId==9001)&&Payload.officeTasks.Any(t=>t.taskId==9301),"formal village driver activates configured county and timed delivery tasks");
        bool asyncDone=false;int asyncCount=-1;ConfigLoader.Instance.LoadDataAsync<ItemTable_ItemConfig>(data=>{asyncDone=true;asyncCount=data?.datas.Count??-1;});float deadline=Time.realtimeSinceStartup+8;while(!asyncDone&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(asyncDone&&asyncCount==ConfigLoader.Instance.LoadData<ItemTable_ItemConfig>().datas.Count,"renamed resources load through identical sync and async type index");
        yield return Press(Key.E);Check(Top is VillageOfficePanel,"physical office E entry preserved");ClickName("OfficeTasks");yield return new WaitForSecondsRealtime(.3f);Check(Top is OfficeTasksPanel,"office opens four-category task panel");
        Check(Top.GetComponentsInChildren<Button>(false).Count(b=>b.name.StartsWith("Category",StringComparison.Ordinal))==4||Top.GetComponentsInChildren<Button>(false).Count(b=>b.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>new[]{"郡县指派事务","行政事件","预设任务","突发任务"}.Contains(t.text)))==4,"four categories are present in real UI");
        foreach(var text in Top.GetComponentsInChildren<TextMeshProUGUI>()){if(text.name=="Summary"||text.name=="DetailTitle"||text.name=="DetailProgress"||text.name=="Title"){text.ForceMeshUpdate();Check(text.rectTransform.rect.height>0&&text.textInfo.characterCount>0,"task heading actually produces visible text: "+text.name);}}
        yield return ViewContracts();
        ClickText("核实情况并提交回复");yield return new WaitForSecondsRealtime(.3f);Check(Record(9001).status==OfficeTaskStatus.Completed,"county task button persists completed result");
        ClickText("行政事件");yield return new WaitForSecondsRealtime(.3f);uint goods=Payload.resGoods;ClickText("会同里中核验界标");yield return new WaitForSecondsRealtime(.3f);Check(Payload.resGoods==goods-3&&Record(9101).completedCycles==1,"administration option uses configured real resource cost");
        ClickText("突发任务");yield return new WaitForSecondsRealtime(.3f);goods=Payload.resGoods;ClickText("交付军粮");yield return new WaitForSecondsRealtime(.3f);Check(Payload.resGoods==goods-20&&Record(9301).status==OfficeTaskStatus.Completed,"timed delivery completes through actual UI");
        double clock=Payload.officeClockSeconds;yield return new WaitForSecondsRealtime(1.3f);Check(Payload.officeClockSeconds>clock,"ordinary task panel does not freeze running countdown clock");
        yield return Press(Key.Escape);Check(Top is VillageOfficePanel,"Esc from task page returns office");yield return Press(Key.Escape);Check(Top==null,"Esc from office returns village");
        yield return Press(Key.Escape);Check(Top is PausePanel&&Time.timeScale==0,"global Esc opens actual pause menu");UIManager.Instance.PushPanel(PanelType.SettingPanel);clock=Payload.officeClockSeconds;yield return new WaitForSecondsRealtime(1.3f);Check(Payload.officeClockSeconds==clock,"pause stack and covered settings freeze task clock");yield return Press(Key.Escape);Check(Top is PausePanel&&Time.timeScale==0,"Esc from settings preserves pause below");yield return Press(Key.Escape);Check(Top==null&&Time.timeScale==1,"Esc closes pause and restores gameplay");yield return new WaitForSecondsRealtime(.3f);
        yield return Press(Key.M);Check(Top is WorldMapPanel&&Top.gameObject.activeInHierarchy,"world map still opens visibly after configuration rename and pause");ClickName("EnterRunBtn");yield return new WaitForSecondsRealtime(.3f);string reason;foreach(var member in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(member.id,out reason);ClickName("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();
        for(int stage=1;stage<=3;stage++)
        {
            deadline=Time.realtimeSinceStartup+40;while((RunEncounter.CurrentSceneIndex!=stage||!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage,"renamed configuration supports formal map "+stage);
            var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var enemy in enemies)enemy.enabled=false;
            if(stage==1){yield return Press(Key.I);clock=Payload.officeClockSeconds;yield return new WaitForSecondsRealtime(1.3f);Check(Payload.officeClockSeconds>clock,"ordinary paused in-run bag still advances task time");yield return Press(Key.Escape);}
            foreach(var enemy in enemies)if(enemy!=null&&!enemy.IsDead)enemy.TakeDamage(99999);RunEncounter.RefreshNow();
        }
        deadline=Time.realtimeSinceStartup+40;while(!(Top is RunSettlementPanel)&&Time.realtimeSinceStartup<deadline)yield return null;Check(Top is RunSettlementPanel,"three maps reach settlement after full config migration");yield return Press(Key.Escape);yield return WaitVillage();
        Check(Record(9001).completedCycles==1&&Record(9301).status==OfficeTaskStatus.Completed,"county and delivery receipts survive expedition and return");
        if(Top is EventPanel)yield return Press(Key.Escape);yield return Press(Key.E);ClickName("OfficeTasks");yield return new WaitForSecondsRealtime(.3f);Check(Top is OfficeTasksPanel,"task interface reopens after village reconstruction");
        ClickText("待处理来报");yield return new WaitForSecondsRealtime(.3f);Check(Top is EventPanel,"legacy event queue remains accessible from new task interface");yield return Press(Key.Escape);Check(Top is OfficeTasksPanel,"legacy event Esc returns task interface");
        yield return Press(Key.Escape);yield return Press(Key.Escape);Check(SaveSlotService.SavesDirectory==storage,"all persistence stayed inside owned temporary directory");
    }
    static IEnumerator WaitVillage(){float end=Time.realtimeSinceStartup+40;while((!VillageSceneBootstrap.IsReady||!VillageSceneFlow.IsVillageScene)&&Time.realtimeSinceStartup<end)yield return null;Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"formal MainScene ready");}

    /// <summary>独立构造长列表和长文案，检查展示组件复用、滚动与事件，不调用任务业务。</summary>
    static IEnumerator ViewContracts()
    {
        var go=new GameObject("OfficeViewContract",typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(Top.transform,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var view=go.AddComponent<OfficeTasksView>();uint clickedTask=0,clickedCycle=0,clickedOption=0;view.OptionSelected+=(task,cycle,option)=>{clickedTask=task;clickedCycle=cycle;clickedOption=option;};
        var state=new OfficeTasksDisplayState{Title="隔离长列表",Summary="声望 1　影响力 2　村庄发展度 3",CategoryLabels=new[]{"郡县指派事务","行政事件","预设任务","突发任务"},SelectedCategory=OfficeTaskCategory.County,SelectedTaskId=1,DetailTitle="长文验证",DetailProgress="进行中",DetailBody=string.Join("\n",Enumerable.Repeat("这是滚动正文的独立检查，不能产生任务或资源变更。",40)),BackLabel="返回",Rows=Enumerable.Range(1,30).Select(i=>new OfficeTaskDisplayRow{TaskId=(uint)i,Title="任务"+i,Status="进行中"}).ToArray(),Options=Enumerable.Range(1,12).Select(i=>new OfficeTaskDisplayOption{TaskId=1,Cycle=2,OptionId=(uint)i,Enabled=true,Text="独立选项 消耗物资 5\n声望+1　影响力+1　发展度+1"}).ToArray()};
        string before=JsonUtility.ToJson(Payload);view.Render(state);yield return null;Canvas.ForceUpdateCanvases();
        var scrolls=go.GetComponentsInChildren<ScrollRect>();Check(scrolls.Length==3&&scrolls.All(s=>s.content.rect.height>s.viewport.rect.height),"long task list, body and options each own a working scroll region");
        int count=go.GetComponentsInChildren<Transform>(true).Length;var list=scrolls.Single(s=>s.content.name=="ListContent");list.verticalNormalizedPosition=.4f;view.Render(state);Check(go.GetComponentsInChildren<Transform>(true).Length==count&&Math.Abs(list.verticalNormalizedPosition-.4f)<.01f,"countdown render reuses rows and preserves list scroll");
        go.GetComponentsInChildren<Button>().Single(b=>b.name=="Option0").onClick.Invoke();Check(clickedTask==1&&clickedCycle==2&&clickedOption==1&&before==JsonUtility.ToJson(Payload),"pure view emits exact task/cycle/option identity without business side effects");
        UnityEngine.Object.Destroy(go);yield return null;
    }
}
#endif
