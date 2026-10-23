using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

// 敌人生成、地形提示及击杀掉落；自然地图消费独立遭遇区与统一投放结果。
public static partial class RunEncounter
{
    // ══════════════════ W5.2 / M14：地形与阵营 ══════════════════

    /// <summary>
    /// M2：**本场景击杀掉落组** = `MapBiome.DropGroupBase`（地下/地表=1 普通、建筑=2 精英）。
    /// 地形读不到时退回普通组并 Warn（§9 E5：组内没数据 → `DropService` 自己 Warn 不掉落）。
    /// </summary>
    private static uint ResolveDropGroup()
    {
        RunMap.BiomeInfo biome = RunMap.RunMapConfigLoader.GetCurrentBiome();
        if (biome == null || biome.DropGroupBase == 0u)
        {
            ChaosLog.Warn(LogChannel.Config, "[地形] 掉落组读不到（地形缺失）→ 退回普通组 " + DropService.GroupNormal + "（§9 E5）");
            return DropService.GroupNormal;
        }

        return biome.DropGroupBase;
    }

    /// <summary>
    /// M3：进场提示「地形：X · 阵营：Y」（3 秒淡出）。**纯文本、不打断操作、不遮挡 HUD**。
    /// 地形取当前场景预设的 `biomeId`；阵营取本局定格的时代映射（`FactionService`）。
    /// </summary>
    private static void ShowTerrainToast(int sceneIndex)
    {
        RunMap.BiomeInfo biome = RunMap.RunMapConfigLoader.GetCurrentBiome();
        string terrain = biome == null ? "未知" : biome.NameCN;
        uint biomeId = biome == null ? 0u : biome.Id;
        uint era = FactionService.ResolveEra();
        string faction = FactionService.GetFactionName(era);

        RunTerrainToast.EnableForRun();
        RunTerrainToast.ShowTerrain(terrain, faction);

        ChaosLog.Info(LogChannel.UI, "[地形] 场景" + sceneIndex + "/" + MaxSceneIndex + " biomeId=" + biomeId
            + " 地形=" + terrain + " 阵营=" + faction + "（Era=" + era
            + "，FactionMul=" + FactionService.GetFactionMul(era).ToString("F2") + "）");
    }

    /// <summary>敌人死亡回调（击杀/掉落死亡都会来；这里只从列表移除，清房由 RefreshRooms 统一判定）。</summary>
    public static void NotifyEnemyKilled(ushort roomId, EnemyPlaceholder enemy, bool allowDrop = true)
    {
        List<EnemyPlaceholder> list;
        if (AliveByRoom.TryGetValue(roomId, out list)) list.Remove(enemy);

        //W3.2d：掉落死亡不触发掉落（防引诱跳崖刷装备 / 进局凭空多掉落）
        if (!allowDrop) return;

        //W3.2b：击杀掉落（可复现：runSeed + killSeq）
        _killSeq++;
        uint itemId, count;
        uint dropGroup = ResolveDropGroup();   //W5.2（M2）：掉落组由地形决定（不再固定普通组）
        if (!DropService.Instance.RollForEnemy(dropGroup, _runSeed, _killSeq, out itemId, out count)) return;

        Vector3 position = enemy == null ? Vector3.zero : enemy.transform.position;
        string reason;
        if (RunInventory.AddItem(itemId, count, out reason)) return;

        //满格/满叠 → 留地可捡
        SpawnGroundDrop(itemId, count, position, reason);
    }

    /// <summary>在指定位置生成可捡掉落物（挂在 PrototypeDropRoot 下）。</summary>
    private static void SpawnGroundDrop(uint itemId, uint count, Vector3 position, string reason)
    {
        GameObject root = GameObject.Find(DropRootName);
        if (root == null) root = new GameObject(DropRootName);

        GameObject go = new GameObject("Drop_" + itemId + "x" + count, typeof(BoxCollider2D), typeof(SpriteRenderer));
        go.transform.SetParent(root.transform, false);
        go.transform.position = position + new Vector3(0f, 0.5f, 0f);
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.2f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/UISprite");
        sr.color = new Color(1f, 0.85f, 0.2f, 1f);
        sr.sortingOrder = 12;

        RunDropItem drop = go.AddComponent<RunDropItem>();
        drop.itemId = itemId;
        drop.count = count;
        ChaosLog.Warn(LogChannel.Config, "[局内库存] 拾取失败（" + reason + "）→ 掉落物留地：" + go.name + " @" + go.transform.position);
    }

    public const string DropRootName = "PrototypeDropRoot";

    /// <summary>清掉所有未捡的掉落物（切场景/撤离）。</summary>
    public static void ClearGroundDrops()
    {
        GameObject root = GameObject.Find(DropRootName);
        if (root == null) return;
        root.name = DropRootName + "_purging";
        Object.Destroy(root);
        ChaosLog.Info(LogChannel.Config, "[局内库存] 已清除未捡掉落物");
    }

    private static bool SpawnEnemiesForScene()
    {
        NaturalMapPlan plan = NaturalMapRuntime.Current;
        if (plan != null) return SpawnNaturalEnemies(plan);
        ChaosLog.Warn(LogChannel.UI, "[诊断] SpawnEnemiesForScene 进入 frameCount=" + Time.frameCount);
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        if (layout == null || layout.Nodes == null) return false;

        GameObject prefab = Resources.Load<GameObject>("Enemy/Enemy_Placeholder");
        if (prefab == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 找不到 Resources/Enemy/Enemy_Placeholder.prefab");
            return false;
        }

        GameObject root = new GameObject("PrototypeEnemyRoot");

        for (int i = 0; i < layout.Nodes.Count; i++)
        {
            RoomNode node = layout.Nodes[i];
            if (node == null) continue;
            if (node.Id == layout.EntryId) continue;

            float x0 = node.CellX * RunMapConst.ChunkW;
            float y0 = node.CellY * RunMapConst.ChunkH;

            var list = new List<EnemyPlaceholder>();
            AliveByRoom[node.Id] = list;
            TotalByRoom[node.Id] = EnemiesPerRoom;

            for (int k = 0; k < EnemiesPerRoom; k++)
            {
                float[] candidates = { x0 + 6f + k * 8f, x0 + RunMapConst.ChunkW * 0.5f, x0 + RunMapConst.ChunkW - 6f };
                Vector2 point;
                if (!PlayerSpawner.TryFindInteriorFloor(x0, y0, candidates, out point))
                {
                    ChaosLog.Warn(LogChannel.UI, "[局内] 房间 " + node.Id + " 找不到室内地面，跳过该敌人");
                    continue;
                }

                GameObject go = Object.Instantiate(prefab, new Vector3(point.x, point.y + 0.6f, 0f), Quaternion.identity, root.transform);
                go.name = "Enemy_" + node.Id + "_" + k;
                EnemyPlaceholder enemy = go.GetComponent<EnemyPlaceholder>();
                if (enemy != null)
                {
                    enemy.roomId = node.Id;
                    enemy.roomX0 = x0;
                    enemy.roomY0 = y0;
                    enemy.fallThresholdY = y0 - RunBounds.FallMargin;
                    enemy.roomW = RunMapConst.ChunkW;
                    enemy.roomH = RunMapConst.ChunkH;
                    list.Add(enemy);
                }
                AllEnemies.Add(go);
            }
        }

        ChaosLog.Info(LogChannel.UI,
            "[局内] 敌人已生成：非入口房间 " + AliveByRoom.Count + " 个 × " + EnemiesPerRoom + " = " + AllEnemies.Count + " 个");
        if (AllEnemies.Count > 0)
        {
            EnemyPlaceholder sample = AllEnemies[0].GetComponent<EnemyPlaceholder>();
            if (sample != null)
                ChaosLog.Info(LogChannel.Config, "[地形] 场景 " + CurrentSceneIndex + " 敌人属性汇总：数量="
                    + AllEnemies.Count + " HP=" + sample.maxHp + " 伤害=" + sample.contactDamage);
        }
        return true;
    }

    private static bool SpawnNaturalEnemies(NaturalMapPlan plan)
    {
        GameObject prefab = Resources.Load<GameObject>("Enemy/Enemy_Placeholder");
        if (prefab == null || prefab.GetComponent<EnemyPlaceholder>() == null)
        {
            ChaosLog.Error(LogChannel.UI, "[局内] 敌人预制体缺失或没有 EnemyPlaceholder，停止遭遇生成");
            return false;
        }

        // Required regions must have their complete planned population before gameplay starts.
        foreach (EncounterRegion region in plan.Regions)
        {
            if (region == null || region.IsEntry || !region.Required) continue;
            int planned = 0;
            foreach (MapPlacement placement in plan.Placements)
                if (placement != null && placement.Kind == MapPlacementKind.Enemy && placement.RegionId == region.Id) planned++;
            int expected = plan.Parameters.enemiesPerRegion;
            if (planned != expected)
            {
                ChaosLog.Error(LogChannel.UI, "[局内] 遭遇区 " + region.StableId + " 敌人投放数 " + planned + "，预期 " + expected);
                return false;
            }
            AliveByRoom[region.Id] = new List<EnemyPlaceholder>();
            TotalByRoom[region.Id] = planned;
        }
        if (TotalByRoom.Count == 0)
        {
            ChaosLog.Error(LogChannel.UI, "[局内] 自然地图没有必经遭遇区，停止初始化以免无法结算");
            return false;
        }

        GameObject root = new GameObject("PrototypeEnemyRoot");
        foreach (MapPlacement placement in plan.Placements)
        {
            if (placement == null || placement.Kind != MapPlacementKind.Enemy) continue;
            EncounterRegion region = plan.FindRegion(placement.RegionId);
            List<EnemyPlaceholder> enemies;
            if (region == null || !AliveByRoom.TryGetValue(region.Id, out enemies)) continue;

            GameObject go = Object.Instantiate(prefab, (Vector3)placement.Position + Vector3.up * 0.6f,
                Quaternion.identity, root.transform);
            go.name = "Enemy_" + placement.StableId;
            EnemyPlaceholder enemy = go.GetComponent<EnemyPlaceholder>();
            enemy.roomId = region.Id;
            enemy.roomX0 = region.Bounds.xMin;
            enemy.roomY0 = region.Bounds.yMin;
            enemy.roomW = region.Bounds.width;
            enemy.roomH = region.Bounds.height;
            enemy.fallThresholdY = plan.Bounds.yMin - RunBounds.FallMargin;
            enemies.Add(enemy);
            AllEnemies.Add(go);
        }
        ChaosLog.Info(LogChannel.UI, "[局内] 自然地图遭遇区 " + TotalByRoom.Count + "，计划敌人 " + AllEnemies.Count
            + "，runSeed=" + plan.RunSeed + " scene=" + plan.SceneIndex);
        return true;
    }

    private static void ClearEnemies()
    {
        for (int i = 0; i < AllEnemies.Count; i++) if (AllEnemies[i] != null) Object.Destroy(AllEnemies[i]);
        AllEnemies.Clear();
        AliveByRoom.Clear();
        TotalByRoom.Clear();
        GameObject root = GameObject.Find("PrototypeEnemyRoot");
        if (root != null) Object.Destroy(root);
    }

}
