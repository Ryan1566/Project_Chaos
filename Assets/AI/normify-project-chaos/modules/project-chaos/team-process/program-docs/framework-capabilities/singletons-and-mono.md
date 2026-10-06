---
uid: 7c0d0063
id: project-chaos.team-process.program-docs.framework-capabilities.singletons-and-mono
parent: project-chaos.team-process.program-docs.framework-capabilities
tags: [framework, singleton]
name: {zh: "单例与 Mono 驱动", en: "Singletons & MonoManager"}
description:
  zh: >
      Singleton 三件套（选错就是 NRE）与 MonoManager/MonoController（给非 MonoBehaviour 类提供生命周期与协程）。
      
  en: >
      The singleton trio (choosing wrong causes NRE) and MonoManager/MonoController as the tick/coroutine provider for non-MonoBehaviour classes.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.807Z"
fingerprint: b0429f6cb346935a01da5f47629c153cccb8dfa4aa73d19245ec78b3bd70deff
source:
  - path: "Assets/Chaos_Story/Story/05_程序/01_框架能力清单.md"
    line: 251
    end_line: 412
apis:
  - protocol: file
    path: "Assets/Chaos_Story/Story/05_程序/01_框架能力清单.md#L251-L412"
    description:
      zh: >
          三种单例基类的选用与如何用 MonoManager/MonoController 给非 Mono 类生命周与协程。
          
      en: >
          Which singleton base to pick for what, and how MonoManager/MonoController give lifecycle and coroutines to plain classes.
          
---
