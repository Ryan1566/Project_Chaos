---
uid: 5c1a7e08
id: project-chaos.bootstrap.singleton.auto-mono
parent: project-chaos.bootstrap.singleton
tags: [bootstrap, singleton]
name: {zh: "自动 Mono 单例", en: "Auto Mono Singleton"}
description:
  zh: >
      继承 MonoBehaviour 的单例基类，但首次 GetInstance() 时自动 new GameObject + AddComponent<T> 并 DontDestroyOnLoad，调用方不需要手动摆放。
      
  en: >
      MonoBehaviour singleton base that auto-creates a GameObject, adds T and marks it DontDestroyOnLoad on the first GetInstance() call — callers never place it manually.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.725Z"
fingerprint: d2d1b4f3b4cbca5419b3cd950cc5fa178450caccbdc5d33b1ffdeada3bc57ed7
source:
  - path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonAutoMono.cs"
    line: 7
    end_line: 23
apis:
  - protocol: rpc
    path: "SingletonAutoMono.GetInstance"
    description:
      zh: >
          取实例；不存在时自动创建并跨场景保留。
          
      en: >
          Returns the instance, auto-creating it across scenes.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonAutoMono.cs#L7-L23"
    description:
      zh: >
          自动 Mono 单例基类全文。
          
      en: >
          Whole auto Mono singleton base source.
          
---
