---
uid: 7d2b4f07
id: project-chaos.mvp.view.settings.panel.bottom-bar
parent: project-chaos.mvp.view.settings.panel
tags: [mvp, ui, settings]
name: {zh: "底部按钮条", en: "Bottom Bar"}
description:
  zh: >
      底部按钮条：应用（CommitEdit + 刷新各页）、恢复默认（按当前分类 ResetPendingSection）、返回。有未应用改动时返回会弹确认浮层；prefab 里没摆 ConfirmGroup 则退化成「直接丢弃 + 一条警告」，不会报错卡住。
      
  en: >
      Bottom button bar: Apply (CommitEdit + refresh every page), Reset (ResetPendingSection of the current category) and Return. Return pops a confirmation overlay when there are unapplied edits; if the prefab has no ConfirmGroup it degrades to discard-plus-warning instead of erroring.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.769Z"
fingerprint: f6e00a704a92904810632d475db69fb4c2a10cbaf9072f1b9a5976923e8002f3
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs"
    line: 301
    end_line: 421
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L301-L370"
    description:
      zh: >
          应用/恢复默认/返回三个按钮与应用按钮亮灭刷新。
          
      en: >
          Apply/Reset/Return handlers and apply-button refresh.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L371-L421"
    description:
      zh: >
          返回确认浮层：显隐与确认/丢弃/取消三个回调。
          
      en: >
          Confirm overlay: show/hide and the three handlers.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L301-L370"
    to_api: "rpc:SettingsPageBase.RefreshAll"
    label: {zh: "应用/重置后刷新各页", en: "Refresh pages after apply"}
---
