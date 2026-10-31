using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Display snapshot for the passive in-run content panel (NPC / chest / gathering).</summary>
    public sealed class RunContentViewState
    {
        public string Title;
        public string Body;
        public string Hint;
        public string CloseText;
        public Sprite Portrait;
        public bool Completed;
        public RunContentChoiceState[] Choices;
    }

    /// <summary>Display snapshot of one selectable content entry.</summary>
    public sealed class RunContentChoiceState
    {
        public int Index;
        public string Name;
        public string Detail;
        public string ButtonText;
        public Sprite Icon;
        public bool Enabled;
        public bool Claimed;
    }
}
