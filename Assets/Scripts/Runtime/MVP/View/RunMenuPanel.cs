using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive collection view; existing RunMenu prefab remains the formal overlay entry.</summary>
public sealed class RunMenuPanel : BasePanel, IGameplayView<RunMenuState>, IRunBagView
{
    public override bool ReturnsToGameplayOnClose => true;
    public event Action<PanelIntent> Intent;
    public event Action<RunBagMove> MoveRequested;
    public event Action<uint> CarryRequested;
    public RunFunctionTab SelectedTab { get; private set; } = RunFunctionTab.Inventory;
    private RunMenuModel _model;
    private RunMenuPresenter _presenter;
    private RectTransform _box, _inventory, _catalog;
    private TextMeshProUGUI _summary, _hint, _placeholder;
    private readonly List<RunBagSlotView> _slots = new List<RunBagSlotView>();
    private readonly List<Button> _tabs = new List<Button>();
    private Button _camp, _retreat, _cancel, _settings;
    private bool _terrainWasVisible;
    private const float BoxWidth = 1100, BoxHeight = 700;
    public static void Open() { Open(RunFunctionTab.Inventory); }
    public static void Open(RunFunctionTab tab)
    {
        if (!DispatchService.Instance.IsInRun || RunSceneFlow.IsTransitioning) return;
        UIManager.Instance.RestoreAfterGameplay();
        var panel = UIManager.Instance.PushPanel(PanelType.RunMenuPanel) as RunMenuPanel;
        if (panel != null) panel.SelectTab(tab);
    }
    public void SelectTab(RunFunctionTab tab) { _presenter?.SelectTab(tab); }

    private void Awake()
    {
        var backdrop = GetComponent<Image>();
        if (backdrop != null) backdrop.color = new Color(0, 0, 0, .78f);
        _box = GameplayUi.Rect(transform, "ContentBox", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(BoxWidth, BoxHeight));
        _box.gameObject.AddComponent<Image>().color = new Color(.055f, .075f, .10f, 1);
        Label(_box, "Title", "功能集合", 28, new Vector2(22, -8), new Vector2(230, 40));
        _summary = Label(_box, "Summary", "", 22, new Vector2(500, -8), new Vector2(570, 40));
        string[] tabs = { "队伍", "局内背包", "局内地图" };
        for (int i = 0; i < tabs.Length; i++)
        {
            uint tab = (uint)i;
            _tabs.Add(GameplayUi.Button(_box, "Tab" + i, tabs[i], new Vector2(22 + i * 220, -55),
                new Vector2(210, 42), () => Send(PanelAction.Toggle, tab)));
        }
        _inventory = GameplayUi.Rect(_box, "Inventory", Vector2.up, Vector2.up, new Vector2(0, -110), new Vector2(BoxWidth, 475));
        _placeholder = Label(_box, "Placeholder", "", 26, new Vector2(50, -190), new Vector2(1000, 220));
        _placeholder.alignment = TextAlignmentOptions.Center;
        string[] names = { "重要道具", "工程道具", "疗伤加成", "杂物" };
        int[] maxima = { 5, 7, 7, 30 };
        for (int section = 0; section < 4; section++)
        {
            float y = -section * 75;
            var heading = Label(_inventory, "Section" + section, names[section] + (section < 3 ? "\n快捷选用预留" : "\n无快捷选用"), 18,
                new Vector2(12, y), new Vector2(156, 60));
            heading.color = section < 3 ? new Color(.75f, .84f, .94f) : new Color(.75f, .76f, .78f);
            for (int index = 0; index < maxima[section]; index++)
            {
                var rect = GameplayUi.Rect(_inventory, "Slot_" + section + "_" + index, Vector2.up, Vector2.up,
                    new Vector2(176 + index % 10 * 89, y - index / 10 * 62), new Vector2(82, 56));
                var slot = rect.gameObject.AddComponent<RunBagSlotView>();
                slot.Initialize(this, (RunBagSection)section, index);
                _slots.Add(slot);
            }
        }
        _catalog = GameplayUi.Rect(_inventory, "PermanentCollection", Vector2.up, Vector2.up, new Vector2(176, -420), new Vector2(900, 40));
        _hint = Label(_box, "Hint", "", 19, new Vector2(24, -578), new Vector2(1052, 52));
        _hint.color = new Color(1, .83f, .42f);
        GameplayUi.Button(_box, "Continue", "继续", new Vector2(24, -640), new Vector2(160, 44), () => Send(PanelAction.Close));
        _camp = GameplayUi.Button(_box, "Camp", "扎营", new Vector2(202, -640), new Vector2(160, 44), () => Send(PanelAction.Heal));
        _settings = GameplayUi.Button(_box, "Settings", "设置", new Vector2(380, -640), new Vector2(160, 44), () => Send(PanelAction.Settings));
        _retreat = GameplayUi.Button(_box, "Retreat", "撤离", new Vector2(736, -640), new Vector2(160, 44), () => Send(PanelAction.Return));
        _cancel = GameplayUi.Button(_box, "CancelRetreat", "取消撤离", new Vector2(914, -640), new Vector2(160, 44), () => Send(PanelAction.Cancel));
    }
    private void LateUpdate()
    {
        if (_box == null || !(transform is RectTransform canvas)) return;
        float scale = Mathf.Min(1, (canvas.rect.width - 24) / BoxWidth, (canvas.rect.height - 24) / BoxHeight);
        _box.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }
    private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Vector2 position, Vector2 dimensions)
    {
        var area = GameplayUi.Rect(parent, name + "Area", Vector2.up, Vector2.up, position, dimensions);
        return GameplayUi.Text(area, name, text, size);
    }
    public override void OnEnter()
    {
        RunDepthHud.SetVisible(false);
        RunTeamHud.SetVisible(false);
        if (_model == null || !_model.IsCurrent)
        { _model?.Dispose(); _model = new RunMenuModel(); _terrainWasVisible = RunTerrainToast.IsShowing; }
        RunTerrainToast.SetVisible(false);
        var selectedTab = SelectedTab;
        _presenter?.Dispose();
        _presenter = new RunMenuPresenter(_model);
        _presenter.Attach(this);
        _presenter.SelectTab(selectedTab);
    }
    public override void OnExit()
    {
        CancelDrag();
        _presenter?.Dispose(); _presenter = null;
        if (uiManager == null || !uiManager.panelStack.Contains(this))
        {
            bool resumeHud = _model != null && _model.InRun;
            _model?.Dispose(); _model = null;
            if (resumeHud) { RunDepthHud.SetVisible(true); RunTeamHud.SetVisible(true); }
            if (resumeHud && _terrainWasVisible) RunTerrainToast.SetVisible(true);
        }
    }
    private void OnDisable() { CancelDrag(); }
    private void OnDestroy() { CancelDrag(); _presenter?.Dispose(); _model?.Dispose(); }
    private void Send(PanelAction action, uint id = 0) { if (IsShowing) Intent?.Invoke(new PanelIntent(action, id)); }
    public void ClosePanel() { UIManager.Instance.PopPanel(); }
    public void ShowMessage(string message, bool success) { _hint.text = message; }
    private int _catalogPage;
    private uint[] _catalogItems = Array.Empty<uint>();
    private void RenderCatalog()
    {
        foreach (Transform child in _catalog) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        int pages = Mathf.Max(1, (_catalogItems.Length + 2) / 3);
        _catalogPage = Mathf.Clamp(_catalogPage, 0, pages - 1);
        for (int i = 0; i < 3; i++)
        {
            int index = _catalogPage * 3 + i;
            if (index >= _catalogItems.Length) break;
            uint id = _catalogItems[index];
            GameplayUi.Button(_catalog, "Carry_" + id, "带入 " + LocalizationSystem.GameplayText.ItemName(id),
                new Vector2(i * 235, 0), new Vector2(226, 36), () => { if (IsShowing) CarryRequested?.Invoke(id); });
        }
        if (pages > 1) GameplayUi.Button(_catalog, "NextOwned", (_catalogPage + 1) + "/" + pages + " 下一页",
            new Vector2(710, 0), new Vector2(170, 36), () => { _catalogPage = (_catalogPage + 1) % pages; RenderCatalog(); });
    }
    public void Render(RunMenuState state)
    {
        CancelDrag();
        SelectedTab = state.Tab;
        _summary.text = state.Summary;
        _hint.text = state.Hint;
        _inventory.gameObject.SetActive(state.Tab == RunFunctionTab.Inventory);
        _placeholder.transform.parent.gameObject.SetActive(state.Tab != RunFunctionTab.Inventory);
        _placeholder.text = state.Placeholder;
        for (int i = 0; i < _tabs.Count; i++) _tabs[i].image.color =
            i == (int)state.Tab ? new Color(.28f, .49f, .62f) : new Color(.12f, .20f, .28f);
        for (int i = 0; i < _slots.Count; i++) _slots[i].Render(state.Slots[i]);
        _catalogItems = new List<uint>(state.Uncarried).ToArray();
        RenderCatalog();
        _retreat.GetComponentInChildren<TextMeshProUGUI>().text = state.RetreatLabel;
        _cancel.gameObject.SetActive(state.ConfirmRetreat);
        _camp.interactable = _settings.interactable = !state.ConfirmRetreat;
        foreach (var slot in _slots) slot.AllowDrag = !state.ConfirmRetreat;
    }
    private RunBagSlotView _dragSource;
    private RectTransform _dragVisual;
    public void BeginDrag(RunBagSlotView source, UnityEngine.EventSystems.PointerEventData data)
    {
        if (!IsShowing || _presenter == null) return;
        CancelDrag(); _dragSource = source;
        _dragVisual = GameplayUi.Rect(_box, "DragVisual", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(110, 60));
        var image = _dragVisual.gameObject.AddComponent<Image>();
        image.color = new Color(.28f, .48f, .58f, .9f); image.raycastTarget = false;
        GameplayUi.Text(_dragVisual, "Item", source.DisplayText, 16);
        Drag(data);
    }
    public void Drag(UnityEngine.EventSystems.PointerEventData data)
    {
        if (_dragVisual == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_box, data.position, data.pressEventCamera, out Vector2 point))
            _dragVisual.localPosition = point;
    }
    public void Drop(RunBagSlotView target)
    {
        if (!IsShowing || _dragSource == null || _presenter == null) { CancelDrag(); return; }
        var move = new RunBagMove(_dragSource.Section, _dragSource.Index, target.Section, target.Index);
        CancelDrag();
        MoveRequested?.Invoke(move);
    }
    public void CancelDrag()
    {
        _dragSource = null;
        if (_dragVisual != null) { _dragVisual.gameObject.SetActive(false); Destroy(_dragVisual.gameObject); }
        _dragVisual = null;
    }
}
