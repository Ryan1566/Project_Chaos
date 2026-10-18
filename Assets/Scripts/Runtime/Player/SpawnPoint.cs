using UnityEngine;

/// <summary>
/// **出生点标记**（用户点名要的形态：空 GameObject + 本组件）。
///
/// 只存数据 + 画 gizmo，**不含任何玩法逻辑** —— 这样：
/// ① 查找是类型安全的（`FindFirstObjectByType<SpawnPoint>()`，不靠名字/标签）；
/// ② 场景视图里能直接看到出生位置与朝向；
/// ③ 将来 4 人队伍可以按索引排开（本 spike 只用 1 个）。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    /// <summary>朝向：1 = 右，-1 = 左。</summary>
    public int facing = 1;

    /// <summary>gizmo 半径（仅编辑器可视化用）。</summary>
    public float radius = 0.5f;

    /// <summary>是否由射线命中真实地面（调试用；不影响玩法）。</summary>
    public bool hasGround;

    private void OnDrawGizmos()
    {
        Gizmos.color = hasGround ? new Color(0.2f, 0.9f, 0.4f, 1f) : new Color(1f, 0.5f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);

        //朝向箭头
        Vector3 dir = new Vector3(Mathf.Sign(facing == 0 ? 1 : facing), 0f, 0f);
        Gizmos.DrawLine(transform.position, transform.position + dir * (radius + 0.5f));
        Vector3 tip = transform.position + dir * (radius + 0.5f);
        Gizmos.DrawLine(tip, tip + new Vector3(-dir.x * 0.25f, 0.2f, 0f));
        Gizmos.DrawLine(tip, tip + new Vector3(-dir.x * 0.25f, -0.2f, 0f));
    }
}
