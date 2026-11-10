using System;
using System.Collections.Generic;
using System.Linq;

namespace Chaos.MVP.Model.WorldMap
{
    /// <summary>汇总地点、交易和任务标记；查询不触发生产、接取或奖励。</summary>
    public sealed class WorldLocationModel
    {
        readonly int context = GameSession.Instance.ContextId;
        readonly Dictionary<uint, OfficeTaskSnapshot> markers = new Dictionary<uint, OfficeTaskSnapshot>();
        public uint SelectedId { get; private set; }
        public WorldLocationIdentity Identity { get; private set; } = WorldLocationIdentity.Family;
        public string Hint { get; private set; }
        uint Era => GameSession.Instance.LoadedPayload?.era ?? 1;

        public WorldLocationsDisplayState Read()
        {
            var service = WorldLocationService.Instance;
            var locations = service.GetLocations(Era, Identity);
            markers.Clear();
            foreach (OfficeTaskCategory category in Enum.GetValues(typeof(OfficeTaskCategory)))
            foreach (var task in OfficeTaskService.Instance.GetTasks(category))
            {
                if (task.MapLocationId == 0 || (task.Status != OfficeTaskStatus.Active && task.Status != OfficeTaskStatus.AwaitingBattle)) continue;
                var anchor = service.GetLocation(Era, task.MapLocationId, Identity);
                if (anchor == null) continue;
                uint id = 0x80000000u | unchecked(task.TaskId * 16777619u ^ task.Cycle);
                while (locations.Exists(p => p.Id == id)) id++;
                markers[id] = task;
                bool villageSupply = task.SupplyBuildingId != 0u;
                bool official = Identity == WorldLocationIdentity.Official;
                bool canUseOffice = anchor.CanOpenOffice || anchor.Kind == WorldLocationKind.PlayerVillage;
                string detail = task.Status == OfficeTaskStatus.AwaitingBattle ? OfficeTaskText.Get("awaitBattle")
                    : task.Timed ? OfficeTaskText.Time(task.RemainingSeconds) : "进行中的事务";
                if (villageSupply)
                    detail = $"{task.SupplyBuildingName}收取 {task.VillageSupplyProgress}/{task.Required} · "
                        + $"仓库{task.SupplyResourceName} {task.Progress}/{task.Required}"
                        + (task.Timed ? "\n" + OfficeTaskText.Time(task.RemainingSeconds) : "");
                string locked = !official ? "切换至官员事务处理此任务。" : anchor.LockReason;
                if (villageSupply && official && canUseOffice)
                    locked = task.VillageSupplyReady ? null : "收取进度和仓库持有量达标后可交付。";
                locations.Add(new WorldLocationSnapshot {
                    Id=id, Era=Era, FactionId=anchor.FactionId, FactionName=anchor.FactionName,
                    Name=task.Title, Description=task.Body+"\n\n"+detail,
                    Position=anchor.Position+(villageSupply ? new UnityEngine.Vector2(-.048f,-.038f)
                        : new UnityEngine.Vector2(.008f,.012f)), Kind=WorldLocationKind.Task,
                    IconKey="task", RelationName=anchor.RelationName,
                    CanOpenOffice=villageSupply ? official && canUseOffice && task.VillageSupplyReady : anchor.CanOpenOffice,
                    OfficialActionLabel=villageSupply ? "交付物资" : null,
                    LockReason=locked
                });
            }
            var selected=locations.Find(p=>p.Id==SelectedId);
            if(selected==null)SelectedId=0;
            var offers=new List<WorldLocationTradeRow>();
            if(selected!=null&&selected.CanTrade)foreach(uint id in service.GetMarketItems(selected.Id))
            {
                var inv=InventoryService.Instance;var def=inv.GetDefinition(id);if(def==null)continue;
                uint buy=service.GetBuyPrice(selected.Id,id),sell=service.GetSellPrice(selected.Id,id);
                uint owned=MarketService.Instance.GetCount(id);
                offers.Add(new WorldLocationTradeRow {ItemId=id,Name=MarketService.Instance.GetDisplayName(id),
                    BuyPrice=buy,SellPrice=sell,Owned=owned,
                    CanBuy=buy>0&&inv.GetResource(InventoryService.ResourceKind.Money)>=buy
                        &&inv.CanReceive(id,1,out _)
                        &&(!RunInventory.IsImportant(id)||!RunBagProgression.Owns(id)),
                    CanSell=sell>0&&owned>0&&!RunInventory.IsImportant(id)
                        &&inv.ResourceRoom(InventoryService.ResourceKind.Money)>=sell});
            }
            return new WorldLocationsDisplayState {Era=Era,Identity=Identity,Locations=locations,SelectedId=SelectedId,
                Selected=selected,Offers=offers,Hint=Hint??"选择地点下达指令。滚轮缩放，拖动空白地图平移。"};
        }

        public void Handle(WorldLocationUiIntent intent)
        {
            if(!GameSession.Instance.HasSession||GameSession.Instance.ContextId!=context){Hint="地图会话已失效，请重新打开。";return;}
            if(intent.Action==WorldLocationUiAction.Select){SelectedId=intent.LocationId;Hint=null;return;}
            if(intent.Action==WorldLocationUiAction.CloseDetail){SelectedId=0;Hint=null;return;}
            if(intent.Action==WorldLocationUiAction.Official){Identity=WorldLocationIdentity.Official;Hint=null;return;}
            if(intent.Action==WorldLocationUiAction.Family){Identity=WorldLocationIdentity.Family;Hint=null;return;}
            if(intent.LocationId!=SelectedId||SelectedId==0){Hint="地点选择已变化，请重新确认。";return;}
            uint target=markers.TryGetValue(SelectedId,out var task)?task.MapLocationId:SelectedId;
            var place=WorldLocationService.Instance.GetLocation(Era,target,Identity);
            if(place==null){Hint="地点在当前时代不可用。";return;}
            if (task != null && task.SupplyBuildingId != 0u)
            {
                if (intent.Action != WorldLocationUiAction.OpenOffice || task.Options.Length != 1)
                { Hint = "请使用官员事务中的交付入口。"; return; }
                if (OfficeTaskService.Instance.TryDeliverVillageSupply(task.TaskId, task.Cycle, task.Options[0].Index,
                    context, target, Identity, out string deliveryReason))
                {
                    SelectedId = 0;
                    Hint = "物资已交付，可在办事单位查阅办结回执。";
                }
                else Hint = deliveryReason;
                return;
            }
            string reason=null;bool ok=false;
            switch(intent.Action)
            {
                case WorldLocationUiAction.OpenOffice:
                    if(Identity!=WorldLocationIdentity.Official||!place.CanOpenOffice){Hint=place.LockReason??"当前身份无权处理此地官员事务。";return;}
                    OfficeTasksPanel.OpenFromWorldMap(target,task?.TaskId??0);return;
                case WorldLocationUiAction.Buy:
                    if(Identity==WorldLocationIdentity.Family&&place.CanTrade)ok=WorldLocationService.Instance.TryBuy(target,intent.ItemId,out reason);break;
                case WorldLocationUiAction.Sell:
                    if(Identity==WorldLocationIdentity.Family&&place.CanTrade)ok=WorldLocationService.Instance.TrySell(target,intent.ItemId,out reason);break;
                case WorldLocationUiAction.Build:
                    if(Identity==WorldLocationIdentity.Family&&place.CanBuild)ok=WorldResourceService.Instance.Build(target,out reason);break;
                case WorldLocationUiAction.Harvest:
                    if(Identity==WorldLocationIdentity.Family&&place.CanHarvest)ok=WorldResourceService.Instance.Harvest(target,out _,out reason);break;
                case WorldLocationUiAction.Resolve:
                    if(Identity==WorldLocationIdentity.Family&&place.CanResolve)ok=WorldLocationService.Instance.ResolveTemporary(target,out reason);break;
            }
            Hint=ok?"操作已保存。":reason??place.LockReason??"当前身份不能执行此操作。";
        }
    }
}
