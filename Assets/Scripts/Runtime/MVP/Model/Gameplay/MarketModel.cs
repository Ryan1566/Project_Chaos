using System.Collections.Generic;

namespace Chaos.MVP.Model.Gameplay
{
    public enum BuyBlock { None, Unavailable, InsufficientMoney, CannotReceive }
    public sealed class MarketOffer
    {
        public uint Id, BuyPrice, SellPrice, Owned;
        public string NameKey, BlockReason;
        public BuyBlock BuyBlock;
        public bool CanSell;
    }
    public sealed class MarketModel
    {
        public IReadOnlyList<MarketOffer> GetOffers()
        {
            var inv = InventoryService.Instance;
            var rows = new List<MarketOffer>();
            foreach (var def in inv.GetAllDefinitions())
            {
                if (def == null || !def.Enable || (def.BuyPrice == 0 && def.SellPrice == 0)) continue;
                uint count = MarketService.Instance.GetCount(def.Index);
                bool canReceive = inv.CanReceive(def.Index, 1, out string reason);
                if (RunInventory.IsImportant(def.Index) && count > 0)
                { canReceive = false; reason = "已永久拥有"; }
                BuyBlock block = def.BuyPrice == 0 ? BuyBlock.Unavailable
                    : inv.GetResource(InventoryService.ResourceKind.Money) < def.BuyPrice ? BuyBlock.InsufficientMoney
                    : canReceive ? BuyBlock.None : BuyBlock.CannotReceive;
                bool canSell = def.SellPrice > 0 && count > 0 && !RunInventory.IsImportant(def.Index)
                    && inv.ResourceRoom(InventoryService.ResourceKind.Money) >= def.SellPrice;
                rows.Add(new MarketOffer { Id = def.Index, NameKey = def.NameKey, BuyPrice = def.BuyPrice,
                    SellPrice = def.SellPrice, Owned = count, BlockReason = reason, BuyBlock = block, CanSell = canSell });
            }
            rows.Sort((a, b) => a.Id.CompareTo(b.Id));
            return rows.ToArray();
        }
        public bool Buy(uint id, out string reason) => MarketService.Instance.TryBuy(id, out reason);
        public bool Sell(uint id, out string reason) => MarketService.Instance.TrySell(id, out reason);
    }
}
