---
uid: f1c00047
id: project-chaos.framework.logging.config
parent: project-chaos.framework.logging
name: {zh: "日志开关配置", en: "Log Configuration"}
description:
  zh: >
      ChaosLog 开关配置：总开关、Debug 开关、最低等级（编辑器 Debug / 发布 Warn）、调用点与时间戳显示。
      
  en: >
      ChaosLog switches: Enabled, EnableDebug, MinLevel (Debug in editor / Warn in release), call-site and timestamp toggles.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.750Z"
fingerprint: 9a18fb3ce66bbd1c9cbcb823f436fa9c13dd6baa63e576304feeabfb8f49c2e0
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/ChaosLog.cs"
    line: 1
    end_line: 115
apis:
  - protocol: rpc
    path: "ChaosLog.Enabled"
    description:
      zh: >
          日志总开关。
          
      en: >
          Master switch for all logging.
          
  - protocol: rpc
    path: "ChaosLog.EnableDebug"
    description:
      zh: >
          是否输出 Debug 级日志。
          
      en: >
          Enables Debug-level logs.
          
  - protocol: rpc
    path: "ChaosLog.MinLevel"
    description:
      zh: >
          最低输出等级。
          
      en: >
          Minimum level that gets emitted.
          
  - protocol: rpc
    path: "ChaosLog.ShowCallSite"
    description:
      zh: >
          是否显示调用点。
          
      en: >
          Whether call-site info is shown.
          
  - protocol: rpc
    path: "ChaosLog.ShowTimestamp"
    description:
      zh: >
          是否显示时间戳。
          
      en: >
          Whether timestamps are shown.
          
---
