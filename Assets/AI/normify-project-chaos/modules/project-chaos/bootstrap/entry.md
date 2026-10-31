---
uid: 5c1a7e01
id: project-chaos.bootstrap.entry
parent: project-chaos.bootstrap
tags: [bootstrap, startup]
name: {zh: "启动入口", en: "Entry Point"}
description:
  zh: >
      Entry 只做一次 UIManager → InputManager → SettingsManager 初始化（static _booted）并路由首屏。从局内重进 EnterScene 不得重复初始化；由于 Entry 挂在会被 DontDestroy 守卫销毁的常驻 Managers 子树下，二次路由挂在静态 SceneManager.sceneLoaded 回调上（幂等，Start 兼底）。
      
  en: >
      Entry initialises UIManager → InputManager → SettingsManager once (static _booted) and routes the first screen. Re-entering EnterScene from a run must NOT re-initialise the managers; because Entry lives under the persistent Managers root that gets destroyed by the DontDestroy guard, the re-entry routing hangs off a static SceneManager.sceneLoaded callback (idempotent, with Start as fallback).
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:57:59.777Z"
fingerprint: a8949980ba7392b442a4942cdb2f8fcc8098b31714c9dd9037da494c88085acb
source:
  - path: "Assets/Scripts/Runtime/Entry.cs"
    line: 5
    end_line: 18
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/Entry.cs#L5-L18"
    description:
      zh: >
          启动入口脚本全文（MonoBehaviour Entry）。
          
      en: >
          Whole entry script (MonoBehaviour Entry).
          
  - protocol: rpc
    path: "Entry.Start"
    description:
      zh: >
          Unity 启动回调：依次初始化 UI、输入、设置三个管理器。
          
      en: >
          Unity start callback: initialises UI, input and settings managers in order.
          
---
