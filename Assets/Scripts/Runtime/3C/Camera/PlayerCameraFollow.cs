using UnityEngine;

/// <summary>Compatibility lease for the run spawner. Cinemachine owns camera movement.</summary>
public sealed class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 8f;
    public Vector3 offset = new Vector3(0, 0, -10);
    GameplayCameraRig _rig;
    Transform _boundTarget;
    public static PlayerCameraFollow Attach(Camera camera, Transform target)
    {
        if (camera == null || target == null) return null;
        if (!RunBounds.TryGetMapBounds(out var bounds)) throw new System.InvalidOperationException("Run camera requires valid map bounds");
        var follow = camera.GetComponent<PlayerCameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<PlayerCameraFollow>();
        follow.target = follow._boundTarget = target;
        follow._rig = GameplayCameraRig.For(camera);
        follow.enabled = true; follow._rig.BindRun(target, bounds);
        return follow;
    }
    public static void Detach()
    {
        var camera = Camera.main; var follow = camera == null ? null : camera.GetComponent<PlayerCameraFollow>();
        if (follow != null) follow.enabled = false;
    }
    void OnDisable()
    {
        if (_rig != null) _rig.Release(_boundTarget);
        target = null; _boundTarget = null;
    }
}
