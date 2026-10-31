#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Chaos.Gameplay.RunTeam;
using UnityEngine;

public static class RunTeamVerification
{
    public static string Domain()
    {
        int checks = 0;
        var report = new StringBuilder();
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); checks++; };
        var g = new RunCharacterGrowth(17);
        check(g.Level == 1 && g.Experience == 0 && g.UnspentPoints == 0, "initial state");
        check(!g.TryAddExperience(-1, out _) && g.Experience == 0, "negative XP atomic");
        check(!g.TrySpendPoint(RunTeamAttribute.Health, out _) && g.HealthBonus == 0, "no point atomic");
        g.TryAddExperience(99, out _); check(g.Level == 1 && g.Experience == 99, "below threshold");
        g.TryAddExperience(1, out _); check(g.Level == 2 && g.Experience == 0 && g.UnspentPoints == 1, "exact threshold");
        check(!g.TrySpendPoint((RunTeamAttribute)999, out _) && g.UnspentPoints == 1, "invalid enum atomic");
        check(g.TrySpendPoint(RunTeamAttribute.Health, out _) && g.UnspentPoints == 0 && g.HealthBonus == 10, "health point");
        g.TryAddExperience(int.MaxValue, out _);
        check(g.Level == 99 && g.Experience == 0 && g.ExperienceToNextLevel == 0 && g.UnspentPoints == 97, "large XP cap");
        g.TryAddExperience(int.MaxValue, out _); check(g.UnspentPoints == 97 && g.Experience == 0, "cap repeat");
        check(g.TrySpendPoint(RunTeamAttribute.Stamina, out _) && g.StaminaBonus == 10, "stamina point");
        check(g.TrySpendPoint(RunTeamAttribute.Mana, out _) && g.ManaBonus == 10, "mana point");
        var random = new System.Random(73819);
        for (int trial = 0; trial < 1000; trial++)
        {
            var member = new RunCharacterGrowth((uint)trial + 1);
            long total = 0;
            for (int step = 0; step < 12; step++)
            {
                int added = trial % 5 == 0 && step == 11 ? int.MaxValue : random.Next(0, 70000);
                total += added; member.TryAddExperience(added, out _);
                int expectedLevel = 1;
                while (expectedLevel < 99 && total >= 50L * expectedLevel * (expectedLevel + 1)) expectedLevel++;
                long consumed = 50L * (expectedLevel - 1) * expectedLevel;
                check(member.Level == expectedLevel && member.UnspentPoints == expectedLevel - 1
                    && member.Experience == (expectedLevel == 99 ? 0 : total - consumed), "independent cumulative XP oracle");
            }
        }
        report.AppendLine("PASS " + checks + " independent domain assertions (1000 sequences)");
        string dir = Application.dataPath + "/QA/RunTeam"; Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/domain.txt", report.ToString()); return report.ToString();
    }
}
#endif
