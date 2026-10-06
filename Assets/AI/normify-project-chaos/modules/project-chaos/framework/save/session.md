---
uid: f1c0005a
id: project-chaos.framework.save.session
parent: project-chaos.framework.save
name: {zh: "游戏会话", en: "Game Session"}
description:
  zh: >
      GameSession 跨场景会话单例：携带当前存档槽、已加载负载、目标场景名与 HasSession 标记，用于读档后跨场景传递。
      
  en: >
      GameSession: cross-scene singleton carrying the selected slot, loaded payload, target scene name and HasSession flag.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.753Z"
fingerprint: 6baf7038d2c4f1dbfd69c0216b575c31510d4f5b9ca559d5a6b6502d963bbb07
source:
  - path: "Assets/Scripts/Runtime/GameBase/SaveManager/GameSession.cs"
    line: 1
    end_line: 81
apis:
  - protocol: rpc
    path: "GameSession.Begin"
    description:
      zh: >
          用槽位、负载与目标场景开始会话。
          
      en: >
          Starts a session with slot, payload and target scene.
          
  - protocol: rpc
    path: "GameSession.SetTargetScene"
    description:
      zh: >
          只设置待切换的目标场景名。
          
      en: >
          Sets only the pending target scene name.
          
  - protocol: rpc
    path: "GameSession.Clear"
    description:
      zh: >
          清空会话。
          
      en: >
          Clears the session.
          
  - protocol: rpc
    path: "GameSession.SelectedSlot"
    description:
      zh: >
          当前选中的槽位序号。
          
      en: >
          Currently selected slot index.
          
deps:
  - kind: reference
    to: project-chaos.framework.save.slot-data
    from_api: "rpc:GameSession.Begin"
    to_api: "rpc:SavePayload.version"
    label: {zh: "负载模型", en: "Payload model"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:GameSession.Begin"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "会话日志", en: "Session logging"}
---
