using System;
using Chaos.Gameplay.RunTeam;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class RunTeamPresenter : IDisposable
    {
        private readonly IRunTeamView _view;
        private readonly RunTeamModel _model = new RunTeamModel();
        private bool _active;
        public RunTeamPresenter(IRunTeamView view)
        {
            _view = view; _view.SelectRequested += Select; _view.AllocateRequested += Allocate;
            RunTeamProgress.Changed += Refresh;
        }
        public void BeginVisit() { _model.BeginVisit(); }
        public void SetActive(bool active) { _active = active; if (active) Refresh(); }
        public void Refresh() { if (_active) _view.Render(_model.Read()); }
        private void Select(uint id) { if (_active) { _model.Select(id); Refresh(); } }
        private void Allocate(RunTeamAttribute attribute) { if (_active) { _model.Allocate(attribute); Refresh(); } }
        public void Dispose()
        {
            _active = false; RunTeamProgress.Changed -= Refresh;
            _view.SelectRequested -= Select; _view.AllocateRequested -= Allocate;
        }
    }
}
