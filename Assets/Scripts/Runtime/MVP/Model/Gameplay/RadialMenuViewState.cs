using System;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Presentation contract shared by item, command or camp radial-menu providers.</summary>
    public sealed class RadialOptionViewState
    {
        public string Id, Name, ShortcutText, CountText, DisabledReason;
        public Sprite Icon;
        public bool Enabled = true;
    }

    public sealed class RadialMenuViewState
    {
        public RadialOptionViewState[] Primary = Array.Empty<RadialOptionViewState>();
        public RadialOptionViewState[] Secondary = Array.Empty<RadialOptionViewState>();
        public int SelectedPrimary = -1, SelectedSecondary = -1;
        public bool IsSecondary;
        public Vector2 PointerPixels;
        public string Hint;
    }
}
