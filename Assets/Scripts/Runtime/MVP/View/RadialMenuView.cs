using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive pooled view. Input, confirmation and item effects belong to its presenter/provider.</summary>
public sealed class RadialMenuView : MonoBehaviour
{
    sealed class Cell
    { public GameObject Root; public RadialWedgeGraphic Wedge; public RectTransform LabelRoot; public TextMeshProUGUI Name, Count, Key; public Image Icon; }
    readonly Cell[][] _cells = { new Cell[12], new Cell[12] };
    RectTransform _root, _primary, _secondary, _pointer;
    TextMeshProUGUI _hint;
    bool _initialized, _secondaryTarget;
    float _blend;
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _root = GameplayUi.Rect(transform, "WheelCenter", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 620);
        _primary = GameplayUi.Rect(_root, "Primary", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
        _secondary = GameplayUi.Rect(_root, "Secondary", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
        for (int tier = 0; tier < 2; tier++) for (int i = 0; i < 12; i++)
        {
            var rect = GameplayUi.Rect(tier == 0 ? _primary : _secondary, "Entry" + i, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
            var c = new Cell { Root = rect.gameObject, Wedge = rect.gameObject.AddComponent<RadialWedgeGraphic>() };
            c.LabelRoot = GameplayUi.Rect(rect, "Label", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(100, 90));
            var iconRect = GameplayUi.Rect(c.LabelRoot, "Icon", Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 17), Vector2.one * 30);
            c.Icon = iconRect.gameObject.AddComponent<Image>(); c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            c.Name = Label(c.LabelRoot, "Name", new Vector2(0, -12), new Vector2(112, 38), 17);
            c.Count = Label(c.LabelRoot, "Count", new Vector2(0, -34), new Vector2(90, 20), 15);
            c.Key = Label(c.LabelRoot, "Shortcut", new Vector2(0, 43), new Vector2(50, 22), 17);
            _cells[tier][i] = c; c.Root.SetActive(false);
        }
        _pointer = GameplayUi.Rect(_root, "RelativePointer", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 8);
        var dot = _pointer.gameObject.AddComponent<Image>(); dot.color = new Color(1, .86f, .4f); dot.raycastTarget = false;
        _hint = Label(_root, "Hint", new Vector2(0, -320), new Vector2(700, 60), 20);
    }
    static TextMeshProUGUI Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = GameplayUi.Rect(parent, name, Vector2.one * .5f, Vector2.one * .5f, position, size);
        return GameplayUi.Text(rect, "Text", "", fontSize);
    }
    public void Render(RadialMenuViewState state)
    {
        Initialize(); _secondaryTarget = state.IsSecondary;
        Draw(0, state.Primary, state.SelectedPrimary);
        Draw(1, state.Secondary, state.SelectedSecondary);
        _pointer.anchoredPosition = state.PointerPixels;
        _hint.text = state.Hint;
    }
    void Draw(int tier, RadialOptionViewState[] options, int selected)
    {
        int count = options == null ? 0 : Mathf.Min(12, options.Length);
        for (int i = 0; i < 12; i++)
        {
            var c = _cells[tier][i]; c.Root.SetActive(i < count);
            if (i >= count) continue;
            var option = options[i]; float angle = i * 360f / count;
            c.Wedge.Configure(tier == 0 ? 80 : 185, tier == 0 ? 180 : 280, angle, 360f / count - 3,
                i == selected ? new Color(.7f, .48f, .12f, .96f) : option.Enabled ? new Color(.07f, .14f, .23f, .96f) : new Color(.13f, .14f, .16f, .95f));
            float radius = tier == 0 ? 133 : 234;
            c.LabelRoot.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;
            c.Name.text = option.Name; c.Name.color = option.Enabled ? Color.white : Color.gray;
            c.Count.text = option.CountText; c.Key.text = option.ShortcutText;
            c.Key.gameObject.SetActive(!string.IsNullOrEmpty(option.ShortcutText));
            c.Icon.sprite = option.Icon; c.Icon.gameObject.SetActive(option.Icon != null);
        }
    }
    void Update()
    {
        if (!_initialized) return;
        _blend = Mathf.MoveTowards(_blend, _secondaryTarget ? 1 : 0, Time.unscaledDeltaTime / .16f);
        _primary.localScale = Vector3.one * Mathf.Lerp(1, .58f, _blend);
        _secondary.gameObject.SetActive(_blend > 0);
        _secondary.localScale = Vector3.one * Mathf.Lerp(.65f, 1, _blend);
        var canvas = GetComponentInParent<Canvas>();
        var size = canvas == null ? new Vector2(Screen.width, Screen.height) : ((RectTransform)canvas.transform).rect.size;
        _root.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(size.x / 730, size.y / 740));
    }
}
