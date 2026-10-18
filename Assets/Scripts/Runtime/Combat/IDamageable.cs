/// <summary>
/// 最小伤害接口（W3.0 spike）：任何能被打的东西实现它。
/// 以后玩家/门客/敌人/可破坏物都走这一套，避免各系统自造"扣血"方法。
/// </summary>
public interface IDamageable
{
    /// <summary>受到伤害。<paramref name="amount"/> 为正数。</summary>
    void TakeDamage(int amount);
}
