---
uid: f1c00027
id: project-chaos.framework.input.tick
parent: project-chaos.framework.input
name: {zh: "按键轮询", en: "Key Polling"}
description:
  zh: >
      按键轮询：StartTickOrNot 开关每帧检测，轮询到按下/松开时抛出事件中心的 GetKeyDown/GetKeyUp 事件，同时服务改键取消键判定。
      
  en: >
      Key polling: StartTickOrNot toggles the per-frame tick that raises GetKeyDown/GetKeyUp events and serves rebind cancel detection.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.747Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 1088
    end_line: 1183
apis:
  - protocol: rpc
    path: "InputManager.StartTickOrNot"
    description:
      zh: >
          开启/关闭按帧的按键轮询。
          
      en: >
          Enables or disables per-frame key polling.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs#L1088-L1183"
    description:
      zh: >
          按键轮询 Tick、取消键边沿与 KeyCode 检查实现段。
          
      en: >
          Key polling loop and cancel-key edge detection.
          
deps:
  - kind: event
    to: project-chaos.framework.events.const-names
    from_api: "rpc:InputManager.StartTickOrNot"
    to_api: "rpc:EventConstName.GetKeyDown"
    label: {zh: "按键按下事件", en: "Key down event"}
---
