using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.AnimationTools;
using UnityEngine;

/// <summary>玩家和门客共用的攻击执行器，统一推进动画、根运动、命中与接招。</summary>
[DefaultExecutionOrder(-40)]
public sealed class CharacterAttack : MonoBehaviour
{
    [SerializeField] string profileId = AttackAnimationTable.DefaultProfile;
    public string ProfileId => profileId;
    public bool IsAttacking => _current != null;
    public bool IsWindingUp => IsAttacking && !_hit;
    public int LastHitCount { get; private set; }
    public string CurrentAnimation => _current?.animation;
    public float Elapsed => _elapsed;

    Rigidbody2D _body;
    SkeletonAnimation _spine;
    AttackAnimationTable _table;
    AttackAnimationTable.Move _current;
    TranslateTimeline _translation;
    TrackEntry _track;
    Bone _motionBone;
    float _startX, _elapsed, _hitTime, _bufferAt, _strength, _worldUnitsPerBoneUnit;
    int _facing, _baseDamage;
    bool _hit, _buffered, _allowCombo, _enemyOnly;
    readonly HashSet<IDamageable> _targets = new HashSet<IDamageable>();
    readonly RaycastHit2D[] _walls = new RaycastHit2D[16];

    public static CharacterAttack For(Component actor)
    {
        return actor.GetComponent<CharacterAttack>() ?? actor.gameObject.AddComponent<CharacterAttack>();
    }

    public AttackAnimationTable.Profile Profile => (_table ??= AttackAnimationTable.Load()).FindProfile(profileId);
    public float LightReach
    {
        get
        {
            var profile = Profile;
            var move = _table.FindMove(profile?.light);
            return move == null ? 1f : move.hitOffset + move.hitWidth * .5f;
        }
    }

    public bool SetProfile(string id)
    {
        if ((_table ??= AttackAnimationTable.Load()).FindProfile(id) == null) return false;
        Cancel();
        profileId = id;
        return true;
    }

    public void AssignRetainer(uint memberId)
    {
        if (!RetainerService.Instance.TryGet(memberId, out var member)) return;
        var id = (_table ??= AttackAnimationTable.Load()).FindRetainerProfile(member.protoId);
        if (!string.IsNullOrEmpty(id)) SetProfile(id);
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _spine = GetComponentInChildren<SkeletonAnimation>(true);
        if (_spine != null) _spine.UpdateLocal += CompensatePose;
    }

    public bool TryLight(int facing, int damage, bool player, float strength = 1f)
    {
        var profile = Profile;
        if (profile == null || !CanSwing()) return false;
        if (IsAttacking)
        {
            if (!_allowCombo || string.IsNullOrEmpty(_current.next) || _elapsed > _current.comboClose) return false;
            _buffered = true;
            _bufferAt = _elapsed;
            return true;
        }
        return Begin(profile.light, facing, damage, player, strength);
    }

    public bool TryHeavy(int facing, int damage)
    {
        var profile = Profile;
        return profile?.heavyMode == "animation" && !IsAttacking && CanSwing()
            && Begin(profile.heavy, facing, damage, true, 1f);
    }

    bool CanSwing() => isActiveAndEnabled && _body != null && _body.simulated && _spine != null
        && GetComponent<LadderClimber>()?.IsClimbing != true;

    bool Begin(string name, int facing, int damage, bool player, float strength)
    {
        var move = _table.FindMove(name);
        _spine.Initialize(false);
        var skeleton = _spine.Skeleton;
        var clip = move == null ? null : skeleton?.Data.FindAnimation(name);
        if (clip == null) return false;
        AttackAnimationTable.ValidateClip(move, skeleton.Data);
        float hitTime = AttackAnimationTable.GetHitTime(move, clip);
        // 连招切换以 Setup 姿势为基准，避免无关键帧的骨骼继承上一段偏移。
        if (_track != null) skeleton.SetBonesToSetupPose();
        _motionBone = string.IsNullOrEmpty(move.rootBone) ? null : skeleton.FindBone(move.rootBone);
        _translation = _motionBone == null ? null : clip.FindTranslateTimelineForBone(_motionBone.Data.Index);
        _startX = _translation?.Evaluate(0f).x ?? 0f;
        float parentScale = 1f;
        for (var parent = _motionBone?.Parent; parent != null; parent = parent.Parent) parentScale *= parent.Data.ScaleX;
        _worldUnitsPerBoneUnit = _spine.transform.TransformVector(new Vector3(skeleton.ScaleX * parentScale, 0f)).x;
        _track = _spine.AnimationState.SetAnimation(0, clip, false);
        // 物理步驱动攻击时钟，渲染阶段只应用同一时刻的骨骼姿势。
        _track.TimeScale = 0f;
        _track.MixDuration = 0f;
        _current = move;
        _elapsed = 0f;
        _hitTime = hitTime;
        _buffered = _hit = false;
        LastHitCount = 0;
        _facing = facing < 0 ? -1 : 1;
        _baseDamage = damage;
        _strength = strength;
        _allowCombo = player;
        _enemyOnly = !player;
        _body.velocity = new Vector2(0, _body.velocity.y);
        return true;
    }

    void FixedUpdate()
    {
        if (!IsAttacking) return;
        if (!CanSwing() || _spine.AnimationState.GetCurrent(0) != _track) { Cancel(); return; }
        _body.velocity = new Vector2(0f, _body.velocity.y);
        float duration = _track.Animation.Duration;
        float next = Mathf.Min(duration, _elapsed + Time.fixedDeltaTime * _current.speed);
        if (!_hit && next >= _hitTime)
        {
            Advance(_hitTime);
            _hit = true;
            ResolveHit();
            if (!IsAttacking) return;
        }
        Advance(next);
        if (_buffered && _elapsed >= _current.comboOpen && _bufferAt <= _current.comboClose
            && _current.comboOpen - _bufferAt <= Profile.bufferSeconds * _current.speed)
        {
            if (Begin(_current.next, _facing, _baseDamage, !_enemyOnly, 1f)) return;
            _buffered = false;
        }
        if (_elapsed >= duration) Cancel();
    }

    void Advance(float time)
    {
        if (_translation != null && time > _elapsed)
        {
            float raw = _translation.Evaluate(time).x - _translation.Evaluate(_elapsed).x;
            MoveHorizontally(raw * _worldUnitsPerBoneUnit);
        }
        _elapsed = time;
        _track.TrackTime = time;
    }

    void MoveHorizontally(float dx)
    {
        if (Mathf.Abs(dx) < .000001f) return;
        float distance = Mathf.Abs(dx);
        var direction = Vector2.right * Mathf.Sign(dx);
        var filter = new ContactFilter2D { useTriggers = false };
        int count = _body.Cast(direction, filter, _walls, distance + .012f);
        for (int i = 0; i < count; i++)
        {
            var hit = _walls[i];
            if (!RunMap.RunMapPhysics.IsTerrain(hit.collider) || Vector2.Dot(hit.normal, direction) > -.35f) continue;
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - .012f));
        }
        if (distance <= 0f) return;
        Vector2 position = _body.position + direction * distance;
        _body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        Physics2D.SyncTransforms();
    }

    void CompensatePose(ISkeletonAnimation _)
    {
        if (!IsAttacking || _translation == null) return;
        _motionBone.X -= _translation.Evaluate(_elapsed).x - _startX;
    }

    void ResolveHit()
    {
        var origin = CombatQuery.Center(transform);
        var center = origin + Vector2.right * (_facing * _current.hitOffset);
        var size = new Vector2(_current.hitWidth, _current.hitHeight);
        int damage = Mathf.Max(1, Mathf.RoundToInt(MetaCharacterStats.AttackDamage(_baseDamage) * _current.damageMultiplier * _strength));
        LastHitCount = DealHits(center, size, damage, _enemyOnly);
        var end = center + Vector2.right * (_facing * size.x * .5f);
        if (LastHitCount > 0) CombatCameraShake.NotifyHit(transform, end);
        RunCombatFeedback.For(this).Strike(origin, end);
    }

    int DealHits(Vector2 center, Vector2 size, int damage, bool enemyOnly)
    {
        int count = 0;
        _targets.Clear();
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            var target = hit.GetComponentInParent<IDamageable>();
            if (!(target is Component actor) || actor.transform == transform || actor.transform.IsChildOf(transform)) continue;
            if (actor.GetComponentInParent<AllyController>() != null || actor.GetComponentInParent<PlayerHealth>() != null) continue;
            if (target is EnemyPlaceholder enemy && enemy.IsDead) continue;
            if (enemyOnly && !(target is EnemyPlaceholder)) continue;
            if (!_targets.Add(target) || !CombatQuery.HasLineOfSight(transform, actor.transform)) continue;
            if (target is EnemyPlaceholder victim) victim.TakeRunDamage(damage);
            else target.TakeDamage(damage);
            count++;
        }
        return count;
    }

    public void Cancel()
    {
        if (!IsAttacking) return;
        _current = null;
        if (_spine != null && _spine.AnimationState.GetCurrent(0) == _track)
        {
            _spine.AnimationState.ClearTrack(0);
            _spine.Skeleton.SetToSetupPose();
            CharacterSpineClips.PlayLocomotion(_spine.AnimationState, _spine.Skeleton.Data, false);
        }
        _track = null;
        _translation = null;
        _motionBone = null;
        _buffered = false;
    }

    void OnDisable() => Cancel();
    void OnDestroy()
    {
        if (_spine != null) _spine.UpdateLocal -= CompensatePose;
    }
}
