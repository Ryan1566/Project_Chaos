using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public interface IRunInteractionView { void Render(RunInteractionState state); }
    public sealed class RunInteractionPresenter : PresenterBase<IRunInteractionView>
    {
        private readonly RunInteractionModel model;
        public RunInteractionPresenter(RunInteractionModel model) { this.model = model; }
        protected override void OnAttach() { Tick(false); }
        protected override void OnDetach() { }
        public void Tick(bool interactIntent)
        {
            if (IsDisposed || View == null) return;
            var state = model.Refresh();
            if (interactIntent) { model.Interact(); state = model.Refresh(); }
            View.Render(state);
        }
    }
}
