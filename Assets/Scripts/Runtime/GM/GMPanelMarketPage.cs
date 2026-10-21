using ChaosDebug;
using TMPro;
using UnityEngine;

/// <summary>
/// GM 面板「道具 → **市场**」页（W5.3 / M5）。
///
/// 本文件是 `GMPanel` 的另一半（`partial`）：`BuildCatalog()` 里挂一项
/// `new GmSection("市场", BuildMarketPage)`，页面控件复用主文件的
/// `AddHint` / `AddActionButton` / `AddPanelButton` / `CreateStatusLabel`，所以这里只放这一页自己的逻辑。
///
/// 三个按钮（照策划 `18_市场.md` §11 的 GM 演示路径）：
/// 1. **打开市场面板** —— `MarketPanel.Open()`（走 `PushPanel`；GM 面板会自动收掉，免得两层 UI 叠着）；
/// 2. **打印价格表** —— `MarketService.DumpPrices()`：逐行「名称 买价 卖价 持有」+「卖价=买价50%」核对；
/// 3. **+500 钱财** —— 直接 `AddResource(Money, 500)`（不写存档），方便反复买到格位满来验 E2（失败退钱）。
/// </summary>
public partial class GMPanel
{
    /// <summary>本页状态行（可变，反复改写）。</summary>
    private TextMeshProUGUI _marketStatus;

    /// <summary>构建「市场」页（首次切到该版块时由 `GMPanel.CreatePage` 调一次）。</summary>
    private void BuildMarketPage(Transform page)
    {
        AddHint(page, "W5.3（M5）：市场只做“资源 ↔ 物品兑换”——每次买 / 卖各 1 个，货币只认钱财；"
            + "价格全部来自 Item 表的 BuyPrice / SellPrice（0 = 不可交易）。买入失败（格位满 / 叠加满）会退还钱财。");

        //不用 AddPanelButton：它只做 PushPanel，而面板已在栈顶时 PushPanel 会提前 return（不刷新），
        //于是"关掉 GM 再打开市场"会看到旧的钱财/持有。MarketPanel.Open() 里带了一次显式刷新。
        AddActionButton(page, "打开市场面板", () =>
        {
            MarketPanel.Open();
            ClosePanel();   //常规面板盖上来之后 GM 面板就没用了，收掉免得两层 UI 压在一起
        });

        AddActionButton(page, "打印价格表", () =>
        {
            string text = MarketService.Instance.DumpPrices();
            Debug.Log(text);
            SetMarketStatus(text);
        });

        AddActionButton(page, "+500 钱财", () =>
        {
            uint now = InventoryService.Instance.AddResource(InventoryService.ResourceKind.Money, 500u);
            string text = "[市场] 已 +500 钱财 → 当前钱财 " + now
                + "（局外库存格位 " + InventoryService.Instance.UsedSlots + "/" + InventoryService.Instance.TotalSlots + "）";
            Debug.Log(text);
            SetMarketStatus(text);
        });

        _marketStatus = CreateStatusLabel(page);
        SetMarketStatus("[市场] 当前钱财 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money)
            + "｜库存格位 " + InventoryService.Instance.UsedSlots + "/" + InventoryService.Instance.TotalSlots
            + "\n点【打印价格表】可核对 Index / BuyPrice / SellPrice 与“卖价 = 买价 50%”。");
    }

    private void SetMarketStatus(string text)
    {
        if (_marketStatus != null) _marketStatus.text = text;
        ChaosLog.Info(LogChannel.UI, "[GM/市场] " + text);
    }
}
