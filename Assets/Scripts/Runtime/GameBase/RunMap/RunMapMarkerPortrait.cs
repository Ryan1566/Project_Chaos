using UnityEngine;

namespace RunMap
{
    public enum RunMapMarkerKind { Player, Ally, Enemy, Npc, Interactive }

    /// <summary>Optional authored map portrait. Dynamic targets never reveal their live position through fog.</summary>
    [DisallowMultipleComponent]
    public sealed class RunMapMarkerPortrait : MonoBehaviour
    {
        public Sprite Portrait;
        public RunMapMarkerKind Kind = RunMapMarkerKind.Interactive;
        public bool RememberWhenHidden;
    }
}
