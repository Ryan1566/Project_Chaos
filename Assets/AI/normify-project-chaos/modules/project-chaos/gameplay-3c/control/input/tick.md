---
uid: f1c00027
id: project-chaos.gameplay-3c.control.input.tick
parent: project-chaos.gameplay-3c.control.input
name: {zh: "按键轮询", en: "Key Polling"}
description:
  zh: >
      按键轮询：StartTickOrNot 开关每帧检测，轮询到按下/松开时抛出事件中心的 GetKeyDown/GetKeyUp 事件，同时服务改键取消键判定。
      
  en: >
      Key polling: StartTickOrNot toggles the per-frame tick that raises GetKeyDown/GetKeyUp events and serves rebind cancel detection.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.546Z"
fingerprint: a3e7fdcf7243984b01ff23825e8d60d83ccd36aee697222dab345f0bff3f3cb3
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 1062
    end_line: 1164
apis:
  - protocol: rpc
    path: "InputManager.StartTickOrNot"
    description:
      zh: >
          StartTickOrNot(openFlag)：开关每帧按键轮询。
          
      en: >
          StartTickOrNot(openFlag): enables or disables per-frame key polling.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs#L1062-L1164"
    description:
      zh: >
          本模块在 InputManager.cs 中的源码区段。
          
      en: >
          Source region of this module inside InputManager.cs.
          
deps:
  - kind: event
    to: project-chaos.framework.events.const-names
    from_api: "rpc:InputManager.StartTickOrNot"
    to_api: "rpc:EventConstName.GetKeyDown"
    label: {zh: "按键按下事件", en: "Key down event"}
---
