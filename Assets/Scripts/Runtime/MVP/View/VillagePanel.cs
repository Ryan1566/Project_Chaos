using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 村庄面板（W1b）：4 个建筑格（名称/等级/状态/倒计时/按钮）+ 顶部资源栏 + 取消返还提示。
/// 按钮按状态置灰（未建造→建造；建造中→取消；已建成→升级/收取；满级→只收取）。
/// </summary>
public class VillagePanel : BasePanel
{
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.76f, 0.82f, 1f);
    private static readonly Color WarnColor = new Color(1.00f, 0.72f, 0.25f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _resources;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;
        _title = FindChildText("Title");
        if (_title != null) _font = _title.font;
        _resources = CreateText("Resources", 64f, NormalColor, 22f);
        _content = CreateRect("Content", 110f);
        _hint = CreateText("Hint", 96f, WarnColor, 20f);
        CreateCloseButton();
    }

    private void Refresh()
    {
        if (_title != null) ApplyLocalized(_title, "ui_village_title", "村庄");

        VillageService svc = VillageService.Instance;
        svc.EnsureLoaded();

        if (_resources != null)
            ApplyLocalized(_resources, "ui_village_resources",
                "物资 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Goods)
                + "　　材料 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Material)
                + "　　图纸 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Blueprint)
                + "　　钱财 " + InventoryService.Instance.GetResource(InventoryService.ResourceKind.Money));

        if (_hint != null)
            ApplyLocalized(_hint, "ui_village_hint",
                svc.IsQueueBusy ? "建造队列忙（同时只能建/升 1 个）——取消建造返还 50%" : "");

        ClearRows();

        uint[] ids = { 2001u, 2002u, 2003u, 2004u };
        for (int i = 0; i < ids.Length; i++) AddBuildingRow(i, ids[i]);
    }

    private void AddBuildingRow(int index, uint buildingId)
    {
        if (_content == null) return;
        VillageService svc = VillageService.Instance;
        BuildingSave save = svc.Find(buildingId);
        VillageService.BuildingState state = svc.GetState(buildingId);
        BuildingConfig def = svc.GetDef(buildingId, save == null ? 1u : save.level);

        string name = def == null ? ("建筑" + buildingId) : def.NameKey;
        string stateText;
        switch (state)
        {
            case VillageService.BuildingState.NotBuilt: stateText = "未建造"; break;
            case VillageService.BuildingState.Building: stateText = "建造中 剩 " + save.remainSeconds + "s"; break;
            case VillageService.BuildingState.Built: stateText = "已建成 Lv" + save.level; break;
            case VillageService.BuildingState.Upgradable: stateText = "已建成 Lv" + save.level + "（可升级）"; break;
            default: stateText = "已满级 Lv" + save.level; break;
        }

        float y = -8f - index * 76f;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, y);
        rect.sizeDelta = new Vector2(-48f, 30f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = state == VillageService.BuildingState.Building ? WarnColor : NormalColor;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        label.text = name + "　" + stateText;

        //主按钮
        string actionLabel;
        bool actionEnabled = true;
        System.Action action;
        switch (state)
        {
            case VillageService.BuildingState.NotBuilt:
                actionLabel = "建造"; action = () => { string why; VillageService.Instance.StartBuild(buildingId, out why); LogWhy("建造", why); Refresh(); };
                break;
            case VillageService.BuildingState.Building:
                actionLabel = "取消（返 50%）"; action = () => { uint refund; string why; VillageService.Instance.CancelBuild(buildingId, out refund, out why); Debug.Log("[村庄] 取消 " + buildingId + "，返还 " + refund); Refresh(); };
                break;
            case VillageService.BuildingState.Upgradable:
                actionLabel = "升级"; action = () => { string why; VillageService.Instance.StartUpgrade(buildingId, out why); LogWhy("升级", why); Refresh(); };
                break;
            case VillageService.BuildingState.MaxLevel:
                actionLabel = "收取"; action = () => { uint amt; string why; VillageService.Instance.Harvest(buildingId, out amt, out why); Debug.Log("[村庄] 收取 " + buildingId + " → " + amt); Refresh(); };
                break;
            default:
                actionLabel = "收取"; action = () => { uint amt; string why; VillageService.Instance.Harvest(buildingId, out amt, out why); Debug.Log("[村庄] 收取 " + buildingId + " → " + amt); Refresh(); };
                break;
        }
        CreateButton("Btn_" + buildingId, actionLabel, new Vector2(0f, -34f), new Vector2(180f, 30f), actionEnabled, action, rect);
    }

    private static void LogWhy(string what, string why)
    {
        if (!string.IsNullOrEmpty(why)) Debug.Log("[村庄] " + what + "失败：" + why);
        else Debug.Log("[村庄] " + what + "成功");
    }

    private TextMeshProUGUI FindChildText(string n) { Transform c = transform.Find(n); return c == null ? null : c.GetComponent<TextMeshProUGUI>(); }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 28f);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft; label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name, float top)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 40f); rect.offsetMax = new Vector2(0f, -top);
        return rect;
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        { Transform c = _content.GetChild(i); c.SetParent(null, false); Destroy(c.gameObject); }
    }

    private Button CreateButton(string name, string label, Vector2 offset, Vector2 size, bool interactable, System.Action onClick, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, offset.y);
        rect.sizeDelta = size;
        Image bg = go.GetComponent<Image>();
        bg.color = interactable ? new Color(0.20f, 0.42f, 0.72f, 1f) : new Color(0.22f, 0.24f, 0.28f, 1f);
        Button button = go.GetComponent<Button>();
        button.interactable = interactable;
        if (interactable && onClick != null) button.onClick.AddListener(() => onClick());

        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 20f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        tmp.text = label;
        return button;
    }

    private void CreateCloseButton()
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 48f);
        go.GetComponent<Image>().color = new Color(0.18f, 0.21f, 0.26f, 1f);
        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 22f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        ApplyLocalized(tmp, "ui_common_close", "关闭");
        go.GetComponent<Button>().onClick.AddListener(() => Hide());
    }

    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = fallback;
        LocalizedText loc = label.GetComponent<LocalizedText>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedText>();
        loc.localizationKey = key; loc.autoUpdateOnStart = false; loc.listenToLanguageChange = false;
    }
}
