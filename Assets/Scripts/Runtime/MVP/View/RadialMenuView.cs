using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>用 Sprite 显示轮盘。输入、确认和道具效果由 Presenter 与选项提供端处理。</summary>
public sealed class RadialMenuView : MonoBehaviour
{
    sealed class Cell
    {
        public GameObject Root;
        public Image Background, Icon;
        public RectTransform LabelRoot;
        public TextMeshProUGUI Name, Count, Key;
    }
    readonly Cell[][] _cells = { new Cell[RadialMenuSkin.MaxEntries], new Cell[RadialMenuSkin.MaxEntries] };
    RadialMenuSkin _skin;
    RectTransform _root, _primary, _secondary, _pointer;
    TextMeshProUGUI _hint;
    bool _initialized, _secondaryTarget;
    float _blend;
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _skin = Resources.Load<RadialMenuSkin>(RadialMenuSkin.ResourcePath);
        if (_skin == null) Debug.LogError("缺少快捷轮盘皮肤：" + RadialMenuSkin.ResourcePath, this);
        _root = GameplayUi.Rect(transform, "WheelCenter", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 620);
        _primary = GameplayUi.Rect(_root, "Primary", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
        _secondary = GameplayUi.Rect(_root, "Secondary", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
        for (int tier = 0; tier < _cells.Length; tier++) for (int i = 0; i < _cells[tier].Length; i++)
        {
            var rect = GameplayUi.Rect(tier == 0 ? _primary : _secondary, "Entry" + i, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
            var backgroundRect = GameplayUi.Rect(rect, "Background", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 600);
            var c = new Cell { Root = rect.gameObject, Background = backgroundRect.gameObject.AddComponent<Image>() };
            c.Background.raycastTarget = false;
            c.LabelRoot = GameplayUi.Rect(rect, "Label", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(100, 90));
            var iconRect = GameplayUi.Rect(c.LabelRoot, "Icon", Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 17), Vector2.one * 30);
            c.Icon = iconRect.gameObject.AddComponent<Image>(); c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            c.Name = Label(c.LabelRoot, "Name", new Vector2(0, -12), new Vector2(112, 38), 17);
            c.Count = Label(c.LabelRoot, "Count", new Vector2(0, -34), new Vector2(90, 20), 15);
            c.Key = Label(c.LabelRoot, "Shortcut", new Vector2(0, 43), new Vector2(50, 22), 17);
            _cells[tier][i] = c; c.Root.SetActive(false);
        }
        _pointer = GameplayUi.Rect(_root, "RelativePointer", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 8);
        var dot = _pointer.gameObject.AddComponent<Image>();
        if (_skin != null) { dot.sprite = _skin.Pointer; dot.color = _skin.PointerTint; }
        dot.raycastTarget = false;
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
        int count = options == null ? 0 : Mathf.Min(_cells[tier].Length, options.Length);
        for (int i = 0; i < _cells[tier].Length; i++)
        {
            var c = _cells[tier][i]; c.Root.SetActive(i < count);
            if (i >= count) continue;
            var option = options[i]; float angle = i * 360f / count;
            Color tint = Color.white;
            c.Background.sprite = _skin == null ? null : _skin.GetSlice(tier, count, i == selected, option.Enabled, out tint);
            c.Background.enabled = c.Background.sprite != null;
            c.Background.color = tint;
            // 只旋转背景扇区。图标和文字始终保持正向。
            c.Background.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
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
