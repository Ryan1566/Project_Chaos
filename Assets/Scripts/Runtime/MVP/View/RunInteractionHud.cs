using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Actor-owned input/view adapter. All target selection and execution belongs to the model/domain.</summary>
public sealed class RunInteractionHud : MonoBehaviour, IRunInteractionView
{
    private RunInteractionPresenter presenter;
    private GameObject canvasRoot;
    private TextMeshProUGUI label;
    private void Start()
    {
        canvasRoot = new GameObject("RunInteractionHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GameplayCanvas));
        // Overlay coordinates must not inherit the actor's facing scale or world motion.
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
        var scaler = canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var box = GameplayUi.Rect(canvasRoot.transform, "Prompt", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 72), new Vector2(1000, 70));
        label = GameplayUi.Text(box, "Text", "", 26);
        presenter = new RunInteractionPresenter(new RunInteractionModel(gameObject)); presenter.Attach(this);
    }
    private void Update()
    { presenter?.Tick(InputManager.Instance.WasGameplayPressed(InputManager.ActionInteract)); }
    public void Render(RunInteractionState state)
    {
        if (label == null) return;
        label.gameObject.SetActive(state.Visible); label.text = state.Text ?? string.Empty;
        label.color = state.Available ? Color.white : new Color(.8f, .8f, .8f);
    }
    private void OnDisable() { if (canvasRoot != null) canvasRoot.SetActive(false); }
    private void OnEnable() { if (canvasRoot != null) canvasRoot.SetActive(true); }
    private void OnDestroy() { presenter?.Dispose(); if (canvasRoot != null) Destroy(canvasRoot); }
}
