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

        internal void Capture(RunMemberSave member)
        {
            member.level = Level;
            member.experience = Experience;
            member.points = UnspentPoints;
            member.health = HealthBonus;
            member.stamina = StaminaBonus;
            member.mana = ManaBonus;
        }

        internal bool Restore(RunMemberSave member)
        {
            if (member == null || member.id != MemberId || member.level < 1 || member.level > RunCharacterGrowthRules.MaxLevel
                || member.experience < 0 || member.points < 0 || member.health < 0 || member.stamina < 0 || member.mana < 0)
                return false;
            int increment = RunCharacterGrowthRules.AttributeIncrement;
            long spent = (long)member.health + member.stamina + member.mana;
            if (member.health % increment != 0 || member.stamina % increment != 0 || member.mana % increment != 0
                || spent / increment + member.points != member.level - 1
                || (member.level == RunCharacterGrowthRules.MaxLevel ? member.experience != 0
                    : member.experience >= RunCharacterGrowthRules.ExperienceForLevel(member.level))) return false;
            Level = member.level;
            Experience = member.experience;
            UnspentPoints = member.points;
            HealthBonus = member.health;
            StaminaBonus = member.stamina;
            ManaBonus = member.mana;
            return true;
        }

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
