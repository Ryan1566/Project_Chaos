---
uid: 18d9f2a6
id: project-chaos.project-infra.plugins-dotween.runtime
parent: project-chaos.project-infra.plugins-dotween
name: {zh: "DOTween 运行时", en: "DOTween Runtime"}
description:
  zh: >
      DOTween 运行时 DLL 及 8 个模块扩展（Audio/Physics/Physics2D/Sprite/UI/EPOOutline）。UI 面板动画走这里。
      
  en: >
      DOTween runtime DLL plus 8 module extension files (Audio/Physics/Physics2D/Sprite/UI/EPOOutline). UI panels animate through these.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.778Z"
fingerprint: 8fa48482d2d58b79495d646eadbdbb38d6c97251abab7b4eaf24717f45c54eff
source:
  - path: "Assets/Plugins/Demigiant/DOTween/DOTween.dll"
  - path: "Assets/Plugins/Demigiant/DOTween/DOTween.XML"
  - path: "Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUI.cs"
  - path: "Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUnityVersion.cs"
  - path: "Assets/Plugins/Demigiant/DOTween/readme.txt"
apis:
  - protocol: file
    path: "Assets/Plugins/Demigiant/DOTween/DOTween.dll"
    description:
      zh: >
          DOTween 运行时程序集（1.0MB），补间引擎本体。
          
      en: >
          DOTween runtime assembly (1.0 MB) providing the tween engine.
          
  - protocol: file
    path: "Assets/Plugins/Demigiant/DOTween/DOTween.XML"
    description:
      zh: >
          DOTween XML API 文档。
          
      en: >
          DOTween XML API documentation.
          
  - protocol: file
    path: "Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUI.cs"
    description:
      zh: >
          DOTweenModuleUI：UGUI 快捷扩展（淡入淡出、位移、颜色）。
          
      en: >
          DOTweenModuleUI: UGUI shortcuts (fade, move, color).
          
  - protocol: file
    path: "Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUnityVersion.cs"
    description:
      zh: >
          DOTweenModuleUnityVersion：随 Unity 版本变化的兼容层。
          
      en: >
          DOTweenModuleUnityVersion: Unity-version-specific compatibility shims.
          
  - protocol: file
    path: "Assets/Plugins/Demigiant/DOTween/readme.txt"
    description:
      zh: >
          DOTween 安装说明 readme。
          
      en: >
          DOTween readme with setup notes.
          
deps:
  - kind: reference
    to: project-chaos.assets.misc-resources.dotween-settings
    from_api: "file:Assets/Plugins/Demigiant/DOTween/DOTween.dll"
    to_api: "file:Assets/Resources/DOTweenSettings.asset"
    label: {zh: "运行时读取设置", en: "Reads settings at runtime"}
---
