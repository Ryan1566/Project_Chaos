using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 库存面板（W1a）：顶部 4 资源数字 + 格子列表（**色块 + 名称首字 + 数量/上限**）
/// + `格位 N/M` + 满格/叠加满提示行 + 关闭按钮。
/// `IconKey` 列保留但**不渲染图标**（W1a 用色块占位，见 Lead 裁决）。
/// </summary>
public class InventoryPanel : BasePanel
{
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.76f, 0.82f, 1f);

    /// <summary>类别 → 占位色（1 药品 / 2 材料 / 3 装备 / 4 图纸 / 5 消耗 / 6 其它）。</summary>
    private static Color CategoryColor(uint category)
    {
        switch (category)
        {
            case 1: return new Color(0.85f, 0.35f, 0.40f, 1f);
            case 2: return new Color(0.55f, 0.62f, 0.45f, 1f);
            case 3: return new Color(0.40f, 0.55f, 0.80f, 1f);
            case 4: return new Color(0.70f, 0.55f, 0.85f, 1f);
            case 5: return new Color(0.90f, 0.70f, 0.35f, 1f);
            default: return new Color(0.60f, 0.62f, 0.66f, 1f);
        }
    }

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _resources;
    private TextMeshProUGUI _slots;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;

    private void Awake()
    {
        BuildUi();
    }

    public override void OnEnter()
    {
        BuildUi();
        Refresh();
    }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null) _font = _title.font;

        _resources = CreateText("Resources", 64f, NormalColor, 22f);
        _slots = CreateText("Slots", 100f, NormalColor, 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 128f, WarnColor, 20f);

        CreateCloseButton();
    }

    private void Refresh()
    {
        if (_title != null) ApplyLocalized(_title, "ui_inventory_panel_title", "库存");

        InventoryService svc = InventoryService.Instance;
        List<ItemStackSave> stacks = svc.GetStacks();

        if (_resources != null)
            ApplyLocalized(_resources, "ui_inventory_resources",
                "物资 " + svc.GetResource(InventoryService.ResourceKind.Goods)
                + "　　材料 " + svc.GetResource(InventoryService.ResourceKind.Material)
                + "　　图纸 " + svc.GetResource(InventoryService.ResourceKind.Blueprint)
                + "　　钱财 " + svc.GetResource(InventoryService.ResourceKind.Money));

        int used = svc.UsedSlots;
        int total = svc.TotalSlots;

        if (_slots != null)
        {
            ApplyLocalized(_slots, "ui_inventory_slots",
                "格位 " + used + "/" + total + "　（基础 " + InventoryService.BaseSlots + " + 效果 " + svc.SlotBonus + "）");
            _slots.color = used >= total ? WarnColor : NormalColor;
        }

        if (_hint != null)
        {
            if (used >= total) ApplyLocalized(_hint, "ui_inventory_full", "格位已满（" + used + "/" + total + "）—— 新种类无法入库，已有种类仍可叠到上限");
            else if (used == 0) ApplyLocalized(_hint, "ui_inventory_empty", "暂无库存（用 GM 发放道具）");
            else ApplyLocalized(_hint, "ui_inventory_hint", "");
        }

        ClearRows();

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStackSave stack = stacks[i];
            if (stack == null || stack.count == 0) continue;

            ItemConfig def = svc.GetDefinition(stack.itemId);
            string name = def == null ? ("道具#" + stack.itemId) : def.NameKey;
            string firstChar = name.Length > 0 ? name.Substring(0, 1) : "?";
            uint limit = def == null ? 0u : def.StackLimit;

            AddRow(i, firstChar, name, stack.count + "/" + limit, CategoryColor(def == null ? 0u : def.Category));
        }
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Transform child = _content.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void AddRow(int index, string firstChar, string name, string countText, Color color)
    {
        if (_content == null) return;

        float y = -8f - index * 46f;

        GameObject block = new GameObject("Block" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform blockRect = (RectTransform)block.transform;
        blockRect.SetParent(_content, false);
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(0f, 1f);
        blockRect.pivot = new Vector2(0f, 1f);
        blockRect.anchoredPosition = new Vector2(24f, y);
        blockRect.sizeDelta = new Vector2(36f, 36f);
        block.GetComponent<Image>().color = color;
        block.GetComponent<Image>().raycastTarget = false;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(72f, y);
        rect.sizeDelta = new Vector2(-96f, 36f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = NormalColor;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        // 格子内容：首字 + 名称 + 数量/上限（如 12/99）
        label.text = firstChar + "　" + name + "　" + countText;
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 28f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 40f);
        rect.offsetMax = new Vector2(0f, -210f);
        return rect;
    }

    /// <summary>右下角「关闭」按钮。</summary>
    private void CreateCloseButton()
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 48f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.21f, 0.26f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = NormalColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        ApplyLocalized(label, "ui_common_close", "关闭");

        go.GetComponent<Button>().onClick.AddListener(() => Hide());
    }

    /// <summary>挂 LocalizedText（key 先给字符串，中文兜底；不自动刷新，避免把兜底中文冲掉）。</summary>
    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = fallback;

        LocalizedText loc = label.GetComponent<LocalizedText>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedText>();
        loc.localizationKey = key;
        loc.autoUpdateOnStart = false;
        loc.listenToLanguageChange = false;
    }
}

