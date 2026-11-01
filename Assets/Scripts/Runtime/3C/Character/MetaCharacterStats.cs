using UnityEngine;

/// <summary>
/// 家族树永久属性的统一计算入口。
///
/// 口径（QA/FamilyTree/contracts.md 第 8 条）：
/// 攻击 = 既有基础伤害 + BonusAttack；
/// 移动 = 既有基础速度 + BonusSpeed；
/// 生命 = 既有基础生命 + BonusHp；
/// 防御只削减普通正伤害，结果至少为 1；直接 QA 伤害与 ForceDown 不经过这里。
///
/// 加成只从 EffectAggregator 的只读快照读取，不写回模板、Prefab 或真实存档。
/// 快照按 GameSession.ContextId 隔离，并在聚合器版本变化时刷新：
/// 即使没有持久化槽位、只在 TestScene 沙盒里，也能正常读取内存态进度。
/// </summary>
public static class MetaCharacterStats
{
    // 缓存只保存当前会话/当前快照版本的派生值，避免同一帧重复查表与分配。
    private static int _cachedContext = int.MinValue;
    private static int _cachedVersion = int.MinValue;
    private static int _healthBonus;
    private static int _attackBonus;
    private static int _defenseBonus;
    private static int _speedBonus;

    /// <summary>家族树提供的生命上限加成（不包含局内成长和道具层）。</summary>
    public static int HealthBonus
    {
        get { Refresh(); return _healthBonus; }
    }

    /// <summary>普通攻击的最终伤害 = 既有基础伤害 + 家族树攻击加成。</summary>
    public static int AttackDamage(int baseDamage)
    {
        Refresh();
        return Saturate((long)baseDamage + _attackBonus);
    }

    /// <summary>移动速度的最终值 = 既有基础速度 + 家族树速度加成。</summary>
    public static float MoveSpeed(float baseSpeed)
    {
        Refresh();
        float result = baseSpeed + _speedBonus;
        return result < 0f || float.IsNaN(result) ? 0f : result;
    }

    /// <summary>
    /// 普通受击的最终伤害：只有正伤害才套用防御，且至少保留 1 点。
    /// 0/负伤害原样返回，供调用方继续沿用“非正伤害不生效”的旧语义。
    /// </summary>
    public static int ReceivedDamage(int incoming)
    {
        if (incoming <= 0) return incoming;
        Refresh();
        long result = (long)incoming - _defenseBonus;
        return result < 1L ? 1 : Saturate(result);
    }

    /// <summary>生命上限 = 既有基础生命 + 家族树生命加成，至少为 1。</summary>
    public static int MaxHealth(int baseHealth)
    {
        Refresh();
        long result = (long)baseHealth + _healthBonus;
        if (result < 1L) return 1;
        return Saturate(result);
    }

    /// <summary>
    /// 读取当前有效会话的属性快照。切换会话或效果重新聚合后只刷新一次；
    /// 不读取/创建真实存档槽位，TestScene 的沙盒与无会话内存态都可使用。
    /// </summary>
    private static void Refresh()
    {
        int context = GameSession.Instance.ContextId;
        MetaEffectSnapshot snapshot = EffectAggregator.Instance.Snapshot;
        if (context == _cachedContext && snapshot.Version == _cachedVersion) return;

        _cachedContext = context;
        _cachedVersion = snapshot.Version;
        _healthBonus = snapshot.HpBonus;
        _attackBonus = snapshot.AttackBonus;
        _defenseBonus = snapshot.DefenseBonus;
        _speedBonus = snapshot.SpeedBonus;
    }

    /// <summary>整数结果饱和，避免极端配置在加法/减法时溢出。</summary>
    private static int Saturate(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        _cachedContext = int.MinValue;
        _cachedVersion = int.MinValue;
        _healthBonus = _attackBonus = _defenseBonus = _speedBonus = 0;
    }
}
