using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 市场面板（W5.3 / M5）—— **资源 ↔ 物品兑换最小版**。
///
/// ══════════════ 布局（与 `RunSettlementPanel` / `InventoryPanel` 同一套坑的解法）══════════════
/// * prefab 自带的 `Title` 在左上角（会被 GM 悬浮按钮压住）→ **运行时隐藏**，改用内容区里居中的标题；
/// * 全部内容放进**居中 800×600 的 `ContentBox`**：标题 / 钱财余额 / 结果行 / 列表 / 关闭按钮；
/// * 文案**纯中文、不挂 `LocalizedText`**（与族人/库存/结算面板一致），杜绝 key 被回填成英文；
/// * 中文字体走 `RunTeamHud.ResolveFont()`（**公共静态方法，禁止再复制第二份取字体逻辑**）；
/// * 行 = 名称 + 买价/卖价 + 持有数 + 「买」「卖」两个按钮 + 行尾原因。
///
/// ══════════════ 行为（严格按 18_市场.md）══════════════
/// * 每次成交 **1 个**；货币只认钱财；两列都 0 的物品**不显示**（E5）；
/// * 按钮 `interactable=false` 时行尾**必须给原因**（钱财不足 / 格位已满 / 已达叠加上限 / 持有 0）；
/// * 买卖全部走 `MarketService`（买失败退钱也在那一层保证），本类**只负责显示与转发**。
/// </summary>
public class MarketPanel : BasePanel
{
    private const float PanelWidth = 800f;
    private const float PanelHeight = 600f;
    private const float RowStep = 36f;
    private const float RowHeight = 32f;

    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);
    private static readonly Color OkColor = new Color(0.45f, 0.90f, 0.55f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _money;
    private TextMeshProUGUI _result;
    private RectTransform _listArea;
    private TMP_FontAsset _font;
    private bool _built;

    /// <summary>GM / 其它面板统一入口：Push 之后**显式刷新**（面板已缓存时 OnEnter 不会重跑）。</summary>
    public static void Open()
    {
        if (UIManager.Instance == null)
        {
            ChaosLog.Error(LogChannel.UI, "[市场] UIManager 还没初始化，打不开市场面板");
            return;
        }

        UIManager.Instance.PushPanel(PanelType.MarketPanel);

        MarketPanel panel = Object.FindFirstObjectByType<MarketPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            panel.RefreshFromOpen();
        }

        ChaosLog.Info(LogChannel.UI, "[市场] 面板已打开（钱财 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money)
            + "，库存格位 " + InventoryService.Instance.UsedSlots + "/" + InventoryService.Instance.TotalSlots + "）");
    }

    public void RefreshFromOpen()
    {
        BuildUi();
        Refresh();
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    // ══════════════════════ UI 构建（居中 800×600）══════════════════════

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null)
        {
            _font = _title.font;
            //prefab 自带的 Title 在左上角（会被 GM 悬浮按钮压住）→ 隐藏，改用内容区里的居中标题
            _title.gameObject.SetActive(false);
        }
        if (_font == null) _font = RunTeamHud.ResolveFont();
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        //居中内容区
        GameObject box = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        RectTransform boxRect = (RectTransform)box.transform;
        boxRect.SetParent(transform, false);
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        bg.raycastTarget = true;

        //标题（纯中文，不挂 LocalizedText）
        _title = MakeText(boxRect, "Title", "市场", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -66f);
        titleRect.offsetMax = new Vector2(0f, -12f);

        //顶部：钱财余额 + 格位
        _money = MakeText(boxRect, "Money", "", 22f, new Color(1f, 0.86f, 0.45f, 1f));
        RectTransform moneyRect = (RectTransform)_money.transform;
        moneyRect.anchorMin = new Vector2(0f, 1f);
        moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.pivot = new Vector2(0.5f, 1f);
        moneyRect.offsetMin = new Vector2(24f, -108f);
        moneyRect.offsetMax = new Vector2(-24f, -70f);

        //结果行（买/卖结果 + 失败原因）
        _result = MakeText(boxRect, "Result", "", 18f, DimColor);
        RectTransform resultRect = (RectTransform)_result.transform;
        resultRect.anchorMin = new Vector2(0f, 1f);
        resultRect.anchorMax = new Vector2(1f, 1f);
        resultRect.pivot = new Vector2(0.5f, 1f);
        resultRect.offsetMin = new Vector2(24f, -146f);
        resultRect.offsetMax = new Vector2(-24f, -110f);
        _result.alignment = TextAlignmentOptions.Left;

        //列表区（中部，带遮罩裁剪）
        GameObject listGo = new GameObject("ListArea", typeof(RectTransform), typeof(RectMask2D));
        _listArea = (RectTransform)listGo.transform;
        _listArea.SetParent(boxRect, false);
        _listArea.anchorMin = new Vector2(0f, 0f);
        _listArea.anchorMax = new Vector2(1f, 1f);
        _listArea.offsetMin = new Vector2(24f, 84f);
        _listArea.offsetMax = new Vector2(-24f, -150f);

        //底部：关闭
        MakeButton(boxRect, "Btn_Close", "关闭", 22f,
            new Vector2(0f, 16f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(180f, 48f),
            Close);
    }

    // ══════════════════════ 刷新 ══════════════════════

    private void Refresh()
    {
        InventoryService inv = InventoryService.Instance;

        if (_money != null)
            _money.text = "钱财：" + inv.GetResource(InventoryService.ResourceKind.Money)
                + "　　格位：" + inv.UsedSlots + "/" + inv.TotalSlots;

        ClearList();

        List<ItemConfig> rows = CollectTradable();
        if (_result != null && string.IsNullOrEmpty(_result.text))
        {
            _result.color = DimColor;
            _result.text = rows.Count == 0
                ? "暂无可交易物品（Item 表 BuyPrice/SellPrice 都为 0）"
                : "每次买 / 卖各 1 个；货币只认钱财。价格来自 Item 表。";
        }

        float y = -2f;
        for (int i = 0; i < rows.Count; i++)
        {
            MakeRow(inv, rows[i], y);
            y -= RowStep;
        }
    }

    /// <summary>可交易 = 启用 且（买价 &gt; 0 或 卖价 &gt; 0）；按 `Item.Index` 升序（稳定、可预期）。</summary>
    private static List<ItemConfig> CollectTradable()
    {
        List<ItemConfig> all = InventoryService.Instance.GetAllDefinitions();
        var rows = new List<ItemConfig>();
        for (int i = 0; i < all.Count; i++)
        {
            ItemConfig def = all[i];
            if (def == null || !def.Enable) continue;
            if (def.BuyPrice == 0u && def.SellPrice == 0u) continue;
            rows.Add(def);
        }
        rows.Sort((a, b) => a.Index.CompareTo(b.Index));
        return rows;
    }

    private void MakeRow(InventoryService inv, ItemConfig def, float y)
    {
        if (_listArea == null || def == null) return;

        GameObject rowGo = new GameObject("Row_" + def.Index, typeof(RectTransform));
        RectTransform row = (RectTransform)rowGo.transform;
        row.SetParent(_listArea, false);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.offsetMin = new Vector2(0f, y - RowHeight);
        row.offsetMax = new Vector2(0f, y);

        //① 名称
        TextMeshProUGUI name = MakeChildText(row, "Name", MarketService.Instance.GetDisplayName(def.Index), 20f, NormalColor);
        SetLeftRect((RectTransform)name.transform, 4f, 146f, RowHeight);

        //② 买价 / 卖价
        TextMeshProUGUI price = MakeChildText(row, "Price", "买 " + def.BuyPrice + " / 卖 " + def.SellPrice, 18f,
            def.BuyPrice > 0u ? NormalColor : DimColor);
        SetLeftRect((RectTransform)price.transform, 154f, 166f, RowHeight);

        //③ 持有数
        uint count = MarketService.Instance.GetCount(def.Index);
        TextMeshProUGUI hold = MakeChildText(row, "Hold", "持有 " + count, 18f,
            count > 0u ? NormalColor : DimColor);
        SetLeftRect((RectTransform)hold.transform, 324f, 80f, RowHeight);

        //④ 买 / 卖（灰显时行尾给原因）
        bool canBuy = CanBuy(inv, def);
        bool canSell = CanSell(def, count);

        Button buyButton = MakeButton(row, "Btn_Buy", "买", 18f,
            new Vector2(-100f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(84f, RowHeight - 2f), () => DoBuy(def.Index));
        buyButton.interactable = canBuy;

        Button sellButton = MakeButton(row, "Btn_Sell", "卖", 18f,
            new Vector2(-8f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(84f, RowHeight - 2f), () => DoSell(def.Index));
        sellButton.interactable = canSell;

        string why = !canBuy ? BuyBlockReason(inv, def)
                   : (!canSell ? (def.SellPrice == 0u ? "不可卖" : "无可卖") : "");
        TextMeshProUGUI reason = MakeChildText(row, "Reason", why, 13f, WarnColor);
        SetLeftRect((RectTransform)reason.transform, 408f, 156f, RowHeight);
    }

    // ══════════════════════ 可买 / 可卖判定（与 MarketService 的失败原因对齐）══════════════════════

    private static bool CanBuy(InventoryService inv, ItemConfig def)
    {
        if (def.BuyPrice == 0u) return false;
        if (inv.GetResource(InventoryService.ResourceKind.Money) < def.BuyPrice) return false;

        uint stackLimit = def.StackLimit == 0u ? 1u : def.StackLimit;
        return InventoryRules.MaxAddableStacks(inv.GetStacks(), inv.TotalSlots, def.Index, 1u, stackLimit) >= 1u;
    }

    private static bool CanSell(ItemConfig def, uint count)
    {
        return def.SellPrice > 0u && count > 0u;
    }

    /// <summary>买按钮为什么灰：优先给「钱财不足」，再给格位/叠加（与 §5.1 的状态表一致）。</summary>
    private static string BuyBlockReason(InventoryService inv, ItemConfig def)
    {
        if (def.BuyPrice == 0u) return "不可买";
        if (inv.GetResource(InventoryService.ResourceKind.Money) < def.BuyPrice) return "钱财不足";

        uint stackLimit = def.StackLimit == 0u ? 1u : def.StackLimit;
        if (InventoryRules.MaxAddableStacks(inv.GetStacks(), inv.TotalSlots, def.Index, 1u, stackLimit) >= 1u)
            return "";

        uint count = MarketService.Instance.GetCount(def.Index);
        if (count == 0u && inv.UsedSlots >= inv.TotalSlots)
            return "格位已满（" + inv.UsedSlots + "/" + inv.TotalSlots + "）";
        return "已达叠加上限（" + stackLimit + "）";
    }

    // ══════════════════════ 点按 ══════════════════════

    private void DoBuy(uint itemId)
    {
        string reason;
        bool ok = MarketService.Instance.TryBuy(itemId, out reason);
        ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        SetResult(ok
                ? ("买入成功：" + MarketService.Instance.GetDisplayName(itemId) + " ×1（-" + (def == null ? 0u : def.BuyPrice) + " 钱财）")
                : ("买入失败：" + reason),
            ok ? OkColor : WarnColor);
        Refresh();
    }

    private void DoSell(uint itemId)
    {
        string reason;
        bool ok = MarketService.Instance.TrySell(itemId, out reason);
        ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        SetResult(ok
                ? ("卖出成功：" + MarketService.Instance.GetDisplayName(itemId) + " ×1（+" + (def == null ? 0u : def.SellPrice) + " 钱财）")
                : ("卖出失败：" + reason),
            ok ? OkColor : WarnColor);
        Refresh();
    }

    private void SetResult(string text, Color color)
    {
        if (_result == null) return;
        _result.text = text;
        _result.color = color;
        ChaosLog.Info(LogChannel.UI, "[市场] 面板结果：" + text);
    }

    /// <summary>
    /// 关闭：走 `UIManager.PopPanel()` 而不是直接 `Hide()`。
    /// 直接 Hide 会把面板留在 `panelStack` 里且 `IsShowing=false`，之后再 `PushPanel` 会命中
    /// “重复打开栈顶面板”的分支直接 return → 面板永远打不开（库存面板就是这么踩过的）。
    /// </summary>
    private void Close()
    {
        if (uiManager != null) uiManager.PopPanel();
        else Hide();
    }

    // ══════════════════════ 通用小工具（纯中文，不挂 LocalizedText）══════════════════════

    private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private TextMeshProUGUI MakeChildText(RectTransform parent, string name, string text, float size, Color color)
    {
        TextMeshProUGUI label = MakeText(parent, name, text, size, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.pivot = new Vector2(0f, 0.5f);
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        return label;
    }

    /// <summary>左侧固定宽度的列（anchorX=0，纵向居中）。</summary>
    private static void SetLeftRect(RectTransform rect, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, height);
    }

    private Button MakeButton(Transform parent, string name, string text, float fontSize,
        Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.20f, 0.42f, 0.72f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
        colors.disabledColor = new Color(0.55f, 0.57f, 0.62f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", text, fontSize, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void ClearList()
    {
        if (_listArea == null) return;
        for (int i = _listArea.childCount - 1; i >= 0; i--)
        {
            Transform child = _listArea.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }
}
