---
uid: 7d2b4f13
id: project-chaos.mvp.view.settings.pages.keybind-bindings.rows
parent: project-chaos.mvp.view.settings.pages.keybind-bindings
tags: [mvp, ui, keybind]
name: {zh: "按键行绑定", en: "Key Rows"}
description:
  zh: >
      两套按键行：键鼠行是双格（键盘格 + 鼠标格，一个操作可同时绑键与鼠标），手柄行是单格。每格交给 KeyIconText——有图标显示图标、没有则回退文字，避免切语言时键名长度撑破固定列宽。
      
  en: >
      The two key-row sets: keyboard rows carry two slots (keyboard + mouse) so one action can be bound to both, gamepad rows carry one. Each row shows an icon via KeyIconText when a KeyIconMap entry exists and falls back to text, which keeps widths stable across languages. Reset writes back the action name, binding kind and part name per row.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.768Z"
fingerprint: 03908ed9b1f158b11dcd01ea8368f47711a172a7e5cf15c0bd5f51e2eebf2f50
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs"
    line: 221
    end_line: 346
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs"
    line: 581
    end_line: 617
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L221-L346"
    description:
      zh: >
          构建按键行，并把 (文字, 图标, 颜色) 刷进行。
          
      en: >
          Builds key rows and refreshes their (text, icon).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L581-L617"
    description:
      zh: >
          Button/marker/KeyIconText 的查找工具。
          
      en: >
          Button/marker/KeyIconText find helpers.
          
---
