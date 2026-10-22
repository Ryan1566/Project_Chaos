using System;
using Chaos.MVP.Model.Startup;

namespace Chaos.MVP.Presenter.Startup
{
    public enum MainMenuIntent { Start, Updates, Settings, Quit }
    public interface IMainMenuView { event Action<MainMenuIntent> Intent; }

    public sealed class MainMenuPresenter : PresenterBase<IMainMenuView>
    {
        private readonly MainMenuModel _model;
        public MainMenuPresenter(MainMenuModel model) { _model = model ?? throw new ArgumentNullException(nameof(model)); }
        protected override void OnAttach() { View.Intent += Handle; }
        protected override void OnDetach() { if (View != null) View.Intent -= Handle; }
        private void Handle(MainMenuIntent intent)
        {
            switch (intent)
            {
                case MainMenuIntent.Start: UIManager.Instance.PushPanel(PanelType.SLPanel); break;
                case MainMenuIntent.Updates: _model.ShowUpdates(); break;
                case MainMenuIntent.Settings: UIManager.Instance.PushPanel(PanelType.SettingPanel); break;
                case MainMenuIntent.Quit: _model.Quit(); break;
            }
        }
    }
}
