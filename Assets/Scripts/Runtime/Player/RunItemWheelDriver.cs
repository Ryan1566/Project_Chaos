using Chaos.Gameplay.Items;
using Chaos.MVP.Presenter.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Owns input and pause for the inventory provider; the view/model never poll devices.</summary>
[DefaultExecutionOrder(-100)]
public sealed class RunItemWheelDriver : MonoBehaviour
{
    RadialMenuPresenter _presenter;
    GameObject _overlay;
    bool _open, _hold, _cursorVisible;
    CursorLockMode _cursorLock;
    float _previousScale;
    int _openedFrame, _context;
    public bool IsOpen => _open;
    public string LastMessage { get; private set; }
    void Update()
    {
        RunItemUseService.Tick(Time.deltaTime);
        var input = InputManager.Instance;
        if (_open)
        {
            if (!RunItemUseService.IsReady || _context != GameSession.Instance.ContextId || input.HasGameplayBlockerOtherThan(this)
                || RunItemUseService.Actor == null || RunItemUseService.Actor.IsDowned) { Close(); return; }
            if (_hold && !input.IsNavigationHeld(InputManager.ActionItemWheel)) { Close(); return; }
            if (Time.frameCount == _openedFrame) return;
            if ((!_hold && input.WasNavigationPressed(InputManager.ActionItemWheel))
                || input.WasNavigationPressed(InputManager.ActionMenu) || Mouse.current?.rightButton.wasPressedThisFrame == true)
            { Close(); return; }
            if (Keyboard.current?.backspaceKey.wasPressedThisFrame == true) _presenter.Back();
            if (!_presenter.Model.IsOpen) { Close(); return; }
            var delta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
            _presenter.Move(delta.x, delta.y, Time.unscaledDeltaTime);
            if (Keyboard.current != null) for (int i = 1; i <= 7; i++)
                if (Keyboard.current[(Key)((int)Key.Digit1 + i - 1)].wasPressedThisFrame) _presenter.Shortcut(i);
            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                bool used = _presenter.Confirm(out string message); LastMessage = message;
                if (!_presenter.Model.IsOpen) { Close(); if (!used && !string.IsNullOrEmpty(message)) Debug.Log("[道具] " + message); }
            }
            return;
        }
        if (RunItemUseService.IsReady && input.CanReadGameplayInput && RunItemUseService.Actor != null
            && !RunItemUseService.Actor.IsDowned && input.WasNavigationPressed(InputManager.ActionItemWheel)) Open();
    }
    public void Open()
    {
        if (_open || !RunItemUseService.IsReady || !InputManager.Instance.CanReadGameplayInput) return;
        _context = GameSession.Instance.ContextId;
        _hold = SettingsManager.Instance.Applied.itemWheelMode != (int)ItemWheelTriggerMode.Toggle;
        _overlay = new GameObject("InventoryRadialOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = _overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 3000;
        var scaler = _overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        _presenter = new RadialMenuPresenter(new InventoryRadialProvider(), _overlay.AddComponent<RadialMenuView>(), InventoryRadialProvider.ResolveIcon);
        _presenter.Open(); _open = true; _openedFrame = Time.frameCount;
        _previousScale = Time.timeScale; Time.timeScale = 0;
        Chaos.MVP.Model.Gameplay.RunControlModel.PauseWorld(true);
        InputManager.Instance.SetGameplayInputBlocked(this, true);
        _cursorLock = Cursor.lockState; _cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }
    public void Close()
    {
        if (!_open) return;
        _open = false; _presenter.Model.Close();
        if (_overlay != null) Destroy(_overlay);
        Time.timeScale = _previousScale;
        Chaos.MVP.Model.Gameplay.RunControlModel.PauseWorld(false);
        InputManager.Instance.SetGameplayInputBlocked(this, false);
        Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible;
    }
    void OnApplicationFocus(bool focused) { if (!focused) Close(); }
    void OnDisable() { Close(); }
}
