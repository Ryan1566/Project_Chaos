using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>A no-reward inspection proves the shared interaction contract without introducing NPC/loot rules.</summary>
    public sealed class MapInspectable : RunInteractable
    {
        public MapPlacementKind Kind;
        public bool Inspected { get; private set; }
        public override string DisplayName => RunInteractionText.Get(Kind == MapPlacementKind.Npc ? "Npc" : "Interactive");
        public override bool CanInteract(GameObject actor, out string reason)
        { reason = Inspected ? RunInteractionText.Get("Completed") : string.Empty; return !Inspected; }
        protected override bool Perform(GameObject actor, out string result)
        { Inspected = true; result = RunInteractionText.Get("Completed"); return true; }
    }
}
