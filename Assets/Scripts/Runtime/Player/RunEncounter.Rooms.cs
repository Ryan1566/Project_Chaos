using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

// 房间清理判定与房间空间查询。
public static partial class RunEncounter
{
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
            ApplyRunWounds(false);   //W3.3：通关 → 只把本局倒地的门客结算成重伤 30s
            ChaosLog.Success(LogChannel.UI, "[局内] 第 3 场景清空 → 本局通关（boss 占位）");
            RunSettlementPanel.Open(true);   //W3.2e：弹通关态结算面板
            RunTeamHud.SetVisible(false);    //W3.3：结算隐藏队伍血条
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

    /// <summary>W3.3：任意世界坐标是否在指定房间内（敌人选目标用：门客与受控角色都要在房间里）。</summary>
    public static bool IsPositionInRoom(ushort roomId, Vector3 worldPosition)
    {
        Rect rect;
        if (!TryGetRoomRectById(roomId, out rect)) return false;
        return worldPosition.x >= rect.xMin && worldPosition.x <= rect.xMax
            && worldPosition.y >= rect.yMin && worldPosition.y <= rect.yMax;
    }

    /// <summary>给定世界坐标，返回它所在房间的矩形（没有则 false）。</summary>
    public static bool TryGetRoomRect(Vector3 worldPosition, out Rect rect)    {
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

}
