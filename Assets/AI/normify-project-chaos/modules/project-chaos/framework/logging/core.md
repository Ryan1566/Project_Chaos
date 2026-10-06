---
uid: f1c00048
id: project-chaos.framework.logging.core
parent: project-chaos.framework.logging
name: {zh: "日志输出核心", en: "Log Emit Core"}
description:
  zh: >
      ChaosLog 输出核心：等级过滤、频道与颜色拼接、调用点截取、内联堆栈展开与线程安全构建器。
      
  en: >
      ChaosLog emit core: level filtering, channel/color formatting, call-site extraction, inline stack traces and the threaded builder.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.751Z"
fingerprint: 9a18fb3ce66bbd1c9cbcb823f436fa9c13dd6baa63e576304feeabfb8f49c2e0
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/ChaosLog.cs"
    line: 116
    end_line: 338
apis:
  - protocol: rpc
    path: "ChaosLog.Write"
    description:
      zh: >
          开始一条链式日志（Write(...).Channel().Color().Info()）。
          
      en: >
          Starts a fluent log line (Write(...).Channel().Color().Info()).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/LogManager/ChaosLog.cs#L116-L298"
    description:
      zh: >
          日志输出管线、等级前缀与内联堆栈实现段。
          
      en: >
          Emit pipeline, level prefix, inline trace block.
          
deps:
  - kind: reference
    to: project-chaos.framework.logging.channels
    from_api: "rpc:ChaosLog.Write"
    to_api: "rpc:LogChannelInfo.GetName"
    label: {zh: "频道表", en: "Channel table"}
  - kind: call
    to: project-chaos.framework.logging.builder-chain
    from_api: "rpc:ChaosLog.Write"
    to_api: "rpc:LogBuilder.Channel"
    label: {zh: "链式构建器", en: "Fluent builder"}
---
