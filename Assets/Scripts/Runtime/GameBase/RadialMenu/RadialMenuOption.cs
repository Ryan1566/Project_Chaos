using System;
using System.Collections.Generic;

namespace Chaos.UI.RadialMenu
{
    /// <summary>Function-neutral entry. A provider owns execution; the menu only selects.</summary>
    public sealed class RadialMenuOption
    {
        public string Id, Name, IconKey, ShortcutText, CountText, DisabledReason;
        public bool Enabled = true;
        public int ShortcutNumber;
        public IReadOnlyList<RadialMenuOption> Children;
    }
    public interface IRadialMenuProvider
    {
        IReadOnlyList<RadialMenuOption> BuildOptions();
        bool TryExecute(string id, out string reason);
    }
}
