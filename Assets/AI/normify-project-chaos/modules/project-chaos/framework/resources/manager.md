---
uid: f1c00056
id: project-chaos.framework.resources.manager
parent: project-chaos.framework.resources
name: {zh: "资源加载器", en: "Resource Manager"}
description:
  zh: >
      ResManager：对 Resources.Load/LoadAsync 的薄封装并打错误日志。注意 Load<GameObject> 内部已经 Instantiate。
      
  en: >
      ResManager: thin Resources.Load/LoadAsync wrapper with error logging. Note Load<GameObject> already instantiates the prefab.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.752Z"
fingerprint: bf446302eec75ed3048abc1a327697f211cbc3adba45ceb5264a2d3e7f68191e
source:
  - path: "Assets/Scripts/Runtime/GameBase/ResourcesLoader/ResManager.cs"
    line: 1
    end_line: 69
apis:
  - protocol: rpc
    path: "ResManager.Load"
    description:
      zh: >
          同步加载 Resources 资源。
          
      en: >
          Synchronously loads a Resources asset.
          
  - protocol: rpc
    path: "ResManager.LoadAsync"
    description:
      zh: >
          异步加载 Resources 资源并回调。
          
      en: >
          Asynchronously loads a Resources asset with a callback.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ResourcesLoader/ResManager.cs"
    description:
      zh: >
          资源加载器脚本。
          
      en: >
          Resources loader script asset.
          
deps:
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:ResManager.Load"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "加载日志", en: "Load logging"}
---
