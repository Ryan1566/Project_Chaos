---
uid: e0a10601
id: project-chaos.editor-tools.input-icon.keymap-window
parent: project-chaos.editor-tools.input-icon
name: {zh: "按键图标映射窗口", en: "Key Icon Map Window"}
description:
  zh: >
      按键图标映射窗口：带映射资产槽与 tint 的头部、选项（优先字形/优先手写风格）、扫描与自动匹配/按页签应用图标动作、覆盖率读数、可筛选的映射表（标记是否被输入资产引用），以及未识别图标清单。同时持有 Resources 下的默认映射表路径常量。
      
  en: >
      Key icon map window: header with the map asset slot and tint, options (prefer glyph, prefer handwritten style), scan/auto-match/tab-icon actions, coverage readout, filterable table of entries with used-in-asset markers, and a list of unrecognised icons. Also owns the default map path under Resources.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.735Z"
fingerprint: 96486d7e832cc7190baf94ca5cbad33b062a22120e8091b257d09d7d65b9a001
source:
  - path: "Assets/Scripts/Editor/InputIcon/KeyIconMapWindow.cs"
    line: 36
    end_line: 364
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/InputIcon/KeyIconMapWindow.cs#L36-L364"
    description:
      zh: >
          映射表窗口 UI、筛选与自动匹配所在行段。
          
      en: >
          Line range holding the map window UI, filters and auto-match.
          
  - protocol: rpc
    path: "Menu/Tools/图标/按键图标映射"
    description:
      zh: >
          打开按键图标映射窗口的菜单项。
          
      en: >
          Menu item opening the key icon map window.
          
  - protocol: rpc
    path: "KeyIconMapWindow.ShowWindow"
    description:
      zh: >
          打开按键图标映射窗口。
          
      en: >
          Opens the key icon mapping window.
          
  - protocol: rpc
    path: "KeyIconMapWindow.AutoMatchInternal"
    description:
      zh: >
          按控制键把图标库匹配进映射表。
          
      en: >
          Matches library icons into the map by control key.
          
  - protocol: rpc
    path: "KeyIconMapWindow.UsedControlKeys"
    description:
      zh: >
          收集输入资产里用到的控制键。
          
      en: >
          Collects control keys referenced by input assets.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.input-icon.icon-naming
    from_api: "rpc:KeyIconMapWindow.AutoMatchInternal"
    to_api: "rpc:IconNaming.Scan"
    label: {zh: "匹配扫描到的图标库", en: "Matches scanned library"}
---
