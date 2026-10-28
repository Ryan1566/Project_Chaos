using System.Collections.Generic;
using ChaosDebug;
using RunMap;
using UnityEngine;

// 队伍生命、观战和重伤结算。保留 RunEncounter 对外 API。
public static partial class RunEncounter
{
    /// <summary>诊断状态串（W3.1n）。</summary>
    /// <summary>W3.2d：本局是否已判负（HP 归零）。</summary>
    public static bool Defeated { get; private set; }

    /// <summary>判负。**W3.3**：只在「受控角色 + 全部门客都倒下」时由 `OnPlayerDowned` 调用；
    /// 失败时**全部门客**结算重伤 30s。</summary>
    public static void TriggerDefeat()
    {
        if (Defeated) return;
        Defeated = true;
        _sceneReady = false;   //停止清房/切场景
        IsSpectator = false;
        int wounded = ApplyRunWounds(true);   //W3.3：全部门客 Wound(30)
        RunSettlementPanel.Open(false);   //W3.2e：弹失败态结算面板
        RunTeamHud.SetVisible(false);     //W3.3：结算隐藏队伍血条
        ChaosLog.Warn(LogChannel.UI, "[局内] TriggerDefeat：本局失败（全员倒下，sceneIndex=" + CurrentSceneIndex
            + "，掉落条目 " + RunInventory.UsedSlots + " 种，重伤门客 " + wounded + " 名/30s）");
    }

    /// <summary>
    /// **W3.3 受控角色倒地入口**（`PlayerHealth` 在 HP 归零时调用）：
    /// * 还有门客站着 → **观战模式**（不做控制权转移，门客 AI 继续）；
    /// * 全部门客也倒地 → **失败**。
    /// </summary>
    public static void OnPlayerDowned()
    {
        if (Defeated) return;

        int aliveAllies = 0;
        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally != null && OwnsRuntimeObject(ally.gameObject) && !ally.IsDowned) aliveAllies++;
        }

        if (aliveAllies == 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[局内] 受控角色与全部门客均已倒下 → 判定本局失败（存活门客 0）");
            TriggerDefeat();
            return;
        }

        EnterSpectator(aliveAllies);
    }

    /// <summary>
    /// W3.3：**任一单位倒地后**调用 —— 受控角色已倒地（观战）**且**门客全部倒地 → 失败。
    /// 没有这条，「先受控角色倒、后门客全倒」就永远判不了负（门客倒地不会经过 `OnPlayerDowned`）。
    /// </summary>
    public static void CheckTeamWipe()
    {
        if (Defeated) return;

        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player == null) return;   //不在局内
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null || !health.IsDowned) return;   //受控角色还站着 → 不判负

        int aliveAllies = 0;
        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally != null && OwnsRuntimeObject(ally.gameObject) && !ally.IsDowned) aliveAllies++;
        }
        if (aliveAllies > 0) return;

        ChaosLog.Warn(LogChannel.UI, "[局内] 受控角色已倒地且全部门客也倒下 → 判定本局失败（存活门客 0）");
        TriggerDefeat();
    }

    private static void EnterSpectator(int aliveAllies)
    {
        if (IsSpectator) return;
        IsSpectator = true;
        ApplyPlayerControl(false);

        GameObject player = PlayerSpawner.CurrentPlayer;
        PlayerHealth health = player == null ? null : player.GetComponent<PlayerHealth>();
        if (health != null) health.MarkDownedVisual(true);

        ChaosLog.Warn(LogChannel.UI, "[局内] 观战模式：门客仍在战斗（存活门客 " + aliveAllies
            + " 名；受控角色 PlayerLocomotion/PlayerAttack 已禁用，不做控制权转移）");
    }

    /// <summary>W3.3：退出观战（扎营治疗把受控角色救回来 / 重开一局）。</summary>
    public static void ResetSpectator()
    {
        if (!IsSpectator) return;
        IsSpectator = false;
        ApplyPlayerControl(true);

        GameObject player = PlayerSpawner.CurrentPlayer;
        PlayerHealth health = player == null ? null : player.GetComponent<PlayerHealth>();
        if (health != null) health.MarkDownedVisual(false);

        ChaosLog.Info(LogChannel.UI, "[局内] 退出观战模式：受控角色恢复操作");
    }

    private static void ApplyPlayerControl(bool enabled)
    {
        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player == null) return;
        PlayerLocomotion locomotion = player.GetComponent<PlayerLocomotion>();
        if (locomotion != null) locomotion.enabled = enabled;
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = enabled;
    }

    /// <summary>
    /// **W3.3 结算重伤**（幂等：每局只结算一次）。
    /// `allRetainers=true` → 全部门客 `Wound(id,30)`（失败）；`false` → 只算**本局倒地**的门客（通关/撤离）。
    /// 返回本次重伤人数。已判负的局不会因后续补员回滚。
    /// </summary>
    public static int ApplyRunWounds(bool allRetainers)
    {
        if (_woundsApplied) return 0;
        _woundsApplied = true;

        RetainerService service = RetainerService.Instance;
        int wounded = 0;
        if (allRetainers)
        {
            List<RetainerSave> all = service.GetAll();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && service.Wound(all[i].id, RetainerService.DefaultWoundSeconds)) wounded++;
        }
        else
        {
            for (int i = 0; i < AllyController.All.Count; i++)
            {
                AllyController ally = AllyController.All[i];
                if (ally == null || !OwnsRuntimeObject(ally.gameObject) || !ally.IsDowned) continue;
                if (service.Wound(ally.memberId, RetainerService.DefaultWoundSeconds)) wounded++;
            }
        }

        LastWoundCount = wounded;
        if (wounded > 0)
            ChaosLog.Warn(LogChannel.UI, "[局内] 结算重伤：" + wounded + " 名门客 wounded=true（30s，"
                + (allRetainers ? "失败→全部门客" : "通关/撤离→仅倒地门客") + "）");
        else
            ChaosLog.Info(LogChannel.UI, "[局内] 结算重伤：0 名（"
                + (allRetainers ? "全部门客都已是重伤/没有门客" : "本局没有倒地门客") + "）");
        return wounded;
    }

    /// <summary>W3.3：GM/验收打印队伍 HP 与观战/失败状态。</summary>
    public static string DumpTeamHp()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[局内队伍] 场景=").Append(CurrentSceneIndex).Append("/").Append(MaxSceneIndex)
          .Append("  观战=").Append(IsSpectator).Append("  已判负=").Append(Defeated)
          .Append("  通关=").Append(RunCleared)
          .Append("  上轮结算重伤=").Append(LastWoundCount);

        GameObject player = PlayerSpawner.CurrentPlayer;
        if (player == null)
        {
            sb.Append("\n  受控角色：未生成");
        }
        else
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            sb.Append("\n  受控角色 HP=").Append(health == null ? "?" : (health.Hp + "/" + health.maxHp))
              .Append(health != null && health.IsDowned ? "  倒地" : "  可用");
        }

        sb.Append("\n  AI 门客 ").Append(AllyController.All.Count).Append(" 名：");
        for (int i = 0; i < AllyController.All.Count; i++)
        {
            AllyController ally = AllyController.All[i];
            if (ally == null || !OwnsRuntimeObject(ally.gameObject)) continue;
            sb.Append("\n    ").Append(ally.AllyName)
              .Append("  HP=").Append(ally.Hp).Append("/").Append(ally.MaxHp)
              .Append(ally.IsDowned ? "  倒地" : "  可用")
              .Append("  位置=").Append(ally.transform.position.x.ToString("F2")).Append(",")
              .Append(ally.transform.position.y.ToString("F2"));
        }
        return sb.ToString();
    }

}
