---
uid: f82d106b
id: project-chaos.project-infra.project-settings.editor-misc
parent: project-chaos.project-infra.project-settings
name: {zh: "编辑器工具链偏好", en: "Editor Tooling Prefs"}
description:
  zh: >
      编辑器工具链配置：包管理器、预设、场景模板与代码覆盖率包设置。
      
  en: >
      Editor tooling configuration: package manager, presets, scene templates and the code-coverage package settings.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.787Z"
fingerprint: bc9179379332d6006cacb7d7e8d90d349945d3f6315c24213b7413a655e0db59
source:
  - path: "ProjectSettings/PackageManagerSettings.asset"
  - path: "ProjectSettings/PresetManager.asset"
  - path: "ProjectSettings/SceneTemplateSettings.json"
  - path: "ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json"
apis:
  - protocol: file
    path: "ProjectSettings/PackageManagerSettings.asset"
    description:
      zh: >
          包管理器设置（注册表作用域、预发布包）。
          
      en: >
          Package manager settings (registry scopes, pre-release packages).
          
  - protocol: file
    path: "ProjectSettings/PresetManager.asset"
    description:
      zh: >
          预设管理器：各类型的默认预设。
          
      en: >
          Preset manager: per-type default presets.
          
  - protocol: file
    path: "ProjectSettings/SceneTemplateSettings.json"
    description:
      zh: >
          新建场景模板设置（3.6KB JSON）。
          
      en: >
          New-scene template settings (3.6 KB JSON).
          
  - protocol: file
    path: "ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json"
    description:
      zh: >
          代码覆盖率包设置（68 字节）。
          
      en: >
          Code coverage package settings (68 bytes).
          
---
