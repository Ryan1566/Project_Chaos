using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity.AnimationTools;
using UnityEngine;

/// <summary>战斗动作表使用动画秒数，伤害范围和偏移使用 Unity 世界单位。</summary>
[Serializable]
public sealed class AttackAnimationTable
{
    public const string ResourcePath = "Combat/AttackAnimations";
    public const string DefaultProfile = "Sword/Test";

    [Serializable]
    public sealed class Profile
    {
        public string id;
        public string light;
        public string heavyMode;
        public string heavy;
        public float holdSeconds;
        public float bufferSeconds;
        public float chargeFullSeconds;
        public float chargeDamageMultiplier;
    }

    [Serializable]
    public sealed class Move
    {
        public string animation;
        public string rootBone;
        public string hitEvent;
        public float hitTime;
        public float hitWidth;
        public float hitHeight;
        public float hitOffset;
        public float damageMultiplier;
        public float speed;
        public string next;
        public float comboOpen;
        public float comboClose;
    }

    [Serializable]
    public sealed class RetainerProfile
    {
        public uint prototypeId;
        public string profile;
    }

    public Profile[] profiles;
    public Move[] moves;
    public RetainerProfile[] retainers;
    static AttackAnimationTable _active;
    readonly Dictionary<string, Move> _lookup = new Dictionary<string, Move>(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { _active = null; }

    public static AttackAnimationTable Load()
    {
        if (_active != null) return _active;
        var source = Resources.Load<TextAsset>(ResourcePath);
        if (source == null) throw new InvalidOperationException("缺少战斗动作表 " + ResourcePath);
        _active = Parse(source.text);
        return _active;
    }

    public Profile FindProfile(string id) => Array.Find(profiles, p => p.id == id);
    public Move FindMove(string name) => !string.IsNullOrEmpty(name) && _lookup.TryGetValue(name, out var value) ? value : null;
    public string FindRetainerProfile(uint prototypeId) => retainers == null
        ? null : Array.Find(retainers, r => r.prototypeId == prototypeId)?.profile;

    public static bool IsAttackName(string name)
    {
        var s = name?.Split('_');
        return s != null && s.Length == 5 && s[0] == "Attack"
            && s[1].Length > 0 && s[2].Length > 0 && (s[3] == "Light" || s[3] == "Heavy")
            && s[4].Length == 3 && int.TryParse(s[4], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int number) && number > 0;
    }

    public static AttackAnimationTable Parse(string json)
    {
        var table = JsonUtility.FromJson<AttackAnimationTable>(json);
        if (table?.profiles == null || table.profiles.Length == 0 || table.moves == null || table.moves.Length == 0)
            throw new FormatException("动作表需要 profiles 和 moves。");

        foreach (var move in table.moves)
        {
            if (move == null || !IsAttackName(move.animation) || table._lookup.ContainsKey(move.animation)
                || !InRange(move.hitTime, 0f, float.MaxValue) || !InRange(move.hitWidth, .001f, float.MaxValue)
                || !InRange(move.hitHeight, .001f, float.MaxValue) || !InRange(move.hitOffset, 0f, float.MaxValue)
                || !InRange(move.speed, .05f, 5f) || !InRange(move.damageMultiplier, .01f, float.MaxValue))
                throw new FormatException("非法或重复招式 " + move?.animation);
            table._lookup.Add(move.animation, move);
        }
        foreach (var move in table.moves)
        {
            if (!string.IsNullOrEmpty(move.next) && (table.FindMove(move.next) == null
                || !InRange(move.comboOpen, move.hitTime, float.MaxValue) || !InRange(move.comboClose, move.comboOpen, float.MaxValue)))
                throw new FormatException("招式接续配置非法 " + move.animation);
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in table.profiles)
        {
            if (p == null || string.IsNullOrEmpty(p.id) || !ids.Add(p.id) || table.FindMove(p.light) == null
                || !p.light.StartsWith("Attack_" + p.id.Replace('/', '_') + "_Light_", StringComparison.Ordinal)
                || !InRange(p.holdSeconds, .1f, .4f) || !InRange(p.bufferSeconds, 0, 1)
                || (p.heavyMode != "none" && p.heavyMode != "animation" && p.heavyMode != "chargeLight")
                || (p.heavyMode == "animation" && (table.FindMove(p.heavy) == null
                    || !p.heavy.StartsWith("Attack_" + p.id.Replace('/', '_') + "_Heavy_", StringComparison.Ordinal)))
                || (p.heavyMode == "chargeLight" && (!InRange(p.chargeFullSeconds, p.holdSeconds, 10f)
                    || !InRange(p.chargeDamageMultiplier, 1f, 10f))))
                throw new FormatException("攻击体系配置非法 " + p?.id);
        }
        if (table.retainers != null)
        {
            var prototypes = new HashSet<uint>();
            foreach (var entry in table.retainers)
                if (entry == null || entry.prototypeId == 0 || !prototypes.Add(entry.prototypeId) || !ids.Contains(entry.profile))
                    throw new FormatException("门客模板的攻击体系配置非法 " + entry?.prototypeId);
        }
        return table;
    }

    static bool InRange(float value, float min, float max) => value >= min && value <= max;

    public static void ValidateClip(Move move, SkeletonData data)
    {
        var clip = data.FindAnimation(move.animation);
        if (clip == null) throw new FormatException("骨骼缺少动画 " + move.animation);
        float hitTime = GetHitTime(move, clip);
        if (hitTime > clip.Duration || (!string.IsNullOrEmpty(move.next) && (move.comboOpen < hitTime || move.comboClose > clip.Duration)))
            throw new FormatException("命中时间或连招窗口超出动画 " + move.animation);
        if (!string.IsNullOrEmpty(move.rootBone)
            && clip.FindTranslateTimelineForBone(data.FindBoneIndex(move.rootBone)) == null)
            throw new FormatException("位移骨缺少平移曲线 " + move.animation + " / " + move.rootBone);
    }

    public static float GetHitTime(Move move, Spine.Animation clip)
    {
        if (string.IsNullOrEmpty(move.hitEvent)) return move.hitTime;
        foreach (var timeline in clip.Timelines)
            if (timeline is EventTimeline events)
                foreach (var marker in events.Events)
                    if (marker.Data.Name == move.hitEvent) return marker.Time;
        throw new FormatException("缺少 Spine 命中事件 " + move.animation + " / " + move.hitEvent);
    }
}
