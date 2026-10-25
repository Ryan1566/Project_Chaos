using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VillageSceneFlow
{
    public const string SceneName = "MainScene";
    public static bool IsVillageScene => SceneManager.GetActiveScene().name == SceneName;
    public static bool IsLoading { get; private set; }
    public static bool RecoveringToSaveSlots { get; private set; }
    private static string entryFailure;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    { SceneManager.sceneLoaded -= Loaded; IsLoading = false; RecoveringToSaveSlots = false; entryFailure = null; }
    public static void ValidateEntry()
    {
        if (!GameSession.Instance.HasSession) throw new InvalidOperationException("请先选择存档");
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MainScene.unity") < 0
            || SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/LoadingScene.unity") < 0)
            throw new InvalidOperationException("MainScene/LoadingScene未加入构建列表");
        VillageSceneSettings.Load();
    }
    public static void EnterFromSave()
    {
        ValidateEntry(); if (IsLoading) return;
        if (IsVillageScene && VillageSceneBootstrap.IsReady) { OnReturnedToVillage(); return; }
        IsLoading = true; RecoveringToSaveSlots = false; entryFailure = null;
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
        GameSession.Instance.SetTargetScene(SceneName);
        try { ScenesLoadManager.Instance.LoadScene("LoadingScene", () => { }); }
        catch { IsLoading = false; SceneManager.sceneLoaded -= Loaded; throw; }
    }
    private static void Loaded(Scene scene, LoadSceneMode mode)
    { if (scene.name == SceneName && IsLoading) MonoManager.Instance.StartCoroutine(CompleteEntry()); }
    private static IEnumerator CompleteEntry()
    {
        yield return null;
        if (!IsVillageScene) yield break;
        if (!VillageSceneBootstrap.IsReady) { ReportEntryFailure(VillageSceneBootstrap.LastError ?? "村庄未能初始化"); yield break; }
        IsLoading = false; SceneManager.sceneLoaded -= Loaded;
        GameSession.Instance.SetTargetScene(null); OnReturnedToVillage();
    }
    public static void OnReturnedToVillage()
    {
        if (!IsVillageScene || !VillageSceneBootstrap.IsReady) return;
        IsLoading = false;
        UIManager.Instance.RestoreAfterGameplay(); UIManager.Instance.CloseAllPanels(); UIManager.Instance.SetSceneBackgroundVisible(false);
    }
    public static void ReportEntryFailure(string reason)
    {
        if (RecoveringToSaveSlots) return;
        IsLoading = false; RecoveringToSaveSlots = true; entryFailure = reason;
        GameSession.Instance.SetTargetScene(null);
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.LoadScene("EnterScene");
    }
    public static void ShowEntryFailure()
    {
        UIManager.Instance.SetSceneBackgroundVisible(true);
        UIManager.Instance.SetRootPanel(PanelType.MainMenuPanel);
        var panel = UIManager.Instance.PushPanel(PanelType.SLPanel) as SLPanel;
        panel?.ShowEntryFailure(entryFailure); RecoveringToSaveSlots = false;
    }
    public static bool ReturnToTitle(out string reason)
    {
        if (!MetaProgressService.Instance.SaveProgress(out reason)) return false;
        GameSession.Instance.Clear(); UIManager.Instance.SetSceneBackgroundVisible(true); SceneManager.LoadScene("EnterScene"); return true;
    }
}
