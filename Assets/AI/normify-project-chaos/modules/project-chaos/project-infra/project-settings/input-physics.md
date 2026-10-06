---
uid: d5c80f12
id: project-chaos.project-infra.project-settings.input-physics
parent: project-chaos.project-infra.project-settings
name: {zh: "输入与物理", en: "Input & Physics"}
description:
  zh: >
      输入与物理配置：旧版输入轴 + 2D/3D 物理 + 集群输入设置。
      
  en: >
      Input and physics configuration: legacy input axes plus 2D/3D physics and cluster input settings.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.789Z"
fingerprint: 02de1827ff973665c969b20c0c913139f68bff05b478398ab455eb36731cb709
source:
  - path: "ProjectSettings/InputManager.asset"
  - path: "ProjectSettings/Physics2DSettings.asset"
  - path: "ProjectSettings/DynamicsManager.asset"
  - path: "ProjectSettings/ClusterInputManager.asset"
apis:
  - protocol: file
    path: "ProjectSettings/InputManager.asset"
    description:
      zh: >
          旧版 InputManager 轴配置（9.7KB）——在已有 Input System 包的情况下仍然保留。
          
      en: >
          Legacy InputManager axes (9.7 KB) — still present alongside the Input System package.
          
  - protocol: file
    path: "ProjectSettings/Physics2DSettings.asset"
    description:
      zh: >
          2D 物理设置：重力、层碰撞矩阵。
          
      en: >
          2D physics settings: gravity, layer collision matrix.
          
  - protocol: file
    path: "ProjectSettings/DynamicsManager.asset"
    description:
      zh: >
          3D 物理动力学设置（工程为 2D，基本为默认值）。
          
      en: >
          3D physics dynamics settings (project is 2D, so mostly defaults).
          
  - protocol: file
    path: "ProjectSettings/ClusterInputManager.asset"
    description:
      zh: >
          集群输入管理器，本工程未使用。
          
      en: >
          Cluster input manager, unused here.
          
---
