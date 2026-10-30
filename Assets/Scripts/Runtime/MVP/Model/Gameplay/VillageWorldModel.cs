using System;
using System.Linq;
using LocalizationSystem;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class VillageWorldState
    { public string Title, Resources, Hint, MapLabel, SettingsLabel, MainMenuLabel; public uint NearbyPlot; }
    public sealed class VillageWorldModel
    {
        public VillageWorldState Read()
        {
            var world = VillageSceneBootstrap.Current;
            if (world == null || world.Avatar == null) return new VillageWorldState();
            var inventory = InventoryService.Instance;
            var near = world.Plots.OrderBy(p => Vector2.Distance(p.Entrance, world.Avatar.transform.position)).FirstOrDefault();
            bool available = near != null && Vector2.Distance(near.Entrance, world.Avatar.transform.position) <= RunMap.RunBehaviorSettings.Load("Ally").interactionRange;
            return new VillageWorldState { Title = VillageText.Get("VillageTitle"),
                Resources = string.Join("　", Enum.GetValues(typeof(InventoryService.ResourceKind)).Cast<InventoryService.ResourceKind>()
                    .Select(k => InventoryService.ResourceDisplayName(k) + " " + inventory.GetResource(k))),
                MapLabel = VillageText.Get("MapHint", VillageSceneBootstrap.BindingText(InputManager.ActionVillageMap)),
                SettingsLabel = GameplayText.Text("ui_mainmenu_settingbtn", "ui_mainmenu_settingbtn"),
                MainMenuLabel = GameplayText.Text("ui_mainmenu_title", "ui_mainmenu_title"),
                Hint = available ? VillageText.Get("Building" + near.BuildingId) + "　" + VillageText.Get("BuildHint", VillageSceneBootstrap.BindingText(InputManager.ActionVillageInteract)) : string.Empty,
                NearbyPlot = available ? near.BuildingId : 0 };
        }
        public bool CanManage(uint id)
        {
            if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady || id == 0) return false;
            return Read().NearbyPlot == id;
        }
        public bool Title(out string reason) { return VillageSceneFlow.ReturnToTitle(out reason); }
    }
}
