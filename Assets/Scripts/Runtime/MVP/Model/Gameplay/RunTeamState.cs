using System;
using Chaos.Gameplay.RunTeam;
using Spine.Unity;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Copied presentation values; views cannot mutate the run or live actors.</summary>
    public sealed class RunTeamMemberState
    {
        public uint Id;
        public string Name, Profession, Skin;
        public bool Controlled, Downed;
        public int Level, Experience, ExperienceToNextLevel, Points;
        public int Health, MaxHealth, MaxStamina, MaxMana;
        public Sprite Portrait;
        public SkeletonDataAsset SkeletonData;
    }

    public sealed class RunTeamState
    {
        public RunTeamMemberState[] Members = Array.Empty<RunTeamMemberState>();
        public uint SelectedId;
        public string Message;
        public bool CanAllocate;
        public Material SpineMaterial;
    }

    public interface IRunTeamView
    {
        event Action<uint> SelectRequested;
        event Action<RunTeamAttribute> AllocateRequested;
        void Render(RunTeamState state);
    }
}
