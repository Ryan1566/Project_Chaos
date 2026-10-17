using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;

/// <summary>
/// 库存面板（W1a）：顶部 4 资源数字 + 格子列表（**色块 + 名称首字 + 数量/上限**）
/// + `已用格位 X/Y` + 满格/叠加满提示行。
/// `IconKey` 列保留但**不渲染图标**（W1a 用色块占位，见 Lead 裁决）。
/// </summary>
public class InventoryPanel : BasePanel
{
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);

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

        _resources = CreateText("Resources", 28f, NormalColor, 22f);
        _slots = CreateText("Slots", 64f, NormalColor, 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 92f, WarnColor, 20f);
    }

    private void Refresh()
    {
        if (_title != null) _title.text = "INVENTORY";

        InventoryService svc = InventoryService.Instance;
        List<ItemStackSave> stacks = svc.GetStacks();

        if (_resources != null)
            _resources.text = "GOODS " + svc.GetResource(InventoryService.ResourceKind.Goods)
                + "    MAT " + svc.GetResource(InventoryService.ResourceKind.Material)
                + "    BP " + svc.GetResource(InventoryService.ResourceKind.Blueprint)
                + "    MONEY " + svc.GetResource(InventoryService.ResourceKind.Money);

        if (_slots != null)
        {
            int used = svc.UsedSlots;
            int total = svc.TotalSlots;
            _slots.text = "SLOTS " + used + "/" + total + "    (base " + InventoryService.BaseSlots + " + effect " + svc.SlotBonus + ")";
            _slots.color = used >= total ? WarnColor : NormalColor;
        }

        if (_hint != null)
        {
            int used = svc.UsedSlots;
            int total = svc.TotalSlots;
            if (used >= total) _hint.text = "SLOTS FULL (" + used + "/" + total + ") - new kinds rejected; existing kinds can still stack";
            else if (used == 0) _hint.text = "(empty - GM > Items > grant items)";
            else _hint.text = "";
        }

        if (_content != null)
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStackSave stack = stacks[i];
            if (stack == null || stack.count == 0) continue;

            ItemConfig def = svc.GetDefinition(stack.itemId);
            string name = def == null ? ("itemId=" + stack.itemId) : def.NameKey;
            string firstChar = name.Length > 0 ? name.Substring(0, 1) : "?";
            uint limit = def == null ? 0u : def.StackLimit;

            AddRow(i, name, firstChar, stack.count + "/" + limit,
                CategoryColor(def == null ? 0u : def.Category));
        }
    }

    private void AddRow(int index, string name, string firstChar, string countText, Color color)
    {
        if (_content == null) return;

        float y = -8f - index * 44f;

        //色块（用 10x10 纯色方块的替身：一个窄 TextMesh 背景块不便 → 用 Image）
        GameObject block = new GameObject("Block" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        RectTransform blockRect = (RectTransform)block.transform;
        blockRect.SetParent(_content, false);
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(0f, 1f);
        blockRect.pivot = new Vector2(0f, 1f);
        blockRect.anchoredPosition = new Vector2(24f, y);
        blockRect.sizeDelta = new Vector2(36f, 36f);
        block.GetComponent<UnityEngine.UI.Image>().color = color;
        block.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

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
        label.text = firstChar + "  " + name + "  " + countText;
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
        rect.offsetMin = new Vector2(0f, 120f);
        rect.offsetMax = new Vector2(0f, -150f);
        return rect;
    }
}
