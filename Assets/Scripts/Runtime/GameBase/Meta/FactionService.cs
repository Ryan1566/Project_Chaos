using ChaosDebug;

/// <summary>
/// 阵营服务（W5.2 / M14「地形与阵营」）：原型期用 **(B) 时代映射** —— `Era → 阵营` 5 条常量，
/// **不新建表、不给任何表加列**（17_地形与阵营.md §3.2 / §4）。
///
/// 口径（Lead 已裁决，不要改）：
/// * `Era` 1/2 → **秦军**、3/4 → **楚军**、5 → **汉军**；
/// * **本波 `FactionMul` 全部 1.00**（只当"标签"，避免与 `Era.BaseDifficulty` 平方级放大，§3.5 / Q2）。
///   常量与接口先留着，**以后启用** —— 到时只改 `GetFactionMul` 一个方法。
/// * 越界 `Era`（不在 1~5）按 1（秦军）兜底 + Warn（§9 E3）。
///
/// 阵营**只影响**：① 敌人属性倍率 ② 进场提示文字。**不影响**掉落与事件（§3.2）。
/// </summary>
public static class FactionService
{
    public const uint MinEra = 1u;
    public const uint MaxEra = 5u;

    /// <summary>
    /// **本波（W5.2）`FactionMul` 恒为 1.00**（Lead 裁决）。
    /// 数值定位：`Era.BaseDifficulty` 已经随时代上升，再乘阵营倍率会把难度**平方级**放大（§3.5 / Q2），
    /// 所以本波只把"阵营"当表现层标签。**以后启用**时改成按 Era 返回 1.00~1.40 即可。
    /// </summary>
    public const float FactionMulThisWave = 1.00f;

    /// <summary>本局锁定的时代（0 = 未锁定）。出发（进第 1 场景）时定格，整局不变（§9 E7）。</summary>
    private static uint _lockedEra;

    /// <summary>本局锁定的时代（0 = 还没锁定）。</summary>
    public static uint LockedEra { get { return _lockedEra; } }

    /// <summary>
    /// 阵营名：`Era` 1/2 → 秦军、3/4 → 楚军、5 → 汉军。
    /// 越界按 1（秦军）兜底并 Warn（§9 E3）。
    /// </summary>
    public static string GetFactionName(uint era)
    {
        switch (era)
        {
            case 1u:
            case 2u:
                return "秦军";
            case 3u:
            case 4u:
                return "楚军";
            case 5u:
                return "汉军";
            default:
                ChaosLog.Warn(LogChannel.Config, "[阵营] Era=" + era + " 不在 1~5 → 按 Era=1（秦军）兜底（§9 E3）");
                return "秦军";
        }
    }

    /// <summary>
    /// 阵营强度倍率。**本波恒为 1.00**（只作标签）；接数值后在此按 Era 返回 1.00~1.40（**以后启用**）。
    /// 接口签名保持不变，所以启用时调用点（敌人属性）一行都不用改。
    /// </summary>
    public static float GetFactionMul(uint era)
    {
        //以后启用（本波故意不启用，见 FactionMulThisWave 注释）：
        //  return 1.00f + (Mathf.Clamp((int)era, 1, 5) - 1) * 0.10f;
        return FactionMulThisWave;
    }

    /// <summary>
    /// **当前生效的时代**：已锁定就用锁定值（整局不变，§9 E7），否则读 `MetaProgressService.Era`。
    /// </summary>
    public static uint ResolveEra()
    {
        if (_lockedEra >= MinEra && _lockedEra <= MaxEra) return _lockedEra;

        MetaProgressService meta = MetaProgressService.Instance;
        if (meta == null) return MinEra;
        return meta.Era;
    }

    /// <summary>
    /// 出发（进第 1 场景）时**定格本局阵营**。GM「强制 Era=5」会先 `ResetRunFaction()` 再进场景，
    /// 语义等同"重新出发"，所以提示会跟着变（§9 E7 的例外只对 GM 开放）。
    /// </summary>
    public static void LockRunFaction(uint era)
    {
        uint clamped = era < MinEra ? MinEra : (era > MaxEra ? MaxEra : era);
        _lockedEra = clamped;
        ChaosLog.Info(LogChannel.Config, "[阵营] 本局阵营已定格：Era=" + clamped
            + " → " + GetFactionName(clamped) + "（FactionMul=" + GetFactionMul(clamped).ToString("F2") + "）");
    }

    /// <summary>撤离 / 重新出发前清锁定（GM「强制 Era」也调它 = 让下一场景重新定格）。</summary>
    public static void ResetRunFaction()
    {
        if (_lockedEra != 0u)
            ChaosLog.Info(LogChannel.Config, "[阵营] 本局阵营锁定已清空（原 Era=" + _lockedEra + "）");
        _lockedEra = 0u;
    }

    /// <summary>GM / 验收打印用一行状态。</summary>
    public static string DumpState()
    {
        uint era = ResolveEra();
        return "[阵营] Era=" + era + "/5 → " + GetFactionName(era)
            + "  FactionMul=" + GetFactionMul(era).ToString("F2")
            + "（本波恒 1.00，以后启用）  本局锁定 Era=" + (_lockedEra == 0u ? "无" : _lockedEra.ToString());
    }
}
