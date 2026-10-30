using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small scene HUD, never a root panel. Movement remains in the physical village.</summary>
public sealed class VillageHud : MonoBehaviour, IVillageWorldView
{
    private VillageWorldPresenter presenter;
    private GameObject root;
    private TextMeshProUGUI title, resources, hint, mapLabel, settingsLabel, mainMenuLabel;
    private float nextRefresh;
    private float messageUntil;
    private void Start()
    {
        root = new GameObject("VillageHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameplayCanvas), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080);
        title = TextArea("Title", new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(400, 60), 30);
        resources = TextArea("Resources", new Vector2(0, 1), new Vector2(445, -86), new Vector2(860, 65), 22);
        hint = TextArea("Hint", new Vector2(.5f, 0), new Vector2(0, 70), new Vector2(1200, 75), 26);
        mapLabel = TextArea("MapHint", new Vector2(1, 1), new Vector2(-200, -40), new Vector2(360, 55), 24);
        var settings = GameplayUi.Button(root.transform, "VillageSettings", "", Vector2.zero, new Vector2(150, 48), () => presenter.Handle(VillageIntent.Settings));
        settingsLabel = settings.GetComponentInChildren<TextMeshProUGUI>();
        PlaceButton(settings, new Vector2(1, 1), new Vector2(-310, -105));
        var menu = GameplayUi.Button(root.transform, "VillageTitle", "", Vector2.zero, new Vector2(180, 48), () => presenter.Handle(VillageIntent.Title));
        mainMenuLabel = menu.GetComponentInChildren<TextMeshProUGUI>();
        PlaceButton(menu, new Vector2(1, 1), new Vector2(-125, -105));
        foreach (var label in new[] { settingsLabel, mainMenuLabel })
        { label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 22; label.enableWordWrapping = false; }
        presenter = new VillageWorldPresenter(); presenter.Attach(this);
    }
    private TextMeshProUGUI TextArea(string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = GameplayUi.Rect(root.transform, name, anchor, new Vector2(.5f, .5f), position, size);
        return GameplayUi.Text(rect, "Text", "", fontSize);
    }
    private static void PlaceButton(Button button, Vector2 anchor, Vector2 position)
    { var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; }
    private void Update()
    {
        if (presenter == null) return;
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .15f; presenter.Refresh(); }
        if (InputManager.Instance.WasGameplayPressed(InputManager.ActionVillageInteract)) presenter.Handle(VillageIntent.Manage);
        // World UI owns input and rendering while open; this HUD never claims an input blocker.
        if (root != null)
        { var group = root.GetComponent<CanvasGroup>(); bool visible = UIManager.Instance.panelStack.Count == 0; group.alpha = visible ? 1 : 0; group.interactable = group.blocksRaycasts = visible; }
    }
    public void Render(VillageWorldState state)
    {
        title.text = state.Title; resources.text = state.Resources;
        if (Time.unscaledTime >= messageUntil) hint.text = state.Hint;
        mapLabel.text = state.MapLabel;
        settingsLabel.text = state.SettingsLabel;
        mainMenuLabel.text = state.MainMenuLabel;
    }
    public void ShowMessage(string text) { hint.text = text; messageUntil = Time.unscaledTime + 4; }
    private void OnDestroy() { presenter?.Dispose(); if (root != null) Destroy(root); }
}
