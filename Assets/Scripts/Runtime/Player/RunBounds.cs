using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

/// <summary>
/// 局内边界 + 掉落重生（W3.0b）。
///
/// 硬地图范围来自自然地图计划；旧地图按节点包围矩形回退。
/// 左、右、顶墙在合法范围外，底部保留掉落判定区域。
/// </summary>
public static class RunBounds
{
    public const string RootName = "PrototypeBoundaryRoot";
    /// <summary>墙厚度。</summary>
    public const float WallThickness = 1f;
    /// <summary>墙高度（覆盖整张图足够）。</summary>
    public const float WallHeight = 400f;
    public const float FallMargin = 20f;

    /// <summary>以整张地图最低房间为基准，地下合法坐标不能被当成掉出地图。</summary>
    public static float GetFallThreshold(MapLayout layout, float fallback = -20f)
    {
        if (NaturalMapRuntime.Current != null) return NaturalMapRuntime.Current.Bounds.yMin - FallMargin;
        if (layout == null || layout.Nodes == null) return fallback;
        int minimum = int.MaxValue;
        foreach (RoomNode node in layout.Nodes)
            if (node != null) minimum = Mathf.Min(minimum, node.CellY);
        return minimum == int.MaxValue ? fallback : minimum * RunMapConst.ChunkH - FallMargin;
    }

    public static bool TryGetMapBounds(out Rect bounds)
    {
        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (plan != null)
        {
            bounds = new Rect(plan.Bounds.xMin, plan.Bounds.yMin, plan.Bounds.width, plan.Bounds.height);
            return bounds.width > 0f && bounds.height > 0f;
        }
        return TryGetLegacyBounds(RunMapBootstrap.CurrentLayout, out bounds);
    }

    private static bool TryGetLegacyBounds(MapLayout layout, out Rect bounds)
    {
        bounds = new Rect();
        if (layout == null || layout.Nodes == null) return false;
        bool found = false;
        foreach (RoomNode node in layout.Nodes)
        {
            if (node == null) continue;
            Rect room = new Rect(node.CellX * RunMapConst.ChunkW, node.CellY * RunMapConst.ChunkH,
                RunMapConst.ChunkW, RunMapConst.ChunkH);
            if (!found) { bounds = room; found = true; }
            else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, room.xMin), Mathf.Min(bounds.yMin, room.yMin),
                Mathf.Max(bounds.xMax, room.xMax), Mathf.Max(bounds.yMax, room.yMax));
        }
        return found;
    }

    /// <summary>范围外创建左、右、顶三面墙，保持整个合法地图空间可用。</summary>
    public static void Create(MapLayout layout, SpawnPoint spawnPoint)
    {
        Clear();
        Rect bounds;
        if (NaturalMapRuntime.Current != null)
        {
            if (!TryGetMapBounds(out bounds)) return;
        }
        else if (!TryGetLegacyBounds(layout, out bounds))
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 无可用地图范围，跳过边界墙创建");
            return;
        }
        GameObject root = new GameObject(RootName);
        float bottom = bounds.yMin - FallMargin;
        float centerY = (bottom + bounds.yMax) * 0.5f;
        Vector2 verticalSize = new Vector2(WallThickness, bounds.yMax - bottom + WallThickness);
        CreateWall(root.transform, "RunBoundaryWall_Left", new Vector2(bounds.xMin - WallThickness * 0.5f, centerY), verticalSize);
        CreateWall(root.transform, "RunBoundaryWall_Right", new Vector2(bounds.xMax + WallThickness * 0.5f, centerY), verticalSize);
        CreateWall(root.transform, "RunBoundaryWall_Top", new Vector2(bounds.center.x, bounds.yMax + WallThickness * 0.5f),
            new Vector2(bounds.width + WallThickness * 2f, WallThickness));
        Physics2D.SyncTransforms();
        ChaosLog.Info(LogChannel.UI, "[局内] 硬边界 " + bounds + "，掉落阈值=" + bottom);
    }

    private static void CreateWall(Transform parent, string name, Vector2 center, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(BoxCollider2D), typeof(Rigidbody2D));
        go.transform.SetParent(parent, false);
        go.transform.position = center;

        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        BoxCollider2D box = go.GetComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = false;
    }

    public static void Clear()
    {
        GameObject root = GameObject.Find(RootName);
        while (root != null)
        {
            root.name = RootName + "_destroying";
            root.SetActive(false);
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
