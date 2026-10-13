---
uid: f1c0004e
id: project-chaos.framework.logging.bootstrap
parent: project-chaos.framework.logging
name: {zh: "日志初始化引导", en: "Log Bootstrap"}
description:
  zh: >
      LogBootstrap：以 RuntimeInitializeOnLoadMethod 与编辑器 InitializeOnLoadMethod 在启动时套用 ChaosLog 默认配置。
      
  en: >
      LogBootstrap: RuntimeInitializeOnLoadMethod plus an editor InitializeOnLoadMethod that configure ChaosLog defaults on startup.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.426Z"
fingerprint: c9bc9718a1bf70d351563773bde68cf887d63b9d61da4f458a85a7fb12599567
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/LogBootstrap.cs"
    line: 1
    end_line: 60
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/LogManager/LogBootstrap.cs"
    description:
      zh: >
          编辑器与运行时日志初始化引导脚本。
          
      en: >
          Editor/runtime log initialization bootstrap script.
          
deps:
  - kind: reference
    to: project-chaos.framework.logging.config
    from_api: "file:Assets/Scripts/Runtime/GameBase/LogManager/LogBootstrap.cs"
    to_api: "rpc:ChaosLog.MinLevel"
    label: {zh: "日志开关", en: "Log switches"}
---
