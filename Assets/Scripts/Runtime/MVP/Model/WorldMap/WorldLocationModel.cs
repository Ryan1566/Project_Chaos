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
                locations.Add(new WorldLocationSnapshot {
                    Id=id, Era=Era, FactionId=anchor.FactionId, FactionName=anchor.FactionName,
                    Name=task.Title, Description=task.Body+"\n\n"+(task.Status==OfficeTaskStatus.AwaitingBattle
                        ?OfficeTaskText.Get("awaitBattle"):task.Timed?OfficeTaskText.Time(task.RemainingSeconds):"进行中的事务"),
                    Position=anchor.Position+new UnityEngine.Vector2(.008f,.012f), Kind=WorldLocationKind.Task,
                    IconKey="task", RelationName=anchor.RelationName, CanOpenOffice=anchor.CanOpenOffice,
                    LockReason=Identity==WorldLocationIdentity.Family?"切换至官员事务处理此任务。":anchor.LockReason
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
