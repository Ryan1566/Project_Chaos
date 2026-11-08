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
    static int teamCount;
    static uint? oldSeed;
    public static string Status{get;private set;}="idle";
    public static string Storage{get;private set;}
    public static Vector2 Target{get;private set;}
    static BasePanel Top=>UIManager.Instance.panelStack.Count==0?null:UIManager.Instance.panelStack.Peek();
    public static void Start(int teamSize=1,uint seed=17)
    {
        if(!Application.isPlaying||Status=="running")throw new InvalidOperationException("需要空闲Play环境");
        teamCount=teamSize;log.Clear();Storage=Path.Combine(Path.GetTempPath(),"ChaosVillageIsolated-Exploration-"+Guid.NewGuid().ToString("N"));
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
    {Directory.CreateDirectory(Application.dataPath+"/QA/ExplorationMap");File.WriteAllText(Application.dataPath+"/QA/ExplorationMap/flow-"+teamCount+".txt",log.ToString());}
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
        Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation();yield return MapReady(1);
    }
    static IEnumerator MapReady(int stage)
    {
        float deadline=Time.realtimeSinceStartup+60;
        while((!RunEncounter.IsSceneReady||RunSceneFlow.IsTransitioning||RunEncounter.CurrentSceneIndex!=stage)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(RunEncounter.IsSceneReady&&RunEncounter.CurrentSceneIndex==stage&&!RunSceneFlow.IsTransitioning,"正式地图Ready "+stage);
        Check(NaturalMapRuntime.Current.Version==3,"正式入口消费v3地图");
        // 隔离地图通行测试。敌人仍登记为活体，停用其行为和物理，避免静止敌人堵住测试路线。
        var enemies=new List<EnemyPlaceholder>();
        RunEncounter.CopyMapEnemies(enemies);
        Check(enemies.Count==NaturalMapRuntime.Current.Parameters.enemyBudget,"正式场景敌人投放数="+enemies.Count);
        foreach(var enemy in enemies)
        {
            enemy.enabled=false;
            enemy.GetComponent<Rigidbody2D>().simulated=false;
        }
        Check(NaturalMapAssembler.VerifyActive(NaturalMapRuntime.Current,RunMapBootstrap.CurrentMapRoot),"实际Tilemap碰撞支撑正确");
        Check(AllyController.All.Count==teamCount-1,"真实出战队伍人数="+teamCount);
        int objects = RunMapBootstrap.CurrentMapRoot.GetComponentsInChildren<Transform>(true).Length;
        Check(objects < NaturalMapRuntime.Current.Parameters.maxContentObjects,
            $"地图根内对象={objects} 最大规划单步={RunMapBootstrap.LastPlanningStepMs:F2}ms 最大装配单步={RunMapBootstrap.LastAssemblyStepMs:F2}ms");
    }
    public static void Travel(Vector2 destination){Target=destination;Run(TravelRoutine(destination));}
    public static void TravelToGoal(){var cell=NaturalMapRuntime.Current.GoalCell;Travel(new Vector2(cell.x+.5f,cell.y+.05f));}
    static IEnumerator TravelRoutine(Vector2 destination)
    {
        Target = destination;
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
                float nextX = step.HorizontalSpeed > 0f ? step.Destination.x
                    : Mathf.Lerp(step.From.x + .5f, step.Destination.x, (age + .02f) / Mathf.Max(.02f, step.FlightTime));
                Steer(nextX-feet.x,age<.12f);yield return null;continue;
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

    public static void Journey(bool defeat = false) { Run(JourneyRoutine(defeat)); }

    static Vector2 Foot(Vector2Int cell) => new Vector2(cell.x + .5f, cell.y + .03f);

    static IEnumerator JourneyRoutine(bool defeat)
    {
        for (int stage = RunEncounter.CurrentSceneIndex; stage <= 3; stage++)
        {
            var map = NaturalMapRuntime.Current;
            using (var model = new Chaos.MVP.Model.Gameplay.RunMapModel())
                Check(model.Refresh() && !model.Markers.Any(m => m.Kind.ToString() == "Objective"
                    || m.Id == "interactive/exit"), "目标尚未探索 " + stage);

            if (stage == 2 && map.Ladders.Count > 0)
                yield return LadderRoutine(map.Ladders[0]);

            yield return TravelRoutine(Foot(map.GoalCell));
            yield return WaitForTeam(Foot(map.GoalCell));
            var enemies = new List<EnemyPlaceholder>();
            RunEncounter.CopyMapEnemies(enemies);
            if (stage < 3)
            {
                Check(enemies.Count == map.Parameters.enemyBudget, "未清怪已到达出口 " + stage);
                yield return ExitRoutine();
                continue;
            }

            using (var model = new Chaos.MVP.Model.Gameplay.RunMapModel())
                Check(model.Refresh() && model.Markers.Any(m => m.Kind.ToString() == "Objective"), "探索后显示核心区");
            if (defeat)
            {
                PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>().ForceDown();
                if (teamCount > 1) Check(!RunEncounter.Defeated && RunEncounter.IsSpectator, "受控角色先倒地进入观战");
                foreach (var ally in AllyController.All.ToArray()) ally.ForceDown();
                Check(RunEncounter.Defeated && !RunEncounter.RunCleared, "全队倒地进入失败结算");
            }
            else
            {
                var core = enemies.Where(e => map.ObjectiveRegionIds.Contains(e.roomId)).ToArray();
                Check(core.Length > 0 && core.Length < enemies.Count, "核心与支线分别登记");
                foreach (var enemy in core) enemy.TakeRunDamage(enemy.Hp);
                // 遭遇统计按固定节奏刷新，等待结果发布后再判定结算。
                float resultDeadline = Time.realtimeSinceStartup + 2f;
                while (!RunEncounter.RunCleared && !RunEncounter.Defeated
                    && Time.realtimeSinceStartup < resultDeadline) yield return null;
                RunEncounter.CopyMapEnemies(enemies);
                Check(RunEncounter.RunCleared && !RunEncounter.Defeated && enemies.Count > 0,
                    "只消灭核心区进入成功结算，支线存活=" + enemies.Count);
            }
        }
        Click("Btn_Next"); Click("Btn_Next"); Click("Btn_Confirm");
        float deadline = Time.realtimeSinceStartup + 30f;
        while ((!VillageSceneBootstrap.IsReady || NaturalMapRuntime.Current != null)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && NaturalMapRuntime.Current == null, "三页结算返回村庄并清理地图");
    }

    static IEnumerator WaitForTeam(Vector2 target)
    {
        if (teamCount <= 1) yield break;
        float deadline = Time.realtimeSinceStartup + 28f;
        bool AtTarget(AllyController ally)
        {
            var feet = RunMapPhysics.Feet(ally.GetComponent<Collider2D>());
            return !ally.IsDowned && Mathf.Abs(feet.x - target.x) <= 8f && Mathf.Abs(feet.y - target.y) <= 2f
                && !ally.GetComponent<LadderClimber>().IsClimbing;
        }
        while (!AllyController.All.All(AtTarget) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(AllyController.All.All(a => a.TeleportCount == 0 && AtTarget(a)),
            "全队正常导航到达 " + target + " " + Snapshot());
    }

    static IEnumerator LadderRoutine(MapLadder ladder)
    {
        yield return TravelRoutine(Foot(ladder.Top));
        yield return WaitForTeam(Foot(ladder.Top));
        var player = PlayerSpawner.CurrentPlayer;
        var motor = player.GetComponent<LadderClimber>();
        var body = player.GetComponent<Rigidbody2D>();
        var collider = player.GetComponent<Collider2D>();
        float gravity = body.gravityScale;
        yield return PressRoutine(Key.S);
        Check(motor.IsClimbing && body.gravityScale == 0 && collider.enabled, "自动 S 附着，保留碰撞");
        SettingsManager.Instance.Applied.ladderEntryMode = 1;
        Check(motor.IsClimbing, "切换到手动模式保留附着");
        yield return PressRoutine(Key.M);
        Check(InputManager.Instance.GameplayInputBlocked && motor.IsClimbing, "打开地图保持附着");
        var pausedAt = body.position;
        yield return PressRoutine(Key.S);
        Check(Vector2.Distance(body.position, pausedAt) < .02f, "地图面板拦截攀梯键");
        yield return PressRoutine(Key.Escape);
        Check(motor.IsClimbing && !InputManager.Instance.GameplayInputBlocked, "关闭地图继续攀爬");
        yield return MoveOnLadder(motor, Key.S);
        Check(Vector2.Distance(Foot(ladder.Bottom), RunMapPhysics.Feet(collider)) < .25f && body.gravityScale == gravity,
            "真实下梯到底并恢复重力");
        yield return WaitForTeam(Foot(ladder.Bottom));
        yield return PressRoutine(Key.E);
        Check(motor.IsClimbing, "手动 E 进入");
        yield return PressRoutine(Key.E);
        Check(!motor.IsClimbing && body.gravityScale == gravity, "手动 E 退出");
        yield return PressRoutine(Key.E);
        Check(motor.IsClimbing, "重新进入梯子");
        var attack = player.GetComponent<PlayerAttack>();
        var attackClock = typeof(PlayerAttack).GetField("_nextAttackTime", BindingFlags.Instance | BindingFlags.NonPublic);
        var readyAt = (float)attackClock.GetValue(attack);
        var mouse = InputSystem.AddDevice<Mouse>("ExplorationMouse");
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
            yield return new WaitForSecondsRealtime(.2f);
        }
        finally { InputSystem.RemoveDevice(mouse); }
        Check((float)attackClock.GetValue(attack) == readyAt, "攀梯期间左键不触发攻击");
        yield return PressRoutine(Key.W);
        Check(player.GetComponent<PlayerHealth>().ApplyContactDamage(1) && !motor.IsClimbing && body.gravityScale == gravity,
            "受击离梯并恢复重力");
        yield return new WaitForSeconds(.5f);
        yield return PressRoutine(Key.E);
        Check(motor.IsClimbing, "受击落地后可重新进入");
        var locomotion = player.GetComponent<PlayerLocomotion>();
        var airJumpUsed = typeof(PlayerLocomotion).GetField("_airJumpUsed", BindingFlags.Instance | BindingFlags.NonPublic);
        airJumpUsed.SetValue(locomotion, true);
        yield return PressRoutine(Key.Space);
        Check(!motor.IsClimbing && body.gravityScale == gravity, "跳离梯子并恢复重力");
        Check((bool)airJumpUsed.GetValue(locomotion), "离梯跳不重置空中跳次数");
        yield return new WaitForSeconds(1.2f);
        yield return PressRoutine(Key.E);
        Check(motor.IsClimbing, "跳离落地后可重新进入");
        var input = InputManager.Instance;
        var binding = input.GetRebindSteps(InputManager.ActionClimbUp, InputManager.BindingKind.Keyboard).Single();
        InputManager.RebindResult? rebound = null;
        input.BeginRebind(InputManager.ActionClimbUp, binding.BindingIndex, InputManager.BindingKind.Keyboard, result => rebound = result);
        yield return PressRoutine(Key.U);
        Check(rebound == InputManager.RebindResult.Completed, "攀梯键只有一个键鼠绑定且可改绑");
        var oldKeyAt = body.position;
        yield return PressRoutine(Key.W);
        Check(Vector2.Distance(oldKeyAt, body.position) < .02f, "改绑后旧 W 键不再攀梯");
        yield return MoveOnLadder(motor, Key.U);
        input.LoadOverridesJson("");
        Check(Vector2.Distance(Foot(ladder.Top), RunMapPhysics.Feet(collider)) < .25f && body.gravityScale == gravity,
            "真实上梯并通过顶部落脚口");
        yield return WaitForTeam(Foot(ladder.Top));
        SettingsManager.Instance.Applied.ladderEntryMode = 0;
    }

    static IEnumerator MoveOnLadder(LadderClimber motor, Key key)
    {
        Keys(key);
        float deadline = Time.realtimeSinceStartup + 18f;
        while (motor.IsClimbing && Time.realtimeSinceStartup < deadline) yield return null;
        Keys();
        Check(!motor.IsClimbing, "攀爬在预算时间内结束");
    }
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
