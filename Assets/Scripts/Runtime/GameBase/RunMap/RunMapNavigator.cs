using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    /// <summary>Executes approved walk/jump/drop edges. Position warps are explicit recovery only.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class RunMapNavigator : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D shape;
        private NaturalMapPlan map;
        private RunMapPathfinder graph;
        private RunBehaviorSettings settings;
        private List<MapPathStep> path;
        private int cursor;
        private float speed = 5, jumpForce = 11, acceleration = 30, nextRepath;
        private Vector2 lastGoal, progressFeet;
        private float progressAt, flightStarted;
        private bool inFlight;
        private bool pendingConfiguration;
        private Vector3 desiredConfiguration;
        public bool HasPath { get; private set; }
        public bool IsStuck { get; private set; }
        public bool Reached { get; private set; }
        public int JumpCount { get; private set; }
        public int RepathCount { get; private set; }
        public Vector2 Feet => shape == null ? (Vector2)transform.position : RunMapPhysics.Feet(shape);
        public IReadOnlyList<MapPathStep> CurrentPath => path;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>();
            settings = RunBehaviorSettings.Load(GetComponent<EnemyPlaceholder>() != null ? "Enemy" : "Ally");
            progressFeet = Feet; progressAt = Time.time;
        }
        public void Configure(float speed, float jumpForce, float acceleration = 30f)
        {
            if (inFlight)
            { pendingConfiguration = true; desiredConfiguration = new Vector3(speed, jumpForce, acceleration); return; }
            if (Mathf.Abs(this.speed - speed) > .001f || Mathf.Abs(this.jumpForce - jumpForce) > .001f)
            { graph = null; path = null; inFlight = false; }
            this.speed = speed; this.jumpForce = jumpForce; this.acceleration = acceleration;
        }
        private void EnsureGraph()
        {
            var current = NaturalMapRuntime.Current;
            if (!ReferenceEquals(map, current)) { map = current; graph = null; path = null; inFlight = false; HasPath = false; }
            if (map == null || graph != null) return;
            var size = shape.bounds.size;
            graph = RunMapPathfinder.For(map, new MapMotionProfile { Speed = speed, JumpForce = jumpForce,
                Gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale), Width = size.x, Height = size.y });
        }
        public bool MoveTo(Vector2 feetDestination, float stopDistance = .35f, Rect? allowedBounds = null)
        {
            if (!inFlight && pendingConfiguration)
            { pendingConfiguration = false; Configure(desiredConfiguration.x, desiredConfiguration.y, desiredConfiguration.z); }
            EnsureGraph(); Reached = false;
            if (map == null || graph == null || !NaturalMapRuntime.IsReady) { Stop(); return false; }
            Vector2 feet = Feet;
            bool grounded = RunMapPhysics.IsGrounded(shape);
            if (!grounded && !inFlight)
            {
                if (path != null && cursor < path.Count && path[cursor].Kind == MapMotionKind.Drop)
                {
                    float delta = path[cursor].Destination.x - feet.x;
                    body.velocity = new Vector2(Mathf.Abs(delta) < .06f ? 0 : Mathf.Clamp(delta / Mathf.Max(.01f, Time.deltaTime), -speed, speed), body.velocity.y);
                }
                return HasPath;
            }
            if (grounded && Vector2.Distance(feet, progressFeet) > .22f)
            { progressFeet = feet; progressAt = Time.time; IsStuck = false; }
            else if (Time.time - progressAt > settings.stuckSeconds) IsStuck = true;

            if (!inFlight && Time.time >= nextRepath && (path == null || (lastGoal - feetDestination).sqrMagnitude > .7f * .7f || IsStuck))
            {
                nextRepath = Time.time + settings.repathInterval; RepathCount++;
                HasPath = graph.TryPath(feet, feetDestination, settings.searchRadius, settings.maxExpandedNodes, allowedBounds, out path);
                cursor = 0; lastGoal = feetDestination;
                if (!HasPath) { path = null; SetVelocity(0); return false; }
            }
            if (!HasPath || path == null) { SetVelocity(0); return false; }
            while (cursor < path.Count)
            {
                var step = path[cursor]; Vector2 target = step.Destination;
                if (inFlight)
                {
                    if (Time.time - flightStarted > step.FlightTime + .75f || IsStuck)
                    { inFlight = false; path = null; HasPath = false; SetVelocity(0); return false; }
                    if (grounded && Time.time - flightStarted > .12f)
                    {
                        inFlight = false;
                        if (Mathf.Abs(feet.x - target.x) < .7f && Mathf.Abs(feet.y - target.y) < .25f) { cursor++; continue; }
                        path = null; HasPath = false; SetVelocity(0); return false;
                    }
                    float dx = target.x - feet.x;
                    float vx = Mathf.Abs(dx) < .06f ? 0 : Mathf.Sign(dx) * (step.HorizontalSpeed > 0 ? step.HorizontalSpeed : Mathf.Min(speed, Mathf.Abs(step.To.x - step.From.x) / Mathf.Max(.1f, step.FlightTime)));
                    body.velocity = new Vector2(vx, body.velocity.y); return true;
                }
                if (Mathf.Abs(feet.x - target.x) <= settings.waypointTolerance && Mathf.Abs(feet.y - target.y) < .25f && grounded)
                { cursor++; continue; }
                if (step.Kind == MapMotionKind.Jump)
                {
                    float launchX = step.From.x + .5f;
                    if (!grounded) { SetVelocity(0); return true; }
                    if (Mathf.Abs(feet.x - launchX) > .08f)
                    {
                        SetVelocity(Mathf.Clamp((launchX - feet.x) / Mathf.Max(Time.deltaTime, .01f), -speed, speed)); return true;
                    }
                    inFlight = true; flightStarted = Time.time; JumpCount++;
                    float vx = step.HorizontalSpeed > 0 ? Mathf.Sign(step.To.x - feet.x + .5f) * step.HorizontalSpeed : (step.To.x - feet.x + .5f) / step.FlightTime;
                    body.velocity = new Vector2(Mathf.Clamp(vx, -speed, speed), jumpForce); return true;
                }
                float distance = target.x - feet.x;
                SetVelocity(Mathf.Abs(distance) < .06f ? 0 : Mathf.Clamp(distance / Mathf.Max(Time.deltaTime, .01f), -speed, speed));
                return true;
            }
            Vector2Int goal;
            bool found = graph.TryNearest(feetDestination, settings.searchRadius, allowedBounds, out goal);
            var destination = new Vector2(goal.x + .5f, goal.y + .03f);
            Reached = found && Vector2.Distance(feet, destination) <= Mathf.Max(stopDistance, .25f);
            SetVelocity(0); if (Reached) { IsStuck = false; progressAt = Time.time; }
            return Reached;
        }
        private void SetVelocity(float x)
        { if (body != null) body.velocity = new Vector2(Mathf.MoveTowards(body.velocity.x, x, acceleration * Time.deltaTime), body.velocity.y); }
        public void Stop()
        {
            HasPath = false; IsStuck = false; Reached = false; path = null; inFlight = false;
            progressAt = Time.time; progressFeet = Feet;
            if (body != null) body.velocity = new Vector2(0, body.velocity.y);
        }
        private void OnDisable() { Stop(); }
        public bool TryFindSafePosition(Vector2 desired, out Vector2 feet, float radius = 6f)
        {
            EnsureGraph(); feet = default;
            if (graph == null) return false;
            Vector2Int cell;
            if (!graph.TryNearest(desired, Mathf.CeilToInt(radius), null, out cell)) return false;
            feet = new Vector2(cell.x + .5f, cell.y + .03f);
            return Vector2.Distance(feet, desired) <= radius && RunMapPhysics.CanOccupy(feet, shape.bounds.size);
        }
        public void WarpToFeet(Vector2 feet)
        {
            if (!RunMapPhysics.CanOccupy(feet, shape.bounds.size)) return;
            var offset = (Vector2)transform.position - Feet;
            transform.position = feet + offset; body.position = feet + offset;
            body.velocity = Vector2.zero; Stop(); Physics2D.SyncTransforms();
        }
    }
}
