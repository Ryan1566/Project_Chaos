using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Village entry/UI routing; readiness and in-flight ownership are coordinated centrally.</summary>
public static class VillageSceneFlow
{
    public const string SceneName = "MainScene";
    public static bool IsVillageScene => SceneManager.GetActiveScene().name == SceneName;
    public static bool IsLoading => SceneTransitionCoordinator.Active != null && SceneTransitionCoordinator.Active.Target == SceneName;
    public static bool RecoveringToSaveSlots { get; private set; }
    private static string entryFailure;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { RecoveringToSaveSlots = false; entryFailure = null; }
    public static void ValidateEntry()
    {
        if (!GameSession.Instance.IsPersistent) throw new InvalidOperationException("请先选择正式存档");
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MainScene.unity") < 0 || SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/LoadingScene.unity") < 0)
            throw new InvalidOperationException("MainScene/LoadingScene未加入构建列表");
        VillageSceneSettings.Load();
    }
    public static void EnterFromSave()
    {
        ValidateEntry(); if (IsLoading) return;
        if (IsVillageScene && VillageSceneBootstrap.IsReady) { OnReturnedToVillage(); return; }
        RecoveringToSaveSlots = false; entryFailure = null;
        if (!SceneTransitionCoordinator.TryBegin(SceneTransitionCoordinator.TransitionKind.EnterVillage, SceneName, true,
            OnReturnedToVillage, RecoverToSlots, out string reason)) throw new InvalidOperationException(reason);
    }
    public static void OnReturnedToVillage()
    {
        if (!IsVillageScene || !VillageSceneBootstrap.IsReady) return;
        UIManager.Instance.RestoreAfterGameplay(); UIManager.Instance.CloseAllPanels(); UIManager.Instance.SetSceneBackgroundVisible(false);
    }
    public static void ReportEntryFailure(string reason)
    {
        var request = SceneTransitionCoordinator.Active;
        if (request != null && request.Target == SceneName) { SceneTransitionCoordinator.Fail(request.Id, reason); return; }
        RecoverToSlots(reason);
    }
    private static void RecoverToSlots(string reason)
    {
        if (RecoveringToSaveSlots) return;
        RecoveringToSaveSlots = true; entryFailure = reason;
        GameSession.Instance.SetTargetScene(null); SceneManager.LoadScene("EnterScene");
    }
    public static void ShowEntryFailure()
    {
        UIManager.Instance.SetSceneBackgroundVisible(true); UIManager.Instance.SetRootPanel(PanelType.MainMenuPanel);
        var panel = UIManager.Instance.PushPanel(PanelType.SLPanel) as SLPanel;
        panel?.ShowEntryFailure(entryFailure); RecoveringToSaveSlots = false;
    }
    public static bool ReturnToTitle(out string reason)
    {
        if (SceneTransitionCoordinator.IsBusy) { reason = "场景切换中，请稍候"; return false; }
        if (!MetaProgressService.Instance.SaveProgress(out reason)) return false;
        GameSession.Instance.Clear(); UIManager.Instance.SetSceneBackgroundVisible(true); SceneManager.LoadScene("EnterScene"); return true;
    }
}
