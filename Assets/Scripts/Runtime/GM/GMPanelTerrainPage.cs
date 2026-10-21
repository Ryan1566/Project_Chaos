using ChaosDebug;
using TMPro;
using UnityEngine;

/// <summary>
/// GM 面板「局内生成 → **地形与阵营**」页（W5.2 / M14）。
///
/// 本文件是 `GMPanel` 的**另一半**（`partial`）：`BuildCatalog()` 里挂一项
/// `new GmSection("地形与阵营", BuildTerrainFactionPage)`，页面控件复用主文件的
/// `AddHint` / `AddActionButton` / `CreateStatusLabel`，所以这里只放这一页自己的逻辑。
///
/// 三个按钮（Lead 裁决的 GM 路径）：
/// 1. **打印当前地形/阵营** —— `biomeId / nameCN / 倍率 / 掉落组 / 阵营 / FactionMul`，并列出敌人实测 HP/伤害；
/// 2. **强制 Era=5** —— 直接改运行时 payload（**不写存档**），并清掉本局阵营锁定（语义 = 重新出发）；
/// 3. **跳到第 3 场景** —— 走 `RunEncounter.JumpToScene(3)`，进建筑图（地形倍率 1.5/1.2、掉落组 2）。
/// </summary>
public partial class GMPanel
{
    /// <summary>本页状态行（可变，反复改写）。</summary>
    private TextMeshProUGUI _terrainStatus;

    /// <summary>构建「地形与阵营」页（首次切到该版块时由 `GMPanel.CreatePage` 调一次）。</summary>
    private void BuildTerrainFactionPage(Transform page)
    {
        AddHint(page, "W5.2（M14）：地形 = 场景预设的 biomeId（1 地下 / 2 地表 / 3 地表建筑）；"
            + "阵营 = Era 映射（1/2 秦军、3/4 楚军、5 汉军）。FactionMul 本波恒 1.00（只当标签）。");

        AddActionButton(page, "打印当前地形/阵营", () =>
        {
            string text = DumpTerrainFaction();
            SetTerrainStatus(text);
            Debug.Log(text);
        });

        AddActionButton(page, "强制 Era=5（重新出发后生效）", () =>
        {
            MetaProgressService.Instance.Payload.era = 5u;   //只改运行时 payload，不写存档
            FactionService.ResetRunFaction();                //语义 = 重新出发：下一场景重新定格阵营

            string text = "[地形/阵营] 已强制 Era=5 → 阵营=" + FactionService.GetFactionName(5u)
                + "（FactionMul=" + FactionService.GetFactionMul(5u).ToString("F2") + "，本波仍 1.00）"
                + "\n阵营锁定已重置：进任意场景后提示显示「阵营：汉军」；敌人 HP **不变**（倍率仍 1.00）。";
            SetTerrainStatus(text);
            Debug.Log(text);
        });

        AddActionButton(page, "跳到第 3 场景", () =>
        {
            bool ok = RunEncounter.JumpToScene(3);
            string text = ok
                ? ("[地形/阵营] 已跳到第 3 场景（地表建筑）\n" + DumpTerrainFaction())
                : "[地形/阵营] 跳转失败（不在局内 / 地图没生成，看 Console）";
            SetTerrainStatus(text);
            Debug.Log(text);
        });

        _terrainStatus = CreateStatusLabel(page);
        SetTerrainStatus("点【打印当前地形/阵营】查看 biomeId / 名称 / 倍率 / 掉落组 / 阵营。\n"
            + "（先在「系统 → 派遣」出发，或直接点【跳到第 3 场景】）");
    }

    /// <summary>当前地形 + 阵营 + 敌人实测数值（GM 打印 / 验收用）。</summary>
    private static string DumpTerrainFaction()
    {
        var sb = new System.Text.StringBuilder();

        RunMap.BiomeInfo biome = RunMap.RunMapConfigLoader.GetCurrentBiome();
        RunMap.MapLayout layout = RunMap.RunMapBootstrap.CurrentLayout;
        uint era = FactionService.ResolveEra();

        sb.Append("[地形/阵营] 场景=").Append(RunEncounter.CurrentSceneIndex).Append("/").Append(RunEncounter.MaxSceneIndex)
          .Append("  presetId=").Append(layout == null ? 0 : layout.PresetId)
          .Append("  biomeId=").Append(biome == null ? 0u : biome.Id)
          .Append("  地形=").Append(biome == null ? "未知" : biome.NameCN)
          .Append("  HP倍率=").Append(biome == null ? "1.00" : biome.EnemyHpMul.ToString("F2"))
          .Append("  伤害倍率=").Append(biome == null ? "1.00" : biome.EnemyDmgMul.ToString("F2"))
          .Append("  掉落组=").Append(biome == null ? 1u : biome.DropGroupBase)
          .Append("\n  Era=").Append(era).Append("/5  阵营=").Append(FactionService.GetFactionName(era))
          .Append("  FactionMul=").Append(FactionService.GetFactionMul(era).ToString("F2"))
          .Append("  本局锁定Era=").Append(FactionService.LockedEra == 0u ? "无" : FactionService.LockedEra.ToString());

        EnemyPlaceholder[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyPlaceholder>(true);
        sb.Append("\n  场上敌人 ").Append(enemies.Length).Append(" 个（列前 3 个）");
        int shown = 0;
        for (int i = 0; i < enemies.Length && shown < 3; i++)
        {
            EnemyPlaceholder enemy = enemies[i];
            if (enemy == null) continue;
            sb.Append("\n    ").Append(enemy.name)
              .Append("  HP=").Append(enemy.Hp).Append("/").Append(enemy.maxHp)
              .Append("  接触伤害=").Append(enemy.contactDamage);
            shown++;
        }

        return sb.ToString();
    }

    private void SetTerrainStatus(string text)
    {
        if (_terrainStatus != null) _terrainStatus.text = text;
    }
}
