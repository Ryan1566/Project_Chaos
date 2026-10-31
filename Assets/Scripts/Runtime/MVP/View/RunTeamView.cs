using System;
using Chaos.Gameplay.RunTeam;
using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Passive uGUI Team tab. Purely a surface: renders the supplied <see cref="RunTeamState"/>
/// and raises request events. No gameplay logic, no service lookups, no polling, no per-frame work.
/// Layout is fixed at 1100 x 475 in top-left coordinates.
/// </summary>
public sealed class RunTeamView : MonoBehaviour, IRunTeamView
{
    public const int SlotCount = 4;
    const int StatCount = 3;

    // ---- layout constants (1100 x 475, top-left origin) ----
    const float PanelWidth = 1100f;
    const float PanelHeight = 475f;
    const float Margin = 18f;
    const float LeftWidth = 430f;
    const float Gutter = 16f;
    const float AvatarSize = 64f;
    const float AvatarGap = 10f;
    const float LeftInnerHeight = PanelHeight - Margin * 2f;

    /// <summary>Readable fallback when a member has Spine data but no usable material.</summary>
    public const string MissingMaterialText = "立绘不可用";

    static readonly Color PanelColor = new Color(.09f, .10f, .13f, .96f);
    static readonly Color LeftColor = new Color(.12f, .13f, .17f, 1f);
    static readonly Color CardColor = new Color(.16f, .18f, .23f, 1f);
    static readonly Color SubTextColor = new Color(.78f, .82f, .90f, 1f);
    static readonly Color AccentColor = new Color(1f, .78f, .22f, 1f);
    static readonly Color ControlledColor = new Color(.45f, .70f, 1f, 1f);
    static readonly Color DownedColor = new Color(.52f, .20f, .20f, 1f);
    static readonly Color DisabledColor = new Color(.70f, .70f, .70f, .55f);
    static readonly Color HealthColor = new Color(.86f, .24f, .24f, 1f);
    static readonly Color StaminaColor = new Color(.30f, .78f, .34f, 1f);
    static readonly Color ManaColor = new Color(.28f, .50f, .92f, 1f);
    static readonly Color XpColor = new Color(.92f, .78f, .30f, 1f);
    static readonly Color SlotColor = new Color(.20f, .22f, .28f, 1f);
    static readonly Color SlotSelectedColor = new Color(.34f, .42f, .24f, 1f);

    public event Action<uint> SelectRequested;
    public event Action<RunTeamAttribute> AllocateRequested;

    bool _initialized;
    bool _interactionEnabled = true;
    bool _hasSelected;
    RunTeamState _state;

    Image _background;

    // left column
    TextMeshProUGUI _nameLabel;
    TextMeshProUGUI _professionLabel;
    TextMeshProUGUI _controlledLabel;
    RunTeamSpinePreview _fullbodyPreview;
    TextMeshProUGUI _fullbodyInitials;
    TextMeshProUGUI _fullbodyStatus;
    readonly RectTransform[] _slotRects = new RectTransform[SlotCount];
    readonly Button[] _slotButtons = new Button[SlotCount];
    readonly Image[] _slotBackgrounds = new Image[SlotCount];
    readonly Image[] _slotPortraitImages = new Image[SlotCount];
    readonly Image[] _slotMarkers = new Image[SlotCount];
    readonly RunTeamSpinePreview[] _slotPreviews = new RunTeamSpinePreview[SlotCount];
    readonly TextMeshProUGUI[] _slotInitials = new TextMeshProUGUI[SlotCount];
    readonly TextMeshProUGUI[] _slotStatuses = new TextMeshProUGUI[SlotCount];

    // right column
    TextMeshProUGUI _levelLabel;
    Image _xpFill;
    RectTransform _xpFillRect;
    TextMeshProUGUI _xpLabel;
    TextMeshProUGUI _pointsLabel;
    readonly RectTransform[] _statFillRects = new RectTransform[StatCount];
    readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[StatCount];
    readonly Button[] _allocateButtons = new Button[StatCount];
    readonly TextMeshProUGUI[] _allocateLabels = new TextMeshProUGUI[StatCount];
    TextMeshProUGUI _messageLabel;

    bool ControlsBuilt
    {
        get { return _initialized && _nameLabel != null && _slotButtons[0] != null && _allocateButtons[0] != null; }
    }

    /// <summary>
    /// Builds the hierarchy once. Safe to call repeatedly and before Show is possible.
    /// The root keeps whatever anchor / pivot / position the owner assigned; only a missing
    /// size is filled in so the fixed 1100 x 475 layout has a canvas to draw on.
    /// </summary>
    public void Initialize()
    {
        if (ControlsBuilt) return;

        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        if (root.rect.width <= 0f || root.rect.height <= 0f)
            root.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        _background = MakeImage(transform, "Background", PanelColor);
        _background.raycastTarget = true; // swallows stray clicks; passive otherwise
        Stretch(_background.rectTransform);

        BuildLeftColumn();
        BuildRightColumn();

        _initialized = true;
        Render(_state);
    }

    /// <summary>Lets the owning presenter freeze the whole tab while it is unavailable.</summary>
    public void SetInteractionEnabled(bool enabled)
    {
        if (_interactionEnabled == enabled) return;
        _interactionEnabled = enabled;
        Render(_state);
    }

    /// <summary>Renders the supplied snapshot. Never mutates it, never polls.</summary>
    public void Render(RunTeamState state)
    {
        _state = state;
        if (!ControlsBuilt) return; // stored and applied once Initialize builds the hierarchy

        if (state == null)
        {
            ClearPresentation();
            return;
        }

        var members = state.Members != null ? state.Members : Array.Empty<RunTeamMemberState>();
        var selected = FindMember(members, state.SelectedId);

        RefreshSlots(members, state.SelectedId);
        RefreshSelected(selected, state.SpineMaterial);
        RefreshStats(selected, state.CanAllocate);
        _messageLabel.text = string.IsNullOrEmpty(state.Message) ? "" : state.Message;
    }

    // ---------------------------------------------------------------- layout

    void BuildLeftColumn()
    {
        var left = MakeImage(transform, "LeftPanel", LeftColor);
        left.raycastTarget = false;
        PlaceTopLeft(left.rectTransform, Margin, -Margin, LeftWidth, LeftInnerHeight);

        _nameLabel = MakeText(left.rectTransform, "Name", "", 26, TextAlignmentOptions.Left);
        PlaceTopLeft(_nameLabel.rectTransform, 14f, -10f, LeftWidth - 28f, 36f);

        _professionLabel = MakeText(left.rectTransform, "Profession", "", 18, TextAlignmentOptions.Left);
        _professionLabel.color = SubTextColor;
        PlaceTopLeft(_professionLabel.rectTransform, 14f, -46f, LeftWidth - 28f, 24f);

        _controlledLabel = MakeText(left.rectTransform, "ControlledTag", "", 16, TextAlignmentOptions.Right);
        _controlledLabel.color = ControlledColor;
        PlaceTopLeft(_controlledLabel.rectTransform, 14f, -12f, LeftWidth - 28f, 24f);

        // full-body preview viewport: clipped by RunTeamSpinePreview (masked + fitted)
        float previewTop = 76f;
        float previewHeight = LeftInnerHeight - previewTop - (AvatarSize + 20f);
        var host = GameplayUi.Rect(left.rectTransform, "FullbodyPreview", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(12f, -previewTop), new Vector2(LeftWidth - 24f, previewHeight));
        _fullbodyPreview = host.gameObject.AddComponent<RunTeamSpinePreview>();

        _fullbodyInitials = MakeText(host, "Initials", "", 76, TextAlignmentOptions.Center);
        Stretch(_fullbodyInitials.rectTransform);
        _fullbodyInitials.color = new Color(1f, 1f, 1f, .20f);

        // readable fallback shown when the member has a skeleton but no usable material;
        // sits above the initials band so both can be read at once
        _fullbodyStatus = MakeText(host, "Status", "", 16, TextAlignmentOptions.Bottom);
        _fullbodyStatus.color = new Color(1f, .72f, .55f, 1f);
        var statusRect = _fullbodyStatus.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(.5f, 0f);
        statusRect.offsetMin = new Vector2(6f, 34f);
        statusRect.offsetMax = new Vector2(-6f, 58f);

        // exactly four avatar selection buttons along the bottom edge
        float slotY = -(LeftInnerHeight - AvatarSize - 10f);
        for (int i = 0; i < SlotCount; i++)
        {
            int index = i;
            var slotRect = GameplayUi.Rect(left.rectTransform, "AvatarButton" + i, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(14f + i * (AvatarSize + AvatarGap), slotY), new Vector2(AvatarSize, AvatarSize));
            _slotRects[i] = slotRect;

            var bg = slotRect.gameObject.AddComponent<Image>();
            bg.color = SlotColor;
            bg.raycastTarget = true;
            _slotBackgrounds[i] = bg;

            var button = slotRect.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => OnSlotClicked(index));
            _slotButtons[i] = button;

            var portraitRect = GameplayUi.Rect(slotRect, "Portrait", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(AvatarSize - 10f, AvatarSize - 10f));
            var portraitImage = portraitRect.gameObject.AddComponent<Image>();
            portraitImage.raycastTarget = false;
            portraitImage.preserveAspect = true;
            portraitImage.enabled = false;
            _slotPortraitImages[i] = portraitImage;

            var previewHost = GameplayUi.Rect(slotRect, "PortraitSpine", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(AvatarSize - 10f, AvatarSize - 10f));
            _slotPreviews[i] = previewHost.gameObject.AddComponent<RunTeamSpinePreview>();

            _slotInitials[i] = MakeText(slotRect, "Initials", "", 24, TextAlignmentOptions.Center);
            Stretch(_slotInitials[i].rectTransform);
            _slotInitials[i].color = new Color(1f, 1f, 1f, .8f);

            var status = MakeText(slotRect, "Status", "", 10, TextAlignmentOptions.Bottom);
            status.color = new Color(1f, .72f, .55f, .9f);
            var avatarStatusRect = status.rectTransform;
            avatarStatusRect.anchorMin = new Vector2(0f, 0f);
            avatarStatusRect.anchorMax = new Vector2(1f, 0f);
            avatarStatusRect.pivot = new Vector2(.5f, 0f);
            avatarStatusRect.offsetMin = new Vector2(2f, 2f);
            avatarStatusRect.offsetMax = new Vector2(-2f, 18f);
            _slotStatuses[i] = status;

            var marker = MakeImage(slotRect, "SelectedMarker", AccentColor);
            var markerRect = marker.rectTransform;
            markerRect.anchorMin = new Vector2(0f, 0f);
            markerRect.anchorMax = new Vector2(1f, 0f);
            markerRect.pivot = new Vector2(.5f, 0f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = new Vector2(0f, 4f);
            marker.enabled = false;
            _slotMarkers[i] = marker;
        }
    }

    void BuildRightColumn()
    {
        float rightX = Margin + LeftWidth + Gutter;
        float rightWidth = PanelWidth - rightX - Margin;

        var card = GameplayUi.Rect(transform, "RightPanel", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(rightX, -Margin), new Vector2(rightWidth, LeftInnerHeight));
        var cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = CardColor;
        cardImage.raycastTarget = false;

        float inner = rightWidth - 28f;
        const float x = 14f;
        float y = -12f;

        _levelLabel = MakeText(card, "Level", "", 22, TextAlignmentOptions.Left);
        PlaceTopLeft(_levelLabel.rectTransform, x, y, inner, 30f);
        y -= 34f;

        var xpTrack = MakeImage(card, "XpTrack", new Color(0f, 0f, 0f, .55f));
        PlaceTopLeft(xpTrack.rectTransform, x, y, inner, 12f);
        // Sprite-less Image: a real left-anchored bar whose width is driven by the ratio,
        // so the geometry (not fillAmount) carries the value.
        _xpFill = MakeBar(xpTrack.rectTransform, "XpFill", XpColor, out _xpFillRect);
        y -= 20f;

        _xpLabel = MakeText(card, "Xp", "", 15, TextAlignmentOptions.Left);
        _xpLabel.color = SubTextColor;
        PlaceTopLeft(_xpLabel.rectTransform, x, y, inner, 20f);
        y -= 26f;

        _pointsLabel = MakeText(card, "Points", "", 18, TextAlignmentOptions.Left);
        _pointsLabel.color = AccentColor;
        PlaceTopLeft(_pointsLabel.rectTransform, x, y, inner, 24f);
        y -= 30f;

        string[] titles = { "生命", "耐力", "法力" };
        Color[] colors = { HealthColor, StaminaColor, ManaColor };
        const float rowHeight = 38f;
        for (int i = 0; i < StatCount; i++)
        {
            var row = GameplayUi.Rect(card, titles[i] + "Row", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(x, y), new Vector2(inner, rowHeight));
            y -= rowHeight + 6f;

            var label = MakeText(row, "Label", titles[i], 16, TextAlignmentOptions.Left);
            PlaceTopLeft(label.rectTransform, 0f, 0f, inner - 56f, 18f);
            _statLabels[i] = label;

            var track = MakeImage(row, "Track", new Color(0f, 0f, 0f, .55f));
            PlaceTopLeft(track.rectTransform, 0f, -20f, inner - 56f, 14f);

            RectTransform fillRect;
            MakeBar(track.rectTransform, "Fill", colors[i], out fillRect);
            _statFillRects[i] = fillRect;

            int index = i;
            var plus = GameplayUi.Button(row, "Allocate" + i, "+", new Vector2(inner - 48f, 2f),
                new Vector2(48f, 30f), () => OnAllocateClicked(index));
            _allocateButtons[i] = plus;
            _allocateLabels[i] = plus.GetComponentInChildren<TextMeshProUGUI>();
        }

        y -= 4f;

        var skillsTitle = MakeText(card, "SkillsTitle", "技能", 18, TextAlignmentOptions.Left);
        skillsTitle.color = SubTextColor;
        PlaceTopLeft(skillsTitle.rectTransform, x, y, inner, 22f);
        y -= 26f;

        float remaining = -y - 34f;
        float boxHeight = Mathf.Clamp(remaining, 40f, 110f);
        var skillsBox = MakeImage(card, "SkillsPlaceholder", new Color(0f, 0f, 0f, .35f));
        PlaceTopLeft(skillsBox.rectTransform, x, y, inner, boxHeight);

        var placeholder = MakeText(skillsBox.rectTransform, "PlaceholderText",
            "技能占位\n耐力／法力消耗与技能后续开放", 15, TextAlignmentOptions.Center);
        Stretch(placeholder.rectTransform);
        placeholder.color = new Color(.70f, .74f, .80f, 1f);

        _messageLabel = MakeText(card, "Message", "", 15, TextAlignmentOptions.Left);
        _messageLabel.color = new Color(1f, .62f, .45f, 1f);
        PlaceTopLeft(_messageLabel.rectTransform, x, -(LeftInnerHeight - 30f), inner, 20f);
    }

    // ---------------------------------------------------------------- render

    void OnEnable()
    {
        // No global subscriptions; simply re-apply the last snapshot so previews resurface.
        Render(_state);
    }

    void ClearPresentation()
    {
        _hasSelected = false;

        _nameLabel.text = "";
        _professionLabel.text = "";
        _controlledLabel.text = "";
        _levelLabel.text = "";
        _xpLabel.text = "";
        _pointsLabel.text = "";
        _messageLabel.text = "";

        SetPreview(_fullbodyPreview, null, null, null, false);
        if (_fullbodyInitials != null) _fullbodyInitials.text = "";
        if (_fullbodyStatus != null) _fullbodyStatus.text = "";

        for (int i = 0; i < SlotCount; i++)
        {
            SetSlotEmpty(i);
            if (_slotButtons[i] != null) _slotButtons[i].interactable = false;
        }

        for (int i = 0; i < StatCount; i++)
        {
            SetBarRatio(_statFillRects[i], 0f);
            _statLabels[i].text = StatTitle(i) + " 上限 -";
            SetAllocate(i, false);
        }

        SetBarRatio(_xpFillRect, 0f);
    }

    void RefreshSlots(RunTeamMemberState[] members, uint selectedId)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var member = i < members.Length ? members[i] : null;
            bool has = member != null && member.Id != 0;
            if (!has)
            {
                SetSlotEmpty(i);
                _slotButtons[i].interactable = false;
                continue;
            }

            _slotButtons[i].interactable = _interactionEnabled;
            bool selected = member.Id == selectedId;
            _slotBackgrounds[i].color = selected ? SlotSelectedColor
                : member.Downed ? DownedColor
                : SlotColor;
            _slotMarkers[i].enabled = selected;
            _slotMarkers[i].color = member.Controlled ? ControlledColor : AccentColor;
            bool useSprite = member.Portrait != null;
            var portraitImage = _slotPortraitImages[i];
            portraitImage.sprite = useSprite ? member.Portrait : null;
            portraitImage.enabled = useSprite;

            // Slots are only refreshed when Render is given a state, so _state is non-null here.
            var material = _state != null ? _state.SpineMaterial : null;
            bool useSpine = !useSprite && member.SkeletonData != null && material != null;
            SetPreview(_slotPreviews[i], useSpine ? member.SkeletonData : null, member.Skin,
                useSpine ? material : null, true);

            // A skeleton without a usable material still gets a readable fallback.
            bool skeletonWithoutMaterial = !useSprite && member.SkeletonData != null && !useSpine;
            _slotInitials[i].text = !useSprite && !useSpine ? InitialsOf(member.Name) : "";
            _slotStatuses[i].text = skeletonWithoutMaterial ? MissingMaterialText : "";
        }
    }

    void SetSlotEmpty(int i)
    {
        _slotBackgrounds[i].color = SlotColor;
        _slotMarkers[i].enabled = false;
        _slotPortraitImages[i].sprite = null;
        _slotPortraitImages[i].enabled = false;
        SetPreview(_slotPreviews[i], null, null, null, true);
        _slotInitials[i].text = "";
        if (_slotStatuses[i] != null) _slotStatuses[i].text = "";
    }

    void RefreshSelected(RunTeamMemberState selected, Material material)
    {
        _hasSelected = selected != null;

        bool useSpine = selected != null && selected.SkeletonData != null && material != null;
        SetPreview(_fullbodyPreview, useSpine ? selected.SkeletonData : null, selected.Skin,
            useSpine ? material : null, false);

        bool skeletonWithoutMaterial = selected != null && selected.SkeletonData != null && !useSpine;
        bool useInitials = selected != null && !useSpine;
        _fullbodyInitials.text = useInitials ? InitialsOf(selected.Name) : "";
        if (_fullbodyStatus != null)
            _fullbodyStatus.text = skeletonWithoutMaterial ? MissingMaterialText : "";

        _nameLabel.text = selected != null ? (selected.Name ?? "") : "无队员";
        _professionLabel.text = selected != null ? (selected.Profession ?? "") : "";
        _controlledLabel.text = selected != null && selected.Controlled ? "场上控制" : "";
    }

    void RefreshStats(RunTeamMemberState selected, bool canAllocate)
    {
        bool has = selected != null;

        _levelLabel.text = has ? "Lv." + Mathf.Max(0, selected.Level) : "";

        float xpRatio = 0f;
        if (has && selected.ExperienceToNextLevel > 0)
            xpRatio = Mathf.Clamp01((float)selected.Experience / selected.ExperienceToNextLevel);
        SetBarRatio(_xpFillRect, xpRatio);
        _xpLabel.text = !has ? ""
            : selected.ExperienceToNextLevel > 0
                ? "经验 " + Mathf.Max(0, selected.Experience) + " / " + selected.ExperienceToNextLevel
                : "经验 " + Mathf.Max(0, selected.Experience) + "（已满）";

        int points = has ? Mathf.Max(0, selected.Points) : 0;
        _pointsLabel.text = has ? "可用点数：" + points : "";

        bool allow = has && canAllocate && _interactionEnabled && points > 0;

        for (int i = 0; i < StatCount; i++)
        {
            int max = has ? MaxOf(selected, i) : 0;
            // Health shows the current ratio; stamina/mana have no current value in the DTO so
            // their bars fill their maximums (full coloured bars).
            float ratio = !has ? 0f
                : i == 0 ? (max > 0 ? Mathf.Clamp01((float)Mathf.Clamp(selected.Health, 0, max) / max) : 0f)
                : 1f;
            SetBarRatio(_statFillRects[i], ratio);

            _statLabels[i].text = !has
                ? StatTitle(i) + " 上限 -"
                : i == 0 && max > 0
                    ? StatTitle(i) + " " + Mathf.Clamp(selected.Health, 0, max) + " / " + max
                    : StatTitle(i) + " 上限 " + max;

            SetAllocate(i, allow);
        }
    }

    void SetAllocate(int i, bool allow)
    {
        var button = _allocateButtons[i];
        if (button != null)
        {
            button.gameObject.SetActive(_hasSelected);
            button.interactable = allow;
        }
        if (_allocateLabels[i] != null)
            _allocateLabels[i].color = allow ? Color.white : DisabledColor;
    }

    /// <summary>
    /// Sprite-less filled bar: left-anchored, stretched vertically, and only offsetMax.x is
    /// driven by the value, so the rendered quad width always equals track width * ratio
    /// without relying on a sprite or on Image.fillAmount.
    /// </summary>
    static Image MakeBar(RectTransform track, string name, Color color, out RectTransform rect)
    {
        rect = GameplayUi.Rect(track, name, new Vector2(0f, 0f), new Vector2(0f, 0f),
            Vector2.zero, Vector2.zero);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0f, .5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void SetBarRatio(RectTransform bar, float ratio)
    {
        if (bar == null) return;

        float clamped = float.IsNaN(ratio) ? 0f : Mathf.Clamp01(ratio);
        bar.offsetMin = Vector2.zero;

        // offsetMax is relative to the track's right edge (anchorMax x == 1).
        var track = bar.parent as RectTransform;
        float width = track != null ? track.rect.width : 0f;
        bar.offsetMax = new Vector2(-width * (1f - clamped), 0f);
    }

    static int MaxOf(RunTeamMemberState m, int index)
    {
        switch (index)
        {
            case 0: return Mathf.Max(0, m.MaxHealth);
            case 1: return Mathf.Max(0, m.MaxStamina);
            default: return Mathf.Max(0, m.MaxMana);
        }
    }

    static string StatTitle(int index)
    {
        return index == 0 ? "生命" : index == 1 ? "耐力" : "法力";
    }

    // ---------------------------------------------------------------- input

    void OnSlotClicked(int index)
    {
        if (!_interactionEnabled || !ControlsBuilt) return;
        var state = _state;
        if (state == null || state.Members == null) return;
        if (index < 0 || index >= state.Members.Length || index >= SlotCount) return;

        var member = state.Members[index];
        if (member == null || member.Id == 0) return;
        if (!_slotButtons[index].interactable) return;

        var handler = SelectRequested;
        if (handler != null) handler(member.Id);
    }

    void OnAllocateClicked(int index)
    {
        if (!_interactionEnabled || !ControlsBuilt) return;
        var state = _state;
        if (state == null || !state.CanAllocate) return;
        if (index < 0 || index >= StatCount || !_allocateButtons[index].interactable) return;

        var selected = FindMember(state.Members, state.SelectedId);
        if (selected == null || selected.Points <= 0) return;

        RunTeamAttribute attribute;
        switch (index)
        {
            case 0: attribute = RunTeamAttribute.Health; break;
            case 1: attribute = RunTeamAttribute.Stamina; break;
            default: attribute = RunTeamAttribute.Mana; break;
        }

        var handler = AllocateRequested;
        if (handler != null) handler(attribute);
    }

    // ---------------------------------------------------------------- helpers

    static void SetPreview(RunTeamSpinePreview preview, Spine.Unity.SkeletonDataAsset asset, string skin,
        Material material, bool portrait)
    {
        if (preview != null) preview.Show(asset, skin, material, portrait);
    }

    static RunTeamMemberState FindMember(RunTeamMemberState[] members, uint id)
    {
        if (members == null || id == 0) return null;
        for (int i = 0; i < members.Length; i++)
        {
            var m = members[i];
            if (m != null && m.Id == id) return m;
        }
        return null;
    }

    static string InitialsOf(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        name = name.Trim();
        if (name.Length == 0) return "?";
        return name.Length >= 2 ? name.Substring(0, 2) : name;
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var rect = GameplayUi.Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size,
        TextAlignmentOptions alignment)
    {
        var label = GameplayUi.Text(parent, name, text, size);
        label.alignment = alignment;
        return label;
    }

    static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
