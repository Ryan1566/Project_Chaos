using System;
using System.Collections.Generic;
using System.Linq;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>家族树被动视图，复用节点和连线，浏览手势只改变视口。</summary>
public class FamilyTreePanel : BasePanel, IFamilyTreeView
{
    public event Action<FamilyTreeIntent> Intent;
    private FamilyTreePresenter _presenter;
    private FamilyTreeVisuals _visuals;
    private TMP_FontAsset _font;
    private RectTransform _viewport, _graph;
    private TextMeshProUGUI _resources,_progress,_boardDescription,_detailTitle,_detailBody,_summary,_message,_zoomText;
    private Button _unlock;
    private Button _manageFacility;
    private FamilyTreeSnapshot _tree;
    private uint _shownBoard;
    private bool _built;
    private float _messageUntil;
    private static readonly Color Gold=new Color(.87f,.70f,.35f), Ink=new Color(.13f,.115f,.095f), Paper=new Color(.94f,.89f,.76f);
    private readonly Dictionary<uint,NodeWidget> _nodes=new Dictionary<uint,NodeWidget>();
    private readonly Dictionary<uint,Image> _lines=new Dictionary<uint,Image>();
    private readonly Dictionary<uint,Button> _boards=new Dictionary<uint,Button>();
    private readonly Dictionary<uint,Pose> _poses=new Dictionary<uint,Pose>();
    private sealed class NodeWidget { public RectTransform Rect; public Image Image,Icon; public Outline Outline; public TextMeshProUGUI Name,Branch,Status; public bool Unlocked; public float PulseUntil; }
    private struct Pose { public Vector2 Position; public float Scale; }
    private void Awake() { BuildUi(); }
    public override void OnEnter()
    {
        BuildUi();
        if(_presenter==null || _presenter.IsDisposed) { _presenter=new FamilyTreePresenter();_presenter.Attach(this); }
        else _presenter.Refresh();
    }
    public override void OnExit() { SavePose();_presenter?.Dispose();_presenter=null; }
    private void OnDestroy() { OnExit(); }
    public override void ClosePanel() { if(UIManager.Instance!=null) UIManager.Instance.PopPanel();else Hide(); }
    private void Send(FamilyTreeAction action,uint id=0) { if(IsShowing) Intent?.Invoke(new FamilyTreeIntent(action,id)); }
    private void BuildUi()
    {
        if(_built) return; _built=true;
        var title=transform.Find("Title"); if(title!=null) { var tmp=title.GetComponent<TextMeshProUGUI>();if(tmp!=null)_font=tmp.font;title.gameObject.SetActive(false); }
        if(_font==null) _font=TMP_Settings.defaultFontAsset;
        _visuals=Resources.Load<FamilyTreeVisuals>("UI/FamilyTreeVisuals");
        var background=GetComponent<Image>(); if(background!=null) { background.sprite=null;background.color=new Color(.08f,.07f,.06f,1f); }
        var frame=Area("FamilyTreeLayout",transform,0,0,1,1);frame.offsetMin=new Vector2(28,20);frame.offsetMax=new Vector2(-28,-20);
        Label("Header",frame,.09f,.935f,.75f,.995f,"家族树  ·  长久之计",32,Gold);
        _resources=Label("Resources",frame,.02f,.88f,.83f,.935f,"",21,Paper);
        MakeButton("CloseButton",frame,.92f,.945f,.99f,.992f,"关闭",()=>Send(FamilyTreeAction.Close));
        _manageFacility=MakeButton("FamilyFacilityManagement",frame,.74f,.945f,.91f,.992f,"",()=>Send(FamilyTreeAction.ManageFacility));
        _progress=Label("Progress",frame,.79f,.88f,.99f,.935f,"",20,Gold);
        var body=Area("Body",frame,.02f,.15f,.98f,.86f);
        var left=Area("Boards",body,0,0,.18f,1);
        for(uint id=1;id<=5;id++) { uint selected=id;float top=1-(id-1)*.18f;_boards[id]=MakeButton("Board_"+id,left,0,top-.15f,1,top,"",()=>Send(FamilyTreeAction.SelectBoard,selected)); }
        _viewport=Area("TreeViewport",body,.195f,0,.70f,1);
        var bg=_viewport.gameObject.AddComponent<Image>();bg.color=Ink;_viewport.gameObject.AddComponent<RectMask2D>();
        var input=_viewport.gameObject.AddComponent<FamilyTreeViewportInput>();input.Owner=this;
        _graph=Area("TreeGraph",_viewport,.5f,.5f,.5f,.5f);_graph.sizeDelta=new Vector2(1300,800);
        _boardDescription=Label("BoardDescription",_viewport,.03f,.91f,.97f,.99f,"",16,Gold);
        _boardDescription.raycastTarget=false;
        _zoomText=Label("Zoom",_viewport,.03f,.015f,.5f,.075f,"",15,Paper);
        MakeButton("ResetTreeView",_viewport,.76f,.02f,.97f,.08f,"恢复视图",()=>Send(FamilyTreeAction.ResetView));
        var detail=Area("NodeDetail",body,.72f,0,1,1);var detailBg=detail.gameObject.AddComponent<Image>();detailBg.sprite=_visuals==null?null:_visuals.DetailBackground;detailBg.type=Image.Type.Sliced;detailBg.color=detailBg.sprite==null?new Color(.19f,.16f,.12f):Color.white;
        _detailTitle=Label("NodeTitle",detail,.055f,.87f,.94f,.98f,"选择节点",27,Gold);
        var scrollArea=Area("DetailScroll",detail,.045f,.19f,.955f,.85f);scrollArea.gameObject.AddComponent<RectMask2D>();var sr=scrollArea.gameObject.AddComponent<ScrollRect>();sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;sr.viewport=scrollArea;
        var content=Area("DetailContent",scrollArea,0,1,1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,600);sr.content=content;
        _detailBody=Label("NodeDescription",content,.03f,0,.97f,1,"",19,Paper);_detailBody.alignment=TextAlignmentOptions.TopLeft;_detailBody.enableWordWrapping=true;
        var scrollBg=scrollArea.gameObject.AddComponent<Image>();scrollBg.color=new Color(0,0,0,.001f);scrollBg.raycastTarget=true;
        _unlock=MakeButton("UnlockSelected",detail,.08f,.045f,.92f,.14f,"解锁节点",()=>Send(FamilyTreeAction.Unlock));
        _summary=Label("Summary",frame,.02f,.07f,.98f,.14f,"",18,Paper);
        _message=Label("Hint",frame,.02f,.025f,.98f,.075f,"点击节点查看详情  ·  拖动浏览 / 滚轮缩放  ·  Esc返回",18,Gold);
    }
    public void Render(FamilyTreeState state)
    {
        BuildUi();_tree=state.Tree;
        _manageFacility.gameObject.SetActive(state.CanManageFacility);
        _manageFacility.GetComponentInChildren<TextMeshProUGUI>().text=$"府邸 Lv{state.FacilityLevel} · 升级";
        _resources.text=_tree.Resources??"";_progress.text="已解锁 "+_tree.Unlocked+" / "+_tree.Total;
        _summary.text=_tree.Summary??"";
        if(!string.IsNullOrEmpty(_tree.Error)) { _detailBody.text=_tree.Error;_unlock.interactable=false;return; }
        foreach(var pair in _boards)
        {
            var row=_tree.Boards.FirstOrDefault(x=>x.Id==pair.Key);pair.Value.gameObject.SetActive(row!=null);
            if(row==null)continue;pair.Value.GetComponentInChildren<TextMeshProUGUI>().text=row.Name+"\n"+row.Unlocked+" / "+row.Total;
            var image=pair.Value.GetComponent<Image>();image.sprite=_visuals==null?null:_tree.Board==row.Id?_visuals.SectionSelected:_visuals.SectionNormal;image.type=Image.Type.Sliced;image.color=image.sprite==null?(_tree.Board==row.Id?new Color(.37f,.29f,.15f):Ink):Color.white;
        }
        bool changed=_shownBoard!=_tree.Board;
        if(changed) { SavePose();_shownBoard=_tree.Board; }
        if(changed)
        { foreach(var widget in _nodes.Values)widget.Rect.gameObject.SetActive(false);foreach(var line in _lines.Values)line.gameObject.SetActive(false); }
        var path=new HashSet<uint>();uint cursor=_tree.Selected;
        while(cursor!=0 && path.Add(cursor)) { var found=_tree.Nodes.FirstOrDefault(x=>x.Id==cursor);cursor=found==null?0:found.Prerequisite; }
        foreach(var row in _tree.Nodes)
        {
            if(!_nodes.TryGetValue(row.Id,out var widget)) { widget=CreateNode(row.Id);_nodes.Add(row.Id,widget); }
            if(!widget.Rect.gameObject.activeSelf)widget.Rect.gameObject.SetActive(true);widget.Rect.anchoredPosition=new Vector2(row.X,row.Y);
            widget.Name.text=(row.Milestone?"◆ ":"")+row.Name;widget.Branch.text=row.Branch;widget.Status.text=StatusName(row.Status);
            widget.Status.color=row.Status==FamilyNodeStatus.Available?Gold:row.Status==FamilyNodeStatus.Unlocked?new Color(.50f,.85f,.64f):new Color(.66f,.63f,.56f);
            widget.Outline.enabled=path.Contains(row.Id);widget.Outline.effectColor=row.Id==_tree.Selected?new Color(1,.83f,.4f):new Color(.6f,.48f,.24f);
            Sprite sprite=null;
            if(_visuals!=null) sprite=row.Status==FamilyNodeStatus.Unlocked?_visuals.NodeUnlocked:row.Status==FamilyNodeStatus.Available?_visuals.NodeReady:_visuals.NodeLocked;
            widget.Image.sprite=sprite;widget.Image.color=sprite==null?new Color(.24f,.20f,.14f):Color.white;
            widget.Icon.sprite=string.IsNullOrEmpty(row.Icon)?null:Resources.Load<Sprite>(row.Icon);widget.Icon.gameObject.SetActive(widget.Icon.sprite!=null);
            bool unlocked=row.Status==FamilyNodeStatus.Unlocked;
            if(unlocked && !widget.Unlocked && !changed)widget.PulseUntil=Time.unscaledTime+.4f;widget.Unlocked=unlocked;
            var parent=_tree.Nodes.FirstOrDefault(x=>x.Id==row.Prerequisite);
            if(parent!=null)
            {
                if(!_lines.TryGetValue(row.Id,out var line)) { var r=Area("Link_"+row.Id,_graph,.5f,.5f,.5f,.5f);line=r.gameObject.AddComponent<Image>();line.raycastTarget=false;_lines.Add(row.Id,line); }
                line.gameObject.SetActive(true);line.transform.SetAsFirstSibling();Vector2 start=new Vector2(parent.X,parent.Y),end=new Vector2(row.X,row.Y),delta=end-start;
                line.rectTransform.anchoredPosition=(start+end)*.5f;line.rectTransform.sizeDelta=new Vector2(delta.magnitude,3);line.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                line.color=path.Contains(row.Id)&&path.Contains(parent.Id)?Gold:unlocked?new Color(.35f,.6f,.42f):new Color(.36f,.31f,.23f);
            }
        }
        _boardDescription.text=_tree.Boards.First(x=>x.Id==_tree.Board).Description;
        if(changed) { if(_poses.TryGetValue(_shownBoard,out var pose))ApplyPose(pose);else ResetTreeView(); }
        var selectedNode=_tree.Nodes.FirstOrDefault(x=>x.Id==_tree.Selected);
        if(selectedNode!=null)
        {
            _detailTitle.text=selectedNode.Name;
            _detailBody.text=selectedNode.Description+"\n\n<color=#DFC179>实际收益</color>\n"+selectedNode.Effects+"\n\n<color=#DFC179>解锁条件</color>\n前置："+selectedNode.PrerequisiteName+"\n费用："+selectedNode.Cost+"\n\n"+selectedNode.Reason;
            _detailBody.ForceMeshUpdate();((RectTransform)_detailBody.transform.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(480,_detailBody.preferredHeight+24));
            _unlock.interactable=selectedNode.Status==FamilyNodeStatus.Available;
            _unlock.GetComponentInChildren<TextMeshProUGUI>().text=selectedNode.Status==FamilyNodeStatus.Unlocked?"已解锁":selectedNode.Status==FamilyNodeStatus.Available?"解锁节点":"条件未满足";
        }
        UpdateZoomText();
    }
    private NodeWidget CreateNode(uint id)
    {
        var root=Area("Node_"+id,_graph,.5f,.5f,.5f,.5f);root.sizeDelta=new Vector2(172,144);
        var image=root.gameObject.AddComponent<Image>();image.preserveAspect=true;var button=root.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>Send(FamilyTreeAction.SelectNode,id));
        var outline=root.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(2,-2);
        var widget=new NodeWidget { Rect=root,Image=image,Outline=outline,
            Branch=Label("Branch",root,.05f,.76f,.95f,.98f,"",16,Gold),Name=Label("Name",root,.06f,.32f,.94f,.75f,"",26,Paper),
            Status=Label("State",root,.05f,.03f,.95f,.30f,"",18,Paper),Icon=Icon(root) };
        widget.Name.alignment=widget.Branch.alignment=widget.Status.alignment=TextAlignmentOptions.Center;return widget;
    }
    private Image Icon(RectTransform parent)
    { var r=Area("Icon",parent,.02f,.42f,.18f,.72f);var image=r.gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;return image; }
    public void ShowMessage(string message,bool success)
    { _message.text=message;_message.color=success?new Color(.55f,.91f,.63f):new Color(1,.58f,.45f);_messageUntil=Time.unscaledTime+8; }
    private void Update()
    {
        if(!IsShowing)return;
        if(_messageUntil>0 && Time.unscaledTime>=_messageUntil) { _messageUntil=0;_message.text="点击节点查看详情  ·  拖动浏览 / 滚轮缩放  ·  Esc返回";_message.color=Gold; }
        foreach(var node in _nodes.Values)if(node.Rect.gameObject.activeSelf)
        { float left=node.PulseUntil-Time.unscaledTime;node.Rect.localScale=Vector3.one*(left>0?1+.075f*Mathf.Sin(left/.4f*Mathf.PI):1); }
    }
    public void Pan(Vector2 delta)
    {
        if(!IsShowing)return;var canvas=GetComponentInParent<Canvas>();_graph.anchoredPosition+=delta/(canvas==null?1:Mathf.Max(.01f,canvas.scaleFactor));ClampPan();SavePose();
    }
    public void Zoom(float amount,Vector2 screen,Camera camera)
    {
        if(!IsShowing)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport,screen,camera,out var point);
        float old=_graph.localScale.x,next=Mathf.Clamp(old*Mathf.Exp(amount*.12f),.35f,1.65f);
        _graph.anchoredPosition=point-(point-_graph.anchoredPosition)*(next/old);_graph.localScale=Vector3.one*next;ClampPan();SavePose();UpdateZoomText();
    }
    private void ClampPan()
    { var extent=new Vector2(Mathf.Max(250,_graph.rect.width*_graph.localScale.x*.6f),Mathf.Max(200,_graph.rect.height*_graph.localScale.x*.6f));_graph.anchoredPosition=new Vector2(Mathf.Clamp(_graph.anchoredPosition.x,-extent.x,extent.x),Mathf.Clamp(_graph.anchoredPosition.y,-extent.y,extent.y)); }
    public void ResetTreeView()
    {
        if(_tree==null || _tree.Nodes.Length==0)return;
        float minX=_tree.Nodes.Min(x=>x.X),maxX=_tree.Nodes.Max(x=>x.X),minY=_tree.Nodes.Min(x=>x.Y),maxY=_tree.Nodes.Max(x=>x.Y);
        float scale=Mathf.Clamp(Mathf.Min(Mathf.Max(300,_viewport.rect.width)/(maxX-minX+240),Mathf.Max(250,_viewport.rect.height)/(maxY-minY+220)),.35f,1.1f);
        ApplyPose(new Pose { Position=-new Vector2((minX+maxX)*.5f,(minY+maxY)*.5f)*scale,Scale=scale });SavePose();
    }
    private void SavePose() { if(_shownBoard!=0 && _graph!=null)_poses[_shownBoard]=new Pose { Position=_graph.anchoredPosition,Scale=_graph.localScale.x }; }
    private void ApplyPose(Pose pose) { _graph.anchoredPosition=pose.Position;_graph.localScale=Vector3.one*pose.Scale;UpdateZoomText(); }
    private void UpdateZoomText() { if(_zoomText!=null)_zoomText.text="缩放 "+Mathf.RoundToInt(_graph.localScale.x*100)+"%"; }
    private static string StatusName(FamilyNodeStatus state) { return state==FamilyNodeStatus.Unlocked?"已解锁":state==FamilyNodeStatus.Available?"可解锁":state==FamilyNodeStatus.Locked?"需要前置":"资源不足"; }
    private static RectTransform Area(string name,Transform parent,float x0,float y0,float x1,float y1)
    { var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect; }
    private TextMeshProUGUI Label(string name,Transform parent,float x0,float y0,float x1,float y1,string text,float size,Color color)
    { var rect=Area(name,parent,x0,y0,x1,y1);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=_font;label.fontSize=size;label.color=color;label.text=text;label.alignment=TextAlignmentOptions.MidlineLeft;label.enableWordWrapping=true;label.raycastTarget=false;return label; }
    private Button MakeButton(string name,Transform parent,float x0,float y0,float x1,float y1,string text,Action click)
    { var rect=Area(name,parent,x0,y0,x1,y1);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.32f,.26f,.16f);var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>click());var label=Label("Label",rect,.06f,.06f,.94f,.94f,text,20,Paper);label.alignment=TextAlignmentOptions.Center;return button; }
}

/// <summary>空白或节点上的拖动传给视口，拖动由事件系统取消点击。</summary>
public sealed class FamilyTreeViewportInput : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler
{
    public FamilyTreePanel Owner;
    public void OnBeginDrag(PointerEventData data) { }
    public void OnDrag(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left)Owner.Pan(data.delta); }
    public void OnEndDrag(PointerEventData data) { }
    public void OnScroll(PointerEventData data) { Owner.Zoom(data.scrollDelta.y,data.position,data.pressEventCamera); }
}
