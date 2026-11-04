using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

// 局内跨场景生命继承：快照只属于当前一局，不写入任何局外存档。
public static partial class RunEncounter
{
    /// <summary>单个角色携带到下一图的绝对 HP 与倒地标记；倒地者 HP 固定为 0。</summary>
    private readonly struct CarriedVitals
    {
        internal readonly int Hp;
        internal readonly bool Downed;
        internal CarriedVitals(int hp, bool downed) { Hp = hp; Downed = downed; }
    }

    /// <summary>快照是否有效；只有抓到过当前局旧角色时才为 true。</summary>
    private static bool _vitalsValid;
    /// <summary>抓取时本局成长上下文的编号，用于拒绝上一局/旧档残留。</summary>
    private static int _vitalsRunId;
    /// <summary>抓取时的本局种子，用于拒绝另一局的快照。</summary>
    private static uint _vitalsRunSeed;
    /// <summary>受控角色单独保存，不按门客 ID 混入队伍字典。</summary>
    private static CarriedVitals _vitalsPlayer;
    private static int _vitalsItemHealthBonus;
    /// <summary>抓取时的队伍顺序（首位即受控角色），用于匹配同一局同一队伍。</summary>
    private static readonly List<uint> _vitalsTeam = new List<uint>();
    /// <summary>门客按稳定 memberId 保存，换图后重新生成也能一一对应。</summary>
    private static readonly Dictionary<uint, CarriedVitals> _vitalsAllies = new Dictionary<uint, CarriedVitals>();

    /// <summary>退出播放模式时清空静态快照，避免下次启动沿用上一次运行的数据。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTeamVitalsForPlay()
    {
        ClearTeamVitals();
    }

    /// <summary>清空跨场景生命快照：新局、撤离/回村、判负时都必须调用。</summary>
    private static void ClearTeamVitals()
    {
        _vitalsValid = false;
        _vitalsRunId = 0;
        _vitalsRunSeed = 0u;
        _vitalsPlayer = default;
        _vitalsItemHealthBonus = 0;
        _vitalsTeam.Clear();
        _vitalsAllies.Clear();
    }

    /// <summary>
    /// 判负后立即丢弃本局快照（全队已倒下，本局不再允许切图）。
    /// 由受控角色/门客确认判负后调用；正常撤离与通关仍由 Clear 统一清理。
    /// </summary>
    internal static void DiscardTeamVitalsOnDefeat()
    {
        if (!_vitalsValid) return;
        ChaosLog.Info(LogChannel.UI, "[局内] 本局判负，丢弃跨场景生命快照");
        ClearTeamVitals();
    }

    /// <summary>
    /// 在旧角色仍有效时抓取本局队伍生命。正式切图会先卸载旧物体，
    /// 所以 SuspendForSceneTransition 必须先调用这里；旧角色已卸载时保留既有快照，绝不用空场覆盖。
    /// </summary>
    private static void CaptureTeamVitals()
    {
        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player == null || !OwnsRuntimeObject(player)) return;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null) return;

        _vitalsValid = true;
        _vitalsRunId = Chaos.Gameplay.RunTeam.RunTeamProgress.RunId;
        _vitalsRunSeed = _runSeed;
        _vitalsPlayer = new CarriedVitals(health.Hp, health.IsDowned);
        _vitalsItemHealthBonus = health.ItemHealthBonus;
        _vitalsTeam.Clear();
        _vitalsTeam.AddRange(Team);
        _vitalsAllies.Clear();

        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally == null || ally.memberId == 0u || !OwnsRuntimeObject(ally.gameObject)) continue;
            _vitalsAllies[ally.memberId] = new CarriedVitals(ally.Hp, ally.IsDowned);
        }

        ChaosLog.Info(LogChannel.UI, "[局内] 已抓取跨场景生命：受控角色 HP=" + _vitalsPlayer.Hp
            + (_vitalsPlayer.Downed ? "（倒地）" : "") + "，门客 " + _vitalsAllies.Count + " 名，scene=" + CurrentSceneIndex);
    }

    /// <summary>
    /// 把匹配快照写回新场景。必须在本局成长与生成初始化完成之后调用，
    /// 只做绝对赋值，不经过受伤、治疗、奖励或判负入口，避免飘字/震动/重复全队判负。
    /// </summary>
    private static void RestoreTeamVitals()
    {
        if (!_vitalsValid) return;
        if (_vitalsRunId != Chaos.Gameplay.RunTeam.RunTeamProgress.RunId
            || _vitalsRunSeed != _runSeed || !TeamMatchesVitalsSnapshot())
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 跨场景生命快照与当前局/队伍不匹配，已丢弃（不跨局继承）");
            ClearTeamVitals();
            return;
        }

        GameObject player = PlayerSpawner.CurrentPlayer;
        PlayerHealth health = player == null ? null : player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 跨场景生命恢复找不到新受控角色，已丢弃快照");
            ClearTeamVitals();
            return;
        }

        int restoredAllies = 0;
        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally == null || !OwnsRuntimeObject(ally.gameObject)) continue;
            CarriedVitals vitals;
            if (!_vitalsAllies.TryGetValue(ally.memberId, out vitals)) continue;
            ally.ApplyCarriedVitals(vitals.Hp, vitals.Downed);
            restoredAllies++;
        }

        // 道具加成由局级服务计时，切图期间不走时；先恢复上限，避免将有效生命截断到基础上限。
        health.SetItemHealthBonus(_vitalsItemHealthBonus);
        health.ApplyCarriedVitals(_vitalsPlayer.Hp, _vitalsPlayer.Downed);
        if (_vitalsPlayer.Downed)
        {
            // 只恢复观战与操作禁用；不调用 OnPlayerDowned/CheckTeamWipe，避免换图时再判一次负。
            EnterSpectator(CountAliveAllies());
        }

        ChaosLog.Info(LogChannel.UI, "[局内] 已恢复跨场景生命：受控角色 HP=" + health.Hp + "/" + health.maxHp
            + (health.IsDowned ? "（倒地）" : "") + "，门客 " + restoredAllies
            + " 名，scene=" + CurrentSceneIndex);
    }

    /// <summary>快照队伍必须与当前队伍同序同人；首位受控角色的对应关系不能错位。</summary>
    private static bool TeamMatchesVitalsSnapshot()
    {
        if (_vitalsTeam.Count != Team.Count) return false;
        for (int i = 0; i < Team.Count; i++)
            if (_vitalsTeam[i] != Team[i]) return false;
        return true;
    }

    /// <summary>统计当前图仍有战斗力的受控方门客（只用于观战提示，不做判负）。</summary>
    private static int CountAliveAllies()
    {
        int alive = 0;
        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally != null && OwnsRuntimeObject(ally.gameObject) && !ally.IsDowned) alive++;
        }
        return alive;
    }
}
