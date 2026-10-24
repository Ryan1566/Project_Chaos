using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class InputManager
{
    public const string ActionCamp = "Camp";
    public const string ActionMenu = "Menu";
    public const string ActionInteract = "Interact";
    public const string GroupKeyboardFixed = "KeyboardFixed";
    private readonly HashSet<object> _gameplayBlockers = new HashSet<object>();
    private readonly List<RaycastResult> _pointerHits = new List<RaycastResult>();
    private int _inputBoundaryFrame = -1;
    private bool _suppressHeldAttack;
    public event Action<bool> GameplayInputBlockedChanged;
    public bool GameplayInputBlocked => _gameplayBlockers.Count != 0 || IsRebinding;
    public bool CanReadGameplayInput => !GameplayInputBlocked && Time.frameCount > _inputBoundaryFrame;

    /// <summary>Independent UI owners must release only their own block.</summary>
    public void SetGameplayInputBlocked(object owner, bool blocked)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        bool before = GameplayInputBlocked;
        if (blocked) _gameplayBlockers.Add(owner); else _gameplayBlockers.Remove(owner);
        InvalidateGameplayInput();
        if (before != GameplayInputBlocked) GameplayInputBlockedChanged?.Invoke(GameplayInputBlocked);
    }

    public bool HasGameplayBlockerOtherThan(object owner)
    {
        foreach (var blocker in _gameplayBlockers) if (!ReferenceEquals(blocker, owner)) return true;
        return IsRebinding;
    }

    private void InvalidateGameplayInput()
    {
        _inputBoundaryFrame = Time.frameCount;
        var attack = _asset == null ? null : _asset.FindAction(MapName + "/" + ActionAttack, false);
        if (attack != null && attack.IsPressed()) _suppressHeldAttack = true;
    }

    private void RefreshGameplayInputLatches()
    {
        if (!_suppressHeldAttack || _asset == null) return;
        var attack = _asset.FindAction(MapName + "/" + ActionAttack, false);
        if (attack == null || !attack.IsPressed()) _suppressHeldAttack = false;
    }

    public float ReadMove()
    {
        if (!CanReadGameplayInput) return 0;
        var action = FindAction(ActionMove);
        if (action == null || !action.enabled) return 0;
        if (action.activeControl?.device is Pointer pointer && IsPointerOverUi(pointer)) return 0;
        return Mathf.Clamp(action.ReadValue<float>(), -1, 1);
    }

    public bool WasGameplayPressed(string actionName)
    {
        if (!CanReadGameplayInput) return false;
        var action = FindAction(actionName);
        if (action == null || !action.enabled || !action.WasPressedThisFrame()) return false;
        return !(action.activeControl?.device is Pointer pointer) || !IsPointerOverUi(pointer);
    }

    public bool IsPointerAttackHeld()
    {
        if (!CanReadGameplayInput || _suppressHeldAttack) return false;
        var action = FindAction(ActionAttack);
        return action != null && action.enabled && action.IsPressed()
            && action.activeControl?.device is Pointer pointer && !IsPointerOverUi(pointer);
    }

    /// <summary>Menu close remains readable in an overlay; its caller checks which UI owns the input.</summary>
    public bool WasNavigationPressed(string actionName)
    {
        if (IsRebinding || Time.frameCount <= _inputBoundaryFrame) return false;
        var action = FindAction(actionName);
        if (action == null || !action.enabled || !action.WasPressedThisFrame()) return false;
        return !(action.activeControl?.device is Pointer pointer) || !IsPointerOverUi(pointer);
    }

    private bool IsPointerOverUi(Pointer pointer)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        _pointerHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = pointer.position.ReadValue() }, _pointerHits);
        foreach (var hit in _pointerHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
}
