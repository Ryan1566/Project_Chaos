using UnityEngine;

/// <summary>Run-level UI shortcuts stay available when the controlled character is downed or paused.</summary>
public sealed class GameplayShortcutDriver : MonoBehaviour
{
    private void Update()
    {
        if (!DispatchService.Instance.IsInRun || RunSceneFlow.IsTransitioning || RunEncounter.Defeated || RunEncounter.RunCleared) return;
        var input = InputManager.Instance;
        var ui = UIManager.Instance;
        if (input.WasNavigationPressed(InputManager.ActionMenu) && !input.HasGameplayBlockerOtherThan(ui))
        {
            var top = ui.panelStack.Count == 0 ? null : ui.panelStack.Peek();
            if (top != null && top.IsShowing && top.gameObject.activeInHierarchy && top.ReturnsToGameplayOnClose)
            { ui.PopPanel(); return; }
            if (input.CanReadGameplayInput) { RunMenuPanel.Open(); return; }
        }
        if (input.WasGameplayPressed(InputManager.ActionCamp)) RunCampPanel.Open();
    }
}
