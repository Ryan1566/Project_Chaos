#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Integration checks use a new temporary slot; never read or copy real player saves.</summary>
public static class VillageFacilitiesVerification
{
    public static string VerifyAuthored()
    {
        if(Application.isPlaying)throw new Exception("Requires Edit Mode");
        var lines=new List<string>();
        Action<bool,string> assert=(ok,text)=>{if(!ok)throw new Exception(text);lines.Add("PASS "+text);};
        var settings=VillageFacilitySettings.Load();var village=VillageSceneSettings.Load();
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var root=scene.GetRootGameObjects().Single(g=>g.GetComponent<VillageSceneBootstrap>()!=null);
            var plots=root.GetComponentsInChildren<VillagePlot>(true);var facilities=root.GetComponentsInChildren<VillageFacility>(true);
            settings.Validate(village,plots);
            assert(plots.Length==4&&facilities.Length==3&&settings.facilities.All(d=>facilities.Count(f=>f.Id==d.id&&f.Kind==d.kind&&Vector3.Distance(f.transform.position,new Vector3(d.x,village.groundY+.03f,0))<.001f)==1),"saved anchors match all three definitions; four production plots preserved");
            assert(root.GetComponentsInChildren<Component>(true).All(c=>c!=null),"saved scene has no missing scripts");
            var texts=typeof(VillageFacilitySettings).GetFields().Where(f=>f.FieldType==typeof(VillageFacilityText)).Select(f=>(VillageFacilityText)f.GetValue(settings)).Concat(settings.facilities.SelectMany(f=>new[]{f.name,f.description}));
            assert(texts.All(t=>t!=null&&new[]{t.zhCN,t.enUS,t.zhTW,t.jaJP,t.koKR}.All(s=>!string.IsNullOrWhiteSpace(s))),"all facility labels and information have five populated languages");
            foreach(var problem in new[]{"duplicate id","out of bounds","overlap"})
            {
                var copy=UnityEngine.Object.Instantiate(settings);
                try
                {
                    if(problem=="duplicate id")copy.facilities[1].id=copy.facilities[0].id;
                    else if(problem=="out of bounds")copy.facilities[0].width=1000;else copy.facilities[1].x=copy.facilities[0].x;
                    bool rejected=false;try{copy.Validate(village,plots);}catch(InvalidOperationException){rejected=true;}
                    assert(rejected,"rejects "+problem);
                }
                finally {UnityEngine.Object.DestroyImmediate(copy);}
            }
            assert(Resources.Load<GameObject>("UIPanels/VillageOfficePanel")?.GetComponent<VillageOfficePanel>()!=null,"office prefab resource resolves");
        }
        finally
        {
            if(original.IsValid()&&original.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        }
        Directory.CreateDirectory(Application.dataPath+"/QA/VillageFacilities");File.WriteAllLines(Application.dataPath+"/QA/VillageFacilities/contracts.txt",lines);
        return string.Join("\n",lines);
    }
    public static string Status {get;private set;}="idle";
    static readonly StringBuilder log=new StringBuilder();
    static Keyboard keyboard;
    static string overrides,storage;
    static LocalizationSystem.LanguageType languageBefore;
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    static void Check(bool ok,string text){log.AppendLine((ok?"PASS ":"FAIL ")+text);Flush();if(!ok)throw new Exception(text);}
    static void Flush(){Directory.CreateDirectory(Application.dataPath+"/QA/VillageFacilities");File.WriteAllText(Application.dataPath+"/QA/VillageFacilities/flow.txt",log.ToString());}
    public static void Start()
    {
        if(!Application.isPlaying||Status=="running")throw new Exception("Requires idle Play");
        Status="running";log.Clear();storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(storage);
        SaveSlotService.UseEditorTestDirectory(storage);overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");
        languageBefore=LocalizationSystem.LocalizationManager.GetInstance().CurrentLanguage;
        keyboard=InputSystem.AddDevice<Keyboard>("ChaosFacilitiesKeyboard");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            bool more;object current=null;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
            catch(Exception e){Status="failed";log.AppendLine("FAIL "+e);Cleanup();Flush();yield break;}
            if(!more){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
        }
        Status="passed";Cleanup();log.AppendLine("DONE isolated storage="+storage);Flush();
    }
    static void Cleanup(){InputManager.Instance.LoadOverridesJson(overrides);LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(languageBefore);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.22f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.28f);}
    static void Click(string name){var button=Top.GetComponentsInChildren<Button>(false).First(b=>b.name==name&&b.interactable);ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
    static void Move(float x)
    {
        var world=VillageSceneBootstrap.Current;var body=world.Avatar.GetComponent<Rigidbody2D>();var old=body.position;
        body.position=new Vector2(x,world.Settings.groundY+.08f);world.Avatar.transform.position=body.position;body.velocity=Vector2.zero;Physics2D.SyncTransforms();GameplayCameraRig.NotifyTargetWarped(world.Avatar.transform,body.position-old);
    }
    static IEnumerator Open(string id)
    {
        Move(VillageFacilitySettings.Load().Get(id).x);yield return new WaitForSecondsRealtime(.2f);yield return Press(Key.E);
    }
    static IEnumerator WaitVillage()
    {
        float deadline=Time.realtimeSinceStartup+40;while((!VillageSceneBootstrap.IsReady||!VillageSceneFlow.IsVillageScene)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady&&VillageSceneFlow.IsVillageScene,"formal MainScene ready");
    }
    sealed class InventoryProbe:IGameplayView<InventoryState>
    {
        public event Action<PanelIntent> Intent;
        public InventoryState State;
        public void Render(InventoryState state){State=state;}
        public void ShowMessage(string message,bool success){} public void ClosePanel(){}
    }
    static InventoryState Snapshot(){var probe=new InventoryProbe();using(var presenter=new InventoryPresenter()){presenter.Attach(probe);return probe.State;}}
    static IEnumerator Run()
    {
        GameSession.Instance.Clear();var empty=Snapshot();Check(!empty.HasSession&&empty.Rows.Count==0&&!string.IsNullOrEmpty(empty.Hint),"warehouse with no session is empty and explicit");
        GameSession.Instance.BeginSandbox(new SavePayload{resMoney=1000,resGoods=1000,resMaterial=1000,resBlueprint=1000});for(int i=0;i<4;i++)RetainerService.Instance.RecruitRandom();
        var payload=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));GameSession.Instance.Clear();
        Check(SaveSlotService.Instance.Save(3,payload),"new owned temporary slot created");GameSession.Instance.Begin(3,payload,"MainScene");VillageSceneFlow.EnterFromSave();yield return WaitVillage();
        var world=VillageSceneBootstrap.Current;var settings=VillageFacilitySettings.Load();var model=new VillageWorldModel();
        Check(world.Plots.Length==4&&world.Workers.Length==4&&world.Facilities.Length==3,"four production plots and workers preserved; three civic facilities");
        settings.Validate(world.Settings,world.Plots);
        foreach(var f in world.Facilities)
        {
            int children=f.GetComponentsInChildren<Transform>(true).Length;f.Initialize(settings.Get(f.Id));f.Initialize(settings.Get(f.Id));
            Check(f.GetComponentsInChildren<Collider2D>().Length==0&&f.GetComponentsInChildren<Transform>(true).Length==children,"idempotent nonblocking facade "+f.Id);
            Move(f.Entrance.x);var state=model.Read();Check(state.NearbyFacilityId==f.Id&&state.NearbyPlot==0&&model.CanUseFacility(f.Id),"exclusive nearest facility "+f.Id);
        }
        var officeAnchor=world.Facilities.Single(f=>f.Id=="office");var officePosition=officeAnchor.transform.position;
        officeAnchor.transform.position=world.Plots[0].transform.position;Move(world.Plots[0].Entrance.x);
        Check(model.Read().NearbyPlot==2001&&!model.CanUseFacility("office"),"same-distance candidates resolve deterministically to one target");officeAnchor.transform.position=officePosition;
        Move(officeAnchor.Entrance.x);officeAnchor.enabled=false;Check(!model.CanUseFacility("office"),"disabled facility cannot be used");officeAnchor.enabled=true;
        Move(18);Check(!model.CanUseFacility("office"),"distant office cannot open");VillageOfficePanel.Open("office");Check(Top==null,"out of range open leaves UI stack empty");
        Move(world.Plots[0].Entrance.x);Check(model.Read().NearbyPlot==2001&&string.IsNullOrEmpty(model.Read().NearbyFacilityId),"production plot selection preserved");yield return Press(Key.E);Check(Top is VillagePanel,"production panel still opens with E");yield return Press(Key.Escape);Check(Top==null,"Esc closes production panel");
        string reason;var def=VillageService.Instance.GetDef(2001,1);var kind=VillageService.ResourceKindOf(def.CostResourceType);uint before=InventoryService.Instance.GetResource(kind);
        Check(VillageService.Instance.StartBuild(2001,out reason),"existing production building command accepted");
        Check(InventoryService.Instance.GetResource(kind)==before-def.CostAmount,"old production cost unchanged");
        Check(VillageService.Instance.CancelBuild(2001,out var refund,out reason)&&refund==def.CostAmount/2,"old production cancellation rule unchanged");
        yield return Open("office");Check(Top is VillageOfficePanel&&!InputManager.Instance.CanReadGameplayInput,"E opens office and blocks world input");
        var office=Top;int stackCount=UIManager.Instance.panelStack.Count;yield return Press(Key.E);yield return Press(Key.M);Check(Top==office&&UIManager.Instance.panelStack.Count==stackCount,"open office prevents duplicate entry and map hotkey");
        string snapshot=JsonUtility.ToJson(GameSession.Instance.LoadedPayload);Click("OfficeTax");yield return null;
        Check(Top==office&&!office.transform.Find("OfficeCard/OfficeTasks").gameObject.activeSelf,"tax secondary information page");
        yield return Press(Key.Escape);Check(Top==office&&office.transform.Find("OfficeCard/OfficeTasks").gameObject.activeSelf,"Esc from tax returns one level");
        Check(snapshot==JsonUtility.ToJson(GameSession.Instance.LoadedPayload),"tax page and back grant no income and mutate no payload");
        Click("OfficeTasks");yield return new WaitForSecondsRealtime(.3f);Check(Top is OfficeTasksPanel,"office opens current four-category task interface");yield return Press(Key.Escape);Check(Top==office,"tasks return to office");
        uint money=InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money);var counts=InventoryService.Instance.GetStacks().ToDictionary(s=>s.itemId,s=>s.count);
        Click("OfficeTrade");yield return new WaitForSecondsRealtime(.3f);Check(Top is MarketPanel,"office trade uses existing market");Click("Btn_Buy");yield return null;
        var added=InventoryService.Instance.GetStacks().FirstOrDefault(s=>s.count>(counts.TryGetValue(s.itemId,out var old)?old:0));
        Check(added!=null&&InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money)<money,"market button transaction changes shared inventory and money");
        uint purchased=added.itemId;yield return Press(Key.Escape);Check(Top==office,"market returns to office");yield return Press(Key.Escape);Check(Top==null,"office Esc returns village");
        yield return Open("family");Check(Top is FamilyTreePanel,"mansion opens real family tree");var node=FamilyTreeService.Instance.GetAll().First(n=>FamilyTreeService.Instance.CanUnlock(n.Index,out reason));Click("Light_"+node.Index);yield return null;Check(FamilyTreeService.Instance.IsUnlocked(node.Index),"family tree existing unlock remains functional");yield return Press(Key.Escape);
        uint important=InventoryService.Instance.GetAllDefinitions().First(d=>d.Enable&&d.RunCategory==1).Index;
        Check(RunBagProgression.AcquireImportant(important,out reason),"permanent important item registered by existing service");int used=InventoryService.Instance.UsedSlots;
        var stateBefore=Snapshot();Check(stateBefore.Rows.Count(r=>r.ItemId==important)==1&&stateBefore.Rows.Single(r=>r.ItemId==important).Permanent,"permanent collection shown once");Check(used==InventoryService.Instance.UsedSlots,"collection display consumes no ordinary slot");
        Check(stateBefore.Rows.Any(r=>r.ItemId==purchased)&&stateBefore.Resources.Contains(InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money).ToString()),"warehouse reads same market stock and resources");
        // Legacy duplicate and unknown ownership must still display once without inventory mutation.
        var live=GameSession.Instance.LoadedPayload;live.ownedImportantItems.Add(important);live.ownedImportantItems.Add(999999);live.items.Add(new ItemStackSave{itemId=important,count=1});
        var legacy=Snapshot();Check(legacy.Rows.Count(r=>r.ItemId==important)==1&&legacy.Rows.Single(r=>r.ItemId==important).Permanent&&legacy.Rows.Any(r=>r.ItemId==999999&&r.Permanent),"legacy duplicate/unknown permanent IDs remain visible and deduplicated");live.items.RemoveAll(s=>s.itemId==important);live.ownedImportantItems.Remove(999999);
        yield return Open("warehouse");Check(Top is InventoryPanel,"warehouse entity opens unified inventory");var warehouse=(InventoryPanel)Top;
        var rows=Enumerable.Range(0,30).Select(i=>new InventoryRow{ItemId=(uint)i,Name="Test "+i,Count="1/99"}).ToArray();var synthetic=new InventoryState{Title="Scroll QA",HasSession=true,Rows=rows};warehouse.Render(synthetic);Canvas.ForceUpdateCanvases();yield return null;
        var scroll=warehouse.GetComponent<ScrollRect>();Check(scroll!=null&&scroll.content.rect.height>scroll.viewport.rect.height&&scroll.viewport.GetComponent<RectMask2D>()!=null,"long inventory has clipped scrollable content");
        int childrenCount=scroll.content.childCount;scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();var last=(RectTransform)scroll.content.Find("Row29");var screen=last.TransformPoint(last.rect.center);Check(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(screen)),"last inventory row reachable by scrolling");
        synthetic.Rows=rows.Take(1).ToArray();warehouse.Render(synthetic);Check(scroll.content.childCount==childrenCount&&scroll.content.GetComponentsInChildren<TextMeshProUGUI>(false).Length==1,"refresh reuses cached widgets and hides surplus labels");
        warehouse.Render(Snapshot());
        foreach(var language in Enum.GetValues(typeof(LocalizationSystem.LanguageType)).Cast<LocalizationSystem.LanguageType>())
        {
            // Use the public language property through its supported setter method below.
            LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(language);yield return null;
            Check(warehouse.GetComponentsInChildren<TextMeshProUGUI>(false).Any(t=>t.text==settings.warehouseTitle.Resolve()),"warehouse localized title "+language);
        }
        LocalizationSystem.LocalizationManager.GetInstance().ChangeLanguage(LocalizationSystem.LanguageType.ChineseSimplified);
        yield return Press(Key.Escape);yield return Open("warehouse");Check(Top is InventoryPanel&&Top.GetComponents<ScrollRect>().Length==1,"cached warehouse reopens without duplicate scroll controls");yield return Press(Key.Escape);
        Move(settings.Get("office").x);var input=InputManager.Instance;var step=input.GetRebindSteps(InputManager.ActionVillageInteract,InputManager.BindingKind.Keyboard).Single();
        bool rebound=false;input.BeginRebind(InputManager.ActionVillageInteract,step.BindingIndex,InputManager.BindingKind.Keyboard,r=>rebound=true);yield return Press(Key.F);yield return new WaitForSecondsRealtime(.3f);
        Check(rebound&&!input.IsRebinding&&model.Read().Hint.Contains("F"),"facility hint follows rebound interaction key");
        yield return Press(Key.E);Check(Top==null,"previous interaction key no longer opens facility");yield return Press(Key.F);Check(Top is VillageOfficePanel,"new interaction key opens office");yield return Press(Key.Escape);input.LoadOverridesJson("");
        yield return Press(Key.M);Check(Top is WorldMapPanel,"M still opens world map from village");Click("EnterRunBtn");yield return new WaitForSecondsRealtime(.3f);foreach(var member in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(member.id,out reason);Click("Btn_Depart");
        for(int stage=1;stage<=3;stage++)
        {
            float deadline=Time.realtimeSinceStartup+40;while((RunEncounter.CurrentSceneIndex!=stage||!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage,"formal expedition stage ready "+stage);
            var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var enemy in enemies)if(enemy!=null&&!enemy.IsDead)enemy.TakeDamage(99999);RunEncounter.RefreshNow();
        }
        float end=Time.realtimeSinceStartup+40;while(!(Top is RunSettlementPanel)&&Time.realtimeSinceStartup<end)yield return null;Check(Top is RunSettlementPanel,"three maps reach existing settlement");yield return Press(Key.Escape);yield return WaitVillage();
        Check(world==null&&VillageSceneBootstrap.Current.Facilities.Length==3&&VillageSceneBootstrap.Current.Workers.Length==4,"return cleans old village and recreates only three civic facilities");
        if(Top is EventPanel)yield return Press(Key.Escape);
        yield return Open("office");Check(Top is VillageOfficePanel,"office works after expedition return");
        Check(EventService.Instance.HasPending,"existing return flow queued events");Click("OfficeTasks");yield return new WaitForSecondsRealtime(.3f);Check(Top is OfficeTasksPanel,"office opens task interface after return");var reports=Top.GetComponentsInChildren<Button>().Single(b=>b.name=="LegacyButton");reports.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Check(Top is EventPanel,"task interface opens existing pending event queue");yield return Press(Key.Escape);Check(Top is OfficeTasksPanel,"pending event panel returns to task interface");yield return Press(Key.Escape);yield return Press(Key.Escape);
        Check(SaveSlotService.SavesDirectory==storage,"all persistence stayed in owned temporary directory");
    }
}
#endif
