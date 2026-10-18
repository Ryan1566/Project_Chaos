using UnityEngine;

/// <summary>
/// 相机最小跟随（W3.0 spike）：LateUpdate 平滑跟玩家 x/y；没有玩家时不做事。
/// 由 <see cref="PlayerSpawner"/> 在进局时挂到 `Camera.main`、撤离时**只禁用不销毁**（W4f 修 P0）。
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

    /// <summary>
    /// 给指定相机挂上跟随：**已有组件（含 disabled）就复用并重新启用**，没有才 AddComponent。
    ///
    /// ⚠ 为什么必须"复用 + 显式 enabled = true"（W4f 修 P0）：
    /// `Object.Destroy(component)` 到**帧末**才真正移除，但在同一帧里 `GetComponent` 仍能拿到它，
    /// 且它的 `enabled` 已被置为 false。撤离后**同帧**再进局时，旧代码会把这个"待销毁"组件
    /// 当成可复用组件、只改 target 不重新启用 → 帧末组件被销毁 → 相机跟随永久丢失。
    /// 现在 `Detach()` 只禁用不销毁，本方法再兜一层"复用并 enabled = true"，两条路都不会丢。
    /// </summary>
    public static PlayerCameraFollow Attach(Camera camera, Transform target)
    {
        if (camera == null || target == null) return null;

        //A：先找已有组件（`GetComponent` 对 inactive GameObject 同样有效），有则复用
        PlayerCameraFollow follow = camera.GetComponent<PlayerCameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<PlayerCameraFollow>();

        follow.target = target;
        follow.offset = new Vector3(0f, 0f, camera.transform.position.z - target.position.z);
        follow.enabled = true;
        return follow;
    }

    /// <summary>
    /// 移除跟随（撤离时调用）：**只禁用、不销毁**（方案 B）。
    /// 由 `Attach` 复用同一个组件，从根上避开"Destroy 帧末生效 vs 同帧 Attach 复用"的竞态。
    /// </summary>
    public static void Detach()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        PlayerCameraFollow follow = camera.GetComponent<PlayerCameraFollow>();
        if (follow != null)
        {
            follow.target = null;
            follow.enabled = false;
        }
    }
}
