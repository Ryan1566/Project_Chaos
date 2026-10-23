using System;
using Chaos.MVP.Model.WorldMap;

namespace Chaos.MVP.Presenter.WorldMap
{
    public enum WorldMapAction { Select, Preview, OpenPanel, Return, Save, ConfirmReturn, CancelReturn }
    public readonly struct WorldMapIntent
    {
        public readonly WorldMapAction Action;
        public readonly uint Point;
        public readonly int Delta;
        public readonly PanelType Panel;
        public WorldMapIntent(WorldMapAction action, uint point = 0, int delta = 0, PanelType panel = default)
        { Action = action; Point = point; Delta = delta; Panel = panel; }
    }
    public interface IWorldMapView : IPanelView<WorldMapSnapshot, WorldMapIntent> { }
    public sealed class WorldMapPresenter : PresenterBase<IWorldMapView>
    {
        private readonly WorldMapModel _model;
        public WorldMapPresenter(WorldMapModel model) { _model = model ?? throw new ArgumentNullException(nameof(model)); }
        protected override void OnAttach() { View.Intent += Handle; View.Render(_model.Snapshot()); }
        protected override void OnDetach() { if (View != null) View.Intent -= Handle; }
        private void Handle(WorldMapIntent intent)
        {
            switch (intent.Action)
            {
                case WorldMapAction.Select: _model.Select(intent.Point); break;
                case WorldMapAction.Preview: _model.Preview(intent.Delta); break;
                case WorldMapAction.OpenPanel: if (UIManager.Instance != null) UIManager.Instance.PushPanel(intent.Panel); return;
                case WorldMapAction.Save: _model.Save(); break;
                case WorldMapAction.Return: _model.RequestReturn(); break;
                case WorldMapAction.CancelReturn: _model.CancelReturn(); break;
                case WorldMapAction.ConfirmReturn:
                    if (_model.ReturnToMenu()) { UIManager.Instance.SetRootPanel(PanelType.MainMenuPanel); return; }
                    break;
            }
            if (!IsDisposed) View.Render(_model.Snapshot());
        }
    }
}
