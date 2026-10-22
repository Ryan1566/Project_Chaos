using System.Collections.Generic;

namespace Chaos.MVP.Model.Gameplay
{
    public enum BuyBlock { None, Unavailable, InsufficientMoney, SlotsFull, StackFull }
    public sealed class MarketOffer
    {
        public uint Id, BuyPrice, SellPrice, Owned, StackLimit;
        public string NameKey;
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
                uint limit = def.StackLimit == 0 ? 1u : def.StackLimit;
                BuyBlock block = def.BuyPrice == 0 ? BuyBlock.Unavailable
                    : inv.GetResource(InventoryService.ResourceKind.Money) < def.BuyPrice ? BuyBlock.InsufficientMoney
                    : InventoryRules.MaxAddableStacks(inv.GetStacks(), inv.TotalSlots, def.Index, 1u, limit) >= 1 ? BuyBlock.None
                    : count == 0 && inv.UsedSlots >= inv.TotalSlots ? BuyBlock.SlotsFull : BuyBlock.StackFull;
                rows.Add(new MarketOffer { Id = def.Index, NameKey = def.NameKey, BuyPrice = def.BuyPrice,
                    SellPrice = def.SellPrice, Owned = count, StackLimit = limit, BuyBlock = block, CanSell = def.SellPrice > 0 && count > 0 });
            }
            rows.Sort((a, b) => a.Id.CompareTo(b.Id));
            return rows.ToArray();
        }
        public bool Buy(uint id, out string reason) => MarketService.Instance.TryBuy(id, out reason);
        public bool Sell(uint id, out string reason) => MarketService.Instance.TrySell(id, out reason);
    }
}
