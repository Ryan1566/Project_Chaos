---
uid: 2e6b94a8
id: project-chaos.project-infra.project-settings.rendering
parent: project-chaos.project-infra.project-settings
name: {zh: "渲染与质量", en: "Rendering & Quality"}
description:
  zh: >
      渲染配置：URP 图形管线、质量档位、URP 与 ShaderGraph 设置及 VFX 管理器。
      
  en: >
      Rendering configuration: URP graphics pipeline, quality tiers, URP and ShaderGraph settings plus the VFX manager.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.790Z"
fingerprint: 0478d3c201a5d2865bad641db06e3477d4affaf50b4958327af2d283d8508352
source:
  - path: "ProjectSettings/GraphicsSettings.asset"
  - path: "ProjectSettings/QualitySettings.asset"
  - path: "ProjectSettings/URPProjectSettings.asset"
  - path: "ProjectSettings/ShaderGraphSettings.asset"
  - path: "ProjectSettings/VFXManager.asset"
apis:
  - protocol: file
    path: "ProjectSettings/GraphicsSettings.asset"
    description:
      zh: >
          图形设置：URP 渲染管线资产与始终包含的着色器。
          
      en: >
          Graphics settings: URP render pipeline assets and always-included shaders.
          
  - protocol: file
    path: "ProjectSettings/QualitySettings.asset"
    description:
      zh: >
          质量等级：URP 各质量档及其渲染缩放。
          
      en: >
          Quality levels: URP quality tiers and their render scale.
          
  - protocol: file
    path: "ProjectSettings/URPProjectSettings.asset"
    description:
      zh: >
          URP 工程级设置（着色器剔除、Volume 默认值）。
          
      en: >
          URP project-wide settings (shader stripping, volume defaults).
          
  - protocol: file
    path: "ProjectSettings/ShaderGraphSettings.asset"
    description:
      zh: >
          ShaderGraph 设置（410 字节）。
          
      en: >
          ShaderGraph settings (410 bytes).
          
  - protocol: file
    path: "ProjectSettings/VFXManager.asset"
    description:
      zh: >
          VFX 管理器设置，目前未使用任何 VFX 资产。
          
      en: >
          VFX manager settings, no VFX asset in use yet.
          
---
