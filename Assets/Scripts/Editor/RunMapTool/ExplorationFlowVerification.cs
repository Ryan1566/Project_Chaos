#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RunMap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>隔离档正式入口与键盘导航验证；移动只发送输入，不修改角色位置。</summary>
public static class ExplorationFlowVerification
{
    static readonly StringBuilder log=new StringBuilder();
    static Keyboard keyboard;
    static string overrides;
    static int ladderMode;
    static uint? oldSeed;
    public static string Status{get;private set;}="idle";
    public static string Storage{get;private set;}
    public static Vector2 Target{get;private set;}
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    public static void Start(int teamSize=1,uint seed=17)
    {
        if(!Application.isPlaying||Status=="running")throw new InvalidOperationException("需要空闲Play环境");
        log.Clear();Storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-Exploration-"+Guid.NewGuid().ToString("N"));
        SaveSlotService.UseEditorTestDirectory(Storage);overrides=InputManager.Instance.SaveOverridesJson();InputManager.Instance.LoadOverridesJson("");
        ladderMode=SettingsManager.Instance.Applied.ladderEntryMode;SettingsManager.Instance.Applied.ladderEntryMode=0;
        oldSeed=NaturalMapRuntime.PreviewSeed;typeof(NaturalMapRuntime).GetProperty("PreviewSeed").SetValue(null,(uint?)seed);
        keyboard=InputSystem.AddDevice<Keyboard>("ExplorationKeyboard");Run(Setup(teamSize));
    }
    public static void Check(bool condition,string description)
    {
        if(!condition)throw new Exception(description);
        log.AppendLine("PASS "+description);Flush();
    }
    static void Flush()
    {Directory.CreateDirectory(Application.dataPath+"/QA/ExplorationMap");File.WriteAllText(Application.dataPath+"/QA/ExplorationMap/flow.txt",log.ToString());}
    static void Run(IEnumerator source)
    {Status="running";UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(source));}
    static IEnumerator Guard(IEnumerator source)
    {
        var stack=new Stack<IEnumerator>();stack.Push(source);
        while(stack.Count>0)
        {
            bool more;object current=null;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
            catch(Exception e){Keys();Status="failed";log.AppendLine("FAIL "+e);Flush();yield break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(current is IEnumerator nested)stack.Push(nested);else yield return current;
        }
        Status="ready";
    }
    public static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    public static void Press(Key key){Run(PressRoutine(key));}
    static IEnumerator PressRoutine(Key key)
    {Keys(key);yield return new WaitForSecondsRealtime(.17f);Keys();yield return new WaitForSecondsRealtime(.2f);}
    public static void Click(string name)
    {
        var button=Top.GetComponentsInChildren<Button>(false).Single(b=>b.name==name&&b.interactable);
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator Setup(int teamSize)
    {
        GameSession.Instance.BeginSandbox(new SavePayload{resMoney=1000,resGoods=1000,resMaterial=1000});
        for(int i=0;i<teamSize;i++)RetainerService.Instance.RecruitRandom();
        var payload=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));GameSession.Instance.Clear();
        Check(SaveSlotService.Instance.Save(3,payload),"独立临时槽3创建");GameSession.Instance.Begin(3,payload,"MainScene");VillageSceneFlow.EnterFromSave();
        float deadline=Time.realtimeSinceStartup+45;
        while(!VillageSceneBootstrap.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(VillageSceneBootstrap.IsReady,"正式实体村庄Ready");yield return PressRoutine(Key.M);Click("EnterRunBtn");
        yield return new WaitForSecondsRealtime(.3f);
        foreach(var member in RetainerService.Instance.GetAll())DispatchService.Instance.Toggle(member.id,out _);
        deadline=Time.realtimeSinceStartup+5;
        while(!Top.GetComponentsInChildren<Button>(false).Any(b=>b.name=="Btn_Depart"&&b.interactable)&&Time.realtimeSinceStartup<deadline)yield return null;
        Click("Btn_Depart");yield return MapReady(1);
    }
    static IEnumerator MapReady(int stage)
    {
        float deadline=Time.realtimeSinceStartup+60;
        while((!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning||RunEncounter.CurrentSceneIndex!=stage)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage&&!RunSceneFlow.IsTransitioning,"正式地图Ready "+stage);
        Check(NaturalMapRuntime.Current.Version==3,"正式入口消费v3地图");
        // 战斗与导航证据分开：先停敌人行为，保留实体碰撞与全队真实跟随。
        var enemies=new List<EnemyPlaceholder>();RunEncounter.CopyMapEnemies(enemies);foreach(var enemy in enemies)enemy.enabled=false;
        Check(NaturalMapAssembler.VerifyActive(NaturalMapRuntime.Current,RunMapBootstrap.CurrentMapRoot),"实际Tilemap碰撞支撑正确");
    }
    public static void Travel(Vector2 destination){Target=destination;Run(TravelRoutine(destination));}
    public static void TravelToGoal(){var cell=NaturalMapRuntime.Current.GoalCell;Travel(new Vector2(cell.x+.5f,cell.y+.05f));}
    static IEnumerator TravelRoutine(Vector2 destination)
    {
        var player=PlayerSpawner.CurrentPlayer;var shape=player.GetComponent<Collider2D>();var movement=player.GetComponent<PlayerLocomotion>();
        var body=player.GetComponent<Rigidbody2D>();var map=NaturalMapRuntime.Current;
        var graph=RunMapPathfinder.For(map,new MapMotionProfile{Speed=movement.moveSpeed,JumpForce=movement.jumpForce,
            Gravity=Mathf.Abs(Physics2D.gravity.y*body.gravityScale),Width=shape.bounds.size.x,Height=shape.bounds.size.y,CanClimb=true,ClimbSpeed=3.5f});
        float deadline=Time.realtimeSinceStartup+180,flightAt=-10,nextPath=0;MapPathStep? flight=null;var path=new List<MapPathStep>();
        Vector2 last=RunMapPhysics.Feet(shape);float progressAt=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup<deadline)
        {
            if(RunEncounter.Defeated||!ReferenceEquals(map,NaturalMapRuntime.Current))throw new Exception("导航时本局结束或地图更换");
            Vector2 feet=RunMapPhysics.Feet(shape);var climber=player.GetComponent<LadderClimber>();bool grounded=RunMapPhysics.IsGrounded(shape);
            if(Vector2.Distance(feet,destination)<.5f&&grounded&&!climber.IsClimbing)break;
            if(Vector2.Distance(feet,last)>.15f){last=feet;progressAt=Time.realtimeSinceStartup;}
            if(Time.realtimeSinceStartup-progressAt>7)throw new Exception("键盘导航卡住 feet="+feet+" goal="+destination);
            if(flight.HasValue)
            {
                var step=flight.Value;float age=Time.time-flightAt;
                if(grounded&&age>.18f){flight=null;Keys();nextPath=0;yield return null;continue;}
                if(age>step.FlightTime+1.2f){flight=null;nextPath=0;Keys();yield return null;continue;}
                Steer(step.Destination.x-feet.x,age<.12f);yield return null;continue;
            }
            if(Time.time>=nextPath&&!climber.IsClimbing)
            {
                if(!graph.TryPath(feet,destination,5,32768,null,out path))throw new Exception("角色实际运动图没有路径 feet="+feet+" target="+destination);
                nextPath=float.PositiveInfinity;
            }
            if(path.Count==0){Keys();nextPath=0;yield return null;continue;}
            var edge=path[0];
            if(grounded&&!climber.IsClimbing&&Vector2.Distance(feet,edge.Destination)<.24f)
            {path.RemoveAt(0);yield return null;continue;}
            if(edge.Kind==MapMotionKind.Climb)
            {
                bool up=edge.To.y>edge.From.y;
                if(!climber.IsClimbing&&Vector2.Distance(feet,new Vector2(edge.From.x+.5f,edge.From.y+.03f))>1.05f)
                {Steer(edge.From.x+.5f-feet.x,false);yield return null;continue;}
                if(SettingsManager.Instance.Applied.ladderEntryMode==1&&!climber.IsClimbing){yield return PressRoutine(Key.E);}
                Keys(up?Key.W:Key.S);
                if(!climber.IsClimbing&&Vector2.Distance(feet,edge.Destination)<.5f){Keys();nextPath=0;path.Clear();}
                yield return null;continue;
            }
            if(climber.IsClimbing){Keys(edge.To.y>=feet.y?Key.W:Key.S);yield return null;continue;}
            if(!grounded){Steer(edge.Destination.x-feet.x,false);yield return null;continue;}
            if(edge.Kind==MapMotionKind.Jump)
            {
                float align=edge.From.x+.5f-feet.x;
                if(Mathf.Abs(align)>.13f){Steer(align,false);yield return null;continue;}
                flight=edge;flightAt=Time.time;Steer(edge.Destination.x-feet.x,true);yield return null;continue;
            }
            Steer(edge.Destination.x-feet.x,false);yield return null;
        }
        Keys();Check(Vector2.Distance(RunMapPhysics.Feet(shape),destination)<.6f,"仅键盘移动到达 "+destination+" stage="+RunEncounter.CurrentSceneIndex);
        Check(AllyController.All.All(a=>a.TeleportCount==0),"本图门客未使用安全恢复传送");
    }
    static void Steer(float dx,bool jump)
    {var keys=new List<Key>();if(Mathf.Abs(dx)>.09f)keys.Add(dx>0?Key.D:Key.A);if(jump)keys.Add(Key.Space);Keys(keys.ToArray());}
    public static void UseExit(){Run(ExitRoutine());}
    static IEnumerator ExitRoutine()
    {int next=RunEncounter.CurrentSceneIndex+1;yield return PressRoutine(Key.E);yield return MapReady(next);}
    public static string Snapshot()
    {
        var player=PlayerSpawner.CurrentPlayer;return Status+" stage="+RunEncounter.CurrentSceneIndex+" player="+(player==null?"none":RunMapPhysics.Feet(player.GetComponent<Collider2D>()).ToString())
            +" target="+Target+" allies="+string.Join(";",AllyController.All.Select(a=>a.memberId+":"+a.transform.position+" recover="+a.TeleportCount));
    }
    public static void Finish()
    {
        Keys();if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);InputManager.Instance.LoadOverridesJson(overrides);
        SettingsManager.Instance.Applied.ladderEntryMode=ladderMode;typeof(NaturalMapRuntime).GetProperty("PreviewSeed").SetValue(null,oldSeed);
        log.AppendLine("DONE isolated="+Storage);Flush();Status="finished";
    }
}
#endif
