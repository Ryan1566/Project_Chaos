---
uid: f1c00029
id: project-chaos.framework.input-icons.cache
parent: project-chaos.framework.input-icons
name: {zh: "图标映射缓存", en: "Icon Map Cache"}
description:
  zh: >
      KeyIconMap 缓存：Active 懒加载访问、Load 显式加载、ResetCache 清缓存。
      
  en: >
      KeyIconMap caching: Active accessor with lazy Resources loading, Load and ResetCache.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.744Z"
fingerprint: dce2712ad67121c6bd9ec2cabd8da2255fe257ba39608aa239a2482f97fe189e
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/KeyIconMap.cs"
    line: 121
    end_line: 163
apis:
  - protocol: rpc
    path: "KeyIconMap.Active"
    description:
      zh: >
          当前生效的映射表实例（自动从 Resources 加载）。
          
      en: >
          The active map instance (auto-loads from Resources).
          
  - protocol: rpc
    path: "KeyIconMap.Load"
    description:
      zh: >
          从 Resources/Data/Input/KeyIconMap 加载映射表。
          
      en: >
          Loads the KeyIconMap asset from Resources.
          
  - protocol: rpc
    path: "KeyIconMap.ResetCache"
    description:
      zh: >
          清空缓存实例。
          
      en: >
          Clears the cached instance.
          
deps:
  - kind: reference
    to: project-chaos.framework.input-icons.map-asset
    from_api: "rpc:KeyIconMap.Load"
    to_api: "rpc:KeyIconMap.style"
    label: {zh: "图标映射表", en: "Icon map asset"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:KeyIconMap.Load"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "加载告警日志", en: "Load warning log"}
---
