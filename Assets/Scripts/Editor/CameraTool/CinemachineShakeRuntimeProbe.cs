#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Cinemachine;
using UnityEngine;

/// <summary>Independent runtime signal probe; uses temporary objects and does not open player saves.</summary>
public static class CinemachineShakeRuntimeProbe
{
    public static string Status { get; private set; } = "idle";
    static readonly StringBuilder Report = new StringBuilder();
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception(message); Report.AppendLine("PASS " + message); Flush(); }
    static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/3C"); File.WriteAllText(Application.dataPath + "/QA/3C/shake-runtime.txt", Report.ToString()); }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new Exception("Requires idle Play mode");
        Status = "running"; Report.Clear();
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator test)
    {
        while (true)
        {
            object value;
            try { if (!test.MoveNext()) { Status = "passed"; Flush(); yield break; } value = test.Current; }
            catch (Exception error) { test.DisposeIfPossible(); Status = "failed"; Report.AppendLine("FAIL " + error); Flush(); yield break; }
            yield return value;
        }
    }
    static void DisposeIfPossible(this IEnumerator value) { (value as IDisposable)?.Dispose(); }
    static Vector3 Sample(CinemachineVirtualCamera camera)
    { camera.UpdateCameraState(Vector3.up, .02f); camera.transform.parent.GetComponent<CinemachineBrain>().ManualUpdate(); return camera.State.PositionCorrection; }
    static IEnumerator Run()
    {
        var root = new GameObject("CinemachineIndependentShakeProbe");
        float scale = Time.timeScale; UIManager.Instance.HideAllPanelsForGameplay(); Time.timeScale = 1;
        bool unscaled = CinemachineImpulseManager.Instance.IgnoreTimeScale;
        try
        {
            var output = root.AddComponent<Camera>(); output.depth = -100; output.cullingMask = 0; output.orthographic = true;
            var brain = root.AddComponent<CinemachineBrain>(); brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate;
            var vgo = new GameObject("ProbeVCam"); vgo.transform.SetParent(root.transform);
            var vcam = vgo.AddComponent<CinemachineVirtualCamera>(); vcam.m_Lens.ModeOverride = LensSettings.OverrideModes.Orthographic; vcam.m_Lens.OrthographicSize = 6;
            vcam.transform.position = new Vector3(0, 0, -10);
            var ago = new GameObject("RegisteredTarget"); ago.transform.SetParent(root.transform); var a = ago.transform;
            var bgo = new GameObject("LaterTarget"); bgo.transform.SetParent(root.transform); var b = bgo.transform;
            var shake = CombatCameraShake.Attach(output, vcam, a);
            Check(shake != null && vcam.GetComponent<CinemachineImpulseListener>() != null, "real Cinemachine impulse listener attached");
            vcam.UpdateCameraState(Vector3.up, -1); yield return null;
            CombatCameraShake.NotifyHit(a, Vector3.zero); yield return new WaitForSecondsRealtime(.025f);
            Vector3 hit = Sample(vcam);
            Check(hit.sqrMagnitude > .000001f && Mathf.Abs(hit.z) < .00001f && hit.magnitude <= .281f, "normal hit produces bounded XY correction");
            yield return new WaitForSecondsRealtime(.25f);
            Check(Sample(vcam).sqrMagnitude < .000001f, "impulse naturally ends without drift");
            CombatCameraShake.NotifyHurt(a, Vector3.zero); yield return new WaitForSecondsRealtime(.03f);
            Vector3 hurt = Sample(vcam); Check(hurt.magnitude > hit.magnitude, "hurt feedback stronger than normal hit");
            CombatCameraShake.Attach(output, vcam, b); shake.Detach(a);
            CombatCameraShake.NotifyHit(b, Vector3.zero); yield return new WaitForSecondsRealtime(.025f);
            Check(Sample(vcam).sqrMagnitude > .000001f, "stale target detach preserves newer binding");
            shake.Detach(b); yield return null;
            Check(Sample(vcam).sqrMagnitude < .000001f, "detach cancels owned pending correction");
            shake = CombatCameraShake.Attach(output, vcam, b);
            CombatCameraShake.NotifyHit(a, Vector3.zero); yield return new WaitForSecondsRealtime(.025f);
            Check(Sample(vcam).sqrMagnitude < .000001f, "unregistered target cannot shake camera");
            Time.timeScale = 0; CombatCameraShake.NotifyHurt(b, Vector3.zero); yield return new WaitForSecondsRealtime(.03f);
            Check(Sample(vcam).sqrMagnitude < .000001f, "pause rejects new impulses"); Time.timeScale = 1;
            object blocker = new object(); InputManager.Instance.SetGameplayInputBlocked(blocker, true);
            try { CombatCameraShake.NotifyHurt(b, Vector3.zero); yield return new WaitForSecondsRealtime(.03f); Check(Sample(vcam).sqrMagnitude < .000001f, "UI input block rejects new impulses"); }
            finally { InputManager.Instance.SetGameplayInputBlocked(blocker, false); }
            yield return new WaitForSecondsRealtime(.08f);
            for (int i = 0; i < 20; i++) CombatCameraShake.NotifyHit(b, Vector3.zero, true);
            yield return new WaitForSecondsRealtime(.035f);
            var burst = Sample(vcam); Check(burst.magnitude <= .281f && Mathf.Abs(burst.z) < .00001f, "burst cannot amplify beyond configured cap or shift Z");
            shake.enabled = false; yield return null;
            Check(Sample(vcam).sqrMagnitude < .000001f, "disable cancels owned impulse");
            shake = CombatCameraShake.Attach(output, vcam, b);
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true; Time.timeScale = .1f;
            CombatCameraShake.NotifyHurt(b, Vector3.zero); yield return new WaitForSecondsRealtime(.22f);
            var owned = (ICollection)typeof(CombatCameraShake).GetField("_active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shake);
            Check(owned.Count == 0, "unscaled impulse clock expires bookkeeping independently of slow motion");
            CombatCameraShake.NotifyHurt(b, Vector3.zero); Time.timeScale = 0; yield return null;
            Check(Sample(vcam).sqrMagnitude < .000001f, "pause also cancels unscaled-clock impulse");
        }
        finally { CinemachineImpulseManager.Instance.IgnoreTimeScale = unscaled; Time.timeScale = scale; UnityEngine.Object.Destroy(root); UIManager.Instance.RestoreAfterGameplay(); }
    }
}
#endif
