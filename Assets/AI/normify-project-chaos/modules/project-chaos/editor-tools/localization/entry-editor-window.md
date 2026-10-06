---
uid: e0a10105
id: project-chaos.editor-tools.localization.entry-editor-window
parent: project-chaos.editor-tools.localization
name: {zh: "条目编辑窗口外壳", en: "Entry Editor Window Shell"}
description:
  zh: >
      本地化编辑器主窗口：左栏搜索与条目列表（按语言显示缺失/完整度）、右栏编辑区、可拖拽分隔条与样式初始化；静默模式跳过清空确认。
      
  en: >
      Main localization editor window: left column with search and entry list (per-language completeness), right column editor area, draggable splitter and style setup; silent mode skips the clear-all confirmation.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.737Z"
fingerprint: f632328e9b94a45e950b53ac46275b6cce6123211b4d3c7a24f45471997cfe6b
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs"
    line: 12
    end_line: 397
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs#L12-L397"
    description:
      zh: >
          窗口外壳、左右面板与分隔条所在行段。
          
      en: >
          Line range holding the window shell, panels and splitter.
          
  - protocol: rpc
    path: "Menu/Tools/本地化编辑器"
    description:
      zh: >
          打开本地化编辑器窗口的菜单项。
          
      en: >
          Menu item opening the localization editor window.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.ShowWindow"
    description:
      zh: >
          打开本地化编辑器窗口。
          
      en: >
          Opens the localization editor window.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.SilentMode"
    description:
      zh: >
          静默开关：自动化驱动时跳过确认框。
          
      en: >
          Silent switch: skips confirmation dialogs under automation.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.DrawLeftPanel"
    description:
      zh: >
          绘制左栏搜索框与条目列表。
          
      en: >
          Draws the left search box and entry list.
          
---
