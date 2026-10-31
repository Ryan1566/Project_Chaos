using System;

namespace Chaos.Gameplay.RunTeam
{
    /// <summary>Prototype rules, centralized for a future exported configuration table.</summary>
    public static class RunCharacterGrowthRules
    {
        public const int MaxLevel = 99;
        public const int KillExperience = 20;
        public const int AttributeIncrement = 10;
        public const int BaseStamina = 100;
        public const int BaseMana = 100;
        public static int ExperienceForLevel(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level == MaxLevel ? 0 : level * 100;
        }
    }
}
