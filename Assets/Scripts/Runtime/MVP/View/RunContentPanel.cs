using System;
using Chaos.Gameplay.Interaction;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using UnityEngine;

/// <summary>Existing UI stack owns input/back navigation. This adapter owns a single interaction visit.</summary>
public sealed class RunContentPanel : BasePanel, IRunContentPanelView
{
    public override bool ReturnsToGameplayOnClose => true;
    public event Action<int> ClaimRequested;
    public event Action CloseRequested;
    RunContentView view;
    RunContentPanelModel model;
    RunContentPanelPresenter presenter;
    bool terrainWasVisible;
    int context;
    RunMap.NaturalMapPlan map;
    public static void Open(RunContentSession session)
    {
        if (session == null || !session.IsValid) return;
        UIManager.Instance.RestoreAfterGameplay();
        var panel = UIManager.Instance.PushPanel(PanelType.RunContentPanel) as RunContentPanel;
        if (panel == null) { session.Close(); return; }
        panel.Bind(session);
    }
    void Awake()
    {
        view = gameObject.AddComponent<RunContentView>(); view.Initialize();
        view.ClaimRequested += ForwardClaim; view.CloseRequested += ForwardClose;
    }
    void ForwardClaim(int index) { if (IsShowing) ClaimRequested?.Invoke(index); }
    void ForwardClose() { if (IsShowing) CloseRequested?.Invoke(); }
    void Bind(RunContentSession session)
    {
        Release(); context = GameSession.Instance.ContextId; map = RunMap.NaturalMapRuntime.Current;
        terrainWasVisible = RunTerrainToast.IsShowing;
        RunDepthHud.SetVisible(false); RunTeamHud.SetVisible(false); RunTerrainToast.SetVisible(false);
        model = new RunContentPanelModel(session);
        presenter = new RunContentPanelPresenter(model); presenter.Attach(this);
        session.Target.GetComponent<RunContentPresentation>()?.OnInteractionOpened(session);
    }
    public override void OnEnter() { CursorEnable(); }
    public override void OnExit()
    { if (uiManager == null || !uiManager.panelStack.Contains(this)) Release(); }
    void Update() { if (IsShowing) presenter?.Tick(); }
    public void Render(RunContentViewState state) { view.Render(state); }
    public void CloseInteraction()
    {
        var stack = UIManager.Instance.panelStack;
        if (stack.Count > 0 && stack.Peek() == this) UIManager.Instance.PopPanel();
    }
    public override void HandleBack() { CloseInteraction(); }
    void Release()
    {
        bool restore = model != null && context == GameSession.Instance.ContextId && ReferenceEquals(map, RunMap.NaturalMapRuntime.Current)
            && RunEncounter.IsSceneReady && !RunSceneFlow.IsTransitioning;
        presenter?.Dispose(); presenter = null; model?.Dispose(); model = null;
        if (restore) { RunDepthHud.SetVisible(true); RunTeamHud.SetVisible(true); if (terrainWasVisible) RunTerrainToast.SetVisible(true); }
    }
    void OnDestroy()
    { Release(); if (view != null) { view.ClaimRequested -= ForwardClaim; view.CloseRequested -= ForwardClose; } }
}
