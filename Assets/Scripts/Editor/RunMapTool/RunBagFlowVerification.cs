#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Chaos.MVP.Model.Gameplay;

/// <summary>Play-only synthetic input and formal-scene smoke checks, with an owned temporary save.</summary>
public static class RunBagFlowVerification
{
    public static string Status { get; private set; } = "idle";
    private static readonly StringBuilder Report = new StringBuilder();
    private static Keyboard Keyboard;
    private static string Storage, Overrides;
    private static ItemConfig Engineering;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    private static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/RunBag"); File.WriteAllText(Application.dataPath + "/QA/RunBag/flow.txt",Report.ToString()); }
    private static void Isolation()
    { if (SaveSlotService.SavesDirectory != Storage || string.IsNullOrEmpty(Storage)) throw new Exception("Save isolation lost"); }
    public static void Start()
    {
        if (!Application.isPlaying || Status=="running") throw new Exception("Requires idle Play");
        Report.Clear(); Status="running";
        Storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Storage); SaveSlotService.UseEditorTestDirectory(Storage); Isolation();
        Overrides=InputManager.Instance.SaveOverridesJson();
        Keyboard=InputSystem.AddDevice<Keyboard>("ChaosBagFlowKeyboard");
        Engineering=new ItemConfig {Index=990001,RunCategory=2,StackLimit=50,Enable=true,NameKey="工程验证"};
        InventoryService.Instance.GetAllDefinitions().Add(Engineering);
        GameSession.Instance.BeginSandbox(new SavePayload {resMoney=1000,resGoods=1000,resMaterial=1000,resBlueprint=1000});
        for(int i=0;i<4;i++)RetainerService.Instance.RecruitRandom();
        var payload=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3,payload),"isolated slot created");
        GameSession.Instance.Begin(3,payload,"MainScene"); VillageSceneFlow.EnterFromSave();
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    private static IEnumerator Guard(IEnumerator source)
    {
        while(true)
        {
            object value;
            try
            {
                if(!source.MoveNext()) { Status="passed"; Cleanup(); Report.AppendLine("DONE storage="+Storage); Flush(); yield break; }
                value=source.Current;
            }
            catch(Exception e) {Status="failed"; Report.AppendLine("FAIL "+e);Cleanup();Flush();yield break;}
            yield return value;
        }
    }
    private static void Cleanup()
    {
        InputManager.Instance.LoadOverridesJson(Overrides);
        if(Keyboard!=null)InputSystem.RemoveDevice(Keyboard);
        InventoryService.Instance.GetAllDefinitions().Remove(Engineering);
    }
    private static void Key(params Key[] keys) { InputSystem.QueueStateEvent(Keyboard,new KeyboardState(keys)); }
    private static BasePanel Top => UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    private static void Click(string name)
    {
        var root=Top;
        var buttons=root==null?UnityEngine.Object.FindObjectsOfType<Button>():root.GetComponentsInChildren<Button>();
        var b=buttons.Single(x=>x.name==name);
        Canvas.ForceUpdateCanvases();
        var canvas=b.GetComponentInParent<Canvas>(); var rect=(RectTransform)b.transform;
        var data=new PointerEventData(EventSystem.current) {position=RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Check(hits.Any(h=>h.gameObject==b.gameObject||h.gameObject.transform.IsChildOf(b.transform)),"raycast "+name);
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Drag(RunBagSection from,int fromIndex,RunBagSection to,int toIndex)
    {
        var panel=(RunMenuPanel)Top;
        var slots=panel.GetComponentsInChildren<RunBagSlotView>();
        var a=slots.Single(s=>s.Section==from&&s.Index==fromIndex);var b=slots.Single(s=>s.Section==to&&s.Index==toIndex);
        var canvas=panel.GetComponentInParent<Canvas>();Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)b.transform;
        var data=new PointerEventData(EventSystem.current) {button=PointerEventData.InputButton.Left,pointerDrag=a.gameObject,
            position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Any(h=>h.gameObject==b.gameObject),"slot target raycast "+to+":"+toIndex);
        if(hits.Count>0)data.pointerPressRaycast=hits[0];
        ExecuteEvents.Execute(a.gameObject,data,ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(a.gameObject,data,ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(a.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static IEnumerator Run()
    {
        string reason;
        yield return new WaitForSecondsRealtime(4);
        Check(VillageSceneFlow.IsVillageScene&&!VillageSceneFlow.IsLoading,"formal MainScene ready");
        Key(UnityEngine.InputSystem.Key.M);yield return new WaitForSecondsRealtime(.6f);
        Check(Top is WorldMapPanel,"village M opens world map"); Key();yield return null;
        Click("EnterRunBtn");yield return new WaitForSecondsRealtime(.6f);
        Isolation();foreach(var r in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(r.id,out reason);
        Click("Btn_Depart");
        yield return new WaitForSecondsRealtime(5);
        Check(RunEncounter.CurrentSceneIndex==1&&RunMap.NaturalMapRuntime.IsReady,"formal WorldScene stage1 ready");
        Key(UnityEngine.InputSystem.Key.Tab);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        Check(Top is RunMenuPanel bag&&bag.SelectedTab==RunFunctionTab.Inventory&&Time.timeScale==0,"Tab opens bag and pauses");
        Check(InputManager.Instance.GameplayInputBlocked,"collection blocks gameplay input");
        Check(!RunTerrainToast.IsShowing,"collection hides paused terrain toast");
        Check(((RunMenuPanel)Top).GetComponentsInChildren<RunBagSlotView>(true).Length==49,"all dedicated maxima and 30 misc cells built");
        Check(RunInventory.AddItem(1,3,out reason)&&RunInventory.AddItem(5,2,out reason)&&RunInventory.AddItem(2,4,out reason)
            &&RunInventory.AddItem(990001,7,out reason),"loot reaches collection snapshot");
        yield return null;
        Drag(RunBagSection.Misc,0,RunBagSection.Recovery,0);
        Check(RunInventory.GetSlot(RunBagSection.Recovery,0)?.itemId==1,"pointer drag equips recovery stack");
        Drag(RunBagSection.Misc,1,RunBagSection.Recovery,0);
        Check(RunInventory.GetSlot(RunBagSection.Recovery,0)?.itemId==5&&RunInventory.GetSlot(RunBagSection.Misc,1)?.itemId==1,"pointer drag swaps occupied stack");
        Drag(RunBagSection.Misc,2,RunBagSection.Recovery,0);
        Check(RunInventory.GetSlot(RunBagSection.Recovery,0)?.itemId==5&&RunInventory.GetSlot(RunBagSection.Misc,2)?.itemId==2,"wrong category pointer drop rejected");
        Drag(RunBagSection.Misc,3,RunBagSection.Engineering,0);
        Check(RunInventory.GetSlot(RunBagSection.Engineering,0)?.count==7,"engineering pointer drag equips whole stack");
        ScreenCapture.CaptureScreenshot(Application.dataPath+"/QA/RunBag/Inventory-populated.png");
        Key(UnityEngine.InputSystem.Key.M);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        Check(Top is RunMenuPanel map&&map.SelectedTab==RunFunctionTab.Map&&!UnityEngine.Object.FindObjectsOfType<WorldMapPanel>().Any(w=>w.IsShowing),
            "run M opens local map placeholder only");
        Key(UnityEngine.InputSystem.Key.M);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        Check(!(Top is RunMenuPanel)&&Time.timeScale==1,"M closes local map and resumes");
        Key(UnityEngine.InputSystem.Key.Tab);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        ((RunMenuPanel)Top).SelectTab(RunFunctionTab.Map);
        Click("Settings");yield return new WaitForSecondsRealtime(.6f);
        Check(Top is SettingPanel&&Time.timeScale==0,"settings cover keeps collection pause");
        Click("Btn_Keybind");yield return new WaitForSecondsRealtime(.6f);
        var settings=(SettingPanel)Top;
        typeof(KeybindSettingsPage).GetMethod("OpenBindingsPage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
            .Invoke(settings.GetComponentsInChildren<KeybindSettingsPage>(true).Single(),null);
        yield return null;
        var page=settings.GetComponentsInChildren<KeybindBindingsPage>(true).Single();
        // Bindings subpage initialization is owned by the existing settings navigation.
        var show=page.GetType().GetMethod("EnsureBound",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        Check(!settings.GetComponentsInChildren<Transform>().Any(t=>t.name=="MouseButton"&&t.gameObject.activeInHierarchy),"settings exposes no second binding button");
        Check(settings.GetComponentsInChildren<SettingRow_Keybind>(true).Any(r=>r.settingId==SettingIds.Functions),"settings includes collection binding row");
        Check(!settings.GetComponentsInChildren<Transform>().Any(t=>t.name=="ReservedKeyHint"&&t.gameObject.activeInHierarchy),"obsolete fixed-space hint hidden");
        ScreenCapture.CaptureScreenshot(Application.dataPath+"/QA/RunBag/Single-binding-settings.png");
        yield return new WaitForSecondsRealtime(.2f);
        UIManager.Instance.PopPanel();yield return new WaitForSecondsRealtime(.6f);
        Check(Top is RunMenuPanel restored&&restored.SelectedTab==RunFunctionTab.Map&&Time.timeScale==0,"closing settings restores selected tab and collection pause");
        var steps=InputManager.Instance.GetRebindSteps(InputManager.ActionFunctions,InputManager.BindingKind.Keyboard);
        bool finished=false;
        InputManager.Instance.BeginRebind(InputManager.ActionFunctions,steps[0].BindingIndex,InputManager.BindingKind.Keyboard,r=>finished=true);
        Key(UnityEngine.InputSystem.Key.F1);yield return new WaitForSecondsRealtime(.4f);Key();yield return new WaitForSecondsRealtime(.6f);
        Check(finished&&InputManager.Instance.GetBindingPath(InputManager.ActionFunctions,InputManager.BindingKind.Keyboard)=="<Keyboard>/f1","collection action rebind replaces one key");
        Key(UnityEngine.InputSystem.Key.Tab);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        Check(Top is RunMenuPanel,"old Tab no longer triggers after rebind");
        Key(UnityEngine.InputSystem.Key.F1);yield return new WaitForSecondsRealtime(.6f);Key();yield return null;
        Check(!(Top is RunMenuPanel)&&Time.timeScale==1,"new F1 closes collection and resumes");
        InputManager.Instance.LoadOverridesJson(Overrides);yield return null;
        for(int scene=1;scene<=3;scene++)
        {
            Check(RunEncounter.CurrentSceneIndex==scene,"stage index "+scene);
            Check(RunInventory.GetSlot(RunBagSection.Engineering,0)?.itemId==990001&&RunInventory.GetCount(990001)==7,"bag layout/count retained stage "+scene);
            RunEncounter.KillAllEnemiesInScene();RunEncounter.RefreshNow();
            if(scene<3) {RunEncounter.ProcessPendingScene();yield return new WaitForSecondsRealtime(4);}
        }
        yield return new WaitForSecondsRealtime(.3f);
        Check(RunEncounter.RunCleared&&Top is RunSettlementPanel,"stage3 reaches formal settlement");
        Isolation();
        Check(RunSettlementModel.Current.Confirm(true,out reason)&&RunSettlementModel.Current.ReturnToMeta(out reason),"settlement saves and returns");
        yield return new WaitForSecondsRealtime(4);
        Check(VillageSceneFlow.IsVillageScene&&Time.timeScale==1&&RunInventory.UsedSlots==0,"return restores village and clears temporary inventory");
    }
}
#endif
