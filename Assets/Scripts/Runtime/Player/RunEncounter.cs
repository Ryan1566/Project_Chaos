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
    /// <summary>本场景是否**已生成完成**（未完成前 RefreshRooms 不许判清房，否则会提前切场景）。</summary>
    private static bool _sceneReady;
    /// <summary>重入保护。</summary>
    private static bool _entering;
    private static int _enterCount;
    /// <summary>本局随机种子（掉落可复现用）。</summary>
    private static uint _runSeed = 12345u;
    /// <summary>本局击杀序号（掉落可复现用）。</summary>
    private static uint _killSeq;
    public static uint RunSeed { get { return _runSeed; } }
    /// <summary>待执行的下一场景（延迟一帧，禁止同步递归 EnterScene）。</summary>
    private static int _pendingNextScene = -1;
    /// <summary>ticker 静态缓存（W3.1n B）。</summary>
    private static RunEncounterTicker _ticker;
    private static readonly int DiagId = typeof(RunEncounter).GetHashCode();

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

        //══════ 诊断（W3.1j）：谁在调 EnterScene ══════
        _enterCount++;
        {
            var stack = new System.Diagnostics.StackTrace(true);
            string frames = "";
            for (int fi = 0; fi < stack.FrameCount && fi < 4; fi++)
            {
                var fr = stack.GetFrame(fi);
                frames += "\n       #" + fi + " " + (fr.GetMethod() == null ? "?" : fr.GetMethod().DeclaringType + "." + fr.GetMethod().Name)
                       + "  (" + (fr.GetFileName() == null ? "?" : System.IO.Path.GetFileName(fr.GetFileName())) + ":" + fr.GetFileLineNumber() + ")";
            }
            ChaosLog.Warn(LogChannel.UI, "[诊断] EnterScene #" + _enterCount + " sceneIndex=" + sceneIndex
                + " staticId=" + DiagId + " frameCount=" + Time.frameCount
                + " ticker数=" + UnityEngine.Object.FindObjectsOfType<RunEncounterTicker>().Length + frames);
        }
        if (Time.frameCount < 0) return false;

        ChaosLog.Warn(LogChannel.UI, "[诊断] EnterScene Team.Count=" + Team.Count + " 传入runTeam=" + (runTeam == null ? -1 : runTeam.Count) + " frameCount=" + Time.frameCount);

        //W3.2b：进场景清空局内库存与未捡掉落物
        RunInventory.Clear();
        ClearGroundDrops();
        RunDepthHud.DisableAndDestroy();   //W4e：关闸 + 销毁   //W4c：撤离销毁 HUD
        CampService.ResetForNewRun();
        _killSeq = 0u;
        _runSeed = (uint)(Time.frameCount * 7919 + sceneIndex * 131);
        ChaosLog.Info(LogChannel.Config, "[局内库存] 场景 " + sceneIndex + " runSeed=" + _runSeed);

        //★W3.1l：进场景第一步先按类型全场景清理（防残留叠加）
        PurgeAllRuntimeObjects();
        _clearedRooms = 0;
        _clearedCurrentScene = false;
        Defeated = false;
        _pendingNextScene = -1;
        AliveByRoom.Clear();
        TotalByRoom.Clear();
        AllEnemies.Clear();

        //重入保护：EnterScene 未完成时再次进入 → 直接拒绝（防重复生成）
        if (_entering)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] EnterScene 正在执行，忽略重复进入（sceneIndex=" + sceneIndex + "）");
            return false;
        }
        _entering = true;
        _sceneReady = false;
        ChaosLog.Info(LogChannel.UI, "[局内] EnterScene 开始：sceneIndex=" + sceneIndex);

        if (runTeam != null && runTeam.Count > 0)
        {
            //★W3.1n：必须**先拷贝**再清空 —— 否则当传入的就是 Team 自身时（ProcessPendingScene 传 Team），
            //  Team.Clear() 会把自己清空，导致 SpawnAllies 收到 0 人 → 切场景后门客=0
            var teamCopy = new List<uint>(runTeam);
            Team.Clear();
            Team.AddRange(teamCopy);
        }
        else if (Team.Count == 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] EnterScene 收到的队伍为空且 Team 也为空 → 门客将不会生成");
        }

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

        _sceneReady = true;
        RunDepthHud.EnableForRun();   //W4e：进局开闸 + 建 HUD（CurrentSceneIndex 已就位）
        _entering = false;

        ChaosLog.Success(LogChannel.UI,
            "[局内] 场景" + sceneIndex + " 生成完成：房间 " + TotalRooms + " / 敌人 " + AllEnemies.Count
            + " / 门客 " + AllyController.All.Count + " / 玩家 " + (PlayerSpawner.CurrentPlayer == null ? 0 : 1));

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

    /// <summary>由 Ticker 每帧调用：执行延迟登记的下一场景（禁止同步递归）。</summary>
    public static void ProcessPendingScene()
    {
        ChaosLog.Warn(LogChannel.UI, "[诊断] ProcessPendingScene 入口 pending=" + _pendingNextScene + " frameCount=" + Time.frameCount);
        if (_pendingNextScene < 0) return;
        int next = _pendingNextScene;
        _pendingNextScene = -1;
        _sceneReady = false;
        EnterScene(next, Team);
    }

    /// <summary>诊断状态串（W3.1n）。</summary>
    /// <summary>W3.2d：本局是否已判负（HP 归零）。</summary>
    public static bool Defeated { get; private set; }

    /// <summary>判负（由 PlayerHealth 在 HP 归零时调用；结算面板下轮接）。</summary>
    public static void TriggerDefeat()
    {
        if (Defeated) return;
        Defeated = true;
        _sceneReady = false;   //停止清房/切场景
        RunSettlementPanel.Open(false);   //W3.2e：弹失败态结算面板
        ChaosLog.Warn(LogChannel.UI, "[局内] TriggerDefeat：本局失败（sceneIndex=" + CurrentSceneIndex + "，掉落条目 " + RunInventory.UsedSlots + " 种）");
    }

    public static string DumpDiagState()
    {
        return "pending=" + _pendingNextScene + " sceneReady=" + _sceneReady + " entering=" + _entering
            + " scene=" + CurrentSceneIndex + " ticker数=" + UnityEngine.Object.FindObjectsOfType<RunEncounterTicker>().Length;
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
    public static void NotifyEnemyKilled(ushort roomId, EnemyPlaceholder enemy, bool allowDrop = true)
    {
        List<EnemyPlaceholder> list;
        if (AliveByRoom.TryGetValue(roomId, out list)) list.Remove(enemy);

        //W3.2d：掉落死亡不触发掉落（防引诱跳崖刷装备 / 进局凭空多掉落）
        if (!allowDrop) return;

        //W3.2b：击杀掉落（可复现：runSeed + killSeq）
        _killSeq++;
        uint itemId, count;
        if (!DropService.Instance.RollForEnemy(DropService.GroupNormal, _runSeed, _killSeq, out itemId, out count)) return;

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

    /// <summary>**按活体重算清房**：每房间统计"存活且在本房间矩形内"的敌人；0 即已清。</summary>
    private static void RefreshRooms()
    {
        if (!_sceneReady) return;
        if (_pendingNextScene > 0) return;          //已有待切场景，本帧不再判
        if (TotalRooms == 0) return;

        //W3.1l：只按"登记的敌人是否 IsDead"统计（掉落死亡已走 Die()），不再用房间矩形过滤
        List<ushort> clearedNow = null;
        foreach (KeyValuePair<ushort, List<EnemyPlaceholder>> pair in AliveByRoom)
        {
            int alive = 0;
            for (int k = 0; k < pair.Value.Count; k++)
            {
                EnemyPlaceholder enemy = pair.Value[k];
                if (enemy == null) continue;
                if (enemy.IsDead) continue;
                alive++;
            }
            if (alive == 0) { if (clearedNow == null) clearedNow = new List<ushort>(); clearedNow.Add(pair.Key); }
        }

        if (clearedNow == null || clearedNow.Count == 0) return;

        for (int k = 0; k < clearedNow.Count; k++)
        {
            AliveByRoom.Remove(clearedNow[k]);
            _clearedRooms++;
            ChaosLog.Info(LogChannel.UI, "[局内] 房间 " + clearedNow[k] + " 已清空（剩余未清房间 " + (TotalRooms - _clearedRooms) + "）");
        }

        if (_clearedRooms >= TotalRooms) OnSceneCleared();
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

        ChaosLog.Info(LogChannel.UI, "[局内] OnSceneCleared：sceneIndex=" + CurrentSceneIndex + " 全部房间已清");

        if (CurrentSceneIndex >= MaxSceneIndex)
        {
            RunCleared = true;
            ChaosLog.Success(LogChannel.UI, "[局内] 第 3 场景清空 → 本局通关（boss 占位，结算留 W3.2）");
            RunSettlementPanel.Open(true);   //W3.2e：弹通关态结算面板
            return;
        }

        //★W3.1l：只登记，交给 Ticker 下一帧执行（禁止同步递归 EnterScene）
        _pendingNextScene = CurrentSceneIndex + 1;
        ChaosLog.Info(LogChannel.UI, "[局内] 已登记下一场景 " + _pendingNextScene + "（下一帧由 Ticker 执行）");
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
        ChaosLog.Warn(LogChannel.UI, "[诊断] SpawnEnemiesForScene 进入 frameCount=" + Time.frameCount);
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
        int sceneTickers = UnityEngine.Object.FindObjectsOfType<RunEncounterTicker>().Length;
        ChaosLog.Warn(LogChannel.UI, "[诊断] EnsureTicker：场景 ticker 数=" + sceneTickers + " 缓存=" + (_ticker == null ? "null" : _ticker.GetInstanceID().ToString()) + " frameCount=" + Time.frameCount);
        if (_ticker != null) return;                       //W3.1n B：缓存优先
        _ticker = UnityEngine.Object.FindObjectOfType<RunEncounterTicker>();
        if (_ticker != null) return;
        GameObject go = new GameObject("RunEncounterTicker");
        _ticker = go.AddComponent<RunEncounterTicker>();
        ChaosLog.Warn(LogChannel.UI, "[诊断] EnsureTicker 新建 ticker id=" + _ticker.GetInstanceID() + " frameCount=" + Time.frameCount);
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

    /// <summary>
    /// 按标记类型全场景清掉本系统的运行时对象（W3.1k）。
    /// 不依赖自己维护的 List —— 即使有未登记的残留也能清掉。
    /// 注意 Destroy 帧末生效：同帧统计会偏大，请等下一帧。
    /// </summary>
    public static void PurgeAllRuntimeObjects()
    {
        int removed = 0;
        removed += DestroyAllOf<AllyController>();
        removed += DestroyAllOf<EnemyPlaceholder>();
        removed += DestroyAllOf<EnemyDummy>();
        removed += DestroyAllOf<SpawnPoint>();
        //W3.1n（A）：ticker 是系统单例，**不参与 purge**（否则帧末被销毁 → ProcessPendingScene 没人调）
        removed += DestroyAllOf<PlayerLocomotion>();

        string[] roots = { "PrototypePlayerRoot", "PrototypeEnemyRoot", "PrototypeSpawnRoot", "PrototypeBoundaryRoot" };
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = GameObject.Find(roots[i]);
            while (go != null)
            {
                go.name = roots[i] + "_purging";
                Object.Destroy(go);
                removed++;
                go = GameObject.Find(roots[i]);
            }
        }

        AllEnemies.Clear();
        AliveByRoom.Clear();
        TotalByRoom.Clear();
        ChaosLog.Warn(LogChannel.UI, "[局内] PurgeAllRuntimeObjects 清理 " + removed + " 个运行时对象 frameCount=" + Time.frameCount);
    }

    private static int DestroyAllOf<T>() where T : Component
    {
        T[] items = Object.FindObjectsOfType<T>(true);
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) Object.Destroy(items[i].gameObject);
        return items.Length;
    }

    /// <summary>撤离：全清。</summary>
    public static void Clear()
    {
        RestoreStrayCitizen();
        RunInventory.Clear();
        ClearGroundDrops();
        RunDepthHud.DisableAndDestroy();   //W4e：关闸 + 销毁   //W4c：撤离销毁 HUD
        PurgeAllRuntimeObjects();
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

    private void Awake()
    {
        ChaosLog.Warn(LogChannel.UI, "[诊断] RunEncounterTicker 创建 id=" + GetInstanceID() + " frameCount=" + Time.frameCount);
    }

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + RunEncounter.ScanInterval;
        RunEncounter.ProcessPendingScene();
        int before = RunEncounter.CurrentSceneIndex;
        RunEncounter.RefreshNow();
        if (RunEncounter.CurrentSceneIndex != before)
            ChaosLog.Warn(LogChannel.UI, "[诊断] Ticker(id=" + GetInstanceID() + ") 调用清房导致场景切换 " + before + " → " + RunEncounter.CurrentSceneIndex + " frameCount=" + Time.frameCount);
    }
}

/// <summary>地上的可捡掉落物：玩家碰到 → 重试入包，成功销毁，失败保留。</summary>
public class RunDropItem : MonoBehaviour
{
    public uint itemId;
    public uint count;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<PlayerLocomotion>() == null) return;   //只认受控玩家

        string reason;
        if (RunInventory.AddItem(itemId, count, out reason))
        {
            ChaosLog.Info(LogChannel.Config, "[局内库存] 捡起掉落物 " + name + "（物品 " + itemId + " ×" + count + "）");
            Destroy(gameObject);
        }
        else
        {
            ChaosLog.Warn(LogChannel.Config, "[局内库存] 捡起失败：" + reason + "（掉落物保留）");
        }
    }
}
