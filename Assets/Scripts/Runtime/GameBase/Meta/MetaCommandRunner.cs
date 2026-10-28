using System;
using UnityEngine;

/// <summary>One synchronous gameplay mutation, one persistence decision, one final-state notification.</summary>
public static class MetaCommandRunner
{
    public delegate bool Mutation(out string reason);
    public static bool IsExecuting { get; private set; }
    public static string CurrentCommand { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { IsExecuting = false; CurrentCommand = null; }

    public static bool Execute(string command, Mutation mutation, out string reason, Action onCommitted = null, Action onRolledBack = null)
    {
        reason = null;
        if (IsExecuting) { reason = "另一玩法命令正在执行，请稍候"; return false; }
        if (mutation == null) { reason = "玩法命令为空"; return false; }
        var session = GameSession.Instance;
        if (!session.CanCommit(out reason)) return false;
        var payload = session.LoadedPayload;
        int owner = session.ContextId;
        string before = JsonUtility.ToJson(payload);
        bool bossBefore = MetaProgressService.Instance.BossClearedThisRun;
        var randomBefore = UnityEngine.Random.state;
        bool committed = false;
        IsExecuting = true; CurrentCommand = command;
        try
        {
            if (mutation(out reason))
            {
                if (owner != session.ContextId || !ReferenceEquals(payload, session.LoadedPayload))
                    reason = "会话已改变，命令不能保存";
                else committed = session.SaveCurrent(out reason);
                if (committed)
                {
                    try { onCommitted?.Invoke(); }
                    catch (Exception error) { Debug.LogError("[MetaCommand] 数据已保存，提交后状态回调失败：" + error); }
                }
            }
        }
        catch (Exception error) { reason = "操作未完成：" + error.Message; }
        finally
        {
            try
            {
                if (!committed)
                {
                    JsonUtility.FromJsonOverwrite(before, payload);
                    MetaProgressService.Instance.RestoreBossCleared(bossBefore);
                    UnityEngine.Random.state = randomBefore;
                    try { onRolledBack?.Invoke(); }
                    catch (Exception error) { Debug.LogError("[MetaCommand] 数据已回滚，回滚后状态回调失败：" + error); }
                }
            }
            finally { IsExecuting = false; CurrentCommand = null; }
        }
        // Observers only see the committed state or the completed rollback, never an intermediate mutation.
        try { MetaRuntimeDriver.NotifyChanged(); }
        catch (Exception error) { Debug.LogError("[MetaCommand] 最终状态通知失败：" + error); }
        return committed;
    }
}
