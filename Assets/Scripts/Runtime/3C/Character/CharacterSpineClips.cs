using System;
using Spine;

/// <summary>按动作语义查找角色片段，旧骨骼仍可使用动作短名。</summary>
public static class CharacterSpineClips
{
    public const string Idle = "Idle_Normal";
    public const string Run = "Run_Normal";

    public static Animation Find(SkeletonData data, string name)
    {
        if (data == null || string.IsNullOrEmpty(name)) return null;
        var exact = data.FindAnimation(name);
        if (exact != null) return exact;

        string prefix = name + "_";
        var clips = data.Animations;
        Animation first = null;
        foreach (var clip in clips)
        {
            if (!clip.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (first == null || string.CompareOrdinal(clip.Name, first.Name) < 0) first = clip;
        }
        return first;
    }

    public static void PlayLocomotion(AnimationState state, SkeletonData data, bool moving)
    {
        if (state == null) return;
        var clip = Find(data, moving ? Run : Idle) ?? Find(data, Idle);
        if (clip != null && state.GetCurrent(0)?.Animation != clip)
            state.SetAnimation(0, clip, true);
    }
}
