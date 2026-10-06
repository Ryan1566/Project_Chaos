---
uid: f1c00061
id: project-chaos.framework.dev-tests.pool-probes
parent: project-chaos.framework.dev-tests
name: {zh: "对象池自测探针", en: "Pool Probes"}
description:
  zh: >
      对象池自测探针：左键点击从池取 Cube（首次经 ResManager 实例化），归位探针在 1 秒后把自身归还到池。
      
  en: >
      Pool dev probes: left click takes a "Cube" from the pool via ResManager; the delete probe returns itself to the pool after 1s.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.742Z"
fingerprint: a3fdd65723c921676b693481ab503c84df0118eeaafd70eedf0da9ffb78fc8a8
source:
  - path: "Assets/Scripts/Runtime/GameTest/ObjPoolTest/ObjPoolTest.cs"
    line: 1
    end_line: 37
  - path: "Assets/Scripts/Runtime/GameTest/ObjPoolTest/ObjPoolDeleteTest.cs"
    line: 1
    end_line: 18
apis:
  - protocol: rpc
    path: "ObjPoolDeleteTest.PushObj"
    description:
      zh: >
          延迟把自身归还到对象池。
          
      en: >
          Returns this object to its pool (delayed invoke).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/ObjPoolTest/ObjPoolTest.cs"
    description:
      zh: >
          鼠标点击取对象的池探针。
          
      en: >
          Mouse-click pool take probe.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/ObjPoolTest/ObjPoolDeleteTest.cs"
    description:
      zh: >
          对象归还到池的探针。
          
      en: >
          Object return-to-pool probe.
          
deps:
  - kind: call
    to: project-chaos.framework.pool.manager
    from_api: "rpc:ObjPoolDeleteTest.PushObj"
    to_api: "rpc:PoolManager.GetObj"
    label: {zh: "归还对象池", en: "Return to pool"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "file:Assets/Scripts/Runtime/GameTest/ObjPoolTest/ObjPoolTest.cs"
    to_api: "rpc:GlobalPath.res_TestPath"
    label: {zh: "测试预制体目录", en: "Test prefab dir"}
---
