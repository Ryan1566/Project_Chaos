using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Pointer drag adapter; all inventory validation is performed by the domain service.</summary>
public sealed class RunBagSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RunBagSection Section { get; private set; }
    public int Index { get; private set; }
    public bool AllowDrag { get; set; } = true;
    public string DisplayText => _label == null ? "" : _label.text;
    private RunMenuPanel _owner;
    private Image _image, _icon;
    private string _description;
    private TextMeshProUGUI _label;
    private bool _unlocked, _hasItem;
    public void Initialize(RunMenuPanel owner, RunBagSection section, int index)
    {
        _owner = owner; Section = section; Index = index;
        _image = gameObject.AddComponent<Image>();
        var rect = GameplayUi.Rect(transform, "Icon", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -3), new Vector2(24, 24));
        _icon = rect.gameObject.AddComponent<Image>(); _icon.preserveAspect = true; _icon.raycastTarget = false;
        _label = GameplayUi.Text(transform, "Item", "", 13);
        _label.enableWordWrapping = true;
    }
    public void Render(Chaos.MVP.Presenter.Gameplay.RunBagSlotState state)
    {
        _unlocked = state.Unlocked; _hasItem = state.Id != 0; _description = state.Description;
        _icon.sprite = state.Icon; _icon.gameObject.SetActive(_hasItem && _unlocked && state.Icon != null);
        _label.rectTransform.offsetMax = new Vector2(-4, _hasItem && _unlocked ? -27 : -4);
        _label.rectTransform.offsetMin = new Vector2(4, 2);
        _image.color = !_unlocked ? new Color(.10f, .12f, .14f) : _hasItem ? new Color(.20f, .33f, .39f) : new Color(.15f, .19f, .23f);
        _label.color = _unlocked ? Color.white : new Color(.4f, .43f, .47f);
        _label.text = !_unlocked ? "未开放" : !_hasItem ? (Index + 1).ToString() : state.Name + "\n×" + state.Count;
    }
    public void OnPointerEnter(PointerEventData data) { if (_hasItem && _unlocked) _owner.ShowItemDetails(_description); }
    public void OnPointerExit(PointerEventData data) { _owner.ShowItemDetails(null); }
    public void OnBeginDrag(PointerEventData data) { if (AllowDrag && _unlocked && _hasItem && data.button == PointerEventData.InputButton.Left) _owner.BeginDrag(this, data); }
    public void OnDrag(PointerEventData data) { _owner.Drag(data); }
    public void OnEndDrag(PointerEventData data) { _owner.CancelDrag(); }
    public void OnDrop(PointerEventData data) { if (AllowDrag) _owner.Drop(this); }
}
