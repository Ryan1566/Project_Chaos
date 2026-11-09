using System;
using System.Collections.Generic;
using Chaos.MVP.Presenter.Gameplay;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>展示建筑总览、单体详情与施工队列，实际操作由 Presenter 提交。</summary>
public class VillagePanel : BasePanel, IGameplayView<VillageState>
{
    [SerializeField] private Sprite buildableCard;
    [SerializeField] private Sprite constructionCard;
    [SerializeField] private Sprite builtCard;

    private static readonly Color Paper = new Color(.96f, .91f, .80f);
    private static readonly Color Muted = new Color(.77f, .73f, .63f);
    private static readonly Color Gold = new Color(.94f, .76f, .44f);
    private static readonly Color Warning = new Color(1f, .66f, .43f);
    private static readonly Color Ink = new Color(.11f, .10f, .09f);
    private static readonly Color Block = new Color(.18f, .16f, .135f);
    private static readonly Color Line = new Color(.37f, .31f, .22f);
    private static readonly Color Green = new Color(.68f, .82f, .62f);
    private static readonly Vector2 LayoutSize = new Vector2(1200, 720);

    private sealed class BuildingRow
    {
        public uint Id;
        public Image Surface, Selection;
        public TextMeshProUGUI Badge, Name, Status;
    }

    private readonly List<BuildingRow> rows = new List<BuildingRow>();
    private readonly TextMeshProUGUI[] resources = new TextMeshProUGUI[4];
    private VillagePresenter presenter;
    private VillageBuildingCard selected;
    private RectTransform layout, listContent, details, cancelDialog;
    private RectTransform workTrack, workFill, queueFill;
    private ScrollRect buildingScroll;
    private Image hero;
    private Button mainAction, collect;
    private TextMeshProUGUI mainActionText;
    private TextMeshProUGUI scope, listTitle, focusNote, noSelection, message, glyph, buildingName, status;
    private TextMeshProUGUI featureTitle, featureValue, featureDetail, requirement, actionHint, queueStatus;
    private float nextRefresh;

    public event Action<PanelIntent> Intent;

    public static void OpenForBuilding(uint id)
    {
        if (UIManager.Instance.PushPanel(PanelType.VillagePanel) is VillagePanel panel)
            panel.presenter?.Focus(id);
    }

    public override void OnEnter()
    {
        BuildUi();
        nextRefresh = Time.unscaledTime + .35f;
        ShowMessage(string.Empty, true);
        if (presenter == null || presenter.IsDisposed)
        {
            presenter = new VillagePresenter();
            presenter.Attach(this);
        }
        else presenter.Refresh();
    }

    public override void OnExit()
    {
        DismissCancel();
        presenter?.Dispose();
        presenter = null;
    }

    private void OnDestroy() => OnExit();

    private void Update()
    {
        if (!IsShowing || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .35f;
        presenter?.Refresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (layout == null) return;
        Vector2 size = ((RectTransform)transform).rect.size;
        float scale = Mathf.Min(1.12f, (size.x - 40) / LayoutSize.x, (size.y - 36) / LayoutSize.y);
        layout.localScale = Vector3.one * Mathf.Max(.01f, scale);
    }

    public override void HandleBack()
    {
        if (cancelDialog != null) DismissCancel();
        else ClosePanel();
    }

    private void BuildUi()
    {
        if (layout != null) return;
        if (transform.Find("Title") is Transform legacyTitle) legacyTitle.gameObject.SetActive(false);
        GetComponent<Image>().color = new Color(.025f, .023f, .02f, .76f);
        layout = GameplayUi.Rect(transform, "VillageBuildingLayout", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, LayoutSize);
        var plate = layout.gameObject.AddComponent<Image>();
        plate.color = Ink;
        var outline = layout.gameObject.AddComponent<Outline>();
        outline.effectColor = Line;
        outline.effectDistance = new Vector2(1, -1);
        Area(layout, "TopRule", 0, 0, 1200, 3, Line);
        Label(layout, "Kicker", "村庄管理", 28, -18, 340, 22, 15, Gold);
        Label(layout, "Heading", "村庄营建", 28, -40, 550, 47, 36, Paper);
        scope = Label(layout, "Scope", "", 799, -49, 372, 29, 18, Muted, TextAlignmentOptions.MidlineRight);
        for (int i = 0; i < resources.Length; i++)
        {
            var badge = Area(layout, "Resource_" + i, 28 + i * 289, -101, 275, 46, Block);
            Area(badge, "Rule", 0, 0, 2, 46, Line);
            resources[i] = Label(badge, "Value", "", 16, -4, 244, 38, 20, Paper);
        }

        var list = Area(layout, "BuildingList", 28, -166, 310, 430, Block);
        listTitle = Label(list, "ListHeading", "", 14, -10, 282, 28, 18, Gold);
        Area(list, "Divider", 14, -42, 282, 1, Line);
        BuildListScroll(list);
        focusNote = Label(list, "FocusNote", "在此管理当前建筑。\n村庄共用一条施工队列。", 20, -145, 270, 63, 17, Muted);
        details = Box(layout, "BuildingDetail", 360, -166, 812, 430);
        BuildDetails();
        noSelection = Label(layout, "Empty", "暂无可显示的建筑", 360, -166, 812, 430, 22, Muted, TextAlignmentOptions.Center);

        message = Label(layout, "Feedback", "", 28, -605, 1144, 31, 17, Gold);
        var queue = Area(layout, "ConstructionQueue", 28, -649, 970, 49, Block);
        Label(queue, "QueueLabel", "施工队列", 14, -4, 123, 35, 17, Gold);
        queueStatus = Label(queue, "QueueStatus", "", 139, -4, 814, 35, 18, Paper);
        queueFill = Meter(queue, "QueueProgress", 14, -40, 942, 3, Gold, out _);
        var close = GameplayUi.Button(layout, "CloseButton", GameplayText.Text("ui_common_close", "关闭"),
            new Vector2(1024, -649), new Vector2(148, 49), ClosePanel);
        close.image.color = Block;
        OnRectTransformDimensionsChange();
    }

    private void BuildListScroll(Transform parent)
    {
        var area = Box(parent, "BuildingScroll", 10, -49, 290, 370);
        var viewport = Box(area, "Viewport", 0, 0, 290, 370);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        listContent = Box(viewport, "Content", 0, 0, 290, 0);
        var stack = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;
        stack.spacing = 7;
        listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        buildingScroll = area.gameObject.AddComponent<ScrollRect>();
        buildingScroll.viewport = viewport;
        buildingScroll.content = listContent;
        buildingScroll.horizontal = false;
        buildingScroll.movementType = ScrollRect.MovementType.Clamped;
        buildingScroll.scrollSensitivity = 32;
    }

    private void BuildDetails()
    {
        var heroRect = Area(details, "BuildingHero", 0, 0, 812, 124, Block);
        hero = heroRect.GetComponent<Image>();
        var stamp = Area(heroRect, "BuildingStamp", 23, -24, 79, 76, Ink);
        Area(stamp, "StampRule", 0, 0, 79, 2, Gold);
        glyph = Label(stamp, "StampText", "", 0, 0, 79, 76, 35, Gold, TextAlignmentOptions.Center);
        buildingName = Label(heroRect, "BuildingName", "", 126, -20, 659, 47, 34, Paper);
        status = Label(heroRect, "BuildingStatus", "", 129, -72, 652, 27, 20, Muted);

        var feature = Area(details, "BuildingFeature", 0, -139, 812, 128, Block);
        featureTitle = Label(feature, "FeatureTitle", "", 20, -11, 775, 23, 16, Gold);
        featureValue = Label(feature, "FeatureValue", "", 20, -36, 775, 32, 24, Paper);
        featureDetail = Label(feature, "FeatureDetail", "", 20, -71, 775, 41, 17, Muted);
        workFill = Meter(feature, "ProductionProgress", 20, -116, 771, 3, Green, out workTrack);

        var action = Area(details, "BuildingActions", 0, -281, 812, 149, Block);
        Label(action, "ActionHeading", "营建操作", 20, -12, 475, 23, 16, Gold);
        requirement = Label(action, "Requirements", "", 20, -40, 515, 43, 22, Paper);
        actionHint = Label(action, "ActionHint", "", 20, -88, 515, 38, 17, Muted);
        mainAction = GameplayUi.Button(action, "Primary", "", new Vector2(562, -24), new Vector2(228, 44), ActivatePrimary);
        mainActionText = mainAction.GetComponentInChildren<TextMeshProUGUI>();
        collect = GameplayUi.Button(action, "Collect", "收取产出", new Vector2(562, -79), new Vector2(228, 44),
            () => Send(PanelAction.Harvest, selected?.Id ?? 0));
    }

    public void Render(VillageState state)
    {
        scope.text = state.FacilityOnly ? "当前位置  /  设施详情"
            : state.FocusId != 0 ? "村庄地块  /  操作限当前位置" : $"营建总览  /  {state.Buildings.Count} 处地块";
        listTitle.text = state.FacilityOnly ? "当前设施" : "生产建筑";
        focusNote.gameObject.SetActive(state.FacilityOnly);
        for (int i = 0; i < resources.Length; i++) resources[i].text = state.Resources[i];
        RenderList(state.Buildings, state.Selected?.Id ?? 0, state.FocusId);
        selected = state.Selected;
        details.gameObject.SetActive(selected != null);
        noSelection.gameObject.SetActive(selected == null);
        if (selected != null) RenderDetail(selected);
        if (cancelDialog != null && (selected?.Action != PanelAction.CancelBuild || !selected.CanAct)) DismissCancel();

        var queue = state.Queue;
        queueStatus.text = queue == null ? "空闲 · 可安排一项建造或升级"
            : $"{queue.Name}  →  Lv{queue.TargetLevel}       剩余 {queue.RemainingSeconds} 秒";
        queueStatus.color = queue == null ? Muted : Gold;
        Fill(queueFill, queue?.ConstructionProgress ?? 0);
    }

    private void RenderList(IReadOnlyList<VillageBuildingCard> buildings, uint selectedId, uint focusId)
    {
        bool rebuild = rows.Count != buildings.Count;
        for (int i = 0; !rebuild && i < rows.Count; i++) rebuild = rows[i].Id != buildings[i].Id;
        if (rebuild)
        {
            foreach (var row in rows)
            {
                row.Surface.gameObject.SetActive(false);
                Destroy(row.Surface.gameObject);
            }
            rows.Clear();
            foreach (var building in buildings) rows.Add(CreateRow(building.Id));
            buildingScroll.verticalNormalizedPosition = 1;
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var card = buildings[i];
            bool active = card.Id == selectedId;
            row.Surface.color = active ? new Color(.31f, .255f, .17f) : Ink;
            row.Selection.enabled = active;
            row.Name.text = card.Name;
            row.Status.text = card.Status + (card.Id == focusId ? "  ·  当前" : "");
            row.Status.color = StatusColor(card);
            row.Badge.color = active ? Gold : Muted;
        }
    }

    private BuildingRow CreateRow(uint id)
    {
        var item = Area(listContent, "Building_" + id, 0, 0, 290, 66, Ink);
        item.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
        var button = item.gameObject.AddComponent<Button>();
        button.targetGraphic = item.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        button.onClick.AddListener(() =>
        {
            ShowMessage(string.Empty, true);
            presenter?.SelectBuilding(id);
        });
        var mark = Area(item, "Selected", 0, 0, 3, 66, Gold).GetComponent<Image>();
        Area(item, "Seal", 14, -13, 42, 40, Block);
        return new BuildingRow
        {
            Id = id, Surface = item.GetComponent<Image>(), Selection = mark,
            Badge = Label(item, "Badge", Badge(id), 14, -13, 42, 40, 20, Gold, TextAlignmentOptions.Center),
            Name = Label(item, "Name", "", 69, -8, 208, 26, 20, Paper),
            Status = Label(item, "Status", "", 69, -34, 208, 24, 15, Muted)
        };
    }

    private void RenderDetail(VillageBuildingCard card)
    {
        bool construction = card.State == VillageService.BuildingState.Building;
        hero.sprite = construction ? constructionCard : card.Level == 0 ? buildableCard : builtCard;
        hero.type = hero.sprite == null ? Image.Type.Simple : Image.Type.Sliced;
        hero.color = hero.sprite == null ? Block : Color.white;
        glyph.text = Badge(card.Id);
        buildingName.text = card.Name;
        status.text = (card.Fixed ? "村庄设施" : "生产建筑") + "   /   " + card.Status;
        status.color = StatusColor(card);
        featureTitle.text = card.FeatureTitle;
        featureValue.text = card.FeatureValue;
        featureDetail.text = card.FeatureDetail;
        workTrack.gameObject.SetActive(!card.Fixed && card.Level != 0 && !construction);
        Fill(workFill, card.ProductionProgress);
        requirement.text = card.Requirement;
        actionHint.text = card.ActionHint ?? string.Empty;
        actionHint.color = !card.CanAct && !string.IsNullOrEmpty(card.ActionLabel) ? Warning : Muted;
        mainAction.gameObject.SetActive(!string.IsNullOrEmpty(card.ActionLabel));
        mainAction.name = "Btn_" + card.Id;
        mainAction.interactable = card.CanAct;
        mainAction.image.color = construction ? new Color(.43f, .27f, .20f) : new Color(.43f, .33f, .18f);
        mainActionText.text = card.ActionLabel ?? string.Empty;
        collect.gameObject.SetActive(card.ShowCollect);
        collect.name = "Harvest_" + card.Id;
        collect.interactable = card.CanCollect;
        collect.image.color = new Color(.26f, .34f, .23f);
    }

    private void ActivatePrimary()
    {
        var card = selected;
        if (card == null || !card.CanAct) return;
        if (card.Action != PanelAction.CancelBuild)
        {
            Send(card.Action, card.Id);
            return;
        }

        // 确认层随界面缩放，并拦截底下的施工与收取按钮。
        if (cancelDialog != null) return;
        cancelDialog = Area(layout, "CancelConstructionDialog", 0, 0, 1200, 720, new Color(0, 0, 0, .72f));
        cancelDialog.GetComponent<Image>().raycastTarget = true;
        var box = Area(cancelDialog, "Dialog", 315, -245, 570, 225, Block);
        Area(box, "DialogRule", 0, 0, 570, 2, Gold);
        Label(box, "DialogTitle", "取消施工", 24, -15, 522, 38, 26, Gold);
        Label(box, "DialogDetail", card.CancelPrompt, 24, -60, 522, 84, 20, Paper);
        var keep = GameplayUi.Button(box, "KeepConstruction", "继续施工", new Vector2(24, -156), new Vector2(245, 47), DismissCancel);
        keep.image.color = Ink;
        var confirm = GameplayUi.Button(box, "ConfirmCancelConstruction", "确认取消", new Vector2(301, -156), new Vector2(245, 47),
            () => { DismissCancel(); Send(PanelAction.CancelBuild, card.Id); });
        confirm.image.color = new Color(.43f, .27f, .20f);
    }

    private void DismissCancel()
    {
        if (cancelDialog == null) return;
        cancelDialog.gameObject.SetActive(false);
        Destroy(cancelDialog.gameObject);
        cancelDialog = null;
    }

    public void ShowMessage(string text, bool success)
    {
        if (message == null) return;
        message.text = text ?? string.Empty;
        message.color = success ? Green : Warning;
    }

    private void Send(PanelAction action, uint id)
    {
        if (IsShowing && id != 0) Intent?.Invoke(new PanelIntent(action, id));
    }

    private static Color StatusColor(VillageBuildingCard card) =>
        card.State == VillageService.BuildingState.Building ? Gold : card.Level == 0 ? Muted : Green;

    private static string Badge(uint id)
    {
        switch (id)
        {
            case 2001: return "田";
            case 2002: return "作";
            case 2003: return "文";
            case 2004: return "市";
            case VillageService.OfficeId: return "政";
            case VillageService.FamilyMansionId: return "宗";
            case VillageService.WarehouseId: return "仓";
            default: return "筑";
        }
    }

    private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h) =>
        GameplayUi.Rect(parent, name, Vector2.up, Vector2.up, new Vector2(x, y), new Vector2(w, h));

    private static RectTransform Area(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var rect = Box(parent, name, x, y, w, h);
        var img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float w, float h, float size,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var label = GameplayUi.Text(Box(parent, name, x, y, w, h), "Text", text, size);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.color = color;
        label.alignment = alignment;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .78f;
        label.fontSizeMax = size;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static RectTransform Meter(Transform parent, string name, float x, float y, float w, float h,
        Color color, out RectTransform track)
    {
        track = Area(parent, name, x, y, w, h, Ink);
        var fill = Area(track, "Fill", 0, 0, 0, 0, color);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.up;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        return fill;
    }

    private static void Fill(RectTransform fill, float progress) =>
        fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
}
