using System;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Physical village office entry. The existing UI stack owns world-input blocking and back navigation.</summary>
public sealed class VillageOfficePanel : BasePanel,IVillageOfficeView
{
    public event Action<VillageOfficeIntent> Intent;
    VillageOfficeModel model;
    VillageOfficePresenter presenter;
    TextMeshProUGUI title,body;
    Button tasks,trade,tax,back,manage;
    float nextRefresh;
    public static void Open(string facilityId)
    {
        if(!InputManager.Instance.CanReadGameplayInput||UIManager.Instance.panelStack.Count!=0)return;
        if(!new VillageWorldModel().CanUseFacility(facilityId))return;
        var panel=UIManager.Instance.PushPanel(PanelType.VillageOfficePanel) as VillageOfficePanel;
        if(panel!=null)panel.Bind(facilityId);
    }
    void Awake()
    {
        var backdrop=GetComponent<Image>();if(backdrop!=null)backdrop.color=new Color(0,0,0,.68f);
        var card=GameplayUi.Rect(transform,"OfficeCard",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(880,560));
        GameplayUiSkin.Apply(card.gameObject.AddComponent<Image>(), "UI_Panel_Sub", GameplayUiSkin.DarkPaper);
        title=Label(card,"Title",new Vector2(24,-22),new Vector2(832,58),30);
        body=Label(card,"Body",new Vector2(24,-100),new Vector2(832,240),23);
        body.enableWordWrapping=true;
        tasks=GameplayUi.Button(card,"OfficeTasks","",new Vector2(24,-365),new Vector2(260,56),()=>Send(VillageOfficeIntent.Tasks));
        trade=GameplayUi.Button(card,"OfficeTrade","",new Vector2(310,-365),new Vector2(260,56),()=>Send(VillageOfficeIntent.Trade));
        tax=GameplayUi.Button(card,"OfficeTax","",new Vector2(596,-365),new Vector2(260,56),()=>Send(VillageOfficeIntent.Tax));
        back=GameplayUi.Button(card,"OfficeBack","",new Vector2(616,-475),new Vector2(240,54),()=>Send(VillageOfficeIntent.Back));
        manage=GameplayUi.Button(card,"OfficeManagement","设施升级",new Vector2(24,-475),new Vector2(240,54),()=>Send(VillageOfficeIntent.Manage));
    }
    static TextMeshProUGUI Label(Transform parent,string name,Vector2 position,Vector2 size,float font)
    {var area=GameplayUi.Rect(parent,name+"Area",Vector2.up,Vector2.up,position,size);var value=GameplayUi.Text(area,name,"",font);value.alignment=TextAlignmentOptions.TopLeft;return value;}
    void Bind(string id){presenter?.Dispose();model=new VillageOfficeModel(id);presenter=new VillageOfficePresenter(model);presenter.Attach(this);}
    public override void OnEnter(){CursorEnable();if(model!=null){presenter?.Dispose();presenter=new VillageOfficePresenter(model);presenter.Attach(this);}}
    public override void OnExit(){presenter?.Dispose();presenter=null;if(uiManager==null||!uiManager.panelStack.Contains(this))model=null;}
    void Update(){if(IsShowing&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.15f;presenter?.Refresh();}}
    void Send(VillageOfficeIntent intent){if(IsShowing)Intent?.Invoke(intent);}
    public override void HandleBack(){Send(VillageOfficeIntent.Back);}
    public void CloseOffice(){var stack=UIManager.Instance.panelStack;if(stack.Count>0&&stack.Peek()==this)UIManager.Instance.PopPanel();}
    public void Render(VillageOfficeState state)
    {
        title.text=state.Title;body.text=state.Body;
        bool menu=!state.TaxPage&&!state.TasksPage;
        tasks.gameObject.SetActive(menu);trade.gameObject.SetActive(menu);tax.gameObject.SetActive(menu);
        manage.gameObject.SetActive(menu);manage.interactable=state.Available;
        Set(tasks,state.Tasks,state.Available);Set(trade,state.Trade,state.Available);Set(tax,state.Tax,state.Available);Set(back,state.Back,true);
    }
    static void Set(Button button,string text,bool enabled){button.interactable=enabled;button.GetComponentInChildren<TextMeshProUGUI>(true).text=text;}
    void OnDestroy(){presenter?.Dispose();}
}
