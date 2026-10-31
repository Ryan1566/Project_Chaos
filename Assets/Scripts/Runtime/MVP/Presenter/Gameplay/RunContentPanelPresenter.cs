using System;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public interface IRunContentPanelView
    {
        event Action<int> ClaimRequested;
        event Action CloseRequested;
        void Render(RunContentViewState state);
        void CloseInteraction();
    }
    public sealed class RunContentPanelPresenter : PresenterBase<IRunContentPanelView>
    {
        readonly RunContentPanelModel model;
        public RunContentPanelPresenter(RunContentPanelModel model) { this.model = model; }
        protected override void OnAttach()
        { View.ClaimRequested += Claim; View.CloseRequested += Close; model.Changed += Refresh; Refresh(); }
        protected override void OnDetach()
        { model.Changed -= Refresh; if (View != null) { View.ClaimRequested -= Claim; View.CloseRequested -= Close; } }
        void Claim(int index) { if (!IsDisposed) model.Claim(index); }
        void Close() { if (!IsDisposed) View.CloseInteraction(); }
        void Refresh() { if (!IsDisposed && model.IsValid) View.Render(model.Read()); }
        public void Tick() { if (!IsDisposed && !model.IsValid) View.CloseInteraction(); }
    }
}
