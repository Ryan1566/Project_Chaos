---
uid: c0d4a71f
id: project-chaos.project-infra.plugins-spine.editor
parent: project-chaos.project-infra.plugins-spine
name: {zh: "Spine 编辑器扩展", en: "Spine Editor"}
description:
  zh: >
      Spine 仅编辑器程序集（44 个 C# 文件）：导入管线、Inspector 与菜单项。运行时代码不得引用。
      
  en: >
      Spine editor-only assembly (44 C# files): import pipeline, inspectors and menu entries. Never referenced by runtime code.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.782Z"
fingerprint: 4868014b77eac5c5a35ae56cd35d1d7f443b3717c93559debc1505373b1c110e
source:
  - path: "Assets/Plugins/Spine/Editor/spine-unity-editor.asmdef"
  - path: "Assets/Plugins/Spine/Editor/spine-unity/Editor/Menus.cs"
  - path: "Assets/Plugins/Spine/Editor/spine-unity/Editor/SpineAttributeDrawers.cs"
apis:
  - protocol: file
    path: "Assets/Plugins/Spine/Editor/spine-unity-editor.asmdef"
    description:
      zh: >
          spine-unity 编辑器程序集定义。
          
      en: >
          spine-unity editor assembly definition.
          
  - protocol: file
    path: "Assets/Plugins/Spine/Editor/spine-unity/Editor/Menus.cs"
    description:
      zh: >
          Spine 编辑器菜单项（导入、创建 atlas/skeleton 资产）。
          
      en: >
          Spine editor menu items (import, atlas/skeleton asset creation).
          
  - protocol: file
    path: "Assets/Plugins/Spine/Editor/spine-unity/Editor/SpineAttributeDrawers.cs"
    description:
      zh: >
          Spine 特性对应的 Inspector 绘制器。
          
      en: >
          Inspector drawers for Spine attributes.
          
---
