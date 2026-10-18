using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

/// <summary>
/// 局内遭遇战 / 场景链（W3.1d 修正版）。
///
/// **C 的核心改动**：清房判定从"事件计数"改为**每 0.3s 按活体重算** ——
/// 对每个房间统计「存活且在该房间矩形内」的敌人数，`存活==0` 即已清（无论打死还是掉落死亡）。
/// 这样 `DumpProgress` 里 `0/2 但 已清 0` 的自相矛盾不会再出现。
/// </summary>
public static class RunEncounter
{
    public const int MaxSceneIndex = 3;
    public const int EnemiesPerRoom = 2;
    /// <summary>清房重算间隔。</summary>
    public const float ScanInterval = 0.3f;

    private static readonly Dictionary<ushort, List<EnemyPlaceholder>> AliveByRoom = new Dictionary<ushort, List<EnemyPlaceholder>>();
    private static readonly Dictionary<ushort, int> TotalByRoom = new Dictionary<ushort, int>();
    private static readonly List<GameObject> AllEnemies = new List<GameObject>();
    private static readonly List<uint> Team = new List<uint>();
    private static bool _clearedCurrentScene;

    public static int CurrentSceneIndex { get; private set; }
    public static bool RunCleared { get; private set; }

    public static int TotalRooms { get { return TotalByRoom.Count; } }
    public static int ClearedRooms { get { return _clearedRooms; } }
    private static int _clearedRooms;

    /// <summary>GM 调试：强制重算（也可等 ticker 自己跑）。</summary>
    public static void RefreshNow() { RefreshRooms(); }

    /// <summary>**唯一入口**：进入指定场景（1~3）。</summary>
    public static bool EnterScene(int sceneIndex, List<uint> runTeam = null)
    {
        if (sceneIndex < 1 || sceneIndex > MaxSceneIndex)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 场景序号越界：" + sceneIndex);
            return false;
        }

        if (runTeam != null) { Team.Clear(); Team.AddRange(runTeam); }

        ClearEnemies();
        PlayerSpawner.Clear(false);
        PrototypeMapSpawner.Clear();

        GameObject container = PrototypeMapSpawner.Spawn(sceneIndex);
        if (container == null)
        {
            ChaosLog.Error(LogChannel.UI, "[局内] 场景 " + sceneIndex + " 生成失败");
            return false;
        }

        CurrentSceneIndex = sceneIndex;
        _clearedCurrentScene = false;
        _clearedRooms = 0;
        AliveByRoom.Clear();
        TotalByRoom.Clear();

        GameObject player = PlayerSpawner.SpawnForTeam(Team);
        if (player == null)
        {
            ChaosLog.Error(LogChannel.UI, "[局内] 场景 " + sceneIndex + " 玩家生成失败");
            return false;
        }

        HideStrayCitizen();
        SpawnEnemiesForScene();
        EnsureTicker();

        ChaosLog.Success(LogChannel.UI,
            "[局内] 进入场景 " + sceneIndex + "/" + MaxSceneIndex + "，地图根=" + container.name
            + "，房间 " + TotalRooms + " 个，敌人 " + AllEnemies.Count + " 个");
        return true;
    }

    /// <summary>清空当前场景（GM 调试用）。</summary>
    public static int KillAllEnemiesInScene()
    {
        int killed = 0;
        for (int i = AllEnemies.Count - 1; i >= 0; i--)
        {
            if (AllEnemies[i] == null) continue;
            EnemyPlaceholder enemy = AllEnemies[i].GetComponent<EnemyPlaceholder>();
            if (enemy == null) continue;
            enemy.TakeDamage(9999);
            killed++;
        }
        RefreshRooms();
        ChaosLog.Info(LogChannel.UI, "[局内] GM：已清空当前场景敌人 " + killed + " 个");
        return killed;
    }

    public static bool SkipToNextScene()
    {
        int next = CurrentSceneIndex + 1;
        if (next > MaxSceneIndex)
        {
            ChaosLog.Info(LogChannel.UI, "[局内] 已在最后场景，无法再跳");
            return false;
        }
        ChaosLog.Info(LogChannel.UI, "[局内] GM：跳转到场景 " + next);
        return EnterScene(next, Team);
    }

    public static string DumpProgress()
    {
        int alive = 0, total = 0;
        foreach (KeyValuePair<ushort, List<EnemyPlaceholder>> pair in AliveByRoom)
        {
            int roomTotal;
            TotalByRoom.TryGetValue(pair.Key, out roomTotal);
            total += roomTotal;
            for (int i = 0; i < pair.Value.Count; i++)
                if (pair.Value[i] != null && !pair.Value[i].IsDead) alive++;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append("[局内] 场景 ").Append(CurrentSceneIndex).Append("/").Append(MaxSceneIndex)
          .Append("  存活 ").Append(alive).Append("/").Append(total)
          .Append("  已清 ").Append(_clearedRooms).Append("/").Append(TotalRooms)
          .Append("  通关=").Append(RunCleared);
        foreach (KeyValuePair<ushort, List<EnemyPlaceholder>> pair in AliveByRoom)
        {
            int a = 0, t = 0;
            TotalByRoom.TryGetValue(pair.Key, out t);
            for (int i = 0; i < pair.Value.Count; i++)
                if (pair.Value[i] != null && !pair.Value[i].IsDead) a++;
            sb.Append("\n    房间 ").Append(pair.Key).Append("：存活 ").Append(a).Append("/").Append(t)
              .Append(a == 0 ? "（已清）" : "");
        }
        return sb.ToString();
    }

    /// <summary>敌人死亡回调（击杀/掉落死亡都会来；这里只从列表移除，清房由 RefreshRooms 统一判定）。</summary>
    public static void NotifyEnemyKilled(ushort roomId, EnemyPlaceholder enemy)
    {
        List<EnemyPlaceholder> list;
        if (!AliveByRoom.TryGetValue(roomId, out list)) return;
        list.Remove(enemy);
    }

    /// <summary>**按活体重算清房**：每房间统计"存活且在本房间矩形内"的敌人；0 即已清。</summary>
    private static void RefreshRooms()
    {
        if (AliveByRoom.Count == 0) return;

        List<ushort> clearedNow = null;
        foreach (KeyValuePair<ushort, List<EnemyPlaceholder>> pair in AliveByRoom)
        {
            int alive = 0;
            for (int i = pair.Value.Count - 1; i >= 0; i--)
            {
                EnemyPlaceholder enemy = pair.Value[i];
                if (enemy == null) { pair.Value.RemoveAt(i); continue; }
                if (enemy.IsDead) continue;
                if (!InsideRoom(enemy)) continue;      //跑出房间的不计入（也别让它挡住清房）
                alive++;
            }
            if (alive == 0) { if (clearedNow == null) clearedNow = new List<ushort>(); clearedNow.Add(pair.Key); }
        }

        if (clearedNow == null || clearedNow.Count == 0)
        {
            if (_clearedRooms >= TotalRooms && TotalRooms > 0) OnSceneCleared();
            return;
        }

        for (int i = 0; i < clearedNow.Count; i++)
        {
            AliveByRoom.Remove(clearedNow[i]);
            _clearedRooms++;
            ChaosLog.Info(LogChannel.UI, "[局内] 房间 " + clearedNow[i] + " 已清空（剩余未清房间 "
                + (TotalRooms - _clearedRooms) + "）");
        }

        if (_clearedRooms >= TotalRooms && TotalRooms > 0) OnSceneCleared();
    }

    private static bool InsideRoom(EnemyPlaceholder enemy)
    {
        Vector3 p = enemy.transform.position;
        return p.x >= enemy.roomX0 && p.x <= enemy.roomX0 + enemy.roomW
            && p.y >= enemy.roomY0 - 1f && p.y <= enemy.roomY0 + enemy.roomH + 1f;
    }

    private static void OnSceneCleared()
    {
        if (_clearedCurrentScene) return;
        _clearedCurrentScene = true;

        ChaosLog.Info(LogChannel.UI, "[局内] 本场景清空，准备进入下一场景");

        if (CurrentSceneIndex >= MaxSceneIndex)
        {
            RunCleared = true;
            ChaosLog.Success(LogChannel.UI, "[局内] 第 3 场景清空 → 本局通关（boss 占位，结算留 W3.2）");
            return;
        }

        EnterScene(CurrentSceneIndex + 1, Team);
    }

    /// <summary>玩家是否在指定房间内（用房间世界矩形近似）。</summary>
    public static bool IsPlayerInRoom(ushort roomId)
    {
        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player == null) return false;
        Rect rect;
        if (!TryGetRoomRectById(roomId, out rect)) return false;
        Vector3 p = player.transform.position;
        return p.x >= rect.xMin && p.x <= rect.xMax && p.y >= rect.yMin && p.y <= rect.yMax;
    }

    /// <summary>给定世界坐标，返回它所在房间的矩形（没有则 false）。</summary>
    public static bool TryGetRoomRect(Vector3 worldPosition, out Rect rect)
    {
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        rect = new Rect();
        if (layout == null || layout.Nodes == null) return false;

        for (int i = 0; i < layout.Nodes.Count; i++)
        {
            RoomNode node = layout.Nodes[i];
            if (node == null) continue;
            float x0 = node.CellX * RunMapConst.ChunkW;
            float y0 = node.CellY * RunMapConst.ChunkH;
            if (worldPosition.x >= x0 && worldPosition.x <= x0 + RunMapConst.ChunkW
                && worldPosition.y >= y0 && worldPosition.y <= y0 + RunMapConst.ChunkH)
            {
                rect = new Rect(x0, y0, RunMapConst.ChunkW, RunMapConst.ChunkH);
                return true;
            }
        }
        return false;
    }

    private static bool TryGetRoomRectById(ushort roomId, out Rect rect)
    {
        rect = new Rect();
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        if (layout == null) return false;
        RoomNode node = layout.Find(roomId);
        if (node == null) return false;
        rect = new Rect(node.CellX * RunMapConst.ChunkW, node.CellY * RunMapConst.ChunkH,
            RunMapConst.ChunkW, RunMapConst.ChunkH);
        return true;
    }

    private static void SpawnEnemiesForScene()
    {
        MapLayout layout = RunMapBootstrap.CurrentLayout;
        if (layout == null || layout.Nodes == null) return;

        GameObject prefab = Resources.Load<GameObject>("Enemy/Enemy_Placeholder");
        if (prefab == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 找不到 Resources/Enemy/Enemy_Placeholder.prefab");
            return;
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
                    enemy.roomW = RunMapConst.ChunkW;
                    enemy.roomH = RunMapConst.ChunkH;
                    list.Add(enemy);
                }
                AllEnemies.Add(go);
            }
        }

        ChaosLog.Info(LogChannel.UI,
            "[局内] 敌人已生成：非入口房间 " + AliveByRoom.Count + " 个 × " + EnemiesPerRoom + " = " + AllEnemies.Count + " 个");
    }

    private static void EnsureTicker()
    {
        if (GameObject.Find("RunEncounterTicker") != null) return;
        GameObject go = new GameObject("RunEncounterTicker");
        go.AddComponent<RunEncounterTicker>();
    }

    private static void HideStrayCitizen()
    {
        if (_hiddenCitizen != null) return;
        GameObject citizen = GameObject.Find("citizen");
        if (citizen == null) return;
        _hiddenCitizen = citizen;
        citizen.SetActive(false);
        ChaosLog.Info(LogChannel.UI, "[局内] 已隐藏场景中遗留的 citizen 对象（撤离恢复）");
    }

    private static void RestoreStrayCitizen()
    {
        if (_hiddenCitizen == null) return;
        _hiddenCitizen.SetActive(true);
        ChaosLog.Info(LogChannel.UI, "[局外] 已恢复场景中的 citizen 对象");
        _hiddenCitizen = null;
    }

    private static GameObject _hiddenCitizen;

    private static void ClearEnemies()
    {
        for (int i = 0; i < AllEnemies.Count; i++) if (AllEnemies[i] != null) Object.Destroy(AllEnemies[i]);
        AllEnemies.Clear();
        AliveByRoom.Clear();
        TotalByRoom.Clear();
        GameObject root = GameObject.Find("PrototypeEnemyRoot");
        if (root != null) Object.Destroy(root);
    }

    /// <summary>撤离：全清。</summary>
    public static void Clear()
    {
        RestoreStrayCitizen();
        ClearEnemies();
        _clearedRooms = 0;
        CurrentSceneIndex = 0;
        RunCleared = false;
        _clearedCurrentScene = false;
        GameObject ticker = GameObject.Find("RunEncounterTicker");
        if (ticker != null) Object.Destroy(ticker);
    }
}

/// <summary>清房重算的驱动（0.3s 一次）。</summary>
public class RunEncounterTicker : MonoBehaviour
{
    private float _next;

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + RunEncounter.ScanInterval;
        RunEncounter.RefreshNow();
    }
}
