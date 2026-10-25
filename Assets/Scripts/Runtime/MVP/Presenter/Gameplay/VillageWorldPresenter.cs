using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public enum VillageIntent { Map, Manage, Settings, Title }
    public interface IVillageWorldView { void Render(VillageWorldState state); void ShowMessage(string text); }
    public sealed class VillageWorldPresenter : PresenterBase<IVillageWorldView>
    {
        private readonly VillageWorldModel model = new VillageWorldModel();
        private VillageWorldState state;
        protected override void OnAttach() { Refresh(); }
        protected override void OnDetach() { }
        public void Refresh() { if (!IsDisposed && View != null) { state = model.Read(); View.Render(state); } }
        public void Handle(VillageIntent intent)
        {
            if (IsDisposed) return;
            switch (intent)
            {
                case VillageIntent.Map:
                    if (InputManager.Instance.CanReadGameplayInput && UIManager.Instance.panelStack.Count == 0)
                        UIManager.Instance.PushPanel(PanelType.WorldMapPanel);
                    break;
                case VillageIntent.Manage:
                    uint plot = state == null ? 0 : state.NearbyPlot;
                    if (InputManager.Instance.CanReadGameplayInput && model.CanManage(plot)) VillagePanel.OpenForBuilding(plot);
                    break;
                case VillageIntent.Settings:
                    if (UIManager.Instance.panelStack.Count == 0) UIManager.Instance.PushPanel(PanelType.SettingPanel);
                    break;
                case VillageIntent.Title: if (!model.Title(out var reason)) View.ShowMessage(reason); break;
            }
        }
    }
}
