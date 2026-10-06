---
uid: f1c0004b
id: project-chaos.framework.logging.builder-emit
parent: project-chaos.framework.logging
name: {zh: "日志构建终结方法", en: "Log Builder Terminals"}
description:
  zh: >
      LogBuilder 终结方法：Emit 与按等级输出。Emit 会反向回调 ChaosLog.Emit（为避免依赖成环，该反向箭头只在描述里记录）。
      
  en: >
      LogBuilder terminals: Emit plus per-level shortcuts that finish the chain and print the line. Emit calls back into ChaosLog.Emit (reverse edge omitted to keep the graph acyclic).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.750Z"
fingerprint: 6773abdfb8e4707eedb70b2374848144684d79756d69fda72304e0f8cad10558
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/LogBuilder.cs"
    line: 136
    end_line: 207
apis:
  - protocol: rpc
    path: "LogBuilder.Emit"
    description:
      zh: >
          按当前等级输出已构建的一行。
          
      en: >
          Emits the built line at its own level.
          
  - protocol: rpc
    path: "LogBuilder.Debug"
    description:
      zh: >
          以 Debug 级输出。
          
      en: >
          Emits as Debug.
          
  - protocol: rpc
    path: "LogBuilder.Info"
    description:
      zh: >
          以 Info 级输出。
          
      en: >
          Emits as Info.
          
  - protocol: rpc
    path: "LogBuilder.Success"
    description:
      zh: >
          以 Success 级输出。
          
      en: >
          Emits as Success.
          
  - protocol: rpc
    path: "LogBuilder.Warn"
    description:
      zh: >
          以 Warn 级输出。
          
      en: >
          Emits as Warn.
          
  - protocol: rpc
    path: "LogBuilder.Error"
    description:
      zh: >
          以 Error 级输出。
          
      en: >
          Emits as Error.
          
deps:
  - kind: reference
    to: project-chaos.framework.logging.channels
    from_api: "rpc:LogBuilder.Emit"
    to_api: "rpc:LogChannelInfo.GetTaggedName"
    label: {zh: "频道标签", en: "Channel tag"}
---
