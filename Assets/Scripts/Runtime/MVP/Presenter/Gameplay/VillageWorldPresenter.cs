using System;
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
                case VillageIntent.Manage: Manage(); break;
                case VillageIntent.Settings:
                    if (UIManager.Instance.panelStack.Count == 0) UIManager.Instance.PushPanel(PanelType.SettingPanel);
                    break;
                case VillageIntent.Title: if (!model.Title(out var reason)) View.ShowMessage(reason); break;
            }
        }
        private void Manage()
        {
            if (!InputManager.Instance.CanReadGameplayInput || UIManager.Instance.panelStack.Count != 0) return;
            // Re-read at commit: the player may have moved or a panel may have opened since the last HUD refresh.
            state = model.Read();
            if (!string.IsNullOrEmpty(state.NearbyFacilityId))
            {
                if (!model.CanUseFacility(state.NearbyFacilityId)) return;
                var facility = FindFacility(state.NearbyFacilityId);
                if (facility == null) return;
                switch (facility.Kind)
                {
                    case VillageFacilityKind.Office: VillageOfficePanel.Open(state.NearbyFacilityId); break;
                    case VillageFacilityKind.FamilyMansion: UIManager.Instance.PushPanel(PanelType.FamilyTreePanel); break;
                    case VillageFacilityKind.Warehouse: UIManager.Instance.PushPanel(PanelType.InventoryPanel); break;
                }
                return;
            }
            if (state.NearbyPlot != 0 && model.CanManage(state.NearbyPlot)) VillagePanel.OpenForBuilding(state.NearbyPlot);
        }
        private static VillageFacility FindFacility(string id)
        {
            var world = VillageSceneBootstrap.Current;
            if (world == null || world.Facilities == null) return null;
            foreach (var facility in world.Facilities)
                if (facility != null && string.Equals(facility.Id, id, StringComparison.Ordinal)) return facility;
            return null;
        }
    }
}
