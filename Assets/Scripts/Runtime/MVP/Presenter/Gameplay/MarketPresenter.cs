using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class MarketRow
    {
        public uint Id;
        public string Name, Price, Owned, Reason;
        public bool CanBuy, CanSell, HasPrice, HasOwned;
    }
    public sealed class MarketState
    {
        public string Money, Hint;
        public IReadOnlyList<MarketRow> Rows;
    }
    public sealed class MarketPresenter : GameplayPresenter<MarketState>
    {
        private readonly MarketModel _model;
        public MarketPresenter(MarketModel model) { _model = model; }
        protected override MarketState CreateState()
        {
            var inv = InventoryService.Instance;
            var rows = new List<MarketRow>();
            foreach (var offer in _model.GetOffers())
            {
                string why;
                switch (offer.BuyBlock)
                {
                    case BuyBlock.Unavailable: why = GameplayText.Text("ui_market_cannot_buy", "不可买"); break;
                    case BuyBlock.InsufficientMoney: why = GameplayText.Text("ui_market_insufficient_money", "钱财不足"); break;
                    case BuyBlock.SlotsFull: why = GameplayText.Text("ui_market_slots_full", "格位已满（{0}/{1}）", inv.UsedSlots, inv.TotalSlots); break;
                    case BuyBlock.StackFull: why = GameplayText.Text("ui_market_stack_full", "已达叠加上限（{0}）", offer.StackLimit); break;
                    default: why = offer.CanSell ? "" : offer.SellPrice == 0 ? GameplayText.Text("ui_market_cannot_sell", "不可卖") : GameplayText.Text("ui_market_nothing_to_sell", "无可卖"); break;
                }
                rows.Add(new MarketRow { Id = offer.Id, Name = GameplayText.Text(offer.NameKey, offer.NameKey),
                    Price = GameplayText.Text("ui_market_price", "买 {0} / 卖 {1}", offer.BuyPrice, offer.SellPrice),
                    Owned = GameplayText.Text("ui_market_owned", "持有 {0}", offer.Owned), Reason = why,
                    CanBuy = offer.BuyBlock == BuyBlock.None, CanSell = offer.CanSell, HasPrice = offer.BuyPrice > 0, HasOwned = offer.Owned > 0 });
            }
            return new MarketState { Rows = rows.ToArray(),
                Money = GameplayText.Text("ui_market_balance", "钱财：{0}　　格位：{1}/{2}", inv.GetResource(InventoryService.ResourceKind.Money), inv.UsedSlots, inv.TotalSlots),
                Hint = rows.Count == 0 ? GameplayText.Text("ui_market_empty", "暂无可交易物品") : GameplayText.Text("ui_market_hint", "每次买 / 卖各 1 个，使用钱财交易。") };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action != PanelAction.Buy && intent.Action != PanelAction.Sell) return;
            bool buy = intent.Action == PanelAction.Buy;
            string reason;
            bool ok = buy ? _model.Buy(intent.Id, out reason) : _model.Sell(intent.Id, out reason);
            var def = InventoryService.Instance.GetDefinition(intent.Id);
            Refresh();
            Message(ok ? (buy ? GameplayText.Text("ui_market_bought", "买入成功：{0} ×1（-{1} 钱财）", GameplayText.ItemName(intent.Id), def == null ? 0u : def.BuyPrice)
                : GameplayText.Text("ui_market_sold", "卖出成功：{0} ×1（+{1} 钱财）", GameplayText.ItemName(intent.Id), def == null ? 0u : def.SellPrice))
                : buy ? GameplayText.Text("ui_market_buy_failed", "买入失败：{0}", reason) : GameplayText.Text("ui_market_sell_failed", "卖出失败：{0}", reason), ok);
        }
    }
}
