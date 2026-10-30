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
public static partial class RunEncounter
{
    public const int MaxSceneIndex = 3;
    public const int EnemiesPerRoom = 2;
    /// <summary>清房重算间隔。</summary>
    public const float ScanInterval = 0.3f;

    private static readonly Dictionary<ushort, List<EnemyPlaceholder>> AliveByRoom = new Dictionary<ushort, List<EnemyPlaceholder>>();
    private static readonly Dictionary<ushort, int> TotalByRoom = new Dictionary<ushort, int>();
    private static readonly List<GameObject> AllEnemies = new List<GameObject>();
    /// <summary>Caller-owned snapshot for read-only UI consumers; never exposes the combat registry for mutation.</summary>
    public static void CopyMapEnemies(List<EnemyPlaceholder> destination)
    {
        destination.Clear();
        foreach (var actor in AllEnemies)
        {
            if (actor == null || !OwnsRuntimeObject(actor)) continue;
            var enemy = actor.GetComponent<EnemyPlaceholder>();
            if (enemy != null && !enemy.IsDead) destination.Add(enemy);
        }
    }
    private static readonly List<uint> Team = new List<uint>();
    private static bool _clearedCurrentScene;
    /// <summary>本场景是否**已生成完成**（未完成前 RefreshRooms 不许判清房，否则会提前切场景）。</summary>
    private static bool _sceneReady;
    /// <summary>重入保护。</summary>
    private static bool _entering;
    /// <summary>本局随机种子（掉落可复现用）。</summary>
    private static uint _runSeed = 12345u;
    /// <summary>本局击杀序号（掉落可复现用）。</summary>
    private static uint _killSeq;
    public static uint RunSeed { get { return _runSeed; } }
    /// <summary>待执行的下一场景（延迟一帧，禁止同步递归 EnterScene）。</summary>
    private static int _pendingNextScene = -1;
    /// <summary>ticker 静态缓存（W3.1n B）。</summary>
    private static RunEncounterTicker _ticker;

    public static int CurrentSceneIndex { get; private set; }
    public static bool RunCleared { get; private set; }
    /// <summary>W3.3：观战模式 —— 受控角色倒地但门客还活着（不做控制权转移）。</summary>
    public static bool IsSpectator { get; private set; }
    /// <summary>W3.3：最近一次结算导致重伤（wounded=true，30s）的门客数（结算面板显示用）。</summary>
    public static int LastWoundCount { get; private set; }
    /// <summary>W3.3：本局是否已结算过重伤（幂等；每局 EnterScene 重置）。</summary>
    private static bool _woundsApplied;

    public static int TotalRooms { get { return TotalByRoom.Count; } }
    public static int ClearedRooms { get { return _clearedRooms; } }
    private static int _clearedRooms;

    /// <summary>GM 调试：强制重算（也可等 ticker 自己跑）。</summary>
    public static void RefreshNow() { RefreshRooms(); }

    /// <summary>**唯一入口**：进入指定场景（1~3）。</summary>
    public static bool EnterScene(int sceneIndex, List<uint> runTeam = null)
    {
        return EnterSceneInternal(sceneIndex, runTeam, false, null);
    }

    /// <summary>GM 重现入口：显式种子包含合法的 0，整个场景链沿用同一本局种子。</summary>
    public static bool EnterSceneWithSeed(int sceneIndex, List<uint> runTeam, uint seed)
    {
        return EnterSceneInternal(sceneIndex, runTeam, false, seed);
    }

    public static bool EnterPreparedScene(int sceneIndex, List<uint> runTeam, uint seed)
    {
        return EnterSceneInternal(sceneIndex, runTeam, true, seed);
    }

    private static bool EnterSceneInternal(int sceneIndex, List<uint> runTeam, bool prepared, uint? seed)
    {
        if (sceneIndex < 1 || sceneIndex > MaxSceneIndex)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 场景序号越界：" + sceneIndex);
            return false;
        }

        // Reject a stale loader callback before retiring any live stage or claiming its map.
        NaturalMapPlan preparedPlan = prepared ? NaturalMapRuntime.Current : null;
        if (preparedPlan != null && (!NaturalMapRuntime.IsReady || !seed.HasValue
            || preparedPlan.RunSeed != seed.Value || preparedPlan.SceneIndex != sceneIndex))
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 忽略与当前准备地图不匹配的阶段请求");
            return false;
        }

        // 重入检查必须早于清场，否则被拒绝的调用也会清掉正在生成的对象。
        if (_entering) return false;
        _entering = true;
        try
        {

            // 已入库存的物品及扎营次数属于整局；只有未捡掉落物随场景清除。
            if (sceneIndex == 1)
            {
                RunInventory.BeginRun();
                CampService.ResetForNewRun();
                _killSeq = 0u;
                _runSeed = seed ?? NaturalMapRuntime.PreviewSeed ?? NaturalMapRuntime.CreateRunSeed();
                RunCleared = false;
            }
            else if (seed.HasValue) _runSeed = seed.Value;
            ClearGroundDrops();
            RunDepthHud.DisableAndDestroy();   //W4e：关闸 + 销毁   //W4c：撤离销毁 HUD
            RunTeamHud.DisableAndDestroy();    //W3.3：队伍血条关闸 + 销毁
            RunTerrainToast.DisableAndDestroy();   //W5.2：地形/阵营提示（进新场景先彻底清掉旧的）
            ChaosLog.Info(LogChannel.Config, "[局内库存] 场景 " + sceneIndex + " runSeed=" + _runSeed);

            // Retire only the previous registered stage; unrelated actors remain untouched.
            PurgeAllRuntimeObjects();
            EnsureObjectScope();
            _clearedRooms = 0;
            _clearedCurrentScene = false;
            Defeated = false;
            IsSpectator = false;          //W3.3：新场景重置观战
            _woundsApplied = false;       //W3.3：新场景重置重伤结算幂等位
            LastWoundCount = 0;
            _pendingNextScene = -1;
            AliveByRoom.Clear();
            TotalByRoom.Clear();
            AllEnemies.Clear();

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
            if (!prepared) PrototypeMapSpawner.Clear();

            GameObject container = prepared ? PrototypeMapSpawner.AdoptPreparedMap() : PrototypeMapSpawner.Spawn(sceneIndex, _runSeed);
            if (container == null)
            {
                ChaosLog.Error(LogChannel.UI, "[局内] 场景 " + sceneIndex + " 生成失败");
                return false;
            }

            EnsureObjectScope().Register(container);
            NaturalMapPlan naturalPlan = NaturalMapRuntime.Current;
            if (naturalPlan != null && (!NaturalMapRuntime.IsReady || naturalPlan.RunSeed != _runSeed
                || naturalPlan.SceneIndex != sceneIndex))
            {
                ChaosLog.Error(LogChannel.UI, "[局内] 自然地图尚未就绪或种子/场景不匹配：计划 " + naturalPlan.RunSeed
                    + "/" + naturalPlan.SceneIndex + "，遭遇 " + _runSeed + "/" + sceneIndex);
                return false;
            }

            CurrentSceneIndex = sceneIndex;

            //★W5.2：进第 1 场景 = 本次「出发」，把本局阵营定格（整局不变，17 号文档 §9 E7）
            if (sceneIndex == 1)
                FactionService.LockRunFaction(MetaProgressService.Instance == null ? 1u : MetaProgressService.Instance.Era);
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

            if (!SpawnEnemiesForScene())
            {
                ChaosLog.Error(LogChannel.UI, "[局内] 敌人投放失败，停止本场景初始化");
                ClearEnemies();
                PlayerSpawner.Clear(false);
                return false;
            }
            EnsureTicker();

            RunDepthHud.EnableForRun();   //W4e：进局开闸 + 建 HUD（CurrentSceneIndex 已就位）
            RunTeamHud.EnableForRun();    //W3.3：队伍血条（左侧，独立 Canvas）
            ShowTerrainToast(sceneIndex);   //W5.2：进场提示「地形：X · 阵营：Y」，3 秒淡出
            _sceneReady = true;

            ChaosLog.Success(LogChannel.UI,
                "[局内] 场景" + sceneIndex + " 生成完成：房间 " + TotalRooms + " / 敌人 " + AllEnemies.Count
                + " / 门客 " + AllyController.All.Count + " / 玩家 " + (PlayerSpawner.CurrentPlayer == null ? 0 : 1));

            ChaosLog.Success(LogChannel.UI,
                "[局内] 进入场景 " + sceneIndex + "/" + MaxSceneIndex + "，地图根=" + container.name
                + "，房间 " + TotalRooms + " 个，敌人 " + AllEnemies.Count + " 个");
            return true;
        }
        finally
        {
            if (!_sceneReady)
            {
                // Partial creation owns the same scope, so every failure uses the same teardown.
                RunDepthHud.DisableAndDestroy(); RunTeamHud.DisableAndDestroy(); RunTerrainToast.DisableAndDestroy();
                PurgeAllRuntimeObjects();
            }
            _entering = false;
        }
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

    /// <summary>
    /// GM / 验收用：**直接跳到指定场景**（1~3），沿用当前队伍（不经过"清房→下一场景"）。
    /// 与 <see cref="SkipToNextScene"/> 一样走唯一入口 `EnterScene`，所以地形/阵营/掉落全部按新场景重算。
    /// </summary>
    public static bool JumpToScene(int sceneIndex)
    {
        ChaosLog.Info(LogChannel.UI, "[局内] GM：直接跳到第 " + sceneIndex + " 场景");
        return EnterScene(sceneIndex, Team);
    }

    /// <summary>由 Ticker 每帧调用：执行延迟登记的下一场景（禁止同步递归）。</summary>
    public static void ProcessPendingScene()
    {
        if (_pendingNextScene < 0) return;
        int next = _pendingNextScene;
        _pendingNextScene = -1;
        _sceneReady = false;
        if (RunSceneFlow.IsFormalRun)
        {
            RunSceneFlow.BeginNextStage(next, Team, _runSeed);
            return;
        }
        EnterScene(next, Team);
    }

    /// <summary>正式阶段切换只清本场景对象；整局背包、扎营、队伍和种子保留。</summary>
    public static void SuspendForSceneTransition()
    {
        _sceneReady = false;
        _pendingNextScene = -1;
        RunDepthHud.DisableAndDestroy();
        RunTeamHud.DisableAndDestroy();
        RunTerrainToast.DisableAndDestroy();
        ClearGroundDrops();
        PurgeAllRuntimeObjects();
        ClearEnemies();
        PlayerSpawner.Clear(false);
        PrototypeMapSpawner.Clear();
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

    private static void EnsureTicker()
    {
        // This service creates and owns its ticker; it never adopts another scene's ticker.
        if (_ticker != null) return;
        GameObject go = new GameObject("RunEncounterTicker");
        _ticker = go.AddComponent<RunEncounterTicker>();
    }

    /// <summary>Compatibility entry point: retire only objects explicitly owned by this stage.</summary>
    public static void PurgeAllRuntimeObjects()
    {
        _sceneReady = false;
        _pendingNextScene = -1;
        RunObjectScope retired = _objectScope;
        _objectScope = null;
        if (retired != null)
        {
            PlayerSpawner.ClearForScope(retired, false);
            // Loading may already have published the next map. Clear metadata only for ours.
            GameObject map = RunMapBootstrap.CurrentMapRoot;
            if (map != null && retired.Owns(map)) RunMapBootstrap.ClearCurrent();
            PrototypeMapSpawner.ClearForScope(retired);
            retired.Dispose();
        }
        AllEnemies.Clear(); AliveByRoom.Clear(); TotalByRoom.Clear();
    }

    /// <summary>撤离：全清。</summary>
    public static void Clear()
    {
        // Wounds are a gameplay transaction owned by Dispatch.Return, never a cleanup side effect.
        RunTeamHud.DisableAndDestroy();   //W3.3：撤离销毁队伍血条
        RunTerrainToast.DisableAndDestroy();   //W5.2：撤离销毁地形/阵营提示（验收：撤离后实例=0）
        FactionService.ResetRunFaction();      //W5.2：撤离清掉本局阵营锁定（下次出发重新定格）
        IsSpectator = false;

        RunInventory.Clear();
        ClearGroundDrops();
        RunDepthHud.DisableAndDestroy();   //W4e：关闸 + 销毁   //W4c：撤离销毁 HUD
        PlayerSpawner.Clear();
        PurgeAllRuntimeObjects();
        ClearEnemies();
        _sceneReady = false;
        _pendingNextScene = -1;
        _entering = false;
        RunEncounterTicker ticker = _ticker;
        _ticker = null;
        if (ticker != null)
        {
            ticker.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(ticker.gameObject); else Object.DestroyImmediate(ticker.gameObject);
        }
        Team.Clear();
        CampService.ResetForNewRun();
        _clearedRooms = 0;
        CurrentSceneIndex = 0;
        RunCleared = false;
        _clearedCurrentScene = false;
    }
}
