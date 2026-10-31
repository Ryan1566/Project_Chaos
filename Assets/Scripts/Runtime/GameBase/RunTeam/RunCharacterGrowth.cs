namespace Chaos.Gameplay.RunTeam
{
    public enum RunTeamAttribute { Health, Stamina, Mana }

    /// <summary>Pure per-member state; awarding XP and allocating never change live HP.</summary>
    public sealed class RunCharacterGrowth
    {
        public uint MemberId { get; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel => RunCharacterGrowthRules.ExperienceForLevel(Level);
        public int UnspentPoints { get; private set; }
        public int HealthBonus { get; private set; }
        public int StaminaBonus { get; private set; }
        public int ManaBonus { get; private set; }

        public RunCharacterGrowth(uint memberId) { MemberId = memberId; }

        public bool TryAddExperience(int amount, out string reason)
        {
            reason = null;
            if (amount < 0) { reason = "经验不能为负数"; return false; }
            long total = (long)Experience + amount;
            while (Level < RunCharacterGrowthRules.MaxLevel && total >= ExperienceToNextLevel)
            {
                total -= ExperienceToNextLevel; Level++; UnspentPoints++;
            }
            Experience = Level == RunCharacterGrowthRules.MaxLevel ? 0 : (int)total;
            return true;
        }

        public bool TrySpendPoint(RunTeamAttribute attribute, out string reason)
        {
            reason = null;
            if (attribute != RunTeamAttribute.Health && attribute != RunTeamAttribute.Stamina && attribute != RunTeamAttribute.Mana)
            { reason = "未知属性"; return false; }
            if (UnspentPoints <= 0) { reason = "没有可用属性点"; return false; }
            switch (attribute)
            {
                case RunTeamAttribute.Health: HealthBonus += RunCharacterGrowthRules.AttributeIncrement; break;
                case RunTeamAttribute.Stamina: StaminaBonus += RunCharacterGrowthRules.AttributeIncrement; break;
                case RunTeamAttribute.Mana: ManaBonus += RunCharacterGrowthRules.AttributeIncrement; break;
            }
            UnspentPoints--; return true;
        }
    }
}
