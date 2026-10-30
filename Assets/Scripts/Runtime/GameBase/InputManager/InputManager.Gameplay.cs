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
    public const string ActionMap = "Map";
    public const string ActionFunctions = "Functions";
    public const string ActionVillageMove = "VillageMove";
    public const string ActionVillageJump = "VillageJump";
    public const string ActionVillageInteract = "VillageInteract";
    public const string ActionVillageMap = "VillageMap";

    public enum BindingScope { Global, InRun, Outside }
    private BindingScope _lastScope;
    public BindingScope ActiveScope => !RunSceneFlow.IsTransitioning && RunMap.NaturalMapRuntime.IsReady
        ? BindingScope.InRun
        : VillageSceneFlow.IsVillageScene && !VillageSceneFlow.IsLoading && VillageSceneBootstrap.IsReady
          && GameSession.Instance.HasSession && !DispatchService.Instance.IsInRun
            ? BindingScope.Outside : BindingScope.Global;

    public static BindingScope ScopeOf(string actionName)
    {
        if (actionName == ActionMenu) return BindingScope.Global;
        if (actionName == ActionVillageMove || actionName == ActionVillageJump
            || actionName == ActionVillageInteract || actionName == ActionVillageMap) return BindingScope.Outside;
        return BindingScope.InRun;
    }

    public static bool ScopesOverlap(string first, string second)
    {
        var a = ScopeOf(first); var b = ScopeOf(second);
        return a == BindingScope.Global || b == BindingScope.Global || a == b;
    }

    private InputAction FindScopedAction(string name)
    {
        var scope = ActiveScope;
        if (scope == BindingScope.Outside)
        {
            if (name == ActionMove) name = ActionVillageMove;
            else if (name == ActionJump) name = ActionVillageJump;
            else if (name == ActionInteract) name = ActionVillageInteract;
            else if (name == ActionMap) name = ActionVillageMap;
        }
        var required = ScopeOf(name);
        return required == BindingScope.Global || required == scope ? FindAction(name) : null;
    }

    private void RefreshInputScope()
    {
        var scope = ActiveScope;
        if (_lastScope == scope) return;
        _lastScope = scope;
        InvalidateGameplayInput();
    }

    private void MigrateSharedVillageOverrides()
    {
        CopyOverrides(ActionMove, ActionVillageMove);
        CopyOverrides(ActionJump, ActionVillageJump);
        CopyOverrides(ActionInteract, ActionVillageInteract);
        CopyOverrides(ActionMap, ActionVillageMap);
    }

    private void CopyOverrides(string sourceName, string targetName)
    {
        var source = FindAction(sourceName); var target = FindAction(targetName);
        if (source == null || target == null) return;
        for (int i = 0; i < source.bindings.Count && i < target.bindings.Count; i++)
        {
            var binding = source.bindings[i];
            if (binding.hasOverrides) target.ApplyBindingOverride(i, new InputBinding {
                overridePath = binding.overridePath, overrideProcessors = binding.overrideProcessors,
                overrideInteractions = binding.overrideInteractions });
        }
    }
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
        var action = FindScopedAction(ActionMove);
        if (action == null || !action.enabled) return 0;
        if (action.activeControl?.device is Pointer pointer && IsPointerOverUi(pointer)) return 0;
        return Mathf.Clamp(action.ReadValue<float>(), -1, 1);
    }

    public bool WasGameplayPressed(string actionName)
    {
        if (!CanReadGameplayInput) return false;
        var action = FindScopedAction(actionName);
        if (action == null || !action.enabled || !action.WasPressedThisFrame()) return false;
        return !(action.activeControl?.device is Pointer pointer) || !IsPointerOverUi(pointer);
    }

    public bool IsPointerAttackHeld()
    {
        if (!CanReadGameplayInput || _suppressHeldAttack) return false;
        var action = FindScopedAction(ActionAttack);
        return action != null && action.enabled && action.IsPressed()
            && action.activeControl?.device is Pointer pointer && !IsPointerOverUi(pointer);
    }

    /// <summary>Menu close remains readable in an overlay; its caller checks which UI owns the input.</summary>
    public bool WasNavigationPressed(string actionName)
    {
        if (IsRebinding || Time.frameCount <= _inputBoundaryFrame) return false;
        var action = FindScopedAction(actionName);
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

    private void TickVillageMapNavigation()
    {
        if (!VillageSceneFlow.IsVillageScene || VillageSceneFlow.IsLoading
            || !GameSession.Instance.HasSession || DispatchService.Instance.IsInRun
            || RunSceneFlow.IsTransitioning) return;
        UIManager ui = UIManager.Instance;
        if (ui == null) return;
        BasePanel top = ui.panelStack.Count > 0 ? ui.panelStack.Peek() : null;
        if (top is WorldMapPanel)
        {
            // The map's own UI block can be bypassed only to close this exact top panel.
            // GM, rebinding and any other input owner remain authoritative.
            if (!HasGameplayBlockerOtherThan(ui) && (WasNavigationPressed(ActionMap) || WasNavigationPressed(ActionMenu))) ui.PopPanel();
        }
        else if (ui.panelStack.Count == 0)
        {
            if (WasGameplayPressed(ActionMenu)) ui.PushPanel(PanelType.SettingPanel);
            else if (WasGameplayPressed(ActionMap)) ui.PushPanel(PanelType.WorldMapPanel);
        }
    }
}
