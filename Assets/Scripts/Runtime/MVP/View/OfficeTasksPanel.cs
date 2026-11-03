using System;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>将可复用的任务展示组件接入已有面板栈与MVP生命周期。</summary>
public sealed class OfficeTasksPanel : BasePanel, IOfficeTasksView
{
    OfficeTasksView content;
    OfficeTasksModel model;
    OfficeTasksPresenter presenter;
    float nextRefresh;
    public event Action<OfficeTaskCategory> CategorySelected {add{content.CategorySelected+=value;}remove{content.CategorySelected-=value;}}
    public event Action<uint> TaskSelected {add{content.TaskSelected+=value;}remove{content.TaskSelected-=value;}}
    public event Action<uint,uint,uint> OptionSelected {add{content.OptionSelected+=value;}remove{content.OptionSelected-=value;}}
    public event Action BackRequested {add{content.BackRequested+=value;}remove{content.BackRequested-=value;}}
    public event Action LegacyEventsRequested {add{content.LegacyEventsRequested+=value;}remove{content.LegacyEventsRequested-=value;}}
    public static void Open(string facilityId)
    {
        var world=new VillageWorldModel();
        if(!world.CanUseFacility(facilityId)||VillageFacilitySettings.Load().Get(facilityId)?.kind!=VillageFacilityKind.Office)return;
        var stack=UIManager.Instance.panelStack;
        if(stack.Count==0||!(stack.Peek() is VillageOfficePanel))return;
        var panel=UIManager.Instance.PushPanel(PanelType.OfficeTasksPanel) as OfficeTasksPanel;
        if(panel!=null)panel.Bind(facilityId);
    }
    /// <summary>世界地图官员入口独立验证所在地权限，不借用村庄建筑的物理交互限制。</summary>
    public static void OpenFromWorldMap(uint locationId,uint selectedTask=0)
    {
        if(!GameSession.Instance.HasSession||!VillageSceneFlow.IsVillageScene||!VillageSceneBootstrap.IsReady)return;
        var stack=UIManager.Instance.panelStack;
        if(stack.Count==0||!(stack.Peek() is WorldMapPanel))return;
        var location=WorldLocationService.Instance.GetLocation(GameSession.Instance.LoadedPayload.era,locationId,WorldLocationIdentity.Official);
        if(location==null||!location.CanOpenOffice)return;
        var panel=UIManager.Instance.PushPanel(PanelType.OfficeTasksPanel) as OfficeTasksPanel;
        if(panel!=null)panel.Bind(null,locationId,selectedTask);
    }
    void Awake()
    {
        var backdrop=GetComponent<Image>();if(backdrop!=null)backdrop.color=new Color(0,0,0,.7f);
        content=gameObject.AddComponent<OfficeTasksView>();
    }
    void Bind(string id,uint locationId=0,uint taskId=0)
    {presenter?.Dispose();model=new OfficeTasksModel(id,locationId,taskId);presenter=new OfficeTasksPresenter(model);presenter.Attach(this);}
    public override void OnEnter()
    {CursorEnable();if(model!=null){presenter?.Dispose();presenter=new OfficeTasksPresenter(model);presenter.Attach(this);}}
    public override void OnExit()
    {presenter?.Dispose();presenter=null;if(uiManager==null||!uiManager.panelStack.Contains(this))model=null;}
    void Update()
    {if(IsShowing&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;presenter?.Refresh();}}
    public override void HandleBack(){CloseTasks();}
    public void Render(OfficeTasksDisplayState state){content.Render(state);}
    public void CloseTasks(){var stack=UIManager.Instance.panelStack;if(stack.Count>0&&stack.Peek()==this)UIManager.Instance.PopPanel();}
    void OnDestroy(){presenter?.Dispose();}
}
