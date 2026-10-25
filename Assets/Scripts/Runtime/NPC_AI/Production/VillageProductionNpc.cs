using Spine.Unity;
using UnityEngine;
using VillageAI;

/// <summary>Production-only behavior tree. Never participates in retainers, damage or encounter statistics.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class VillageProductionNpc : MonoBehaviour
{
    public uint BuildingId { get; private set; }
    public string State { get; private set; } = "Idle";
    public float WorkedSeconds { get; private set; }
    public int CompletedShifts { get; private set; }
    public bool IsInside { get; private set; }
    private VillageSceneSettings config;
    private VillagePlot plot;
    private Rigidbody2D body;
    private GameObject visual;
    private SkeletonAnimation skeleton;
    private RunMap.RunMapRng rng;
    private INode tree;
    private float wantedX, shiftElapsed, restRemaining, wanderWait;
    private bool mustWander, wanderChosen;
    private TMPro.TextMeshPro label;
    private string animationName;

    public void Initialize(uint buildingId, int index, VillageSceneSettings settings, GameObject appearance)
    {
        BuildingId = buildingId; config = settings; plot = VillageSceneBootstrap.FindPlot(buildingId);
        body = GetComponent<Rigidbody2D>(); visual = appearance; skeleton = visual.GetComponentInChildren<SkeletonAnimation>();
        rng = new RunMap.RunMapRng(RunMap.MapRandom.Hash("village/worker/" + index));
        wantedX = transform.position.x;
        label = VillageVisuals.Label(transform, "WorkerLabel", new Vector2(0, 2.3f), 5, 1.3f);
        tree = new ReactiveSelector(
            new Guard(CanWork, new Sequence(new ActionNode(Acquire), new ActionNode(WalkToWork), new ActionNode(WorkShift)), LeaveWork),
            new Guard(() => restRemaining > 0, new ActionNode(Rest)),
            new Sequence(new ActionNode(ChooseWander), new ActionNode(Wander), new ActionNode(WaitWander, () => { wanderWait = 0; wanderChosen = false; })));
    }
    private bool CanWork()
    { var save = VillageService.Instance.Find(BuildingId); return !mustWander && restRemaining <= 0 && plot != null && save != null && save.completed; }
    private Status Acquire(float delta)
    { return VillageWorkforce.TryAcquire(BuildingId, this) ? Status.Success : Status.Failure; }
    private Status WalkToWork(float delta)
    {
        State = "Working"; wantedX = plot.Entrance.x;
        return Mathf.Abs(transform.position.x - wantedX) <= .16f ? Status.Success : Status.Running;
    }
    private Status WorkShift(float delta)
    {
        if (!IsInside)
        {
            IsInside = true; shiftElapsed = 0; body.velocity = Vector2.zero; body.simulated = false;
            visual.SetActive(false); transform.position = plot.Entrance; VillageWorkforce.Working(BuildingId, this, true);
        }
        State = "Working";
        float worked = Mathf.Min(delta, config.workSeconds - shiftElapsed);
        VillageService.Instance.TickProduction(BuildingId, worked, this);
        shiftElapsed += worked; WorkedSeconds += worked;
        if (shiftElapsed < config.workSeconds) return Status.Running;
        CompletedShifts++; restRemaining = config.restSeconds; mustWander = true; return Status.Success;
    }
    private void LeaveWork()
    {
        VillageWorkforce.Release(BuildingId, this);
        if (!IsInside) return;
        IsInside = false;
        if (plot != null) transform.position = plot.Entrance;
        if (body != null) { body.position = transform.position; body.simulated = true; body.velocity = Vector2.zero; }
        if (visual != null) visual.SetActive(true);
        wantedX = transform.position.x;
    }
    private Status Rest(float delta)
    { State = "Rest"; wantedX = transform.position.x; restRemaining = Mathf.Max(0, restRemaining - delta); return restRemaining > 0 ? Status.Running : Status.Success; }
    private Status ChooseWander(float delta)
    {
        if (!wanderChosen)
        {
            float home = plot != null ? plot.Entrance.x : 0;
            wantedX = Mathf.Clamp(home + (rng.Range(1601) - 800) * .01f, -config.widthTiles * .5f + 2, config.widthTiles * .5f - 2);
            wanderChosen = true;
        }
        return Status.Success;
    }
    private Status Wander(float delta)
    { State = "Wander"; return Mathf.Abs(transform.position.x - wantedX) < .16f ? Status.Success : Status.Running; }
    private Status WaitWander(float delta)
    {
        State = "Idle";
        if (wanderWait <= 0) wanderWait = Mathf.Lerp(config.wanderMinSeconds, config.wanderMaxSeconds, rng.Range(1001) / 1000f);
        wanderWait -= delta;
        if (wanderWait > 0) return Status.Running;
        mustWander = false; return Status.Success;
    }
    private void Update()
    {
        if (tree == null || !VillageSceneBootstrap.IsReady) return;
        tree.Tick(Time.deltaTime);
        if (label != null) { label.text = VillageText.Get("Worker" + BuildingId) + " · " + VillageText.Get(State); label.gameObject.SetActive(!IsInside); }
        if (!IsInside && skeleton != null)
        {
            string next = Mathf.Abs(body.velocity.x) > .1f ? "Run_Normal" : "Idle_Normal";
            if (next != animationName) { skeleton.AnimationState.SetAnimation(0, next, true); animationName = next; }
        }
    }
    private void FixedUpdate()
    {
        if (config == null || IsInside) return;
        float dx = wantedX - body.position.x;
        body.velocity = new Vector2(Mathf.Abs(dx) < .12f ? 0 : Mathf.Clamp(dx / Time.fixedDeltaTime, -config.workerMoveSpeed, config.workerMoveSpeed), body.velocity.y);
        if (Mathf.Abs(dx) > .12f && visual != null)
        { var scale = visual.transform.localScale; scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dx); visual.transform.localScale = scale; }
        if (body.position.y < config.groundY - config.groundDepth)
        { body.position = new Vector2(plot == null ? 0 : plot.Entrance.x, config.groundY + .05f); body.velocity = Vector2.zero; }
    }
    private void OnDisable() { tree?.Reset(); LeaveWork(); if (body != null) body.velocity = Vector2.zero; }
}
