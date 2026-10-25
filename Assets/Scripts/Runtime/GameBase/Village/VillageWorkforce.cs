using System.Collections.Generic;
using UnityEngine;

/// <summary>Exclusive building jobs prevent duplicate NPCs from multiplying production.</summary>
public static class VillageWorkforce
{
    private sealed class Job { public object Owner; public bool Working; }
    private static readonly Dictionary<uint, Job> Jobs = new Dictionary<uint, Job>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() { Jobs.Clear(); }
    public static bool TryAcquire(uint building, object owner)
    {
        Job job;
        if (Jobs.TryGetValue(building, out job)) return ReferenceEquals(job.Owner, owner);
        Jobs.Add(building, new Job { Owner = owner }); return true;
    }
    public static void Working(uint building, object owner, bool working)
    { if (Jobs.TryGetValue(building, out var job) && ReferenceEquals(job.Owner, owner)) job.Working = working; }
    public static void Release(uint building, object owner)
    { if (Jobs.TryGetValue(building, out var job) && ReferenceEquals(job.Owner, owner)) Jobs.Remove(building); }
    public static bool CanProduce(uint building, object owner)
    { return VillageSceneFlow.IsVillageScene && VillageSceneBootstrap.IsReady && GameSession.Instance.HasSession
            && !DispatchService.Instance.IsInRun && Jobs.TryGetValue(building, out var job) && job.Working && ReferenceEquals(job.Owner, owner); }
    public static int ActiveWorkers(uint building)
    { return Jobs.TryGetValue(building, out var job) && job.Working ? 1 : 0; }
}
