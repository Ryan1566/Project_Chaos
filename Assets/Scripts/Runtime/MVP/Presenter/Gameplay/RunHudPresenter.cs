using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class HealthRow
    {
        public string Text;
        public float Ratio;
        public bool Downed;
    }
    public interface ITeamHudView { void RenderTeam(IReadOnlyList<HealthRow> rows); }
    public interface IDepthHudView { void RenderDepth(string text); }

    public sealed class TeamHudPresenter : PresenterBase<ITeamHudView>
    {
        private readonly RunHudModel _model = new RunHudModel();
        protected override void OnAttach() { Refresh(); }
        protected override void OnDetach() { }
        public void Refresh()
        {
            if (IsDisposed || View == null) return;
            var rows = new List<HealthRow>();
            foreach (var actor in _model.ReadTeam())
                rows.Add(new HealthRow { Text = actor.Name + "　" + actor.Hp + "/" + actor.MaxHp + "　" + (actor.Downed ? "倒地" : "可用"),
                    Ratio = actor.MaxHp <= 0 ? 0 : Math.Max(0, Math.Min(1, (float)actor.Hp / actor.MaxHp)), Downed = actor.Downed });
            View.RenderTeam(rows.ToArray());
        }
    }
    public sealed class DepthHudPresenter : PresenterBase<IDepthHudView>
    {
        private readonly RunHudModel _model = new RunHudModel();
        protected override void OnAttach() { Refresh(); }
        protected override void OnDetach() { }
        public void Refresh()
        {
            if (!IsDisposed && View != null)
                View.RenderDepth("深度 " + _model.Depth + "/" + _model.DepthCap + " · 场景 " + _model.SceneIndex + "/3 · 可带出 " + _model.DeployCap + " 件");
        }
    }
}
