using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 进局生成器（W3.0 spike）：补地面碰撞 → 放 SpawnPos → 生成受控角色 → 放训练靶 → 挂相机跟随。
///
/// ⚠ **不改 RunMap 源码**：chunk 预制体里没有 Collider2D，所以这里在**原型层**给
///   `Tilemap_Ground` 补 `TilemapCollider2D + CompositeCollider2D + 静态 Rigidbody2D`。
/// 容器：`SpawnRoot`（SpawnPos）与 `PlayerRoot`（角色/靶）都挂在 `PrototypeRunMap` 之外的单例根下，
/// 撤离时统一销毁。
/// </summary>
public static class PlayerSpawner
{
    public const string SpawnRootName = "PrototypeSpawnRoot";
    public const string PlayerRootName = "PrototypePlayerRoot";

    /// <summary>局内相机正交尺寸（比局外取景拉近）。</summary>
    public const float GameplayOrthoSize = 6f;

    private static float _savedOrthoSize = -1f;

    /// <summary>
    /// **必须在生图之前调用**：记下局外相机 orthoSize。
    /// （`RunMapBootstrap.FrameCameraOnEntry` 会在生图时把 ortho 改成整房取景的 12，
    ///   若在生图之后再记，记到的就是 12 而不是用户原本的局外值。）
    /// </summary>
    public static void CaptureOutsideCameraState()
    {
        Camera camera = Camera.main;
        if (camera != null && _savedOrthoSize < 0f) _savedOrthoSize = camera.orthographicSize;
    }

    private static readonly List<GameObject> Spawned = new List<GameObject>();

    public static GameObject CurrentPlayer { get; private set; }
    public static SpawnPoint CurrentSpawnPoint { get; private set; }

    /// <summary>
    /// 进局：补地面碰撞 → 放 SpawnPos → 生成玩家（+ 训练靶）→ 相机跟随。
    /// 返回生成的玩家对象（失败 null）。
    /// </summary>
    public static GameObject SpawnForTeam(List<uint> runTeam)
    {
        Clear();

        EnsureGroundColliders();

        SpawnPoint spawnPoint = PlaceSpawnPoint();
        if (spawnPoint == null)
        {
            ChaosLog.Error(LogChannel.UI, "[玩家] 出生点放置失败（没有入口房信息？）");
            return null;
        }
        CurrentSpawnPoint = spawnPoint;

        PlayerRoot root = GetOrCreate<PlayerRoot>(PlayerRootName);

        //受控角色（本轮只生成 1 个；接口收 runTeam 以便将来 4 人按索引排开）
        GameObject playerPrefab = Resources.Load<GameObject>("Player/Player");
        if (playerPrefab == null)
        {
            ChaosLog.Error(LogChannel.UI, "[玩家] 找不到 Resources/Player/Player.prefab");
            return null;
        }

        Vector3 spawnPosition = spawnPoint.transform.position + new Vector3(0f, 1.0f, 0f);
        GameObject player = Object.Instantiate(playerPrefab, spawnPosition, Quaternion.identity, root.transform);
        player.name = "Player";
        CurrentPlayer = player;
        Spawned.Add(player);

        PlayerLocomotion locomotion = player.GetComponent<PlayerLocomotion>();
        if (locomotion != null) locomotion.SetFacing(spawnPoint.facing);

        //训练靶：出生点右侧 3.5 格
        SpawnTrainingDummy(root.transform, spawnPoint.transform.position + new Vector3(3.5f, 0.5f, 0f));

        //相机跟随
        PlayerCameraFollow.Attach(Camera.main, player.transform);

        //局内相机拉近（记录原值，撤离时恢复）
        Camera camera = Camera.main;
        if (camera != null)
        {
            if (_savedOrthoSize < 0f) _savedOrthoSize = camera.orthographicSize;
            camera.orthographicSize = GameplayOrthoSize;
            ChaosLog.Info(LogChannel.UI, "[局内] 相机 orthographicSize " + _savedOrthoSize + " → " + GameplayOrthoSize);
        }

        //边界墙 + 掉落重生（W3.0b）
        RunBounds.Create(RunMapBootstrap.CurrentLayout, spawnPoint);
        PlayerFallGuard guard = player.GetComponent<PlayerFallGuard>();
        if (guard == null) guard = player.AddComponent<PlayerFallGuard>();
        guard.respawnPoint = spawnPoint.transform;
        guard.fallThresholdY = spawnPoint.transform.position.y - 20f;
        ChaosLog.Info(LogChannel.UI, "[局内] 掉落重生阈值 y < " + guard.fallThresholdY);

        ChaosLog.Success(LogChannel.UI,
            "[玩家] 进局生成完成：队伍 [" + (runTeam == null ? "" : string.Join(",", System.Array.ConvertAll(runTeam.ToArray(), x => x.ToString())))
            + "]，玩家在 " + spawnPosition + "，朝向 " + spawnPoint.facing);
        return player;
    }

    /// <summary>只做"补地面 + 放 SpawnPos"（GM 调试按钮用，不生成角色）。</summary>
    public static SpawnPoint PlaceSpawnPointAndGround()
    {
        EnsureGroundColliders();
        SpawnPoint point = PlaceSpawnPoint();
        CurrentSpawnPoint = point;
        return point;
    }

    /// <summary>撤离：清掉玩家/训练靶/SpawnPos/相机跟随。</summary>
    public static void Clear()
    {
        for (int i = 0; i < Spawned.Count; i++)
            if (Spawned[i] != null) Object.Destroy(Spawned[i]);
        Spawned.Clear();
        CurrentPlayer = null;

        PlayerCameraFollow.Detach();

        //恢复局外相机尺寸
        if (_savedOrthoSize >= 0f)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = _savedOrthoSize;
                ChaosLog.Info(LogChannel.UI, "[局外] 相机 orthographicSize 已恢复为 " + _savedOrthoSize);
            }
            _savedOrthoSize = -1f;
        }

        RunBounds.Clear();

        DestroyRoot(SpawnRootName);
        DestroyRoot(PlayerRootName);
        CurrentSpawnPoint = null;
    }

    // ══════════════════ 内部 ══════════════════

    private static void DestroyRoot(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        while (root != null)
        {
            //Destroy 到帧末才生效 → 先改名，避免同一帧里再次 Find 到旧对象
            root.name = rootName + "_destroying";
            Object.Destroy(root);
            root = GameObject.Find(rootName);
        }
    }

    private static T GetOrCreate<T>(string rootName) where T : Component
    {
        GameObject existing = GameObject.Find(rootName);
        if (existing != null) return existing.GetComponent<T>() ?? existing.AddComponent<T>();

        GameObject go = new GameObject(rootName);
        return go.AddComponent<T>();
    }

    /// <summary>给生成根下所有 Tilemap_Ground 补碰撞体（已有就跳过）。</summary>
    private static void EnsureGroundColliders()
    {
        GameObject mapRoot = RunMapBootstrap.CurrentMapRoot;
        if (mapRoot == null) mapRoot = GameObject.Find("PrototypeRunMap");
        if (mapRoot == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 找不到地图根，跳过地面碰撞体补齐");
            return;
        }

        Tilemap[] tilemaps = mapRoot.GetComponentsInChildren<Tilemap>(true);
        int added = 0;
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null) continue;
            if (tilemap.name != "Tilemap_Ground" && tilemap.name != "Tilemap_Deco") continue;
            if (tilemap.GetComponent<TilemapCollider2D>() != null) continue;

            Rigidbody2D body = tilemap.GetComponent<Rigidbody2D>();
            if (body == null) body = tilemap.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            TilemapCollider2D tileCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            tileCollider.usedByComposite = true;

            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite == null) composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            added++;
        }

        ChaosLog.Info(LogChannel.UI, "[玩家] 地面碰撞体补齐：新增 " + added + " 个 Tilemap（共扫描 " + tilemaps.Length + " 个 Tilemap）");

        //⚠ 地图里还有别的 Collider2D（例如 R1_Entry_L2/CameraBounds 的 BoxCollider2D）——
        //  它们是给相机限位用的**实心碰撞体**，会把角色困住。原型层统一**禁用**非瓦片碰撞体。
        Collider2D[] allColliders = mapRoot.GetComponentsInChildren<Collider2D>(true);
        int disabled = 0;
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            if (collider == null) continue;
            if (collider is CompositeCollider2D) continue;
            if (collider is TilemapCollider2D) continue;
            if (!collider.enabled) continue;
            collider.enabled = false;
            disabled++;
            ChaosLog.Warn(LogChannel.UI, "[玩家] 已禁用地图里的非瓦片碰撞体（避免困住角色）：「" + collider.name + "」 " + collider.GetType().Name);
        }
        if (disabled > 0) ChaosLog.Info(LogChannel.UI, "[玩家] 共禁用 " + disabled + " 个非瓦片碰撞体");
    }

    /// <summary>在入口房左侧找地面并放 SpawnPos。</summary>
    private static SpawnPoint PlaceSpawnPoint()
    {
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        if (layout == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] CurrentLayout 为 null，无法定位入口房");
            return null;
        }

        RoomNode entry = layout.Find(layout.EntryId);
        if (entry == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 找不到入口房 id=" + layout.EntryId);
            return null;
        }

        //1 格 = 1 世界单位；chunk = 40×24 格
        float roomX = entry.CellX * RunMapConst.ChunkW;
        float roomY = entry.CellY * RunMapConst.ChunkH;
        float spawnX = roomX + 2f;                       //"场景左侧"：房间左边界 + 2
        float rayStartY = roomY + RunMapConst.ChunkH - 2f;

        bool hasGround = false;
        float groundY = roomY + 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(spawnX, rayStartY), Vector2.down, RunMapConst.ChunkH + 8f);
        if (hit.collider != null)
        {
            hasGround = true;
            groundY = hit.point.y + 0.1f;
        }
        else
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 出生点射线没打到地面，回退到房间左下角 +0.5");
        }

        PlayerRoot root = GetOrCreate<PlayerRoot>(SpawnRootName);
        GameObject go = new GameObject("SpawnPos");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(spawnX, groundY, 0f);

        SpawnPoint point = go.AddComponent<SpawnPoint>();
        point.facing = 1;
        point.hasGround = hasGround;

        ChaosLog.Info(LogChannel.UI,
            "[玩家] 入口房 Cell=(" + entry.CellX + "," + entry.CellY + ") 房间世界矩形 x[" + roomX + "," + (roomX + RunMapConst.ChunkW) + "]"
            + " y[" + roomY + "," + (roomY + RunMapConst.ChunkH) + "] → SpawnPos=" + go.transform.position
            + " hasGround=" + hasGround);
        return point;
    }

    private static void SpawnTrainingDummy(Transform parent, Vector3 position)
    {
        //落点贴地：向下再打一条射线
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(position.x, position.y + 6f), Vector2.down, 14f);
        if (hit.collider != null) position.y = hit.point.y + 0.5f;

        GameObject prefab = Resources.Load<GameObject>("Enemy/TrainingDummy");
        if (prefab == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 找不到 Resources/Enemy/TrainingDummy.prefab（跳过训练靶）");
            return;
        }

        GameObject dummy = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        dummy.name = "TrainingDummy";
        Spawned.Add(dummy);
        ChaosLog.Info(LogChannel.UI, "[玩家] 训练靶已放在 " + position);
    }

    /// <summary>根节点的标记组件（仅用于 GetOrCreate 的类型参数）。</summary>
    public class PlayerRoot : MonoBehaviour { }
}
