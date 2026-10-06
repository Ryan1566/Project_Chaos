---
uid: f1c00049
id: project-chaos.framework.logging.levels
parent: project-chaos.framework.logging
name: {zh: "日志分级快捷方法", en: "Log Level Shortcuts"}
description:
  zh: >
      ChaosLog 分级快捷方法：Debug/Info/Success/Warn/WarnTrace/Error，各自带频道、上下文对象与调用者信息重载。
      
  en: >
      ChaosLog level shortcuts: Debug/Info/Success/Warn/WarnTrace/Error, each with optional channel, context and caller info overloads.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.751Z"
fingerprint: 9a18fb3ce66bbd1c9cbcb823f436fa9c13dd6baa63e576304feeabfb8f49c2e0
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/ChaosLog.cs"
    line: 339
    end_line: 511
apis:
  - protocol: rpc
    path: "ChaosLog.Debug"
    description:
      zh: >
          Debug 级日志（仅编辑器/开发版）。
          
      en: >
          Debug-level log (editor/dev only).
          
  - protocol: rpc
    path: "ChaosLog.Info"
    description:
      zh: >
          Info 级日志。
          
      en: >
          Info-level log.
          
  - protocol: rpc
    path: "ChaosLog.Success"
    description:
      zh: >
          Success 级日志。
          
      en: >
          Success-level log.
          
  - protocol: rpc
    path: "ChaosLog.Warn"
    description:
      zh: >
          Warn 级日志。
          
      en: >
          Warn-level log.
          
  - protocol: rpc
    path: "ChaosLog.WarnTrace"
    description:
      zh: >
          Warn 级日志并强制附堆栈。
          
      en: >
          Warn-level log with a forced stack trace.
          
  - protocol: rpc
    path: "ChaosLog.Error"
    description:
      zh: >
          Error 级日志。
          
      en: >
          Error-level log.
          
deps:
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:ChaosLog.Info"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "输出管线", en: "Emit pipeline"}
---
