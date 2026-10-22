using System;
using Chaos.MVP.Model.Startup;
using Chaos.MVP.Presenter.Startup;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel, IMainMenuView
{
    private Button _startBtn;
    private Button _updateBtn;
    private Button _settingBtn;
    private Button _quitBtn;
    private MainMenuModel _model;
    private MainMenuPresenter _presenter;
    public event Action<MainMenuIntent> Intent;

    private void Awake()
    {
        _startBtn = transform.GetChild(1).Find("StartBtn").GetComponent<Button>();
        _updateBtn = transform.GetChild(1).Find("UpdateBtn").GetComponent<Button>();
        _settingBtn = transform.GetChild(1).Find("SettingBtn").GetComponent<Button>();
        _quitBtn = transform.GetChild(1).Find("QuitBtn").GetComponent<Button>();
        _startBtn.onClick.AddListener(StartGame);
        _updateBtn.onClick.AddListener(UpdateEnter);
        _settingBtn.onClick.AddListener(SettingEnter);
        _quitBtn.onClick.AddListener(QuitGame);
    }
    public override void OnEnter()
    {
        _presenter?.Dispose();
        _model?.Dispose();
        _model = new MainMenuModel();
        _presenter = new MainMenuPresenter(_model);
        _presenter.Attach(this);
    }
    public override void OnExit()
    {
        _presenter?.Dispose();
        _presenter = null;
        _model?.Dispose();
        _model = null;
    }
    private void OnDestroy() { OnExit(); }
    private void StartGame() { Intent?.Invoke(MainMenuIntent.Start); }
    private void UpdateEnter() { Intent?.Invoke(MainMenuIntent.Updates); }
    private void SettingEnter() { Intent?.Invoke(MainMenuIntent.Settings); }
    private void QuitGame() { Intent?.Invoke(MainMenuIntent.Quit); }
}
