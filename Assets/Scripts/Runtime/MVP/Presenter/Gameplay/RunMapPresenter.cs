using System;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class RunMapPresenter : IDisposable
    {
        private readonly RunMapModel _model = new RunMapModel();
        private readonly RunMapView _view;
        public RunMapPresenter(RunMapView view)
        { _view = view; _view.RefreshRequested += Refresh; }
        public void Refresh() { _model.Refresh(); _view.Render(_model); }
        public void Dispose() { _view.RefreshRequested -= Refresh; _model.Dispose(); }
    }
}
