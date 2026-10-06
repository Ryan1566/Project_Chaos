---
uid: c58e2d17
id: project-chaos.editor-tools
parent: project-chaos
tags: [editor, tooling]
name: {zh: "编辑器工具集", en: "Editor Tooling"}
description:
  zh: >
      Assets/Scripts/Editor 的编辑器工具集：本地化编辑器（文本收集/合并/字体映射/窗口）、Git 工具窗、场景 UI 概览、图标命名与图集管线、对话编辑器、InputIcon 图标映射、UIPanelAnimator 工具。只在编辑器运行，禁止被运行时代码引用。
      
  en: >
      Editor tooling under Assets/Scripts/Editor: localization editors (text collection, merging, font mapping, window), the Git tool window, scene UI overview, icon naming and atlas pipeline, dialogue editor, InputIcon mapping, and the UIPanelAnimator tool. Editor-only code; runtime code must never reference it.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:02:58.748Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
deps:
  - kind: call
    to: project-chaos.config-pipeline
    label: {zh: "菜单驱动导出", en: "Menus drive export"}
  - kind: reference
    to: project-chaos.framework
    label: {zh: "复用框架类型", en: "Reuses framework types"}
---
