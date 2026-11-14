using System;
using System.Collections.Generic;
using System.Linq;
using Chaos.Gameplay.Items;
using Chaos.MVP.Model.Gameplay;
using TMPro;
using LocalizationSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>战备仓库、局内格位和组合弹窗的显示与指针适配。</summary>
public sealed class BattlePreparationPanel : BasePanel, IBattlePreparationView
{
    private static readonly Color CardColor = new Color(.06f, .085f, .11f);
    private static readonly Color NormalSlot = new Color(.16f, .22f, .28f);
    private static readonly Color FilledSlot = new Color(.19f, .34f, .39f);
    private static readonly Color SelectedColor = new Color(.46f, .33f, .16f);
    private readonly Dictionary<uint, BattleSlotPointer> rows = new Dictionary<uint, BattleSlotPointer>();
    private readonly List<BattleSlotPointer> cells = new List<BattleSlotPointer>();
    private BattlePreparationPresenter presenter;
    private RectTransform card, warehouse, presetsOverlay, detailOverlay, statusContent;
    private TextMeshProUGUI message, comboMessage;
    private Button enter;
    private BattleSlotPointer dragged;
    private RectTransform dragGhost;
    private bool dragAll;
    private string latestMessage;
    private Color latestMessageColor;

    public override void OnEnter()
    {
        Build();
        presenter = new BattlePreparationPresenter(BattlePreparationFlow.GetDraft(), this);
        MetaRuntimeDriver.Changed -= Refresh;
        MetaRuntimeDriver.Changed += Refresh;
        ShowMessage(string.Empty, true);
        Refresh();
        CursorEnable();
    }

    public override void OnExit()
    {
        MetaRuntimeDriver.Changed -= Refresh;
        HidePresets();
        RemoveOverlay(ref detailOverlay);
        EndDrag();
        presenter = null;
        BattlePreparationFlow.DiscardWhenDispatchClosed();
    }

    private void OnEnable()
    {
        if (presenter == null || !IsShowing) return;
        MetaRuntimeDriver.Changed -= Refresh;
        MetaRuntimeDriver.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        MetaRuntimeDriver.Changed -= Refresh;
        presenter?.ResetGestures();
        EndDrag();
    }

    public override void HandleBack()
    {
        if (BattlePreparationFlow.IsStarting) return;
        if (detailOverlay != null) { RemoveOverlay(ref detailOverlay); return; }
        if (presetsOverlay != null) { HidePresets(); return; }
        ClosePanel();
    }

    private void Update()
    {
        if (!IsShowing || card == null) return;
        var size = ((RectTransform)transform).rect.size;
        card.localScale = Vector3.one * Mathf.Min(1f, size.x / 1400f, size.y / 850f);
        if (detailOverlay == null && presetsOverlay == null && dragged == null) presenter?.Repeat(Time.unscaledTimeAsDouble);
        if (enter != null) enter.interactable = !BattlePreparationFlow.IsStarting && DispatchService.Instance.CanDepart;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        presenter?.ResetGestures();
        EndDrag();
    }

    private void Refresh() => presenter?.Refresh();

    private void Build()
    {
        if (card != null) return;
        GetComponent<Image>().color = new Color(0, 0, 0, .74f);
        card = GameplayUi.Rect(transform, "BattleCard", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(1360, 810));
        GameplayUiSkin.Apply(card.gameObject.AddComponent<Image>(), "UI_Panel_Sub", GameplayUiSkin.DarkPaper);
        Title(card, "战备", 28, -16, 280, 45, 34);
        Title(card, "选择本次携带物品。入局成功才扣仓库，失败按结算额度带回，主动撤离丢失未结算物品。", 30, -63, 1300, 34, 19);
        Title(card, "局外仓库", 30, -110, 480, 34, 24);
        Title(card, "局内装备栏", 570, -110, 740, 34, 24);
        warehouse = Scroll(card, "Warehouse", 28, -153, 495, 475, out var viewport);
        viewport.gameObject.AddComponent<BattleSlotPointer>().Configure(this, true);
        Title(card, "单击 +1  ·  长按连加  ·  双击调数量  ·  Ctrl＋单击加满", 30, -639, 525, 31, 16);
        Title(card, "装备栏单击减一，右键移出整叠。拖拽调整物理格位。", 570, -622, 740, 31, 16);
        string[] labels = { "重要道具", "工程道具", "疗伤加成", "杂物" };
        float[] positions = { 150, 240, 330, 420 };
        for (int row = 0; row < labels.Length; row++)
        {
            var section = (RunBagSection)row;
            Title(card, labels[row], 570, -positions[row], 720, 23, 17);
            for (int i = 0; i < RunInventory.GetMaximum(section); i++)
            {
                int columns = row == 3 ? 10 : 7;
                var cell = Box(card, "BattleSlot_" + row + "_" + i, 570 + i % columns * 74,
                    -positions[row] - 25 - i / columns * 55, 69, row == 3 ? 51 : 59);
                var pointer = cell.gameObject.AddComponent<BattleSlotPointer>();
                pointer.Configure(this, false, section, i);
                cells.Add(pointer);
            }
        }
        statusContent = Scroll(card, "Feedback", 29, -679, 1300, 54, out _);
        message = GameplayUi.Text(statusContent, "Message", string.Empty, 18);
        message.alignment = TextAlignmentOptions.TopLeft;
        var messageHeight = message.gameObject.AddComponent<LayoutElement>();
        messageHeight.minHeight = 40;
        GameplayUi.Button(card, "BackToDispatch", "返回编队", new Vector2(29, -749), new Vector2(180, 43), HandleBack);
        GameplayUi.Button(card, "BattlePresets", "快捷组合", new Vector2(918, -749), new Vector2(180, 43), () => presenter?.OpenPresets());
        enter = GameplayUi.Button(card, "EnterRun", "入局", new Vector2(1118, -749), new Vector2(210, 43), () => presenter?.EnterRun());
    }

    public void Render(IReadOnlyList<BattleWarehouseRow> available, IReadOnlyList<BattleSlotSave> slots)
    {
        var ids = new HashSet<uint>(available.Select(row => row.Id));
        foreach (var pair in rows.ToArray())
            if (!ids.Contains(pair.Key)) { pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); rows.Remove(pair.Key); }
        for (int i = 0; i < available.Count; i++)
        {
            var row = available[i];
            if (!rows.TryGetValue(row.Id, out var item))
            {
                var rect = Box(warehouse, "WarehouseItem_" + row.Id, 0, 0, 460, 57);
                rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 57;
                item = rect.gameObject.AddComponent<BattleSlotPointer>();
                item.Configure(this, true);
                rows.Add(row.Id, item);
            }
            item.transform.SetSiblingIndex(i);
            uint free = row.Stock >= row.Selected ? row.Stock - row.Selected : 0;
            string label = GameplayText.ItemName(row.Id) + (RunInventory.IsImportant(row.Id) ? " · 永久收藏" : "")
                + "\n库存 " + row.Stock + "   已选 " + row.Selected + "   可加入 " + free;
            item.Render(row.Id, label, true, true);
        }
        foreach (var cell in cells)
        {
            bool open = cell.Index < RunInventory.GetCapacity(cell.Section);
            var item = slots.FirstOrDefault(slot => slot.section == cell.Section && slot.index == cell.Index);
            string key = cell.Section == RunBagSection.Misc ? "" : (cell.Index + 1) + "  ";
            string label = !open ? "未开放" : item == null ? key.Trim() : key + GameplayText.ItemName(item.itemId) + "\n×" + item.count;
            cell.Render(item?.itemId ?? 0, label, open, false);
        }
    }

    public void ShowMessage(string text, bool success)
    {
        latestMessage = text ?? string.Empty;
        latestMessageColor = success ? new Color(.72f, .9f, .8f) : new Color(1f, .64f, .45f);
        if (message != null)
        {
            message.text = latestMessage;
            message.color = latestMessageColor;
            message.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(40, 24 * latestMessage.Split('\n').Length);
        }
        if (comboMessage != null) { comboMessage.text = latestMessage; comboMessage.color = latestMessageColor; }
    }

    public void ShowPresets(IReadOnlyList<BattlePresetSave> presets, int selected)
    {
        RemoveOverlay(ref presetsOverlay);
        var box = OpenOverlay("BattlePresetPopup", new Vector2(770, 345), out presetsOverlay);
        Title(box, "快捷组合", 24, -18, 720, 42, 28);
        for (int index = 0; index < presets.Count; index++)
        {
            int id = index;
            var option = presets[index];
            string text = "档位 " + (index + 1) + "\n" + (option.saved ? option.slots.Count + " 种物品" : "空白");
            var button = GameplayUi.Button(box, "Preset_" + index, text, new Vector2(24 + index % 5 * 145, -82 - index / 5 * 69),
                new Vector2(135, 60), () => presenter?.SelectPreset(id, Time.unscaledTimeAsDouble));
            button.image.color = index == selected ? SelectedColor : NormalSlot;
        }
        var plus = GameplayUi.Button(box, "AddPreset", "＋", new Vector2(24, -229), new Vector2(54, 46), () => presenter?.AddPreset());
        var minus = GameplayUi.Button(box, "RemovePreset", "−", new Vector2(88, -229), new Vector2(54, 46), () => presenter?.RemovePreset());
        plus.interactable = presets.Count < BattleLoadout.MaxPresets;
        bool valid = selected >= 0 && selected < presets.Count;
        minus.interactable = valid && presets.Count > BattleLoadout.MinPresets;
        GameplayUi.Button(box, "SavePreset", "保存当前组合", new Vector2(162, -229), new Vector2(178, 46),
            () => presenter?.SavePreset()).interactable = valid;
        GameplayUi.Button(box, "LoadPreset", "加载组合", new Vector2(356, -229), new Vector2(162, 46),
            () => presenter?.LoadPreset()).interactable = valid && presets[selected].saved;
        GameplayUi.Button(box, "ClosePresets", "返回", new Vector2(595, -229), new Vector2(145, 46), HidePresets);
        comboMessage = Title(box, latestMessage, 24, -291, 720, 40, 17);
        comboMessage.color = latestMessageColor;
    }

    public void HidePresets()
    { presenter?.ResetGestures(); RemoveOverlay(ref presetsOverlay); comboMessage = null; }

    public void Confirm(string text, Action confirm)
    {
        RemoveOverlay(ref detailOverlay);
        var box = OpenOverlay("BattleConfirm", new Vector2(500, 175), out detailOverlay);
        Title(box, text, 24, -24, 452, 65, 24);
        GameplayUi.Button(box, "CancelBattleAction", "取消", new Vector2(56, -112), new Vector2(168, 42), () => RemoveOverlay(ref detailOverlay));
        GameplayUi.Button(box, "ConfirmBattleAction", "确认", new Vector2(276, -112), new Vector2(168, 42),
            () => { RemoveOverlay(ref detailOverlay); confirm(); });
    }

    public void ShowQuantity(uint id, uint current, uint maximum, Action<uint> confirm)
    {
        RemoveOverlay(ref detailOverlay);
        var box = OpenOverlay("BattleQuantity", new Vector2(500, 230), out detailOverlay);
        Title(box, GameplayText.ItemName(id) + " · 携带总量 0～" + maximum, 24, -19, 452, 48, 24);
        var fieldRect = Box(box, "Quantity", 143, -83, 215, 47);
        var field = fieldRect.gameObject.AddComponent<TMP_InputField>();
        var label = GameplayUi.Text(fieldRect, "Value", "", 23);
        field.textViewport = fieldRect;
        field.textComponent = label;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.characterLimit = 10;
        field.text = Math.Min(current, maximum).ToString();
        uint Value() => uint.TryParse(field.text, out uint parsed) ? Math.Min(parsed, maximum) : 0;
        GameplayUi.Button(box, "MinusQuantity", "−", new Vector2(69, -83), new Vector2(57, 47),
            () => { uint value = Value(); field.text = (value > 0 ? value - 1 : 0).ToString(); });
        GameplayUi.Button(box, "PlusQuantity", "＋", new Vector2(374, -83), new Vector2(57, 47),
            () => { uint value = Value(); field.text = (value < maximum ? value + 1 : maximum).ToString(); });
        GameplayUi.Button(box, "CancelQuantity", "取消", new Vector2(55, -168), new Vector2(166, 42), () => RemoveOverlay(ref detailOverlay));
        GameplayUi.Button(box, "ConfirmQuantity", "确认数量", new Vector2(276, -168), new Vector2(166, 42),
            () => { uint value = Value(); RemoveOverlay(ref detailOverlay); confirm(value); });
    }

    private RectTransform OpenOverlay(string name, Vector2 size, out RectTransform overlay)
    {
        overlay = GameplayUi.Rect(card, name, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;
        overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .7f);
        var box = GameplayUi.Rect(overlay, name + "Card", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, size);
        GameplayUiSkin.Apply(box.gameObject.AddComponent<Image>(), "UI_Panel_Dialog", GameplayUiSkin.DarkPaper);
        return box;
    }

    private static void RemoveOverlay(ref RectTransform overlay)
    {
        if (overlay == null) return;
        overlay.gameObject.SetActive(false);
        Destroy(overlay.gameObject);
        overlay = null;
    }

    private static RectTransform Scroll(Transform parent, string name, float x, float y, float w, float h, out RectTransform viewport)
    {
        var rect = Box(parent, name, x, y, w, h);
        rect.GetComponent<Image>().color = new Color(.08f, .11f, .14f);
        viewport = GameplayUi.Rect(rect, "Viewport", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        var content = GameplayUi.Rect(viewport, "Content", Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
        content.anchorMax = Vector2.one;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 5;
        layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = rect.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 27;
        return content;
    }

    private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = GameplayUi.Rect(parent, name, Vector2.up, Vector2.up, new Vector2(x, y), new Vector2(w, h));
        GameplayUiSkin.Apply(rect.gameObject.AddComponent<Image>(), "UI_Slot", GameplayUiSkin.DarkPaper);
        return rect;
    }

    private static TextMeshProUGUI Title(Transform parent, string text, float x, float y, float w, float h, float size)
    {
        var rect = GameplayUi.Rect(parent, "Label", Vector2.up, Vector2.up, new Vector2(x, y), new Vector2(w, h));
        var label = GameplayUi.Text(rect, "Text", text, size);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    internal static bool Ctrl => Keyboard.current?.ctrlKey.isPressed == true;
    internal void Press(BattleSlotPointer source)
    {
        if (source.IsWarehouse && source.Id != 0 && detailOverlay == null && presetsOverlay == null)
            presenter?.BeginPress(source.Id, Time.unscaledTimeAsDouble, Ctrl);
    }
    internal void CancelPress() => presenter?.CancelPress();
    internal void Release(BattleSlotPointer source, bool right)
    {
        if (dragged != null || source.Id == 0) return;
        if (source.IsWarehouse)
        {
            if (!right) presenter?.Release(source.Id, Ctrl, Time.unscaledTimeAsDouble);
        }
        else presenter?.Remove(source.Id, right);
    }
    internal void BeginDrag(BattleSlotPointer source, PointerEventData data)
    {
        if (source.Id == 0 || presenter?.CanDrag != true) { presenter?.ResetGestures(); return; }
        presenter.ResetGestures();
        dragged = source;
        dragAll = Ctrl;
        dragGhost = GameplayUi.Rect(transform, "BattleDrag", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(180, 40));
        dragGhost.gameObject.AddComponent<Image>().raycastTarget = false;
        GameplayUi.Text(dragGhost, "Text", GameplayText.ItemName(source.Id), 17).color = new Color(.9f, .75f, .45f);
        UpdateDrag(data);
    }
    internal void UpdateDrag(PointerEventData data)
    {
        if (dragGhost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.position, data.pressEventCamera, out var position)) dragGhost.anchoredPosition = position;
    }
    internal void Drop(BattleSlotPointer target)
    {
        if (dragged == null || dragged == target) return;
        if (target.IsWarehouse)
        { if (!dragged.IsWarehouse) presenter?.Remove(dragged.Id, true); }
        else if (dragged.IsWarehouse) presenter?.Add(dragged.Id, dragAll || Ctrl, target.Section, target.Index);
        else presenter?.Move(dragged.Section, dragged.Index, target.Section, target.Index);
        EndDrag();
    }
    internal void EndDrag()
    {
        dragged = null;
        if (dragGhost != null) Destroy(dragGhost.gameObject);
        dragGhost = null;
    }

    internal static Color SlotColor(bool open, bool occupied) =>
        !open ? new Color(.10f, .12f, .15f) : occupied ? FilledSlot : NormalSlot;
}

/// <summary>把 uGUI 指针事件转换成战备意图。</summary>
public sealed class BattleSlotPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private BattlePreparationPanel owner;
    private TextMeshProUGUI label;
    private Image icon, background;
    private bool pressed, dragging;
    public bool IsWarehouse { get; private set; }
    public RunBagSection Section { get; private set; }
    public int Index { get; private set; }
    public uint Id { get; private set; }

    internal void Configure(BattlePreparationPanel panel, bool isWarehouse, RunBagSection section = RunBagSection.Misc, int index = -1)
    {
        owner = panel; IsWarehouse = isWarehouse; Section = section; Index = index;
        background = GetComponent<Image>();
        label = GameplayUi.Text(transform, "ItemLabel", "", isWarehouse ? 17 : 12);
        if (isWarehouse)
        {
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.rectTransform.offsetMin = new Vector2(48, 3);
            var rect = GameplayUi.Rect(transform, "ItemIcon", new Vector2(0, .5f), Vector2.one * .5f, new Vector2(24, 0), Vector2.one * 31);
            icon = rect.gameObject.AddComponent<Image>();
            icon.enabled = false;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
        }
    }
    internal void Render(uint id, string text, bool open, bool warehouse)
    {
        if (Id != id && icon != null)
        {
            var item = ItemBase.Create(id);
            icon.sprite = item == null || string.IsNullOrEmpty(item.IconKey) ? null : Resources.Load<Sprite>(item.IconKey);
            icon.enabled = icon.sprite != null;
        }
        Id = id;
        label.text = text;
        label.color = open ? Color.white : new Color(.51f, .55f, .59f);
        background.color = BattlePreparationPanel.SlotColor(open, !warehouse && id != 0);
    }
    public void OnPointerDown(PointerEventData data)
    {
        if (Id == 0) return;
        pressed = true;
        if (data.button == PointerEventData.InputButton.Left) owner.Press(this);
    }
    public void OnPointerUp(PointerEventData data)
    {
        if (pressed && !dragging && data.button != PointerEventData.InputButton.Middle)
            owner.Release(this, data.button == PointerEventData.InputButton.Right);
        pressed = false;
    }
    public void OnPointerExit(PointerEventData data)
    {
        if (!dragging && pressed) { pressed = false; owner.CancelPress(); }
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        dragging = true; pressed = false; owner.BeginDrag(this, data);
    }
    public void OnDrag(PointerEventData data) => owner.UpdateDrag(data);
    public void OnEndDrag(PointerEventData data) { dragging = false; owner.EndDrag(); }
    public void OnDrop(PointerEventData data) => owner.Drop(this);
}
