---
uid: 5c1a7e07
id: project-chaos.bootstrap.singleton.mono
parent: project-chaos.bootstrap.singleton
tags: [bootstrap, singleton]
name: {zh: "Mono 单例基类", en: "Mono Singleton"}
description:
  zh: >
      继承 MonoBehaviour 的单例基类：实例由 Unity 在 Awake 里写回静态字段，外部用 GetInstance() 取；不自动创建对象，需要手动挂到场景里。
      
  en: >
      Singleton base for MonoBehaviours: Unity writes the instance into a static field during Awake and callers use GetInstance(). It never auto-creates a GameObject — the component must be placed in the scene manually.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.726Z"
fingerprint: cb01d77795ee5a5efa9028f4a75a0a55c52e53f8e9eecea1c231f6cb8d3b773e
source:
  - path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonMono.cs"
    line: 6
    end_line: 21
apis:
  - protocol: rpc
    path: "SingletonMono.GetInstance"
    description:
      zh: >
          取当前已挂载的实例（未挂载时为 null）。
          
      en: >
          Returns the attached instance (null when unattached).
          
  - protocol: rpc
    path: "SingletonMono.Awake"
    description:
      zh: >
          Awake 里把 this 写回静态实例字段。
          
      en: >
          Writes this into the static instance field in Awake.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonMono.cs#L6-L21"
    description:
      zh: >
          Mono 单例基类全文。
          
      en: >
          Whole Mono singleton base source.
          
---
