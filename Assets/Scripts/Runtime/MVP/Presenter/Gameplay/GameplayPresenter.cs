using System;

namespace Chaos.MVP.Presenter.Gameplay
{
    public enum PanelAction
    {
        Refresh, Close, Buy, Sell, Recruit, Toggle, Depart, Return, Unlock,
        Build, CancelBuild, Upgrade, Harvest, Advance, Cancel, ChooseOption,
        Continue, Confirm, Discard, Heal, Refill
    }

    public readonly struct PanelIntent
    {
        public PanelAction Action { get; }
        public uint Id { get; }
        public PanelIntent(PanelAction action, uint id = 0) { Action = action; Id = id; }
    }

    public enum TextTone { Normal, Dim, Warning, Success }

    public interface IGameplayView<TState> : IPanelView<TState, PanelIntent>
    {
        void ShowMessage(string message, bool success);
        void ClosePanel();
    }

    public abstract class GameplayPresenter<TState> : PresenterBase<IGameplayView<TState>>
    {
        protected override void OnAttach() { View.Intent += HandleIntent; Refresh(); }
        protected override void OnDetach() { if (View != null) View.Intent -= HandleIntent; }
        public void Refresh() { if (!IsDisposed && View != null) View.Render(CreateState()); }
        protected abstract TState CreateState();
        protected abstract void Execute(PanelIntent intent);
        protected virtual void CloseView() { View.ClosePanel(); }
        protected void Message(string message, bool success = false)
        {
            if (!IsDisposed && View != null) View.ShowMessage(message, success);
        }
        private void HandleIntent(PanelIntent intent)
        {
            if (IsDisposed) return;
            if (intent.Action == PanelAction.Close) { CloseView(); return; }
            if (intent.Action == PanelAction.Refresh) { Refresh(); return; }
            Execute(intent);
        }
    }

    public sealed class TextRow
    {
        public uint Id;
        public string Text;
        public TextTone Tone;
        public bool Enabled;
        public bool Selected;
        public uint Group;
        public string ActionLabel;
        public PanelAction Action;
    }
}
