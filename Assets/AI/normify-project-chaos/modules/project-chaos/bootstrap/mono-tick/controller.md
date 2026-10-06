---
uid: 5c1a7e03
id: project-chaos.bootstrap.mono-tick.controller
parent: project-chaos.bootstrap.mono-tick
tags: [bootstrap, tick]
name: {zh: "心跳控制器", en: "Tick Controller"}
description:
  zh: >
      真正挂在 GameObject 上的 MonoBehaviour：每帧 Update 里扇出 updateEvent，并把宿主对象标记为 DontDestroyOnLoad，让订阅者跨场景存活。
      
  en: >
      The actual MonoBehaviour on a GameObject: fans out updateEvent every Update and marks its host DontDestroyOnLoad so subscribers survive scene switches.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.722Z"
fingerprint: 2a9d77d41b667e893ee9e634a008fe3c44b2c9b7efc778e3a0ba8c126ed04683
source:
  - path: "Assets/Scripts/Runtime/GameBase/MonoTool/MonoController.cs"
    line: 8
    end_line: 45
apis:
  - protocol: rpc
    path: "MonoController.AddUpdateListener"
    description:
      zh: >
          注册每帧回调。
          
      en: >
          Registers a per-frame callback.
          
  - protocol: rpc
    path: "MonoController.RemoveUpdateListener"
    description:
      zh: >
          注销每帧回调。
          
      en: >
          Unregisters a per-frame callback.
          
  - protocol: rpc
    path: "MonoController.Clear"
    description:
      zh: >
          清空全部每帧回调。
          
      en: >
          Clears all per-frame callbacks.
          
  - protocol: rpc
    path: "MonoController.updateEvent"
    description:
      zh: >
          每帧扇出的事件（UnityAction）。
          
      en: >
          Per-frame fan-out event (UnityAction).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/MonoTool/MonoController.cs#L8-L45"
    description:
      zh: >
          心跳控制器全文。
          
      en: >
          Whole tick controller source.
          
---
