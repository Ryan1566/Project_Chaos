---
uid: 7d2b4f05
id: project-chaos.mvp.view.settings.panel.lifecycle
parent: project-chaos.mvp.view.settings.panel
tags: [mvp, ui, settings]
name: {zh: "面板生命周期", en: "Panel Lifecycle"}
description:
  zh: >
      SettingPanel 的生命周期与切页：OnEnter 开启编辑会话，OnExit 处理未应用的改动，ShowCategory 必须先 SetActive(true) 再 EnsureBound()——顺序反了会在未激活状态下绑定，取值域没设、回调没接上却一声不响。
      
  en: >
      SettingPanel lifecycle and tab switching: OnEnter opens an edit session, OnExit deals with unapplied edits, and ShowCategory always activates the page before calling EnsureBound (reversing that order binds silently-nothing because Awake has not run yet).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.769Z"
fingerprint: f6e00a704a92904810632d475db69fb4c2a10cbaf9072f1b9a5976923e8002f3
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs"
    line: 62
    end_line: 135
  - path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs"
    line: 249
    end_line: 300
apis:
  - protocol: rpc
    path: "SettingPanel.OnEnter"
    description:
      zh: >
          面板打开：开启一次编辑会话。
          
      en: >
          Panel enter: begins an edit session.
          
  - protocol: rpc
    path: "SettingPanel.OnExit"
    description:
      zh: >
          面板关闭：结束编辑会话。
          
      en: >
          Panel exit: disposes the edit session.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L62-L135"
    description:
      zh: >
          生命周期段：Awake/OnEnable/OnDisable/OnEnter/OnExit 与取消未应用编辑。
          
      en: >
          Lifecycle: Awake/OnEnable/OnDisable/OnEnter/OnExit.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L249-L300"
    description:
      zh: >
          ShowCategory：切页时先 SetActive 再 EnsureBound（顺序反了会静默失效）。
          
      en: >
          ShowCategory: activates then binds the target page.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L249-L300"
    to_api: "rpc:SettingsPageBase.EnsureBound"
    label: {zh: "切页后确保绑定", en: "Bind page after activation"}
---
