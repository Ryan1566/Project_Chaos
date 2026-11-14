using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Passive in-run content panel (NPC dialogue / chest / gathering) built on the BasePanel root transform.
/// Display only: no presenter/domain/inventory access, no input polling, timers or global subscriptions.
/// Every button emits at most one intent per click; rows are pooled and reused across renders.
/// </summary>
public sealed class RunContentView : MonoBehaviour
{
    public event Action<int> ClaimRequested;
    public event Action CloseRequested;

    private const float CardWidth = 920f, CardHeight = 620f;
    private const float Margin = 24f, HeaderGap = 16f, SectionGap = 12f;
    private const float PortraitSize = 180f, PortraitGap = 20f;
    private const float BodyGap = 8f;
    private const float FooterHeight = 60f;
    private const float CloseWidth = 208f, CloseRightMargin = 32f;
    private const float TitleSize = 30f, BodySize = 21f, HintSize = 19f;
    private const float ChoiceHeight = 96f, ChoiceGap = 8f;
    private const float RowInset = 6f, ScrollBarReserve = 12f;
    private const float ActionWidth = 124f, StatusWidth = 96f;

    private sealed class ChoiceRow
    {
        public RectTransform Root, IconFrame, NameArea, DetailArea, CountArea, StateArea, ActionArea;
        public Image Frame, Icon, ActionBackground;
        public Button Button;
        public TextMeshProUGUI Name, Detail, Count, State, ButtonText;
    }

    private readonly List<ChoiceRow> _rows = new List<ChoiceRow>();
    private RectTransform _card, _portraitArea, _titleArea, _bodyArea, _hintArea, _scrollArea, _scrollContent, _closeArea;
    private Image _cardImage, _portrait, _scrollBackground, _closeBackground;
    private ScrollRect _scroll;
    private TextMeshProUGUI _title, _body, _hint, _closeText;
    private Button _close;
    private bool _initialized;
    private int[] _choiceIds = Array.Empty<int>();

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _card = GameplayUi.Rect(transform, "ContentCard", Vector2.one * .5f, Vector2.one * .5f,
            Vector2.zero, new Vector2(CardWidth, CardHeight));
        _cardImage = _card.gameObject.AddComponent<Image>();
        GameplayUiSkin.Apply(_cardImage, "UI_DialogueBox", GameplayUiSkin.DarkPaper);

        _portraitArea = GameplayUi.Rect(_card, "PortraitArea", Vector2.up, Vector2.up,
            new Vector2(Margin, -Margin), new Vector2(PortraitSize, PortraitSize));
        var portraitBack = _portraitArea.gameObject.AddComponent<Image>();
        GameplayUiSkin.Apply(portraitBack, "UI_Portrait_Bust", GameplayUiSkin.DarkPaper);
        portraitBack.raycastTarget = false;
        var portrait = GameplayUi.Rect(_portraitArea, "Portrait", Vector2.one * .5f, Vector2.one * .5f,
            Vector2.zero, new Vector2(PortraitSize - 12f, PortraitSize - 12f));
        _portrait = portrait.gameObject.AddComponent<Image>();
        _portrait.preserveAspect = true;
        _portrait.raycastTarget = false;

        _title = RowLabel(_card, "Title", TitleSize, TextAlignmentOptions.Left, out _titleArea);
        _title.overflowMode = TextOverflowModes.Ellipsis;

        _body = RowLabel(_card, "Body", BodySize, TextAlignmentOptions.TopLeft, out _bodyArea);
        _body.enableWordWrapping = true;
        _body.overflowMode = TextOverflowModes.Ellipsis;

        _scrollArea = GameplayUi.Rect(_card, "ChoiceScroll", Vector2.up, Vector2.up,
            new Vector2(Margin, -Margin), new Vector2(CardWidth - Margin * 2f, ChoiceHeight));
        _scrollBackground = _scrollArea.gameObject.AddComponent<Image>();
        GameplayUiSkin.Apply(_scrollBackground, "UI_Panel_ListBody", GameplayUiSkin.DarkPaper);
        _scrollBackground.raycastTarget = true;
        _scrollArea.gameObject.AddComponent<RectMask2D>();
        _scroll = _scrollArea.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = _scrollArea;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 24f;
        _scrollContent = GameplayUi.Rect(_scrollArea, "Content", Vector2.up, Vector2.up,
            Vector2.zero, new Vector2(CardWidth - Margin * 2f, 0f));
        _scroll.content = _scrollContent;

        _hintArea = GameplayUi.Rect(_card, "HintArea", Vector2.up, Vector2.up,
            new Vector2(Margin, -(CardHeight - Margin - FooterHeight)), new Vector2(CardWidth - Margin - CloseRightMargin - CloseWidth - HeaderGap, FooterHeight));
        _hint = GameplayUi.Text(_hintArea, "Hint", string.Empty, HintSize);
        _hint.alignment = TextAlignmentOptions.Left;
        _hint.enableWordWrapping = true;
        _hint.color = new Color(1f, .83f, .42f);

        _closeArea = GameplayUi.Rect(_card, "Close", Vector2.up, Vector2.up,
            new Vector2(CardWidth - CloseRightMargin - CloseWidth, -(CardHeight - Margin - FooterHeight)),
            new Vector2(CloseWidth, FooterHeight));
        _closeBackground = _closeArea.gameObject.AddComponent<Image>();
        _closeBackground.color = new Color(.18f, .32f, .48f, 1);
        _close = _closeArea.gameObject.AddComponent<Button>();
        _close.targetGraphic = _closeBackground;
        GameplayUiSkin.Apply(_closeBackground, "UI_Btn_Md_Normal", GameplayUiSkin.DarkPaper);
        _close.onClick.AddListener(OnCloseClicked);
        _closeText = GameplayUi.Text(_closeArea, "Text", string.Empty, 22);
    }

    public void Render(RunContentViewState state)
    {
        Initialize();
        bool hasState = state != null;

        _title.text = hasState ? state.Title ?? string.Empty : string.Empty;
        _title.gameObject.SetActive(!string.IsNullOrEmpty(_title.text));
        _body.text = hasState ? state.Body ?? string.Empty : string.Empty;
        _body.gameObject.SetActive(!string.IsNullOrEmpty(_body.text));
        _hint.text = hasState ? state.Hint ?? string.Empty : string.Empty;
        _hintArea.gameObject.SetActive(!string.IsNullOrEmpty(_hint.text));

        string closeText = hasState ? state.CloseText ?? string.Empty : string.Empty;
        _closeText.text = closeText;
        _closeArea.gameObject.SetActive(!string.IsNullOrEmpty(closeText));

        bool hasPortrait = hasState && state.Portrait != null;
        _portraitArea.gameObject.SetActive(hasPortrait);
        _portrait.sprite = hasPortrait ? state.Portrait : null;
        _portrait.enabled = hasPortrait;

        float contentTop = LayoutHeader(hasPortrait);
        float scrollHeight = CardHeight - Margin - FooterHeight - SectionGap - contentTop;
        if (scrollHeight < ChoiceHeight + HeaderGap) scrollHeight = ChoiceHeight + HeaderGap;
        RenderChoices(hasState ? state.Choices : null, contentTop, scrollHeight);
    }

    /// <summary>Stacks title/body under an optional portrait and returns the top of the choice area.</summary>
    private float LayoutHeader(bool hasPortrait)
    {
        float cursor = Margin;
        float contentLeft = Margin;
        float contentWidth = CardWidth - Margin * 2f;

        if (hasPortrait)
        {
            contentLeft = Margin + PortraitSize + PortraitGap;
            contentWidth = CardWidth - CloseRightMargin - contentLeft;
        }

        bool hasTitle = _title.gameObject.activeSelf;
        bool hasBody = _body.gameObject.activeSelf;

        if (hasTitle)
        {
            float titleHeight = Measure(_title, contentWidth, TitleSize + 20f);
            Pin(_titleArea, contentLeft, cursor, contentWidth, titleHeight);
            cursor += titleHeight + (hasBody ? BodyGap : HeaderGap);
        }

        if (hasBody)
        {
            float bodyHeight = Measure(_body, contentWidth, 150f);
            Pin(_bodyArea, contentLeft, cursor, contentWidth, bodyHeight);
            cursor += bodyHeight + HeaderGap;
        }

        if (hasPortrait) cursor = Mathf.Max(cursor, Margin + PortraitSize + HeaderGap);
        return cursor;
    }

    private void RenderChoices(RunContentChoiceState[] choices, float scrollTop, float scrollHeight)
    {
        float viewportWidth = CardWidth - Margin * 2f;
        _scrollArea.anchoredPosition = new Vector2(Margin, -scrollTop);
        _scrollArea.sizeDelta = new Vector2(viewportWidth, scrollHeight);

        int count = choices == null ? 0 : choices.Length;
        bool sameChoices = _choiceIds.Length == count;
        for (int i = 0; sameChoices && i < count; i++) sameChoices = _choiceIds[i] == choices[i].Index;
        float previousScroll = _scroll.verticalNormalizedPosition;
        float rowWidth = viewportWidth - RowInset * 2f - ScrollBarReserve;
        for (int i = 0; i < count; i++)
        {
            var row = Row(i);
            row.Root.sizeDelta = new Vector2(rowWidth, ChoiceHeight);
            row.Root.anchoredPosition = new Vector2(RowInset, -i * (ChoiceHeight + ChoiceGap));
            RenderChoice(row, choices[i], rowWidth);
        }
        for (int i = count; i < _rows.Count; i++) _rows[i].Root.gameObject.SetActive(false);

        float contentHeight = count <= 0 ? 0f : count * ChoiceHeight + (count - 1) * ChoiceGap;
        _scrollContent.sizeDelta = new Vector2(viewportWidth, contentHeight);

        bool scrollable = contentHeight > scrollHeight + 1f;
        _scroll.vertical = scrollable;
        _scroll.enabled = scrollable;
        _scrollBackground.enabled = count > 0;
        if (scrollable) _scroll.verticalNormalizedPosition = sameChoices ? previousScroll : 1f;
        else _scrollContent.anchoredPosition = Vector2.zero;
        _choiceIds = new int[count];
        for (int i = 0; i < count; i++) _choiceIds[i] = choices[i].Index;
    }

    private void LayoutRow(ChoiceRow row, float rowWidth)
    {
        Pin(row.IconFrame, 12f, 20f, 56f, 56f);

        float rightColumn = rowWidth - 10f - ActionWidth - StatusWidth - 16f;
        float nameLeft = 80f, nameWidth = Mathf.Max(140f, rightColumn - nameLeft);
        Pin(row.NameArea, nameLeft, 14f, nameWidth, 28f);
        Pin(row.CountArea, rightColumn, 14f, StatusWidth, 28f);
        Pin(row.StateArea, rightColumn, 46f, StatusWidth, 26f);
        Pin(row.DetailArea, nameLeft, 46f, rightColumn - nameLeft, 38f);
        Pin(row.ActionArea, rowWidth - 10f - ActionWidth, 14f, ActionWidth, ChoiceHeight - 28f);
    }

    private void RenderChoice(ChoiceRow row, RunContentChoiceState choice, float rowWidth)
    {
        row.Root.gameObject.SetActive(true);

        row.Name.text = choice.Name ?? string.Empty;
        row.Detail.text = choice.Detail ?? string.Empty;
        row.Detail.gameObject.SetActive(!string.IsNullOrEmpty(row.Detail.text));
        row.Count.text = string.Empty;
        row.Count.gameObject.SetActive(false);

        bool hasIcon = choice.Icon != null;
        row.Icon.sprite = choice.Icon;
        row.Icon.enabled = hasIcon;

        string stateText = choice.ButtonText ?? string.Empty;
        row.State.text = stateText;
        row.State.gameObject.SetActive(!string.IsNullOrEmpty(stateText));
        row.ButtonText.text = stateText;
        row.ActionArea.gameObject.SetActive(!string.IsNullOrEmpty(stateText));

        bool claimed = choice.Claimed;
        bool enabled = choice.Enabled && !claimed;

        row.Name.color = enabled ? Color.white : new Color(.58f, .61f, .65f);
        row.Detail.color = enabled ? new Color(.78f, .82f, .86f) : new Color(.46f, .49f, .53f);
        row.State.color = claimed ? new Color(1f, .83f, .42f) : new Color(.55f, .58f, .62f);
        row.Frame.color = enabled ? new Color(.20f, .33f, .39f)
            : claimed ? new Color(.11f, .14f, .16f)
            : new Color(.15f, .17f, .19f);
        row.ActionBackground.color = enabled ? new Color(.18f, .32f, .48f) : new Color(.16f, .18f, .20f);
        row.ButtonText.color = enabled ? Color.white : new Color(.5f, .53f, .57f);
        row.Button.interactable = enabled;

        LayoutRow(row, rowWidth);

        // Stable identity across pooled slots: always emit the choice's own Index, never the slot number.
        int choiceIndex = choice.Index;
        row.Button.onClick.RemoveAllListeners();
        row.Button.onClick.AddListener(() => Claim(choiceIndex));
        row.Root.name = "Claim" + choiceIndex;
    }

    private ChoiceRow Row(int slot)
    {
        while (_rows.Count <= slot)
        {
            var row = new ChoiceRow();
            row.Root = GameplayUi.Rect(_scrollContent, "Claim" + _rows.Count, Vector2.up, Vector2.up,
                Vector2.zero, new Vector2(800f, ChoiceHeight));
            row.Frame = row.Root.gameObject.AddComponent<Image>();
            GameplayUiSkin.Apply(row.Frame, "UI_Row", GameplayUiSkin.DarkPaper);
            row.Button = row.Root.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = row.Frame;

            row.IconFrame = GameplayUi.Rect(row.Root, "IconFrame", Vector2.up, Vector2.up,
                new Vector2(12f, -20f), new Vector2(56f, 56f));
            var iconBack = row.IconFrame.gameObject.AddComponent<Image>();
            GameplayUiSkin.Apply(iconBack, "UI_Slot", GameplayUiSkin.DarkPaper);
            iconBack.raycastTarget = false;
            var iconRect = GameplayUi.Rect(row.IconFrame, "Icon", Vector2.one * .5f, Vector2.one * .5f,
                Vector2.zero, new Vector2(44f, 44f));
            row.Icon = iconRect.gameObject.AddComponent<Image>();
            row.Icon.preserveAspect = true;
            row.Icon.raycastTarget = false;

            row.Name = RowLabel(row.Root, "Name", 22, TextAlignmentOptions.Left, out row.NameArea);
            row.Count = RowLabel(row.Root, "Count", 18, TextAlignmentOptions.Right, out row.CountArea);
            row.Count.color = new Color(.85f, .88f, .92f);
            row.State = RowLabel(row.Root, "State", 16, TextAlignmentOptions.Right, out row.StateArea);
            row.Detail = RowLabel(row.Root, "Detail", 17, TextAlignmentOptions.TopLeft, out row.DetailArea);
            row.Detail.enableWordWrapping = true;
            row.Detail.color = new Color(.78f, .82f, .86f);

            row.ActionArea = GameplayUi.Rect(row.Root, "ActionArea", Vector2.up, Vector2.up,
                Vector2.zero, new Vector2(ActionWidth, ChoiceHeight - 28f));
            row.ActionBackground = row.ActionArea.gameObject.AddComponent<Image>();
            GameplayUiSkin.Apply(row.ActionBackground, "UI_Btn_Sm_Normal", GameplayUiSkin.DarkPaper);
            row.ActionBackground.raycastTarget = false;
            row.ButtonText = GameplayUi.Text(row.ActionArea, "Action", string.Empty, 17);
            row.ButtonText.enableWordWrapping = true;

            _rows.Add(row);
        }
        return _rows[slot];
    }

    private static TextMeshProUGUI RowLabel(Transform parent, string name, float size,
        TextAlignmentOptions alignment, out RectTransform area)
    {
        area = GameplayUi.Rect(parent, name + "Area", Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
        var label = GameplayUi.Text(area, name, string.Empty, size);
        label.alignment = alignment;
        return label;
    }

    /// <summary>Width must be applied before measuring so wrapped text reports the correct height.</summary>
    private static float Measure(TextMeshProUGUI label, float width, float maxHeight)
    {
        // Labels stretch in their Area. A positive width in the child's sizeDelta would
        // add that width again, shifting centered text outside the card.
        float height = Mathf.Ceil(label.GetPreferredValues(label.text, Mathf.Max(1f, width - 24f), Mathf.Infinity).y + 8f);
        return Mathf.Clamp(height, label.fontSize + 8f, Mathf.Max(label.fontSize + 8f, maxHeight));
    }

    private static void Pin(RectTransform rect, float left, float top, float width, float height)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void Claim(int index)
    {
        if (!isActiveAndEnabled) return;
        ClaimRequested?.Invoke(index);
    }

    private void OnCloseClicked()
    {
        if (!isActiveAndEnabled) return;
        CloseRequested?.Invoke();
    }
}
