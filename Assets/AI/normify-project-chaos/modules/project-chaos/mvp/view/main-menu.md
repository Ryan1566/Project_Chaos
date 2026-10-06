---
uid: 7d2b4f02
id: project-chaos.mvp.view.main-menu
parent: project-chaos.mvp.view
tags: [mvp, ui]
name: {zh: "主菜单", en: "Main Menu"}
description:
  zh: >
      主菜单面板：Awake 里绑定 StartBtn/UpdateBtn/SettingBtn/QuitBtn 四个按钮，点开始游戏按 PanelType 推入 SLPanel、点设置推入 SettingPanel。更新公告目前只打日志，退出直接 Application.Quit。
      
  en: >
      Main menu panel: binds StartBtn/UpdateBtn/SettingBtn/QuitBtn in Awake, then pushes SLPanel by PanelType when the player starts a game and SettingPanel when the player opens settings. Update announcement and Quit only log / call Application.Quit today.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.762Z"
fingerprint: 5903c3d0fb3041b9931310f7a283bc591ab4121a00dd03224f6881583460d768
source:
  - path: "Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs"
    line: 8
    end_line: 66
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs#L1-L26"
    description:
      zh: >
          按子节点名取四个按钮并注册点击监听。
          
      en: >
          Node lookup and button listener registration.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs#L28-L36"
    description:
      zh: >
          面板打开/关闭钩子（都只转调基类）。
          
      en: >
          Panel enter/exit hooks (both call base).
          
  - protocol: rpc
    path: "MainMenuPanel.OnEnter"
    description:
      zh: >
          面板被 UIManager 推入并打开时调用。
          
      en: >
          Panel opened by UIManager (pushed onto the stack).
          
  - protocol: rpc
    path: "MainMenuPanel.OnExit"
    description:
      zh: >
          面板被关闭时调用。
          
      en: >
          Panel closed by UIManager.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs#L38-L65"
    description:
      zh: >
          开始游戏/更新公告/设置/退出四个按钮的行为。
          
      en: >
          Start/Update/Setting/Quit button behaviours.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.save-load.panel.lifecycle
    from_api: "file:Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs#L38-L65"
    to_api: "rpc:SLPanel.OnEnter"
    label: {zh: "开始游戏→存档面板", en: "Start game opens SLPanel"}
  - kind: call
    to: project-chaos.mvp.view.settings.panel.lifecycle
    from_api: "file:Assets/Scripts/Runtime/MVP/View/MainMenuPanel.cs#L38-L65"
    to_api: "rpc:SettingPanel.OnEnter"
    label: {zh: "设置→设置面板", en: "Settings opens settings panel"}
---
