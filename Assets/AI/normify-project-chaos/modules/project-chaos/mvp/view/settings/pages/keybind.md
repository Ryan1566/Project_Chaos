---
uid: 7d2b4f10
id: project-chaos.mvp.view.settings.pages.keybind
parent: project-chaos.mvp.view.settings.pages
tags: [mvp, ui, settings]
name: {zh: "按键设置页", en: "Keybind Page"}
description:
  zh: >
      按键一级页：水平/垂直反转、灵敏度、攻击触发方式——都只有鼠标才有意义，手柄方案下整行置灰但保留可见（玩家得先看见它存在）。「更改按键绑定」按钮把控制权交给二级页，并收回子页的返回回调。
      
  en: >
      Keybind first-level page: horizontal/vertical invert, sensitivity and attack trigger mode — all mouse-only, so they are greyed out (kept visible on purpose) while the gamepad scheme is active. Its 更改按键绑定 button hands over to the second-level bindings page and takes the back callback.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.768Z"
fingerprint: 550dd7f596775fa6ab8fa5151993f4904dab93c9a88c2823628be7887989e9ee
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindSettingsPage.cs"
    line: 24
    end_line: 85
apis:
  - protocol: rpc
    path: "KeybindSettingsPage.OnBind"
    description:
      zh: >
          绑定鼠标专用项并接上进入改键二级页的按钮。
          
      en: >
          Binds mouse-only rows and opens the rebind page.
          
  - protocol: rpc
    path: "KeybindSettingsPage.BindingsPageName"
    description:
      zh: >
          改键二级页在本页同级的节点名常量。
          
      en: >
          Node name of the second-level bindings page.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindSettingsPage.cs#L24-L85"
    description:
      zh: >
          鼠标专用项与二级页转交（含手柄方案置灰）。
          
      en: >
          Mouse-only rows and the sub page hand-off.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "rpc:KeybindSettingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.OnBind"
    label: {zh: "继承设置页基类", en: "Extends settings page base"}
  - kind: call
    to: project-chaos.mvp.view.settings.pages.keybind-bindings.header-tabs
    from_api: "rpc:KeybindSettingsPage.OnBind"
    to_api: "rpc:KeybindBindingsPage.OnBind"
    label: {zh: "打开改键二级页", en: "Opens rebind sub page"}
---
