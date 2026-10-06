---
uid: f1c00060
id: project-chaos.framework.dev-tests.input-probe
parent: project-chaos.framework.dev-tests
name: {zh: "输入自测探针", en: "Input Probe"}
description:
  zh: >
      输入自测探针：开启 InputManager 轮询、订阅 GetKeyDown 事件并打印按下的键（Escape/W）。
      
  en: >
      Input dev probe: enables InputManager polling, subscribes to GetKeyDown and logs which key was pressed (Escape/W).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.741Z"
fingerprint: b0349d3689646a05d0d98c2a1e2399e8e91e43315763564b862724ad00bbbf7e
source:
  - path: "Assets/Scripts/Runtime/GameTest/InputTest/InputTest.cs"
    line: 1
    end_line: 48
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/InputTest/InputTest.cs"
    description:
      zh: >
          输入轮询与按键事件探针。
          
      en: >
          Input polling + key event probe.
          
deps:
  - kind: call
    to: project-chaos.framework.input.tick
    from_api: "file:Assets/Scripts/Runtime/GameTest/InputTest/InputTest.cs"
    to_api: "rpc:InputManager.StartTickOrNot"
    label: {zh: "按键轮询", en: "Key polling"}
  - kind: call
    to: project-chaos.framework.events.manager
    from_api: "file:Assets/Scripts/Runtime/GameTest/InputTest/InputTest.cs"
    to_api: "rpc:EventManager.AddListener"
    label: {zh: "监听按键事件", en: "Listen key event"}
  - kind: reference
    to: project-chaos.framework.events.const-names
    from_api: "file:Assets/Scripts/Runtime/GameTest/InputTest/InputTest.cs"
    to_api: "rpc:EventConstName.GetKeyDown"
    label: {zh: "事件名", en: "Event name"}
---
