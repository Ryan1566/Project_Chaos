using UnityEngine;

/// <summary>Run-level UI shortcuts stay available when the controlled character is downed or paused.</summary>
public sealed class GameplayShortcutDriver : MonoBehaviour
{
    private void Awake()
    { if (GetComponent<RunItemWheelDriver>() == null) gameObject.AddComponent<RunItemWheelDriver>(); }

    private void Update()
    {
        if (!DispatchService.Instance.IsInRun || RunSceneFlow.IsTransitioning || RunEncounter.Defeated || RunEncounter.RunCleared) return;
        var input = InputManager.Instance;
        var ui = UIManager.Instance;
        if (!input.HasGameplayBlockerOtherThan(ui))
        {
            var collection = ui.panelStack.Count == 0 ? null : ui.panelStack.Peek() as RunMenuPanel;
            if (input.WasNavigationPressed(InputManager.ActionFunctions))
            {
                if (collection != null && collection.IsShowing)
                { if (!collection.TryCloseSecondaryWindow()) ui.PopPanel(); }
                else if (input.CanReadGameplayInput) RunMenuPanel.Open();
                return;
            }
            if (input.WasNavigationPressed(InputManager.ActionInventory) || input.WasNavigationPressed(InputManager.ActionTeam))
            {
                var tab = input.WasNavigationPressed(InputManager.ActionTeam) ? RunFunctionTab.Team : RunFunctionTab.Inventory;
                if (collection != null && collection.IsShowing)
                {
                    if (collection.TryCloseSecondaryWindow()) return;
                    if (collection.SelectedTab == tab) ui.PopPanel(); else collection.SelectTab(tab);
                }
                else if (input.CanReadGameplayInput) RunMenuPanel.Open(tab);
                return;
            }
            if (input.WasNavigationPressed(InputManager.ActionMap))
            {
                if (collection != null && collection.IsShowing)
                {
                    if (collection.TryCloseSecondaryWindow()) return;
                    if (collection.SelectedTab == RunFunctionTab.Map) ui.PopPanel();
                    else collection.SelectTab(RunFunctionTab.Map);
                }
                else if (input.CanReadGameplayInput) RunMenuPanel.Open(RunFunctionTab.Map);
                return;
            }
        }
        if (input.WasNavigationPressed(InputManager.ActionMenu) && !input.HasGameplayBlockerOtherThan(ui))
        {
            var top = ui.panelStack.Count == 0 ? null : ui.panelStack.Peek();
            if (top is RunMenuPanel menu && menu.TryCloseSecondaryWindow()) return;
            if (top != null && top.IsShowing && top.gameObject.activeInHierarchy && top.ReturnsToGameplayOnClose)
            { ui.PopPanel(); return; }
            if (input.CanReadGameplayInput) { RunMenuPanel.Open(); return; }
        }
        if (input.WasGameplayPressed(InputManager.ActionCamp)) RunCampPanel.Open();
    }
}
