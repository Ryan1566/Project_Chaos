using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 进局生成器（W3.0 spike）：补地面碰撞 → 放 SpawnPos → 生成受控角色 → 放训练靶 → 挂相机跟随。
///
/// 自然地图的出生与投放点来自 NaturalMapPlan，地形碰撞由装配器负责。
/// legacy chunk 缺失地面碰撞时仍保留补齐路径。
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

        ChaosLog.Warn(LogChannel.UI, "[诊断] SpawnForTeam 进入 frameCount=" + Time.frameCount + " 队伍=" + (runTeam == null ? 0 : runTeam.Count));

        EnsureGroundColliders();

        SpawnPoint spawnPoint = PlaceSpawnPoint();
        if (spawnPoint == null)
        {
            ChaosLog.Error(LogChannel.UI, "[玩家] 出生点放置失败（没有入口房信息？）");
            return null;
        }
        CurrentSpawnPoint = spawnPoint;

        NaturalMapPlan naturalPlan = NaturalMapRuntime.Current;
        if (naturalPlan != null && runTeam != null)
        {
            for (int i = 0; i < Mathf.Min(runTeam.Count - 1, 3); i++)
            {
                if (naturalPlan.FindPlacement(MapPlacementKind.Ally, i) != null) continue;
                ChaosLog.Error(LogChannel.UI, "[门客] 缺少完整队伍的安全投放点，取消角色生成");
                return null;
            }
        }

        PlayerRoot root = GetOrCreate<PlayerRoot>(PlayerRootName);

        //受控角色（本轮只生成 1 个；接口收 runTeam 以便将来 4 人按索引排开）
        GameObject playerPrefab = Resources.Load<GameObject>("Player/Player");
        if (playerPrefab == null)
        {
            ChaosLog.Error(LogChannel.UI, "[玩家] 找不到 Resources/Player/Player.prefab");
            return null;
        }

        Vector3 spawnPosition = spawnPoint.transform.position + new Vector3(0f, 1.0f, 0f);
        //防重复：先清掉场景里已有的受控玩家（W3.1b 遗留2）
        PlayerLocomotion[] existingPlayers = Object.FindObjectsOfType<PlayerLocomotion>();
        if (existingPlayers.Length > 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[玩家] 生成前发现已存在的玩家 " + existingPlayers.Length + " 个，先清理（防重复）");
            for (int i = 0; i < existingPlayers.Length; i++) if (existingPlayers[i] != null) Object.Destroy(existingPlayers[i].gameObject);
        }

        GameObject player = Object.Instantiate(playerPrefab, spawnPosition, Quaternion.identity, root.transform);
        player.name = "Player";
        CurrentPlayer = player;
        Spawned.Add(player);

        //血量（W3.1b）
        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();
        if (player.GetComponent<RunInteractionHud>() == null) player.AddComponent<RunInteractionHud>();

        //AI 门客（W3.1b）：runTeam[1..] 最多 3 个
        SpawnAllies(root.transform, runTeam, spawnPoint);

        PlayerLocomotion locomotion = player.GetComponent<PlayerLocomotion>();
        if (locomotion != null) locomotion.SetFacing(spawnPoint.facing);

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
        guard.fallThresholdY = RunBounds.GetFallThreshold(RunMapBootstrap.CurrentLayout,
            spawnPoint.transform.position.y - RunBounds.FallMargin);
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
    public static void Clear(bool restoreCamera = true)
    {
        for (int i = 0; i < Spawned.Count; i++)
            if (Spawned[i] != null) Object.Destroy(Spawned[i]);
        Spawned.Clear();
        CurrentPlayer = null;

        AllyController.ClearAll();
        PlayerCameraFollow.Detach();

        //恢复局外相机尺寸（切场景时不恢复）
        if (restoreCamera && _savedOrthoSize >= 0f)
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
        if (NaturalMapRuntime.Current != null)
        {
            // NaturalMap assembler owns collision geometry, including intentional solid props.
            Physics2D.SyncTransforms();
            return;
        }
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
            //⚠ 新加的 CompositeCollider2D 在同一帧里几何体还没生成 → 立刻 GenerateGeometry，
            //   否则紧接着的室内射线会全部打空（W3.1a 实测踩到）。
            composite.GenerateGeometry();
            added++;
        }

        ChaosLog.Info(LogChannel.UI, "[玩家] 地面碰撞体补齐：新增 " + added + " 个 Tilemap（共扫描 " + tilemaps.Length + " 个 Tilemap）");

        // Legacy CameraBounds is an authoring range, not a physical obstacle.
        // All other colliders keep their authored behavior.
        Collider2D[] allColliders = mapRoot.GetComponentsInChildren<Collider2D>(true);
        int disabled = 0;
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            if (collider == null) continue;
            if (collider is CompositeCollider2D) continue;
            if (collider is TilemapCollider2D) continue;
            if (collider.name != "CameraBounds") continue;
            if (!collider.enabled) continue;
            collider.enabled = false;
            disabled++;
            ChaosLog.Info(LogChannel.UI, "[玩家] 已忽略旧地图的 CameraBounds 辅助范围");
        }
        if (disabled > 0) ChaosLog.Info(LogChannel.UI, "[玩家] 已忽略 " + disabled + " 个旧相机辅助范围");

        //让新碰撞体立刻对 Physics2D 查询可见
        Physics2D.SyncTransforms();
    }

    /// <summary>
    /// **室内地面射线**（W3.1a 修正）：从房间垂直中部向下打，避免打到天花板/屋檐。
    /// 依次尝试多个 x，取第一个命中且 y 落在房间内部区间 `(roomBottom+1, roomTop-1)` 的结果。
    /// </summary>
    public static bool TryFindInteriorFloor(float roomLeftX, float roomBottomY, float[] candidateXs, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;
        float roomTop = roomBottomY + RunMapConst.ChunkH;
        float startY = roomBottomY + RunMapConst.ChunkH * 0.5f;
        float distance = RunMapConst.ChunkH;

        for (int i = 0; i < candidateXs.Length; i++)
        {
            float x = candidateXs[i];
            if (x <= roomLeftX || x >= roomLeftX + RunMapConst.ChunkW) continue;

            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, startY), Vector2.down, distance);
            if (hit.collider == null) continue;
            if (hit.point.y <= roomBottomY + 1f || hit.point.y >= roomTop - 1f) continue;

            hitPoint = new Vector2(x, hit.point.y);
            return true;
        }
        return false;
    }

    /// <summary>在入口房左侧、**室内地面**上放 SpawnPos（W3.1a 修正）。</summary>
    private static SpawnPoint PlaceSpawnPoint()
    {
        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (plan != null)
        {
            MapPlacement placement = plan.FindPlacement(MapPlacementKind.Player);
            if (placement == null)
            {
                ChaosLog.Error(LogChannel.UI, "[玩家] 自然地图计划缺少玩家安全出生点");
                return null;
            }
            PlayerRoot naturalRoot = GetOrCreate<PlayerRoot>(SpawnRootName);
            GameObject naturalSpawn = new GameObject("SpawnPos");
            naturalSpawn.transform.SetParent(naturalRoot.transform, false);
            naturalSpawn.transform.position = placement.Position;
            SpawnPoint naturalPoint = naturalSpawn.AddComponent<SpawnPoint>();
            naturalPoint.facing = placement.Facing < 0 ? -1 : 1;
            naturalPoint.hasGround = true;
            ChaosLog.Info(LogChannel.UI, "[玩家] 自然地图出生点 " + placement.StableId + " @" + placement.Position);
            return naturalPoint;
        }
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        if (layout == null) { ChaosLog.Warn(LogChannel.UI, "[玩家] CurrentLayout 为 null，无法定位入口房"); return null; }
        RoomNode entry = layout.Find(layout.EntryId);
        if (entry == null) { ChaosLog.Warn(LogChannel.UI, "[玩家] 找不到入口房 id=" + layout.EntryId); return null; }

        float roomX = entry.CellX * RunMapConst.ChunkW;
        float roomY = entry.CellY * RunMapConst.ChunkH;
        float roomTop = roomY + RunMapConst.ChunkH;

        //室内射线：优先左侧（贴近"场景左侧"语义），再房间中部、右侧
        float[] candidates = { roomX + 3f, roomX + RunMapConst.ChunkW * 0.5f, roomX + RunMapConst.ChunkW - 3f };
        Vector2 hit;
        bool hasGround = TryFindInteriorFloor(roomX, roomY, candidates, out hit);

        float spawnX = roomX + 2f;
        float spawnY;
        if (hasGround)
        {
            spawnY = hit.y + 0.1f;
        }
        else
        {
            int[] rows = RunMapConst.ProfileFloorRow;
            int midRow = rows == null || rows.Length == 0 ? 4 : rows[rows.Length / 2];
            spawnY = roomY + midRow + 0.1f;
            ChaosLog.Warn(LogChannel.UI, "[玩家] 室内射线全部未命中，回退到 ProfileFloorRow 中部行 " + midRow + "（y=" + spawnY + "）");
        }

        PlayerRoot root = GetOrCreate<PlayerRoot>(SpawnRootName);
        GameObject go = new GameObject("SpawnPos");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        SpawnPoint point = go.AddComponent<SpawnPoint>();
        point.facing = 1;
        point.hasGround = hasGround;

        bool insideRoom = spawnY > roomY && spawnY < roomTop;
        ChaosLog.Info(LogChannel.UI,
            "[玩家] 入口房 Cell=(" + entry.CellX + "," + entry.CellY + ")  roomTop=" + roomTop
            + "  射线起点 y=" + (roomY + RunMapConst.ChunkH * 0.5f) + "  命中 y=" + (hasGround ? hit.y.ToString("F2") : "无")
            + "  命中点 x=" + (hasGround ? hit.x.ToString("F2") : "无")
            + "  → SpawnPos=" + go.transform.position + "  hasGround=" + hasGround
            + "  在房间内部=" + insideRoom);
        if (!insideRoom) ChaosLog.Error(LogChannel.UI, "[玩家] ★SpawnPos 不在房间内部（y=" + spawnY + "，房间 y 范围 [" + roomY + "," + roomTop + "]）");
        return point;
    }

    /// <summary>按 runTeam 生成 AI 门客（第 1 位是受控玩家，其余最多 3 个）。</summary>
    private static void SpawnAllies(Transform parent, List<uint> runTeam, SpawnPoint spawnPoint)
    {
        ChaosLog.Warn(LogChannel.UI, "[诊断] SpawnAllies 进入 frameCount=" + Time.frameCount);
        if (runTeam == null || runTeam.Count <= 1) return;
        GameObject allyPrefab = Resources.Load<GameObject>("Player/Ally");
        if (allyPrefab == null) { ChaosLog.Warn(LogChannel.UI, "[门客] 找不到 Resources/Player/Ally.prefab"); return; }

        //W3.1m：清理统一由 RunEncounter.PurgeAllRuntimeObjects() 负责，这里不再 ClearAll（否则会把刚生成的清掉）

        int count = Mathf.Min(runTeam.Count - 1, 3);
        for (int i = 0; i < count; i++)
        {
            Vector3 position = spawnPoint.transform.position + new Vector3(-1.2f * (i + 1), 1f, 0f);
            NaturalMapPlan plan = NaturalMapRuntime.Current;
            if (plan != null)
            {
                MapPlacement placement = plan.FindPlacement(MapPlacementKind.Ally, i);
                if (placement == null)
                {
                    ChaosLog.Error(LogChannel.UI, "[门客] 自然地图缺少第 " + (i + 1) + " 个门客安全投放点，停止生成");
                    break;
                }
                position = (Vector3)placement.Position + Vector3.up;
            }
            GameObject ally = Object.Instantiate(allyPrefab, position, Quaternion.identity, parent);
            AllyController controller = ally.GetComponent<AllyController>();
            if (controller != null)
            {
                controller.followIndex = i + 1;
                controller.memberId = runTeam[i + 1];
                controller.fallThresholdY = RunBounds.GetFallThreshold(RunMapBootstrap.CurrentLayout);
                //统一命名：Ally_{位序}(id={门客id}) —— 日志/来源全用它
                ally.name = "Ally_" + (i + 1) + "(id=" + runTeam[i + 1] + ")";
            }

            //C：门客与玩家、门客之间互不碰撞（保留与地形/边界墙的碰撞）
            {
                Collider2D allyCollider = ally.GetComponent<Collider2D>();
                Collider2D playerCollider = CurrentPlayer == null ? null : CurrentPlayer.GetComponent<Collider2D>();
                if (allyCollider != null && playerCollider != null) Physics2D.IgnoreCollision(playerCollider, allyCollider, true);
                for (int k = 0; k < Spawned.Count; k++)
                {
                    if (Spawned[k] == null || Spawned[k] == ally) continue;
                    Collider2D other = Spawned[k].GetComponent<Collider2D>();
                    if (other != null && allyCollider != null) Physics2D.IgnoreCollision(other, allyCollider, true);
                }
            }
            Spawned.Add(ally);
        }
        ChaosLog.Info(LogChannel.UI, "[门客] 已生成 AI 门客 " + count + " 个（队伍 " + runTeam.Count + " 人）");
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
