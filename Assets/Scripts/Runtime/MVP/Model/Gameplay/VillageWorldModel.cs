using System;
using System.Collections.Generic;
using System.Linq;
using LocalizationSystem;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class VillageWorldState
    { public string Title, Resources, Hint, MapLabel, SettingsLabel, MainMenuLabel; public uint NearbyPlot; public string NearbyFacilityId; }
    public sealed class VillageWorldModel
    {
        private sealed class Candidate
        {
            public string Id;
            public float Distance;
            public VillagePlot Plot;
            public VillageFacility Facility;
        }

        public VillageWorldState Read()
        {
            var world = VillageSceneBootstrap.Current;
            if (world == null || world.Avatar == null) return new VillageWorldState();
            var inventory = InventoryService.Instance;
            var avatar = (Vector2)world.Avatar.transform.position;
            float range = RunMap.RunBehaviorSettings.Load("Ally").interactionRange;
            Candidate nearest = null;
            if (world.Plots != null)
                foreach (var plot in world.Plots)
                {
                    if (plot == null) continue;
                    Consider(ref nearest, range, avatar, plot.Entrance, plot.BuildingId.ToString(), plot, null);
                }
            if (world.Facilities != null)
                foreach (var facility in world.Facilities)
                {
                    if (facility == null || !facility.isActiveAndEnabled || facility.Kind == VillageFacilityKind.None
                        || string.IsNullOrEmpty(facility.Id)) continue;
                    Consider(ref nearest, range, avatar, facility.Entrance, facility.Id, null, facility);
                }
            string hint = string.Empty; uint nearbyPlot = 0; string nearbyFacility = null;
            if (nearest != null && nearest.Plot != null)
            {
                nearbyPlot = nearest.Plot.BuildingId;
                hint = VillageText.Get("Building" + nearbyPlot) + "　"
                    + VillageText.Get("BuildHint", VillageSceneBootstrap.BindingText(InputManager.ActionVillageInteract));
            }
            else if (nearest != null && nearest.Facility != null)
            {
                nearbyFacility = nearest.Facility.Id;
                string format = VillageFacilitySettings.Load().interactHint?.Resolve();
                string name = nearest.Facility.DisplayName ?? string.Empty;
                hint = string.IsNullOrEmpty(format) ? name : string.Format(format, name,
                    VillageSceneBootstrap.BindingText(InputManager.ActionVillageInteract));
            }
            return new VillageWorldState { Title = VillageText.Get("VillageTitle"),
                Resources = string.Join("　", Enum.GetValues(typeof(InventoryService.ResourceKind)).Cast<InventoryService.ResourceKind>()
                    .Select(k => InventoryService.ResourceDisplayName(k) + " " + inventory.GetResource(k))),
                MapLabel = VillageText.Get("MapHint", VillageSceneBootstrap.BindingText(InputManager.ActionVillageMap)),
                SettingsLabel = GameplayText.Text("ui_mainmenu_settingbtn", "ui_mainmenu_settingbtn"),
                MainMenuLabel = GameplayText.Text("ui_mainmenu_title", "ui_mainmenu_title"),
                Hint = hint, NearbyPlot = nearbyPlot, NearbyFacilityId = nearbyFacility };
        }

        /// <summary>One nearest target at most; equal distances resolve by ordinal ID so refreshes never flicker.</summary>
        private static void Consider(ref Candidate nearest, float range, Vector2 avatar, Vector2 entrance, string id,
            VillagePlot plot, VillageFacility facility)
        {
            float distance = Vector2.Distance(entrance, avatar);
            if (distance > range) return;
            if (nearest != null)
            {
                if (distance > nearest.Distance + .0001f) return;
                if (Mathf.Abs(distance - nearest.Distance) <= .0001f
                    && string.CompareOrdinal(id, nearest.Id) >= 0) return;
            }
            nearest = new Candidate { Id = id, Distance = distance, Plot = plot, Facility = facility };
        }

        public bool CanManage(uint id)
        {
            if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady || id == 0) return false;
            var current = Read();
            return current.NearbyPlot == id && string.IsNullOrEmpty(current.NearbyFacilityId);
        }

        /// <summary>Revalidates scene/session/object state and the current nearest target immediately before opening a facility.</summary>
        public bool CanUseFacility(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!VillageSceneFlow.IsVillageScene || !VillageSceneBootstrap.IsReady || !GameSession.Instance.HasSession) return false;
            var world = VillageSceneBootstrap.Current;
            if (world == null || world.Avatar == null || world.Facilities == null) return false;
            VillageFacility facility = null;
            foreach (var item in world.Facilities)
                if (item != null && string.Equals(item.Id, id, StringComparison.Ordinal)) { facility = item; break; }
            if (facility == null || !facility.isActiveAndEnabled || facility.Kind == VillageFacilityKind.None) return false;
            float range = RunMap.RunBehaviorSettings.Load("Ally").interactionRange;
            if (Vector2.Distance(facility.Entrance, world.Avatar.transform.position) > range) return false;
            return string.Equals(Read().NearbyFacilityId, id, StringComparison.Ordinal);
        }
        public bool Title(out string reason) { return VillageSceneFlow.ReturnToTitle(out reason); }
    }
}
