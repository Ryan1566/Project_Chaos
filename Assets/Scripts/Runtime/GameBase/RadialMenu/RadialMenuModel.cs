using System;
using System.Collections.Generic;

namespace Chaos.UI.RadialMenu
{
    /// <summary>Relative-pointer selection, clockwise from up. No engine or gameplay dependencies.</summary>
    public sealed class RadialMenuModel
    {
        readonly float _deadZone, _maxRadius, _hoverDelay;
        float _hover;
        public bool IsOpen { get; private set; }
        public int Level { get; private set; } = 1;
        public int ParentIndex { get; private set; } = -1;
        public int SelectedIndex { get; private set; } = -1;
        public float PointerX { get; private set; }
        public float PointerY { get; private set; }
        public IReadOnlyList<RadialMenuOption> RootOptions { get; private set; } = Array.Empty<RadialMenuOption>();
        public IReadOnlyList<RadialMenuOption> CurrentOptions => Level == 2 ? RootOptions[ParentIndex].Children : RootOptions;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public RadialMenuModel(float deadZone, float maxRadius, float hoverDelaySeconds)
        {
            if (!Finite(deadZone) || !Finite(maxRadius) || !Finite(hoverDelaySeconds)
                || deadZone < 0 || maxRadius <= deadZone || hoverDelaySeconds < 0) throw new ArgumentOutOfRangeException();
            _deadZone = deadZone; _maxRadius = maxRadius; _hoverDelay = hoverDelaySeconds;
        }
        static IReadOnlyList<RadialMenuOption> Copy(IReadOnlyList<RadialMenuOption> source, int depth)
        {
            if (source == null) return Array.Empty<RadialMenuOption>();
            if (source.Count > 12 || depth > 2) throw new ArgumentException("轮盘每层最多12项，最多两层");
            var result = new RadialMenuOption[source.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var s = source[i];
                if (s == null) throw new ArgumentException("空轮盘项");
                result[i] = new RadialMenuOption { Id = s.Id, Name = s.Name, IconKey = s.IconKey,
                    ShortcutText = s.ShortcutText, ShortcutNumber = s.ShortcutNumber, CountText = s.CountText,
                    Enabled = s.Enabled, DisabledReason = s.DisabledReason,
                    Children = s.Children == null || s.Children.Count == 0 ? null : Copy(s.Children, depth + 1) };
            }
            return Array.AsReadOnly(result);
        }
        public void Open(IReadOnlyList<RadialMenuOption> roots)
        { var copy = Copy(roots, 1); RootOptions = copy; IsOpen = true; Level = 1; ParentIndex = -1; ResetPointer(); }
        public void Close() { IsOpen = false; Level = 1; ParentIndex = -1; ResetPointer(); }
        void ResetPointer() { PointerX = PointerY = _hover = 0; SelectedIndex = -1; }
        public bool MovePointer(float deltaX, float deltaY)
        {
            if (!IsOpen || !Finite(deltaX) || !Finite(deltaY)) return false;
            // Clamp in double precision so even a finite extreme device delta cannot overflow.
            double x = (double)PointerX + deltaX, y = (double)PointerY + deltaY;
            double magnitude = Math.Sqrt(x * x + y * y);
            if (magnitude > _maxRadius) { x *= _maxRadius / magnitude; y *= _maxRadius / magnitude; }
            PointerX = (float)x; PointerY = (float)y;
            int next = -1, count = CurrentOptions.Count;
            if (magnitude > _deadZone && count > 0)
            {
                double angle = Math.Atan2(x, y) * 180 / Math.PI;
                next = (int)Math.Floor(((angle + 360 + 180d / count) % 360) / (360d / count)) % count;
            }
            if (next == SelectedIndex) return false;
            SelectedIndex = next; _hover = 0; return true;
        }
        bool EnterChildren()
        {
            if (Level != 1 || SelectedIndex < 0) return false;
            var option = CurrentOptions[SelectedIndex];
            if (!option.Enabled || option.Children == null || option.Children.Count == 0) return false;
            ParentIndex = SelectedIndex; Level = 2; ResetPointer(); return true;
        }
        public bool Tick(float unscaledDeltaTime)
        {
            if (!IsOpen || Level != 1 || SelectedIndex < 0 || !Finite(unscaledDeltaTime) || unscaledDeltaTime < 0) return false;
            _hover += unscaledDeltaTime;
            return _hover >= _hoverDelay && EnterChildren();
        }
        public bool SelectShortcut(int number)
        {
            if (!IsOpen || number <= 0) return false;
            for (int i = 0; i < CurrentOptions.Count; i++) if (CurrentOptions[i].ShortcutNumber == number)
            { SelectedIndex = i; _hover = 0;
                double angle = i * 2 * Math.PI / CurrentOptions.Count;
                PointerX = (float)Math.Sin(angle) * _maxRadius; PointerY = (float)Math.Cos(angle) * _maxRadius; return true; }
            return false;
        }
        public bool TryConfirm(out RadialMenuOption leaf, out string reason)
        {
            leaf = null; reason = null;
            if (!IsOpen || SelectedIndex < 0) { reason = "移动鼠标选择道具"; return false; }
            var option = CurrentOptions[SelectedIndex];
            if (!option.Enabled) { reason = option.DisabledReason ?? "当前不可使用"; return false; }
            if (EnterChildren()) return false;
            leaf = option; Close(); return true;
        }
        public void Back()
        { if (Level == 2) { Level = 1; ParentIndex = -1; ResetPointer(); } else Close(); }
    }
}
