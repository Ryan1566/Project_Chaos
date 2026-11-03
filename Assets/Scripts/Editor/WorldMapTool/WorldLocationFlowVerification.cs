#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.MVP.Model.WorldMap;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>只使用临时测试档，独立验证地点事务、任务接取与正式M键地图。</summary>
public static class WorldLocationFlowVerification
{
    public static string Status {get;private set;}="idle";
    public static bool Continue;
    static readonly StringBuilder Log=new StringBuilder();
    static Keyboard keyboard;
    static string overrides,storage;
    static SavePayload P=>GameSession.Instance.LoadedPayload;
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    static void Check(bool value,string message)
    {Log.AppendLine((value?"PASS ":"FAIL ")+message);Flush();if(!value)throw new Exception(message);}
    static void Flush()=>File.WriteAllText(Application.dataPath+"/QA/WorldLocations/flow.txt",Log.ToString());
    public static void Start()
    {
        if(!Application.isPlaying)throw new Exception("需要Play模式");
        Status="running";Continue=false;Log.Clear();storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));
        SaveSlotService.UseEditorTestDirectory(storage);overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");
        keyboard=InputSystem.AddDevice<Keyboard>("WorldLocationsQA");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0)
        {
            bool more;object next=null;
            try {more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
            catch(Exception e){Status="failed";Log.AppendLine("FAIL "+e);Cleanup();Flush();yield break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        Status="passed";Cleanup();Log.AppendLine("DONE isolated storage="+storage);Flush();
    }
    static void Cleanup()
    {if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);InputManager.Instance.LoadOverridesJson(overrides);}
    static IEnumerator Press(Key key)
    {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.15f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.25f);}
    static void Click(string name)
    {
        var button=Top.GetComponentsInChildren<Button>(false).First(b=>b.name==name&&b.interactable);
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    static void ClickText(string text)
    {
        var button=Top.GetComponentsInChildren<Button>(false).First(b=>b.interactable&&b.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains(text)));
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator UiChecks()
    {
        var service=WorldLocationService.Instance;
        var market=service.GetLocations(1,WorldLocationIdentity.Family).First(x=>x.Kind==WorldLocationKind.Trade&&x.CanTrade);
        Click("Location_"+market.Id);yield return null;
        var map=(WorldMapPanel)Top;var card=map.transform.Find("LocationDetail") as RectTransform;
        Check(card.gameObject.activeSelf&&card.rect.height>400,"actual marker click opens positive-height detail card");
        var scroll=card.GetComponentsInChildren<ScrollRect>(false).First(s=>s.name=="TradeViewport");
        Check(scroll.viewport.rect.height>50&&scroll.content.rect.width>250,"market has visible scroll viewport and full-width rows");
        uint item=service.GetMarketItems(market.Id)[0],held=MarketService.Instance.GetCount(item),money=P.resMoney,price=service.GetBuyPrice(market.Id,item);
        Click("Buy");Check(MarketService.Instance.GetCount(item)==held+1&&P.resMoney==money-price,"actual market buy button executes displayed price once");
        Click("Sell");Check(MarketService.Instance.GetCount(item)==held,"actual market sell button returns one item");
        var marker=map.transform.Find("WorldMapViewport/MapContent/LocationMarkers/Location_"+market.Id) as RectTransform;
        float size=marker.rect.width*marker.lossyScale.x;
        var nav=map.GetComponentInChildren<WorldMapNavigation>();nav.SetZoom(3,new Vector2(Screen.width*.4f,Screen.height*.5f));
        Check(Math.Abs(marker.rect.width*marker.lossyScale.x-size)<.1,"map marker screen size remains stable at three-times zoom");nav.ResetView();
        Click("Location_1002");Click("Tab_Official");
        Check(!card.GetComponentsInChildren<Button>(false).Any(b=>b.name=="OpenOffice"),"hostile Zhao detail exposes no official action button");
        Click("Tab_Family");
        var resource=service.GetLocations(1,WorldLocationIdentity.Family).First(x=>x.Kind==WorldLocationKind.Resource&&x.CanBuild);
        Click("Location_"+resource.Id);money=P.resMoney;Click("Build");
        Check(P.resMoney==money-50&&service.GetLocation(1,resource.Id,WorldLocationIdentity.Family).Built,"actual detail construction button creates facility once");
        WorldResourceService.Instance.Tick(60);var resourceState=service.GetLocation(1,resource.Id,WorldLocationIdentity.Family);held=MarketService.Instance.GetCount(resourceState.OutputItemId);
        Click("Harvest");Check(MarketService.Instance.GetCount(resourceState.OutputItemId)==held+1,"actual detail harvest button transfers concrete output to warehouse");
        var temporary=service.GetLocations(1,WorldLocationIdentity.Family).First(x=>x.Kind==WorldLocationKind.Temporary&&x.CanResolve);
        Click("Location_"+temporary.Id);Click("Resolve");
        Check(service.GetLocation(1,temporary.Id,WorldLocationIdentity.Family)==null,"actual temporary resolve button consumes discoverable point once");
        Click("Location_1001");Click("Tab_Official");Click("OpenOffice");yield return new WaitForSecondsRealtime(.3f);
        Check(Top is OfficeTasksPanel,"official action navigates to real existing task panel");
        ClickText("预设任务");ClickText("边地物资交接");ClickText("接取事务");
        Check(OfficeTaskService.Instance.GetTask(15001).Status==OfficeTaskStatus.Active,"actual task accept button starts map-bound task");
        yield return Press(Key.Escape);Check(Top is WorldMapPanel,"task panel Esc returns to world map");
        var eventPoint=new WorldLocationModel().Read().Locations.First(p=>p.Kind==WorldLocationKind.Task&&p.Name=="边地物资交接");
        Click("Location_"+eventPoint.Id);Click("Tab_Official");Click("OpenOffice");yield return new WaitForSecondsRealtime(.3f);
        Check(Top.GetComponentsInChildren<TextMeshProUGUI>(false).Any(t=>t.text=="边地物资交接"),"task marker reopens its own selected task");
        uint goods=P.resGoods;ClickText("交付物资");Check(OfficeTaskService.Instance.GetTask(15001).Status==OfficeTaskStatus.Completed&&P.resGoods==goods-10,"actual task delivery commits payment and completion");
        yield return Press(Key.Escape);
        Check(!new WorldLocationModel().Read().Locations.Any(p=>p.Kind==WorldLocationKind.Task&&p.Name=="边地物资交接"),"completed task marker disappears after returning");
        Click("Location_"+market.Id);yield return Press(Key.Escape);
        Check(Top is WorldMapPanel&&!Top.transform.Find("LocationDetail").gameObject.activeSelf,"first Esc dismisses detail and keeps map open");
        Click("Location_"+market.Id);
    }
    static void Domain()
    {
        GameSession.Instance.BeginSandbox(new SavePayload{resMoney=10000,resMaterial=10000,resGoods=10000,resBlueprint=10000});
        var service=WorldLocationService.Instance;service.Reload();InventoryService.Instance.Reload();
        int[] expected={7,1,8,19,11};int sum=0;
        for(uint era=1;era<=5;era++)
        {
            P.era=era;var points=service.GetLocations(era,WorldLocationIdentity.Family);
            var capitals=points.Where(x=>x.Kind==WorldLocationKind.Capital).ToArray();sum+=capitals.Length;
            Check(capitals.Length==expected[era-1]&&capitals.Select(x=>x.FactionId).Distinct().Count()==capitals.Length,"era "+era+" has one capital per current faction");
            Check(points.Count(x=>x.Kind==WorldLocationKind.Trade||x.Kind==WorldLocationKind.Tribe)==13,"era "+era+" retains twelve markets and one tribe");
            Check(points.Count(x=>x.Kind==WorldLocationKind.Resource)==10,"era "+era+" retains ten resource sites");
            Check(points.Select(x=>x.Id).Distinct().Count()==points.Count&&points.All(x=>x.Position.x>=0&&x.Position.x<=1&&x.Position.y>=0&&x.Position.y<=1),"era "+era+" IDs unique and coordinates bounded");
        }
        Check(sum==46,"five era capitals total 46 states");P.era=1;
        var family=service.GetLocations(1,WorldLocationIdentity.Family);
        var qin=service.GetLocations(1,WorldLocationIdentity.Official).First(x=>x.Kind==WorldLocationKind.Capital&&x.FactionId==1);
        var zhao=service.GetLocations(1,WorldLocationIdentity.Official).First(x=>x.Kind==WorldLocationKind.Capital&&x.FactionId==2);
        Check(qin.CanOpenOffice&&!zhao.CanOpenOffice&&!zhao.CanTrade,"Qin official authorized and Zhao public/private access blocked");
        string before=JsonUtility.ToJson(P);service.GetLocations(1,WorldLocationIdentity.Family);service.GetLocations(4,WorldLocationIdentity.Official);
        Check(before==JsonUtility.ToJson(P),"view and era preview queries never mutate save");
        Check(!service.TryBuy(zhao.Id,9101,out _)&&before==JsonUtility.ToJson(P),"direct hostile trade rejected atomically");
        var market=family.First(x=>x.Kind==WorldLocationKind.Trade&&x.CanTrade);
        uint item=service.GetMarketItems(market.Id).First(x=>!RunInventory.IsImportant(x));
        var tradeAction=WorldLocationCatalog.Action((uint)market.Kind);bool tradeEnabled=tradeAction.CanTrade;
        try{tradeAction.CanTrade=false;before=JsonUtility.ToJson(P);
            Check(!service.TryBuy(market.Id,item,out _)&&before==JsonUtility.ToJson(P),"disabled trade action rejects direct commands without payment");}
        finally{tradeAction.CanTrade=tradeEnabled;}
        uint price=service.GetBuyPrice(market.Id,item),cash=P.resMoney,count=MarketService.Instance.GetCount(item);
        Check(price==(uint)Math.Ceiling(InventoryService.Instance.GetDefinition(item).BuyPrice*market.BuyPercent/100d),"configured buy percentage rounds upward");
        Check(service.TryBuy(market.Id,item,out _)&&P.resMoney==cash-price&&MarketService.Instance.GetCount(item)==count+1,"buy takes exact discounted money and one item");
        uint sell=service.GetSellPrice(market.Id,item);cash=P.resMoney;
        Check(service.TrySell(market.Id,item,out _)&&P.resMoney==cash+sell&&MarketService.Instance.GetCount(item)==count,"sell retains base price and removes one item");
        P.resMoney=0;before=JsonUtility.ToJson(P);Check(!service.TryBuy(market.Id,item,out _)&&before==JsonUtility.ToJson(P),"insufficient money leaves full payload unchanged");P.resMoney=10000;
        var resource=family.First(x=>x.Kind==WorldLocationKind.Resource&&x.CanBuild&&service.GetLocation(4,x.Id,WorldLocationIdentity.Family)?.CanBuild!=true);
        var resourceAction=WorldLocationCatalog.Action((uint)resource.Kind);bool buildEnabled=resourceAction.CanBuild;
        try{resourceAction.CanBuild=false;before=JsonUtility.ToJson(P);
            Check(!WorldResourceService.Instance.Build(resource.Id,out _)&&before==JsonUtility.ToJson(P),"disabled construction action rejects direct commands without payment");}
        finally{resourceAction.CanBuild=buildEnabled;}
        uint money=P.resMoney,material=P.resMaterial;
        Check(WorldResourceService.Instance.Build(resource.Id,out _)&&P.resMoney==money-50&&P.resMaterial==material-20,"resource facility build deducts configured cost exactly once");
        before=JsonUtility.ToJson(P);Check(!WorldResourceService.Instance.Build(resource.Id,out _)&&before==JsonUtility.ToJson(P),"duplicate construction cannot charge twice");
        WorldResourceService.Instance.Tick(60);var point=service.GetLocation(1,resource.Id,WorldLocationIdentity.Family);
        Check(point.PendingOutput==1,"sixty active seconds produce one real item");
        bool harvestEnabled=resourceAction.CanHarvest;
        try{resourceAction.CanHarvest=false;before=JsonUtility.ToJson(P);
            Check(!WorldResourceService.Instance.Harvest(resource.Id,out _,out _)&&before==JsonUtility.ToJson(P),"disabled harvest action preserves warehouse and facility inventory");}
        finally{resourceAction.CanHarvest=harvestEnabled;}
        WorldResourceService.Instance.Tick(6000);point=service.GetLocation(1,resource.Id,WorldLocationIdentity.Family);
        Check(point.PendingOutput==10,"resource buffer caps at ten");
        P.items=new List<ItemStackSave>{new ItemStackSave{itemId=point.OutputItemId,count=98}};
        Check(WorldResourceService.Instance.Harvest(resource.Id,out uint amount,out _)&&amount==1&&MarketService.Instance.GetCount(point.OutputItemId)==99,"partial collection fills stack without losing remainder");
        Check(service.GetLocation(1,resource.Id,WorldLocationIdentity.Family).PendingOutput==9,"uncollected items remain at resource site");
        P.era=4;before=JsonUtility.ToJson(P);WorldResourceService.Instance.Tick(120);
        Check(!WorldResourceService.Instance.Harvest(resource.Id,out _,out _)&&JsonUtility.ToJson(P)==before,"era access loss freezes facility and prevents collection without confiscation");P.era=1;
        P.items.Clear();Check(WorldResourceService.Instance.Harvest(resource.Id,out _,out _),"restored access can collect retained goods");
        WorldResourceService.Instance.Tick(1);Check(service.GetLocation(1,resource.Id,WorldLocationIdentity.Family).PendingOutput==0,"full buffer never stores hidden production time");
        P.items.Clear();P.runs=5;P.clears=5;
        var temp=service.GetLocations(1,WorldLocationIdentity.Family).First(x=>x.Kind==WorldLocationKind.Temporary&&x.CanResolve);
        Check(service.ResolveTemporary(temp.Id,out _),"temporary point resolves through real inventory transaction");before=JsonUtility.ToJson(P);
        Check(!service.ResolveTemporary(temp.Id,out _)&&before==JsonUtility.ToJson(P),"repeat click cannot duplicate temporary reward");
        var clone=JsonUtility.FromJson<SavePayload>(before);GameSession.Instance.BeginSandbox(clone);
        Check(!service.ResolveTemporary(temp.Id,out _)&&JsonUtility.ToJson(P)==before,"temporary completion survives save round trip");
        OfficeTaskCatalog.Reload();OfficeTaskService.Instance.RefreshTriggers();
        var task=OfficeTaskService.Instance.GetTask(15001);
        Check(task!=null&&task.Status==OfficeTaskStatus.Available,"new map task waits for explicit acceptance");
        OfficeTaskService.Instance.Tick(400);task=OfficeTaskService.Instance.GetTask(15001);
        Check(task.Status==OfficeTaskStatus.Available&&task.RemainingSeconds==0,"unaccepted timed task has no running deadline");
        Check(OfficeTaskService.Instance.TryAccept(task.TaskId,task.Cycle,GameSession.Instance.ContextId,out _),"accept starts actual task cycle");
        task=OfficeTaskService.Instance.GetTask(15001);Check(task.Status==OfficeTaskStatus.Active&&task.RemainingSeconds==300,"acceptance starts configured remaining time");
        before=JsonUtility.ToJson(P);Check(!OfficeTaskService.Instance.TryAccept(task.TaskId,task.Cycle,GameSession.Instance.ContextId,out _)&&before==JsonUtility.ToJson(P),"duplicate acceptance does not reset deadline");
        var model=new WorldLocationModel();Check(model.Read().Locations.Any(x=>x.Kind==WorldLocationKind.Task),"accepted task creates real map marker");
        OfficeTaskService.Instance.Tick(301);Check(OfficeTaskService.Instance.GetTask(15001).Status==OfficeTaskStatus.Failed,"accepted task expires normally");
        Check(!model.Read().Locations.Any(x=>x.Kind==WorldLocationKind.Task&&x.Name==task.Title),"expired task marker removed");
        var legacy=JsonUtility.FromJson<SavePayload>("{\"era\":1,\"resMoney\":20,\"officeTasks\":[{\"taskId\":9001,\"cycle\":1,\"status\":0,\"deadlineClock\":123}]}");
        GameSession.Instance.BeginSandbox(legacy);Check(P.officeTasks[0].status==OfficeTaskStatus.Active&&P.officeTasks[0].deadlineClock==123,"legacy task state and deadline remain unchanged");
        var gated=new SavePayload{era=1,runs=5,clears=5};
        gated.worldLocations.Add(new WorldLocationSave{locationId=4001,completed=true});
        gated.officeTasks.Add(new OfficeTaskSave{taskId=15001,status=OfficeTaskStatus.Active});
        GameSession.Instance.BeginSandbox(gated);
        Check(service.GetLocation(1,4002,WorldLocationIdentity.Family)==null,"temporary point waits for configured task completion");
        P.officeTasks[0].status=OfficeTaskStatus.Completed;P.officeTasks[0].completedCycles=1;
        Check(service.GetLocation(1,4002,WorldLocationIdentity.Family)!=null,"completed prerequisite task reveals temporary point without granting reward");
    }
    static IEnumerator Run()
    {
        Domain();
        var payload=new SavePayload{resMoney=10000,resMaterial=10000,resGoods=10000,resBlueprint=10000,runs=1};
        Check(SaveSlotService.Instance.Save(3,payload),"owned formal save created");GameSession.Instance.Begin(3,payload,"MainScene");VillageSceneFlow.EnterFromSave();
        float deadline=Time.realtimeSinceStartup+90;
        while(!VillageSceneBootstrap.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"save loading reaches physical village");
        while(Top!=null)UIManager.Instance.PopPanel();yield return Press(Key.M);
        Check(Top is WorldMapPanel,"outside M opens formal world map");
        var map=(WorldMapPanel)Top;var view=map.GetComponent<WorldLocationsView>();
        Check(view!=null&&view.MarkerCount>=30,"formal map renders capital/trade/resource markers");
        var nav=map.GetComponentInChildren<WorldMapNavigation>(true);
        Check(nav!=null&&Math.Abs(nav.Zoom-1)<.01f,"map starts at one-to-one zoom");
        nav.SetZoom(100,new Vector2(Screen.width*.4f,Screen.height*.5f));Check(Math.Abs(nav.Zoom-3)<.01f,"zoom upper bound clamps to three");
        nav.SetZoom(.01f,new Vector2(Screen.width*.4f,Screen.height*.5f));Check(Math.Abs(nav.Zoom-1)<.01f,"zoom lower bound clamps to one");
        int count=view.MarkerCount;for(int i=0;i<5;i++){UIManager.Instance.PopPanel();yield return Press(Key.M);}
        map=(WorldMapPanel)Top;view=map.GetComponent<WorldLocationsView>();Check(view.MarkerCount==count,"five reopen cycles reuse markers without accumulation");
        yield return UiChecks();
        Status="ui-ready";while(!Continue)yield return null;Status="running";
        yield return Press(Key.Escape);Check(Top is WorldMapPanel,"detail returns before closing map");
        yield return Press(Key.Escape);Check(!(Top is WorldMapPanel),"Esc closes map when detail already closed");
        yield return Press(Key.Escape);Check(Top is PausePanel,"next Esc opens pause menu");double clock=P.officeClockSeconds;
        yield return new WaitForSecondsRealtime(1.3f);Check(P.officeClockSeconds==clock,"pause menu freezes shared gameplay clock");
        yield return Press(Key.Escape);yield return new WaitForSecondsRealtime(1.3f);Check(P.officeClockSeconds>clock,"clock resumes after pause");
        Check(UnityEngine.Object.FindObjectsOfType<WorldMapNavigation>(true).Length==1,"one navigation component survives cached panel lifecycle");
        yield return RunClockCheck();
    }
    static IEnumerator RunClockCheck()
    {
        while(Top!=null)UIManager.Instance.PopPanel();
        var member=RetainerService.Instance.RecruitRandom();var dispatch=DispatchService.Instance;dispatch.ResetSelection();
        Check(member!=null&&dispatch.Toggle(member.id,out _),"isolated clock test prepares real dispatched member");
        var site=P.worldLocations.Find(x=>x.built);site.productionSeconds=59;site.pendingOutput=0;
        double clock=P.officeClockSeconds;
        Check(dispatch.Depart(out _),"formal dispatch enters loading for resource-clock verification");
        bool frozen=true;float deadline=Time.realtimeSinceStartup+90;
        while(SceneTransitionCoordinator.IsBusy&&Time.realtimeSinceStartup<deadline)
        {if(P.officeClockSeconds!=clock)frozen=false;yield return null;}
        Check(frozen&&RunEncounter.IsSceneReady,"shared clock stays frozen throughout actual loading transition");
        yield return new WaitForSecondsRealtime(2.2f);
        Check(site.pendingOutput>=1&&P.officeClockSeconds>clock,"WorldScene advances resource production without opening map");
        Check(dispatch.ReturnToMeta(out _),"resource-clock test returns through formal village flow");
        deadline=Time.realtimeSinceStartup+60;
        while((!VillageSceneBootstrap.IsReady||SceneTransitionCoordinator.IsBusy)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene&&P.worldLocations.Exists(x=>x.built),"resource facility survives actual run-to-village transition");
        File.WriteAllText(Application.dataPath+"/QA/WorldLocations/clock.txt","PASS actual loading freezes shared clock\nPASS actual WorldScene production continues without opening map\nPASS formal return retains resource facility\n");
    }
}
#endif
