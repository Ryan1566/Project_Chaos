---
uid: 7d2b4f0b
id: project-chaos.mvp.view.settings.page-base.binding-helpers
parent: project-chaos.mvp.view.settings.page-base
tags: [mvp, ui, settings]
name: {zh: "绑定工具", en: "Binding Helpers"}
description:
  zh: >
      id 到字段的绑定工具箱：BindSlider/BindSelector/BindToggle/BindButton 各自把一个 settingId 接到 SettingsData 的读写委托上，玩家改动时通知 SettingsManager，并返回行引用供页面稍后置灰。FindRow 与 ValidateRows 负责揪出「prefab 里摆了行却没绑定」的情况。
      
  en: >
      The id-to-field binding toolkit: BindSlider/BindSelector/BindToggle/BindButton each map one settingId onto a getter and setter of SettingsData, notify SettingsManager when the player changes something, and return the row so a page can grey it out later. FindRow and ValidateRows catch rows that were placed in the prefab but never bound.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.766Z"
fingerprint: 566f91ff5e4fd192b5d959c2369f661d2efc4a42ee64f280b1b24860fab98f4f
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs"
    line: 158
    end_line: 307
apis:
  - protocol: rpc
    path: "SettingsPageBase.BindSlider"
    description:
      zh: >
          把拉条行接到 SettingsData 的某个字段。
          
      en: >
          Binds a slider row to a SettingsData field.
          
  - protocol: rpc
    path: "SettingsPageBase.BindSelector"
    description:
      zh: >
          把选择器行接到字段（按下标读写）。
          
      en: >
          Binds a selector row (index-based).
          
  - protocol: rpc
    path: "SettingsPageBase.BindToggle"
    description:
      zh: >
          把开关行接到布尔字段。
          
      en: >
          Binds a toggle row.
          
  - protocol: rpc
    path: "SettingsPageBase.BindButton"
    description:
      zh: >
          把按钮行接到一个动作。
          
      en: >
          Binds a button row to an action.
          
  - protocol: rpc
    path: "SettingsPageBase.AddRefresher"
    description:
      zh: >
          注册刷新动作（按注册顺序执行）。
          
      en: >
          Registers a refresh action (order matters).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs#L158-L307"
    description:
      zh: >
          绑定工具、FindRow 与「摆了行但没绑」校验。
          
      en: >
          Bind helpers, FindRow and row validation.
          
---
