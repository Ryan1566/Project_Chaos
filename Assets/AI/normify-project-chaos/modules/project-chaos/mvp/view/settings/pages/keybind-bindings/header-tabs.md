---
uid: 7d2b4f12
id: project-chaos.mvp.view.settings.pages.keybind-bindings.header-tabs
parent: project-chaos.mvp.view.settings.pages.keybind-bindings
tags: [mvp, ui, keybind]
name: {zh: "页签与手柄型号", en: "Header & Tabs"}
description:
  zh: >
      改键页顶部：返回按钮、键鼠/手柄页签、PS/Xbox 型号切换。切方案或切型号把 inputDevice / gamepadModel 写进暂存区，刷新两套按键行与页签图标，再通知 SettingsManager 点亮「应用」按钮。
      
  en: >
      Header of the rebind page: back button, keyboard/gamepad tabs and the PS/Xbox model switch. Switching scheme or model writes inputDevice / gamepadModel into the pending buffer, refreshes both row sets and the tab icons, then notifies SettingsManager so the Apply button lights up.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.767Z"
fingerprint: 03908ed9b1f158b11dcd01ea8368f47711a172a7e5cf15c0bd5f51e2eebf2f50
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs"
    line: 94
    end_line: 220
apis:
  - protocol: rpc
    path: "KeybindBindingsPage.OnBind"
    description:
      zh: >
          注册按键行与顶部刷新（刷新动作必须第一个注册）。
          
      en: >
          Registers rows and the header refresh first.
          
  - protocol: rpc
    path: "KeybindBindingsPage.PageNodeName"
    description:
      zh: >
          本二级页的节点名常量。
          
      en: >
          Node name of this second-level page.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L94-L220"
    description:
      zh: >
          OnBind、页签、手柄型号切换与返回按钮。
          
      en: >
          OnBind, header tabs, gamepad model, back.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "rpc:KeybindBindingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.OnBind"
    label: {zh: "继承设置页基类", en: "Extends settings page base"}
  - kind: call
    to: project-chaos.mvp.view.settings.page-base.subpage
    from_api: "rpc:KeybindBindingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.CloseSubPage"
    label: {zh: "返回一级按键页", en: "Back to first-level page"}
---
