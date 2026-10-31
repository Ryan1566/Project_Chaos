#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Cinemachine;
using UnityEngine;

/// <summary>Measured framing, warp, aspect and lifecycle checks, using temporary targets without saves.</summary>
public static class CinemachineFramingRuntimeProbe
{
    public static string Status { get; private set; } = "idle";
    static readonly StringBuilder Report = new StringBuilder();
    static void Check(bool value, string message)
    { if (!value) throw new Exception(message); Report.AppendLine("PASS " + message); Flush(); }
    static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/3C"); File.WriteAllText(Application.dataPath + "/QA/3C/framing-runtime.txt", Report.ToString()); }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new Exception("Requires idle Play mode");
        Status = "running"; Report.Clear(); UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        while (true)
        {
            object value;
            try { if (!run.MoveNext()) { Status = "passed"; Flush(); yield break; } value = run.Current; }
            catch (Exception error) { (run as IDisposable)?.Dispose(); Status = "failed"; Report.AppendLine("FAIL " + error); Flush(); yield break; }
            yield return value;
        }
    }
    static IEnumerator Run()
    {
        var output = Camera.main; float aspect = output.aspect;
        var actor = new GameObject("CameraFramingTemporaryTarget");
        var body = actor.AddComponent<Rigidbody2D>(); body.simulated = false;
        var capsule = actor.AddComponent<CapsuleCollider2D>(); capsule.size = new Vector2(.7f, 1.7f); capsule.offset = Vector2.up * .85f;
        var movement = actor.AddComponent<PlayerLocomotion>();
        GameObject hitTarget = null;
        UIManager.Instance.HideAllPanelsForGameplay(); var rig = GameplayCameraRig.For(output);
        try
        {
            GameplayCameraRig.NotifyTargetWarped(null, Vector3.zero);
            Check(rig.Mode == GameplayCameraRig.CameraMode.None, "null warp before first binding is harmless");
            Rect bounds = new Rect(-50, -30, 100, 60); rig.BindRun(actor.transform, bounds); yield return null;
            var transposer = rig.VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            Check(transposer.m_YDamping > transposer.m_XDamping && transposer.m_DeadZoneHeight > 0, "vertical damping and jump buffer configured");
            float y = output.transform.position.y; actor.transform.position += Vector3.up * .5f;
            yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(output.transform.position.y - y) < .03f, "small vertical motion stays inside jump buffer");
            actor.transform.position += Vector3.up * 12; yield return new WaitForSecondsRealtime(.25f);
            Check(output.transform.position.y > y + .3f, "large vertical change follows beyond buffer");
            actor.transform.position = Vector3.zero; movement.SetFacing(1); rig.SnapToTarget(); yield return null;
            float x = output.transform.position.x; movement.SetFacing(-1); yield return null;
            Check(Mathf.Abs(output.transform.position.x - x) < .4f, "turning does not instantly jump camera");
            yield return new WaitForSecondsRealtime(1.4f);
            Check(output.transform.position.x < x - .5f, "facing gradually biases view toward travel direction");
            Vector3 delta = new Vector3(30, 7); actor.transform.position += delta;
            GameplayCameraRig.NotifyTargetWarped(actor.transform, delta); yield return null;
            var view = output.WorldToViewportPoint(actor.transform.position);
            Check(view.x > .2f && view.x < .8f && view.y > .2f && view.y < .8f, "warp resets damping within one frame");
            int revision = rig.BoundaryRevision; yield return new WaitForSecondsRealtime(.2f);
            Check(rig.BoundaryRevision == revision, "unchanged view does not rebuild boundary cache");
            actor.transform.position = new Vector3(49, 0); rig.SnapToTarget();
            output.aspect = .75f; GameplayCameraRig.NotifyTargetWarped(actor.transform, Vector3.zero);
            Check(Mathf.Abs(output.transform.position.x - (bounds.xMax - output.orthographicSize * output.aspect)) < .05f, "same-frame aspect change and warp use refreshed view size");
            yield return null; yield return null;
            Check(rig.BoundaryRevision == revision + 1, "aspect change invalidates boundary once");
            actor.transform.position = new Vector3(49, 0); rig.SnapToTarget(); yield return null;
            float hx = output.orthographicSize * output.aspect;
            Check(output.transform.position.x <= bounds.xMax - hx + .01f, "narrow-aspect viewport respects right boundary");
            output.aspect = 2.4f; yield return null; rig.SnapToTarget(); yield return null;
            hx = output.orthographicSize * output.aspect;
            Check(output.transform.position.x <= bounds.xMax - hx + .01f, "wide-aspect viewport respects right boundary");
            actor.transform.position = Vector3.zero; rig.BindRun(actor.transform, new Rect(-1, -1, 2, 2)); yield return new WaitForSecondsRealtime(.2f);
            Vector3 small = output.transform.position; yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(small.x) < .02f && Mathf.Abs(small.y) < .02f && Vector3.Distance(small, output.transform.position) < .01f, "map smaller than viewport remains centered and stable");
            bool rejected = false;
            try { rig.BindRun(actor.transform, new Rect(float.NaN, 0, 10, 10)); } catch (ArgumentException) { rejected = true; }
            Check(rejected && rig.Target == actor.transform, "invalid bounds rejected before changing binding");
            hitTarget = new GameObject("CameraProbeEnemy"); hitTarget.transform.position = new Vector3(-.65f, .85f);
            var damaged = hitTarget.AddComponent<CinemachineProbeDamageTarget>(); hitTarget.AddComponent<BoxCollider2D>().size = new Vector2(.2f, .4f);
            var second = new GameObject("SecondCollider"); second.transform.SetParent(hitTarget.transform, false); second.AddComponent<BoxCollider2D>().size = new Vector2(.2f, .4f);
            var attack = actor.AddComponent<PlayerAttack>(); Physics2D.SyncTransforms();
            typeof(PlayerAttack).GetMethod("DoAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(attack, null);
            Check(damaged.Hits == 1 && attack.LastHitCount == 1, "real attack deduplicates multiple colliders on one target");
            var active = (ICollection)typeof(CombatCameraShake).GetField("_active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(output.GetComponent<CombatCameraShake>());
            Check(active.Count == 1, "one real attack emits one camera impulse");
            yield return new WaitForSecondsRealtime(.3f); hitTarget.transform.position = Vector3.right * 100; Physics2D.SyncTransforms();
            typeof(PlayerAttack).GetMethod("DoAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(attack, null);
            Check(attack.LastHitCount == 0 && active.Count == 0, "empty swing does not emit camera impulse");
            Check(!rig.Release(output.transform) && rig.Target == actor.transform, "stale owner cannot release current target");
            Check(rig.Release(actor.transform) && !output.GetComponent<CinemachineBrain>().enabled && !rig.VirtualCamera.gameObject.activeSelf, "release disables owned output control and virtual camera");
        }
        finally
        {
            rig.Release(actor.transform); output.aspect = aspect;
            UnityEngine.Object.Destroy(actor); UnityEngine.Object.Destroy(rig);
            if (hitTarget != null) UnityEngine.Object.Destroy(hitTarget);
            UIManager.Instance.RestoreAfterGameplay();
        }
        yield return null;
        Check(GameObject.Find("GameplayCinemachineRig") == null && output.GetComponent<CinemachineBrain>() == null, "destroy removes owned rig objects and Brain");
        Check(Resources.Load<GameplayCameraSettings>(GameplayCameraSettings.ResourcesPath) != null, "borrowed settings asset survives cleanup");
    }
}
public sealed class CinemachineProbeDamageTarget : MonoBehaviour, IDamageable
{
    public int Hits { get; private set; }
    public void TakeDamage(int amount) { if (amount > 0) Hits++; }
}
#endif
