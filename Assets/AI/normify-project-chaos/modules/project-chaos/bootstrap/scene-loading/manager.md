---
uid: 5c1a7e0a
id: project-chaos.bootstrap.scene-loading.manager
parent: project-chaos.bootstrap.scene-loading
tags: [bootstrap, scene, loading]
name: {zh: "加载管理器", en: "Load Manager"}
description:
  zh: >
      ScenesLoadManager：LoadScene 同步切换并回调、LoadSceneAsync 用 MonoManager 协程一口气加载并激活、BeginLoadSceneAsync 返回 AsyncOperation 且默认 allowSceneActivation=false（进度最高 0.9，需 Clamp01）。场景名为空或不在 Build Settings 时返回 null 并报错，不抛异常。
      
  en: >
      ScenesLoadManager: LoadScene switches synchronously and invokes the callback; LoadSceneAsync runs through a MonoManager coroutine; BeginLoadSceneAsync returns the AsyncOperation with allowSceneActivation defaulting to false (progress tops out at 0.9, so callers must Clamp01). A null or unregistered scene returns null with an error instead of throwing.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.723Z"
fingerprint: 734f7dbf660a8a647e04661de05ee62ad67d4040d0b6271f4b62a2d2a6e7f4f7
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/ScenesLoadManager.cs"
    line: 9
    end_line: 126
apis:
  - protocol: rpc
    path: "ScenesLoadManager.LoadScene"
    description:
      zh: >
          同步加载场景，随后无条件回调（传 null 会 NRE）。
          
      en: >
          Loads a scene synchronously, then invokes (null throws NRE).
          
  - protocol: rpc
    path: "ScenesLoadManager.LoadSceneAsync"
    description:
      zh: >
          协程异步加载并自动激活。
          
      en: >
          Loads asynchronously via coroutine and auto-activates.
          
  - protocol: rpc
    path: "ScenesLoadManager.BeginLoadSceneAsync"
    description:
      zh: >
          返回 AsyncOperation，调用方自己持控进度与激活时机。
          
      en: >
          Returns AsyncOperation; caller controls progress/activation.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/ScenesLoadManager.cs#L9-L126"
    description:
      zh: >
          加载管理器全文。
          
      en: >
          Whole load manager source.
          
deps:
  - kind: call
    to: project-chaos.bootstrap.mono-tick.manager
    from_api: "rpc:ScenesLoadManager.LoadSceneAsync"
    to_api: "rpc:MonoManager.StartCoroutine"
    label: {zh: "用协程驱动异步加载", en: "Drives async load by coroutine"}
---
