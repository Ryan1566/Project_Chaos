---
uid: a3c00031
id: project-chaos.gameplay-3c.control.input.gameplay-scope
parent: project-chaos.gameplay-3c.control.input
tags: [input, scope, gameplay]
name: {zh: "局内输入作用域", en: "Gameplay Input Scope"}
description:
  zh: >
      InputManager 的局内分期（独立文件的 partial class）：按 action 划分绑定作用域（Global/InRun/Outside，局外时 Move 重定向到 VillageMove）、多所有者的局内输入阻断（允许多个 UI 各自加锁）、输入闩锁刷新，以及玩法侧读取原语（ReadMove、WasGameplayPressed、IsPointerAttackHeld、导航按下/消费）。
      
  en: >
      The gameplay half of InputManager (a partial class in its own file): per-action binding scopes (Global/InRun/Outside) with Move redirected to VillageMove outside a run, multi-owner gameplay input blocking for overlapping UI, latch refreshing and the read primitives gameplay code uses (ReadMove, WasGameplayPressed, IsPointerAttackHeld, navigation press/consume).
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.748Z"
fingerprint: fd389284fc5aee2502fdbfac50b5b1f1705a4fd19a52291551b47a782a983214
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.Gameplay.cs"
apis:
  - protocol: rpc
    path: "InputManager.ReadMove"
    description:
      zh: >
          ReadMove()：当前作用域下的横向移动意图。
          
      en: >
          ReadMove(): horizontal move intent for the current scope.
          
  - protocol: rpc
    path: "InputManager.WasGameplayPressed"
    description:
      zh: >
          WasGameplayPressed(action)：受阻断所有者约束的局内按键边沿。
          
      en: >
          WasGameplayPressed(action): edge-detected gameplay press that respects the blocking owners.
          
  - protocol: rpc
    path: "InputManager.SetGameplayInputBlocked"
    description:
      zh: >
          SetGameplayInputBlocked(owner, blocked)：UI 各自阻断并只释放自己的锁。
          
      en: >
          SetGameplayInputBlocked(owner, blocked): UI owners block independently and release only their own block.
          
  - protocol: rpc
    path: "InputManager.BindingScope"
    description:
      zh: >
          BindingScope：Global / InRun / Outside 三分作用域，按 action 解析。
          
      en: >
          BindingScope: Global, InRun or Outside, resolved per action.
          
  - protocol: rpc
    path: "InputManager.CanReadGameplayInput"
    description:
      zh: >
          CanReadGameplayInput：被阻断或处于输入边界帧时为 false。
          
      en: >
          CanReadGameplayInput: false while blocked or within the input-boundary frame.
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "读切图门控", en: "Reads the transition gate"}
  - kind: reference
    to: project-chaos.framework.runmap
    label: {zh: "读地图就绪状态", en: "Reads map readiness"}
---
