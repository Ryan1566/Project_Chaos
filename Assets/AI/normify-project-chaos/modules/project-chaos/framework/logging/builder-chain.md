---
uid: f1c0004a
id: project-chaos.framework.logging.builder-chain
parent: project-chaos.framework.logging
name: {zh: "日志构建链式接口", en: "Log Builder Chain"}
description:
  zh: >
      LogBuilder 链式构建：频道、上下文、堆栈与颜色（含 White/Gray/Red 等具名颜色快捷方法）。
      
  en: >
      LogBuilder fluent chain: channel, context, trace and color helpers (named colors via White/Gray/Red/... one-liners).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.749Z"
fingerprint: 6773abdfb8e4707eedb70b2374848144684d79756d69fda72304e0f8cad10558
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/LogBuilder.cs"
    line: 1
    end_line: 135
apis:
  - protocol: rpc
    path: "LogBuilder.Channel"
    description:
      zh: >
          设置日志频道。
          
      en: >
          Sets the log channel.
          
  - protocol: rpc
    path: "LogBuilder.With"
    description:
      zh: >
          附带 Unity 上下文对象。
          
      en: >
          Attaches a Unity context object.
          
  - protocol: rpc
    path: "LogBuilder.Trace"
    description:
      zh: >
          强制附堆栈。
          
      en: >
          Forces a stack trace.
          
  - protocol: rpc
    path: "LogBuilder.Color"
    description:
      zh: >
          按枚举或十六进制设颜色。
          
      en: >
          Sets the color from enum or hex.
          
  - protocol: rpc
    path: "LogBuilder.Hex"
    description:
      zh: >
          按十六进制字符串设颜色。
          
      en: >
          Sets the color by hex string.
          
deps:
  - kind: reference
    to: project-chaos.framework.logging.channels
    from_api: "rpc:LogBuilder.Channel"
    to_api: "rpc:LogChannelInfo.GetTaggedName"
    label: {zh: "频道标签", en: "Channel tag"}
  - kind: reference
    to: project-chaos.framework.logging.colors
    from_api: "rpc:LogBuilder.Color"
    to_api: "rpc:LogColorInfo.GetHex"
    label: {zh: "颜色表", en: "Color table"}
---
