#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chaos.Gameplay.Interaction;
using RunMap;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>自然游玩探针。只发送正常设备输入，记录实际战斗和通行，不改角色、奖励或地形。</summary>
public static class NaturalPlayProbe
{
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static string originalBindings;
    private static Vector2 destination;
    private static bool moving, followingStage, jumping;
    private static float nextSample, nextPath, lastProgress, flightStarted, pulse, startedAt;
    private static float jumpHoldUntil, nextJumpAt;
    private static Vector2 progressPosition;
    private static List<MapPathStep> path;
    private static int cursor, stage;
    private static readonly Dictionary<int, Vector2> allyPositions = new Dictionary<int, Vector2>();
    public static string State { get; private set; } = "idle";
    public static int Jumps { get; private set; }
    public static int AllyRecoveries { get; private set; }
    public static bool HoldAtExit = true;
    private static string LogPath => Application.dataPath + "/QA/NaturalPlay/input-play.txt";

    public static void Begin()
    {
        Stop();
        originalBindings = InputManager.Instance.SaveOverridesJson();
        InputManager.Instance.LoadOverridesJson("");
        keyboard = InputSystem.AddDevice<Keyboard>("NaturalPlayKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("NaturalPlayMouse");
        Jumps = AllyRecoveries = 0;
        startedAt = Time.realtimeSinceStartup;
        nextSample = nextPath = 0;
        jumpHoldUntil = nextJumpAt = 0;
        stage = 0;
        allyPositions.Clear();
        if (File.Exists(LogPath))
            File.Copy(LogPath, Application.dataPath + "/QA/NaturalPlay/input-play-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(LogPath, "实际按键游玩。禁止发资源、直接伤害/清敌、改属性、瞬移和替换碰撞。\n");
        EditorApplication.update += Tick;
        State = "ready";
    }

    public static void Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        System.Collections.IEnumerator Release()
        {
            yield return new WaitForSecondsRealtime(.15f);
            if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Release());
    }

    public static void Click(string name)
    {
        var top = UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
        var button = top == null ? null : top.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name);
        if (button == null) button = UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b => b.name == name);
        if (button == null || !button.interactable) throw new InvalidOperationException("按钮不可用 " + name);
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        // 扇形中心是镂空区，点击其文字所在的实际扇格。
        var rect = button.GetComponent<RadialWedgeGraphic>() == null
            ? (RectTransform)button.transform : button.GetComponentInChildren<TMPro.TextMeshProUGUI>().rectTransform;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
            throw new InvalidOperationException("按钮被遮挡 " + name);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Note("UI " + name);
    }

    public static void Travel(Vector2 feet)
    {
        destination = feet;
        moving = true;
        followingStage = false;
        path = null;
        nextPath = 0;
        lastProgress = Time.time;
        progressPosition = PlayerSpawner.CurrentPlayer == null ? Vector2.zero : (Vector2)PlayerSpawner.CurrentPlayer.transform.position;
        State = "travel";
    }

    public static void Advance()
    {
        moving = followingStage = true;
        path = null;
        lastProgress = Time.time;
        progressPosition = PlayerSpawner.CurrentPlayer == null ? Vector2.zero : (Vector2)PlayerSpawner.CurrentPlayer.transform.position;
        State = "advance";
    }

    public static void Halt()
    {
        moving = followingStage = false;
        jumpHoldUntil = nextJumpAt = 0;
        if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        if (mouse != null && mouse.added) InputSystem.QueueStateEvent(mouse, new MouseState());
        State = "halted";
    }

    public static void Stop()
    {
        EditorApplication.update -= Tick;
        moving = followingStage = false;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        keyboard = null;
        mouse = null;
        if (originalBindings != null && Application.isPlaying)
            InputManager.Instance.LoadOverridesJson(originalBindings);
        originalBindings = null;
        State = "stopped";
    }

    public static void Note(string text)
    {
        File.AppendAllText(LogPath, (Time.realtimeSinceStartup - startedAt).ToString("F2") + " " + text + "\n");
    }

    private static void Tick()
    {
        if (!Application.isPlaying || keyboard == null || !keyboard.added) return;
        var player = PlayerSpawner.CurrentPlayer;
        if (Time.realtimeSinceStartup >= nextSample)
        {
            nextSample = Time.realtimeSinceStartup + 1;
            if (player != null)
            {
                var health = player.GetComponent<PlayerHealth>();
                var body = player.GetComponent<Rigidbody2D>();
                Note("stage=" + RunEncounter.CurrentSceneIndex + " pos=" + player.transform.position.ToString("F2")
                    + " velocity=" + body.velocity.ToString("F2") + " hp=" + health.Hp + " kills=" + RunEncounter.RunKills
                    + " jumps=" + Jumps + " state=" + State);
                foreach (var ally in AllyController.All)
                {
                    if (ally == null) continue;
                    Vector2 position = ally.transform.position;
                    int id = ally.GetInstanceID();
                    if (allyPositions.TryGetValue(id, out var previous) && Vector2.Distance(position, previous) > 12)
                    { AllyRecoveries++; Note("ALLY_POSITION_JUMP " + ally.name + " " + previous + " -> " + position); }
                    allyPositions[id] = position;
                }
                Note("team=" + string.Join(";", AllyController.All.Where(a => a != null)
                    .Select(a => a.name + " hp=" + a.Hp + " down=" + a.IsDowned
                        + " jumps=" + a.JumpCount + " recovery=" + a.TeleportCount)));
            }
        }
        if (!moving || player == null || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) return;
        if (!InputManager.Instance.CanReadGameplayInput || RunEncounter.Defeated || RunEncounter.RunCleared)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            return;
        }
        if (stage != RunEncounter.CurrentSceneIndex)
        {
            stage = RunEncounter.CurrentSceneIndex;
            path = null;
            jumping = false;
            allyPositions.Clear();
            lastProgress = Time.time;
        }
        var shape = player.GetComponent<Collider2D>();
        var body2D = player.GetComponent<Rigidbody2D>();
        var locomotion = player.GetComponent<PlayerLocomotion>();
        Vector2 feet = RunMapPhysics.Feet(shape);
        var enemy = UnityEngine.Object.FindObjectsOfType<EnemyPlaceholder>()
            .Where(e => !e.IsDead).OrderBy(e => Vector2.Distance(e.transform.position, player.transform.position)).FirstOrDefault();
        bool fighting = enemy != null && Vector2.Distance(enemy.transform.position, player.transform.position) < 1.8f
            && Mathf.Abs(CombatQuery.Feet(enemy.transform).y - feet.y) < .45f
            && CombatQuery.HasLineOfSight(player.transform, enemy.transform);
        if (followingStage)
        {
            var door = UnityEngine.Object.FindObjectOfType<RunExitDoor>();
            if (stage < 3 && door != null) destination = door.transform.position;
            else if (enemy != null) destination = RunMapPhysics.Feet(enemy.GetComponent<Collider2D>());
            else { Halt(); return; }
        }
        float horizontal = 0;
        bool jump = false;
        if (fighting && !jumping)
        {
            float dx = enemy.transform.position.x - player.transform.position.x;
            horizontal = Mathf.Abs(dx) > .45f || Mathf.Sign(dx) != locomotion.Facing ? Mathf.Sign(dx) : 0;
            path = null;
        }
        else if ((feet - destination).sqrMagnitude < .7f && !followingStage) { Halt(); return; }
        else
        {
            bool grounded = locomotion.IsGrounded;
            if ((path == null || cursor >= path.Count) && Time.time >= nextPath && grounded)
            {
                var graph = RunMapPathfinder.For(NaturalMapRuntime.Current, new MapMotionProfile
                {
                    Speed = MetaCharacterStats.MoveSpeed(locomotion.moveSpeed), JumpForce = locomotion.jumpForce,
                    Gravity = Mathf.Abs(Physics2D.gravity.y * body2D.gravityScale),
                    Width = shape.bounds.size.x, Height = shape.bounds.size.y
                });
                graph.TryPath(feet, destination, 6, 12000, null, out path);
                cursor = 0;
                nextPath = Time.time + .4f;
            }
            if (path != null && cursor < path.Count)
            {
                var step = path[cursor];
                if (grounded && body2D.velocity.y <= .1f && Time.time - flightStarted > .15f
                    && Vector2.Distance(feet, step.Destination) < .65f)
                { cursor++; jumping = false; }
                else if (jumping)
                {
                    if (grounded && body2D.velocity.y <= .1f && Time.time - flightStarted > .2f)
                    { jumping = false; path = null; }
                    else
                    {
                        float dx = step.Destination.x - feet.x;
                        horizontal = Mathf.Abs(dx) < .12f ? 0 : Mathf.Sign(dx);
                    }
                }
                else if (step.Kind == MapMotionKind.Jump)
                {
                    float launch = step.From.x + .5f - feet.x;
                    if (grounded && Mathf.Abs(launch) < .65f && Time.time >= nextJumpAt)
                    {
                        jump = jumping = true;
                        flightStarted = Time.time;
                        jumpHoldUntil = Time.time + .18f;
                        nextJumpAt = Time.time + .45f;
                        Jumps++;
                        horizontal = Mathf.Sign(step.To.x - step.From.x);
                    }
                    else horizontal = Mathf.Abs(launch) < .08f ? 0 : Mathf.Sign(launch);
                }
                else horizontal = Mathf.Sign(step.Destination.x - feet.x);
            }
            if (followingStage && stage < 3 && Vector2.Distance(feet, destination) < 1)
            {
                if (HoldAtExit)
                {
                    Note("REACHED_EXIT stage=" + stage + " hp=" + player.GetComponent<PlayerHealth>().Hp
                        + " kills=" + RunEncounter.RunKills + " medicine=" + CampService.MedicineCount);
                    Halt();
                    return;
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                path = null;
                return;
            }
        }
        if (Mathf.Abs(progressPosition.x - feet.x) > 1)
        { progressPosition = feet; lastProgress = Time.time; }
        if (Time.time - lastProgress > 10 && !fighting)
        {
            Note("STUCK destination=" + destination + " feet=" + feet + " grounded=" + locomotion.IsGrounded
                + " path=" + (path == null ? -1 : path.Count) + " cursor=" + cursor);
            Halt();
            return;
        }
        pulse = Time.time % .42f;
        jump |= Time.time < jumpHoldUntil;
        var keys = new List<Key>();
        if (horizontal < 0) keys.Add(Key.A);
        if (horizontal > 0) keys.Add(Key.D);
        if (jump) keys.Add(Key.Space);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
        InputSystem.QueueStateEvent(mouse, new MouseState
        {
            position = new Vector2(Screen.width * .54f, Screen.height * .52f),
            buttons = (ushort)(pulse < .09f ? 1 : 0)
        });
    }
}
#endif
