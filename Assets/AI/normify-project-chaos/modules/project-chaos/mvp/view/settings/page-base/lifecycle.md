---
uid: 7d2b4f0a
id: project-chaos.mvp.view.settings.page-base.lifecycle
parent: project-chaos.mvp.view.settings.page-base
tags: [mvp, ui, settings]
name: {zh: "页绑定生命周期", en: "Page Binding Lifecycle"}
description:
  zh: >
      用到才绑的生命周期：面板调 EnsureBound，后者只跑一次 OnBind，并在物体未激活时明确报错。RefreshAll 把 Pending 快照灌回每个已注册的刷新动作，刻意走 SetValueWithoutNotify 以免「刷新→回调→写数据→再刷新」的自激循环。
      
  en: >
      Bind-on-first-use lifecycle: the panel calls EnsureBound, which runs OnBind exactly once and logs an error when the GameObject is still inactive. RefreshAll replays the Pending snapshot into every registered refresher, deliberately via SetValueWithoutNotify so refresh cannot feed back into itself.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.766Z"
fingerprint: 566f91ff5e4fd192b5d959c2369f661d2efc4a42ee64f280b1b24860fab98f4f
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs"
    line: 119
    end_line: 157
apis:
  - protocol: rpc
    path: "SettingsPageBase.EnsureBound"
    description:
      zh: >
          幂等绑定；页未激活时明确报错而不是静默失效。
          
      en: >
          Idempotent bind; errors if the page is inactive.
          
  - protocol: rpc
    path: "SettingsPageBase.OnBind"
    description:
      zh: >
          子类钩子：在这里声明「哪一行改哪个字段」。
          
      en: >
          Subclass hook declaring id-to-field bindings.
          
  - protocol: rpc
    path: "SettingsPageBase.RefreshAll"
    description:
      zh: >
          把 Pending 的值灌回本页所有控件（不再触发回调）。
          
      en: >
          Replays Pending values into every widget.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs#L119-L157"
    description:
      zh: >
          用到才绑的生命周期段。
          
      en: >
          Bind-on-first-use lifecycle.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.settings.page-base.binding-helpers
    from_api: "rpc:SettingsPageBase.RefreshAll"
    to_api: "rpc:SettingsPageBase.AddRefresher"
    label: {zh: "回填控件不触发回调", en: "Replays values into widgets"}
---
