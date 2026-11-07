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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.749Z"
fingerprint: c11d341f13f442ad4c81a2e57d2833d76598d5112b05d252706445ca41dec428
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
