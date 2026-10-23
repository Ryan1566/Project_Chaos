using System;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Formal in-run bag and navigation; business commands are handled by its presenter.</summary>
public sealed class RunMenuPanel : BasePanel, IGameplayView<RunMenuState>
{
    public override bool ReturnsToGameplayOnClose => true;
    public event Action<PanelIntent> Intent;
    private RunMenuModel _model;
    private RunMenuPresenter _presenter;
    private TextMeshProUGUI _summary, _bag, _hint;
    private Button _camp, _retreat, _cancel, _settings;

    public static void Open()
    {
        UIManager.Instance.RestoreAfterGameplay();
        UIManager.Instance.PushPanel(PanelType.RunMenuPanel);
    }

    private void Awake()
    {
        var backdrop = GetComponent<Image>();
        if (backdrop != null) backdrop.color = new Color(0, 0, 0, .65f);
        var box = GameplayUi.Rect(transform, "ContentBox", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(820, 660));
        box.gameObject.AddComponent<Image>().color = new Color(.08f, .11f, .15f, .99f);
        Label(box, "Title", "行囊与出征", 28, new Vector2(24, -12), new Vector2(772, 48));
        _summary = Label(box, "Summary", "", 22, new Vector2(24, -66), new Vector2(772, 42));
        _bag = Label(box, "Bag", "", 23, new Vector2(24, -118), new Vector2(772, 300));
        _bag.alignment = TextAlignmentOptions.TopLeft;
        _hint = Label(box, "Hint", "", 21, new Vector2(24, -426), new Vector2(772, 72));
        _hint.color = new Color(1, .84f, .45f);
        GameplayUi.Button(box, "Continue", "继续战斗", new Vector2(24, -512), new Vector2(240, 52), () => Send(PanelAction.Close));
        _camp = GameplayUi.Button(box, "Camp", "扎营修整", new Vector2(290, -512), new Vector2(240, 52), () => Send(PanelAction.Heal));
        _retreat = GameplayUi.Button(box, "Retreat", "撤离返回局外", new Vector2(556, -512), new Vector2(240, 52), () => Send(PanelAction.Return));
        _cancel = GameplayUi.Button(box, "CancelRetreat", "取消撤离", new Vector2(290, -584), new Vector2(240, 48), () => Send(PanelAction.Cancel));
        _settings = GameplayUi.Button(box, "Settings", "设置 / 按键绑定", new Vector2(24, -584), new Vector2(240, 48), () => Send(PanelAction.Settings));
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Vector2 position, Vector2 dimensions)
    {
        var area = GameplayUi.Rect(parent, name + "Area", new Vector2(0, 1), new Vector2(0, 1), position, dimensions);
        return GameplayUi.Text(area, name, text, size);
    }

    public override void OnEnter()
    {
        RunDepthHud.SetVisible(false);
        RunTeamHud.SetVisible(false);
        if (_model == null) _model = new RunMenuModel();
        _presenter?.Dispose();
        _presenter = new RunMenuPresenter(_model);
        _presenter.Attach(this);
    }

    public override void OnExit()
    {
        _presenter?.Dispose();
        _presenter = null;
        if (uiManager == null || !uiManager.panelStack.Contains(this))
        {
            bool resumeHud = _model != null && _model.InRun;
            _model?.Dispose(); _model = null;
            if (resumeHud) { RunDepthHud.SetVisible(true); RunTeamHud.SetVisible(true); }
        }
    }

    private void OnDestroy() { _presenter?.Dispose(); _model?.Dispose(); }
    private void Send(PanelAction action) { if (IsShowing) Intent?.Invoke(new PanelIntent(action)); }
    public void ClosePanel() { UIManager.Instance.PopPanel(); }
    public void ShowMessage(string message, bool success) { _hint.text = message; }
    public void Render(RunMenuState state)
    {
        _summary.text = state.Summary;
        _bag.text = state.Items.Count == 0 ? "行囊为空，击败敌人后拾取掉落。" : string.Join("\n", state.Items);
        _hint.text = state.Hint;
        _retreat.GetComponentInChildren<TextMeshProUGUI>().text = state.RetreatLabel;
        _cancel.gameObject.SetActive(state.ConfirmRetreat);
        _camp.interactable = !state.ConfirmRetreat;
        _settings.interactable = !state.ConfirmRetreat;
    }
}
