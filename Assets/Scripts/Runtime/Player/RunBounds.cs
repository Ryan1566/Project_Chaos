using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

/// <summary>
/// 局内边界 + 掉落重生（W3.0b）。
///
/// ⚠ 名字/容器刻意与地图里的 `CameraBounds` 区分（那个是相机限位用的实心碰撞体，
///   会被 `PlayerSpawner` 的"禁用非瓦片碰撞体"逻辑禁用）。本类的墙**挂在独立的
///   `PrototypeBoundaryRoot` 下、且不在地图根内**，不会被误伤。
/// </summary>
public static class RunBounds
{
    public const string RootName = "PrototypeBoundaryRoot";
    /// <summary>墙厚度。</summary>
    public const float WallThickness = 1f;
    /// <summary>墙高度（覆盖整张图足够）。</summary>
    public const float WallHeight = 400f;

    /// <summary>按 layout 的节点范围创建左右两面不可见静态墙。</summary>
    public static void Create(MapLayout layout, SpawnPoint spawnPoint)
    {
        Clear();
        if (layout == null || layout.Nodes == null || layout.Nodes.Count == 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] layout 为空，跳过边界墙创建");
            return;
        }

        int minCellX = int.MaxValue, maxCellX = int.MinValue;
        for (int i = 0; i < layout.Nodes.Count; i++)
        {
            RoomNode node = layout.Nodes[i];
            if (node == null) continue;
            if (node.CellX < minCellX) minCellX = node.CellX;
            if (node.CellX > maxCellX) maxCellX = node.CellX;
        }
        if (minCellX == int.MaxValue) { ChaosLog.Warn(LogChannel.UI, "[局内] 没有可用节点，跳过边界墙"); return; }

        float leftX = minCellX * RunMapConst.ChunkW - WallThickness * 0.5f;
        float rightX = (maxCellX + 1) * RunMapConst.ChunkW + WallThickness * 0.5f;
        float centerY = spawnPoint == null ? 0f : spawnPoint.transform.position.y;

        GameObject root = new GameObject(RootName);

        CreateWall(root.transform, "RunBoundaryWall_Left", new Vector2(leftX, centerY));
        CreateWall(root.transform, "RunBoundaryWall_Right", new Vector2(rightX, centerY));

        ChaosLog.Info(LogChannel.UI,
            "[局内] 边界墙已创建：x ∈ [" + leftX + ", " + rightX + "]（cellX " + minCellX + "~" + maxCellX
            + "，chunkW=" + RunMapConst.ChunkW + "）");
    }

    private static void CreateWall(Transform parent, string name, Vector2 center)
    {
        GameObject go = new GameObject(name, typeof(BoxCollider2D), typeof(Rigidbody2D));
        go.transform.SetParent(parent, false);
        go.transform.position = center;

        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        BoxCollider2D box = go.GetComponent<BoxCollider2D>();
        box.size = new Vector2(WallThickness, WallHeight);
        box.isTrigger = false;
    }

    public static void Clear()
    {
        GameObject root = GameObject.Find(RootName);
        while (root != null)
        {
            root.name = RootName + "_destroying";
            Object.Destroy(root);
            root = GameObject.Find(RootName);
        }
    }
}

/// <summary>
/// 掉落重生兜底（W3.0b）：每 0.2s 检查 y，掉到阈值以下就放回出生点。
/// 挂在玩家身上，随玩家一起销毁。
/// </summary>
public class PlayerFallGuard : MonoBehaviour
{
    /// <summary>掉落判定阈值（世界 y）。</summary>
    public float fallThresholdY = -20f;
    /// <summary>检查间隔（秒）。</summary>
    public float checkInterval = 0.2f;
    /// <summary>重生目标（出生点）。</summary>
    public Transform respawnPoint;

    private float _nextCheck;
    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + Mathf.Max(0.05f, checkInterval);

        if (transform.position.y >= fallThresholdY) return;

        Vector3 target = respawnPoint == null ? transform.position : respawnPoint.position + new Vector3(0f, 1f, 0f);
        transform.position = target;
        if (_body != null) _body.velocity = Vector2.zero;

        ChaosLog.Warn(LogChannel.UI,
            "[局内] 玩家掉落，已重生于出生点：触发 y=" + fallThresholdY + " → 回到 " + target);
    }
}
