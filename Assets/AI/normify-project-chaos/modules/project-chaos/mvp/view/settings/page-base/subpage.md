---
uid: 7d2b4f09
id: project-chaos.mvp.view.settings.page-base.subpage
parent: project-chaos.mvp.view.settings.page-base
tags: [mvp, ui, settings]
name: {zh: "二级界面", en: "Second-Level Page"}
description:
  zh: >
      二级界面支持：一级页按节点名找自己的二级页，二级页只抛 OnBackRequested 而不知道自己在谁下面，CancelPendingEdit 逐层下问 OnCancelPendingEdit，让二级页丢掉采了一半的输入。
      
  en: >
      Second-level page support: a page finds its sub page by node name, the sub page raises OnBackRequested instead of knowing who owns it, and CancelPendingEdit walks down to OnCancelPendingEdit so a sub page can drop its half-collected input.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.766Z"
fingerprint: 566f91ff5e4fd192b5d959c2369f661d2efc4a42ee64f280b1b24860fab98f4f
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs"
    line: 27
    end_line: 118
apis:
  - protocol: rpc
    path: "SettingsPageBase.FindSubPage"
    description:
      zh: >
          按节点名在同级里找二级页面组件。
          
      en: >
          Finds a sub page component by node name.
          
  - protocol: rpc
    path: "SettingsPageBase.ShowSubPage"
    description:
      zh: >
          打开二级页面并触发父页回调。
          
      en: >
          Shows a sub page and notifies the parent.
          
  - protocol: rpc
    path: "SettingsPageBase.CollapseSubPage"
    description:
      zh: >
          收起二级页面（保留未应用改动）。
          
      en: >
          Hides the sub page, keeping edits.
          
  - protocol: rpc
    path: "SettingsPageBase.CloseSubPage"
    description:
      zh: >
          关闭二级页面并返回一级。
          
      en: >
          Closes the sub page and returns.
          
  - protocol: rpc
    path: "SettingsPageBase.IsSubPageOpen"
    description:
      zh: >
          当前是否有二级页面打开。
          
      en: >
          Whether a sub page is currently open.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/SettingsPageBase.cs#L27-L118"
    description:
      zh: >
          二级界面段：查找/显隐二级页与取消待定编辑。
          
      en: >
          Second-level page stack and pending-edit cancel.
          
---
