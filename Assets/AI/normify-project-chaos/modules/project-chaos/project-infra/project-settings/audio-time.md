---
uid: b703d5e9
id: project-chaos.project-infra.project-settings.audio-time
parent: project-chaos.project-infra.project-settings
name: {zh: "音频时序与内存", en: "Audio, Time & Memory"}
description:
  zh: >
      运行时时序与资源预算：音频、时间步长、内存与 NavMesh 区域定义。
      
  en: >
      Runtime timing and resource budgets: audio, time step, memory and NavMesh area definitions.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:15.063Z"
fingerprint: 9390cbaba3baa0ca640793d4b02f1ffb4b71e0780fdd4c2bb2f45ed3c6711f60
source:
  - path: "ProjectSettings/AudioManager.asset"
  - path: "ProjectSettings/TimeManager.asset"
  - path: "ProjectSettings/MemorySettings.asset"
  - path: "ProjectSettings/NavMeshAreas.asset"
apis:
  - protocol: file
    path: "ProjectSettings/AudioManager.asset"
    description:
      zh: >
          音频管理器：全局音量、DSP 缓冲、空间化器。
          
      en: >
          Audio manager: global volume, DSP buffer, spatializer.
          
  - protocol: file
    path: "ProjectSettings/TimeManager.asset"
    description:
      zh: >
          时间管理器：固定时间步长与最大允许步长。
          
      en: >
          Time manager: fixed timestep and maximum allowed timestep.
          
  - protocol: file
    path: "ProjectSettings/MemorySettings.asset"
    description:
      zh: >
          内存设置：流式加载与内存管理器预算。
          
      en: >
          Memory settings: streaming and memory manager budgets.
          
  - protocol: file
    path: "ProjectSettings/NavMeshAreas.asset"
    description:
      zh: >
          NavMesh 区域与代理半径默认值。
          
      en: >
          NavMesh areas and agent radius defaults.
          
---
