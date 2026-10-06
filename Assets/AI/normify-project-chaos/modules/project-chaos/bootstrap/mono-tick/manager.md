---
uid: 5c1a7e04
id: project-chaos.bootstrap.mono-tick.manager
parent: project-chaos.bootstrap.mono-tick
tags: [bootstrap, tick, singleton]
name: {zh: "心跳管理器", en: "Tick Manager"}
description:
  zh: >
      MonoController 的单例门面（继承 SingletonBase）。构造函数创建一个名为 MonoController 的 GameObject 并挂上控制器，保证唯一性；对外转发帧更新注册与 StartCoroutine。
      
  en: >
      Singleton facade over MonoController (extends SingletonBase). Its constructor creates the uniquely named "MonoController" GameObject and attaches the controller; it forwards update registration and StartCoroutine to the outside world.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.723Z"
fingerprint: 9bb93c0997b8bd054de58b69876c6cd9b768ff9543a7f2b38acbc0e349681313
source:
  - path: "Assets/Scripts/Runtime/GameBase/MonoTool/MonoManager.cs"
    line: 9
    end_line: 52
apis:
  - protocol: rpc
    path: "MonoManager.MonoManager"
    description:
      zh: >
          构造函数：创建唯一 MonoController 宿主对象。
          
      en: >
          Constructor: creates the unique MonoController host object.
          
  - protocol: rpc
    path: "MonoManager.AddUpdateListener"
    description:
      zh: >
          转发注册每帧回调。
          
      en: >
          Forwards per-frame callback registration.
          
  - protocol: rpc
    path: "MonoManager.RemoveUpdateListener"
    description:
      zh: >
          转发注销每帧回调。
          
      en: >
          Forwards per-frame callback removal.
          
  - protocol: rpc
    path: "MonoManager.StartCoroutine"
    description:
      zh: >
          让非 MonoBehaviour 类也能起协程（三个重载）。
          
      en: >
          Lets non-MonoBehaviour classes run coroutines (three overloads).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/MonoTool/MonoManager.cs#L9-L52"
    description:
      zh: >
          心跳管理器全文。
          
      en: >
          Whole tick manager source.
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.singleton.base
    from_api: "rpc:MonoManager.MonoManager"
    to_api: "rpc:SingletonBase.Instance"
    label: {zh: "继承普通单例基类", en: "Extends singleton base"}
  - kind: call
    to: project-chaos.bootstrap.mono-tick.controller
    from_api: "rpc:MonoManager.AddUpdateListener"
    to_api: "rpc:MonoController.AddUpdateListener"
    label: {zh: "转发帧更新委托", en: "Forwards update delegate"}
---
