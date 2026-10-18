using UnityEngine;

/// <summary>
/// 相机最小跟随（W3.0 spike）：LateUpdate 平滑跟玩家 x/y；没有玩家时不做事。
/// 由 <see cref="PlayerSpawner"/> 在进局时动态挂到 `Camera.main`、撤离时移除。
/// </summary>
public class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 8f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 wanted = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, wanted, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    /// <summary>给指定相机挂上跟随（没有就加），返回组件。</summary>
    public static PlayerCameraFollow Attach(Camera camera, Transform target)
    {
        if (camera == null || target == null) return null;
        PlayerCameraFollow follow = camera.GetComponent<PlayerCameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<PlayerCameraFollow>();
        follow.target = target;
        follow.offset = new Vector3(0f, 0f, camera.transform.position.z - target.position.z);
        return follow;
    }

    /// <summary>移除跟随组件（撤离时调用）。</summary>
    public static void Detach()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        PlayerCameraFollow follow = camera.GetComponent<PlayerCameraFollow>();
        if (follow != null) Object.Destroy(follow);
    }
}
