using Chaos.MVP.Model.Gameplay;
using Chaos.UI.RadialMenu;
using UnityEngine;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class RadialMenuPresenter
    {
        public RadialMenuModel Model { get; } = new RadialMenuModel(32, 250, .22f);
        readonly IRadialMenuProvider _provider;
        readonly RadialMenuView _view;
        readonly System.Func<RadialMenuOption, Sprite> _resolveIcon;
        RadialOptionViewState[] _primary, _secondary;
        string _hint;
        int _renderedParent = -2;
        public RadialMenuPresenter(IRadialMenuProvider provider, RadialMenuView view, System.Func<RadialMenuOption, Sprite> resolveIcon = null)
        { _provider = provider; _view = view; _resolveIcon = resolveIcon; }
        public void Open() { Model.Open(_provider.BuildOptions()); _primary = Map(Model.RootOptions); _renderedParent = -2; _hint = null; Render(); }
        public void Move(float x, float y, float dt) { Model.MovePointer(x, y); Model.Tick(dt); Render(); }
        public void Shortcut(int number) { Model.SelectShortcut(number); Render(); }
        public bool Confirm(out string message)
        {
            if (Model.TryConfirm(out var leaf, out message))
            {
                if (_provider.TryExecute(leaf.Id, out message)) return true;
                Model.Open(_provider.BuildOptions()); _primary = Map(Model.RootOptions); _renderedParent = -2;
            }
            _hint = message; Render(); return false;
        }
        public void Back() { Model.Back(); _hint = null; Render(); }
        void Render()
        {
            if (_renderedParent != Model.ParentIndex)
            {
                _renderedParent = Model.ParentIndex;
                _secondary = Model.Level == 2 ? Map(Model.CurrentOptions) : System.Array.Empty<RadialOptionViewState>();
            }
            string hint = _hint;
            if (hint == null && Model.SelectedIndex >= 0)
            { var current = Model.CurrentOptions[Model.SelectedIndex]; hint = current.Enabled ? current.Name : current.DisabledReason; }
            _view.Render(new RadialMenuViewState { Primary = _primary, Secondary = _secondary,
                IsSecondary = Model.Level == 2, SelectedPrimary = Model.Level == 2 ? Model.ParentIndex : Model.SelectedIndex,
                SelectedSecondary = Model.Level == 2 ? Model.SelectedIndex : -1, PointerPixels = new Vector2(Model.PointerX, Model.PointerY),
                Hint = (hint ?? "移动鼠标选择分类") + "\n左键确认 · 右键 / Esc 取消 · Backspace 返回分类" });
        }
        RadialOptionViewState[] Map(System.Collections.Generic.IReadOnlyList<RadialMenuOption> options)
        {
            var states = new RadialOptionViewState[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var icon = _resolveIcon != null ? _resolveIcon(option) : string.IsNullOrEmpty(option.IconKey) ? null : Resources.Load<Sprite>(option.IconKey);
                states[i] = new RadialOptionViewState { Id = option.Id, Name = option.Name, Icon = icon,
                    CountText = option.CountText, ShortcutText = option.ShortcutText, Enabled = option.Enabled, DisabledReason = option.DisabledReason };
            }
            return states;
        }
    }
}
