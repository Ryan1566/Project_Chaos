using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit sandbox-only contracts. Never reads or writes a player save slot.</summary>
public static class MetaCommandVerification
{
    [MenuItem("Tools/玩法/验证会话与命令事务")]
    public static void VerifyMenu() { Debug.Log(Verify()); }

    public static string Verify()
    {
        var session = GameSession.Instance;
        if (Application.isPlaying || session.HasSession || SceneTransitionCoordinator.IsBusy || RunMap.RunMapBootstrap.CurrentMapRoot != null)
            throw new InvalidOperationException("Run contracts in Edit mode without an active session, transition or map");
        var checks = new List<string>();
        var random = UnityEngine.Random.state;
        Action observer = null, throwing = null;
        try
        {
            bool mutated = false;
            Check(!session.SaveCurrent(out _) && !MetaCommandRunner.Execute("None", (out string why) =>
                { mutated = true; why = null; return true; }, out _) && !mutated && MetaProgressService.Instance.SaveSlot == 0,
                "no session has no implicit slot or mutation", checks);
            var original = new SavePayload { resMoney = 17 };
            original.runTeam.Add(9);
            session.BeginSandbox(original);
            var payload = session.LoadedPayload;
            payload.runTeam.Add(10);
            Check(session.IsSandbox && session.SelectedSlot == 0 && !ReferenceEquals(payload, original)
                && original.runTeam.Count == 1 && session.SaveCurrent(out _), "sandbox deep copy commits in memory", checks);

            int notifications = 0;
            bool nestedRejected = false, clearRejected = false, beginRejected = false;
            bool callbackDone = false;
            observer = () =>
            {
                notifications++;
                Check(MetaCommandRunner.IsExecuting && callbackDone && payload.resMoney == 23,
                    "final observer sees committed payload and callback while command guard is held", checks);
                nestedRejected = !MetaCommandRunner.Execute("Nested", (out string why) => { why = null; return true; }, out _);
                try { session.Clear(); } catch (InvalidOperationException) { clearRejected = true; }
                try { session.BeginSandbox(); } catch (InvalidOperationException) { beginRejected = true; }
                MetaRuntimeDriver.NotifyChanged();
            };
            MetaRuntimeDriver.Changed += observer;
            Check(MetaCommandRunner.Execute("Success", (out string why) =>
                { payload.resMoney = 23; MetaRuntimeDriver.NotifyChanged(); why = null; return true; }, out _, () => callbackDone = true),
                "successful command commits", checks);
            MetaRuntimeDriver.Changed -= observer; observer = null;
            Check(notifications == 1 && nestedRejected && clearRejected && beginRejected && !MetaCommandRunner.IsExecuting,
                "exactly one notification rejects nested commands, session changes and recursive notification", checks);

            string before = JsonUtility.ToJson(payload);
            bool rolledBack = false, sawRollback = false;
            var beforeRandom = UnityEngine.Random.state;
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.state = beforeRandom;
            observer = () => { sawRollback = rolledBack && JsonUtility.ToJson(payload) == before && !MetaProgressService.Instance.BossClearedThisRun; };
            MetaRuntimeDriver.Changed += observer;
            bool failed = MetaCommandRunner.Execute("Failure", (out string why) =>
            {
                payload.resMoney = 999; payload.runTeam.Clear();
                MetaProgressService.Instance.MarkBossCleared();
                float unused = UnityEngine.Random.value;
                why = "expected rejection"; return false;
            }, out string reason, onRolledBack: () => rolledBack = true);
            MetaRuntimeDriver.Changed -= observer; observer = null;
            Check(!failed && reason == "expected rejection" && sawRollback && UnityEngine.Random.value == expected,
                "false mutation restores payload, list, boss state and RNG before rollback notification", checks);
            Check(!MetaCommandRunner.Execute("Throw", (out string why) =>
                { payload.resMoney = 321; throw new InvalidOperationException("expected mutation error"); }, out _)
                && JsonUtility.ToJson(payload) == before && !MetaCommandRunner.IsExecuting,
                "mutation exception rolls back and releases command guard", checks);

            int delivered = 0;
            throwing = () => { throw new InvalidOperationException("EXPECTED_CONTRACT_LISTENER"); };
            observer = () => { delivered++; MetaRuntimeDriver.NotifyChanged(); };
            MetaRuntimeDriver.Changed += throwing; MetaRuntimeDriver.Changed += observer;
            Check(MetaCommandRunner.Execute("CallbackThrows", (out string why) =>
                { payload.resMoney++; why = null; return true; }, out string successReason,
                () => { throw new InvalidOperationException("EXPECTED_CONTRACT_COMMITTED_CALLBACK"); }) && successReason == null
                && payload.resMoney == 24 && delivered == 1 && !MetaCommandRunner.IsExecuting,
                "commit callback and subscriber exceptions preserve success and later subscribers", checks);
            Check(!MetaCommandRunner.Execute("RollbackCallbackThrows", (out string why) =>
                { payload.resMoney++; why = "expected failure"; return false; }, out reason,
                onRolledBack: () => { throw new InvalidOperationException("EXPECTED_CONTRACT_ROLLBACK_CALLBACK"); })
                && reason == "expected failure" && payload.resMoney == 24 && delivered == 2 && !MetaCommandRunner.IsExecuting,
                "rollback callback exception preserves failure, rollback, delivery and lock release", checks);
            MetaRuntimeDriver.Changed -= throwing; throwing = null;
            MetaRuntimeDriver.Changed -= observer; observer = null;
            int rejectedNotifications = 0;
            observer = () => rejectedNotifications++;
            MetaRuntimeDriver.Changed += observer;
            Check(!MetaCommandRunner.Execute("Null", null, out _) && rejectedNotifications == 0,
                "precondition rejection does not publish a command", checks);
            MetaRuntimeDriver.Changed -= observer; observer = null;

            RunSettlementModel.Begin(true);
            var settlement = RunSettlementModel.Current;
            int oldContext = session.ContextId;
            session.Begin(1, payload, null); // Bind only: no save service read or write.
            Check(session.ContextId != oldContext && !settlement.Confirm(true, out _) && !settlement.ReturnToMeta(out _),
                "stale settlement is rejected even when the same payload reference is rebound", checks);
            session.BeginSandbox();
            var timerObject = new GameObject("MetaTimerContract");
            try
            {
                var driver = timerObject.AddComponent<MetaRuntimeDriver>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var type = typeof(MetaRuntimeDriver);
                var samePayload = session.LoadedPayload;
                int previousContext = session.ContextId;
                type.GetField("_payload", flags).SetValue(driver, samePayload);
                type.GetField("_contextId", flags).SetValue(driver, previousContext);
                type.GetField("_elapsed", flags).SetValue(driver, .4f);
                type.GetField("_saveElapsed", flags).SetValue(driver, 29f);
                session.BeginSandbox();
                // Simulate a reused payload identity without binding or touching any real slot.
                typeof(GameSession).GetField("<LoadedPayload>k__BackingField", flags).SetValue(session, samePayload);
                float delta = Time.deltaTime;
                type.GetMethod("Update", flags).Invoke(driver, null);
                Check((int)type.GetField("_contextId", flags).GetValue(driver) == session.ContextId
                    && Mathf.Approximately((float)type.GetField("_elapsed", flags).GetValue(driver), delta - Mathf.Floor(delta))
                    && Mathf.Approximately((float)type.GetField("_saveElapsed", flags).GetValue(driver), delta >= 30 ? 0 : delta),
                    "new context resets timer accumulators even with identical payload reference", checks);
            }
            finally { UnityEngine.Object.DestroyImmediate(timerObject); }
            return string.Join("\n", checks) + "\nPASS meta contracts: " + checks.Count;
        }
        finally
        {
            if (observer != null) MetaRuntimeDriver.Changed -= observer;
            if (throwing != null) MetaRuntimeDriver.Changed -= throwing;
            session.Clear(); UnityEngine.Random.state = random;
        }
    }
    private static void Check(bool condition, string text, List<string> checks)
    { if (!condition) throw new InvalidOperationException("FAIL " + text); checks.Add("PASS " + text); }
}
