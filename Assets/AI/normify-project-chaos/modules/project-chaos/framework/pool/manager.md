---
uid: f1c00055
id: project-chaos.framework.pool.manager
parent: project-chaos.framework.pool
name: {zh: "对象池管理", en: "Pool Manager"}
description:
  zh: >
      PoolManager 对象池管理：按名字分池，GetObj 复用或经 ResManager 实例化，PushObj 回收，Clear 清空。已知缺陷：GetObj 返回值恒为 null。
      
  en: >
      PoolManager: dictionary of named pools, GetObj reuses or loads via ResManager, PushObj returns, Clear empties. Known defect: GetObj returns null.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.752Z"
fingerprint: f03d0c3c98a00b3fed7488a60428ce24496e6a69cde5958034233ba7098aa650
source:
  - path: "Assets/Scripts/Runtime/GameBase/GameObjectPool/PoolManager.cs"
    line: 43
    end_line: 115
apis:
  - protocol: rpc
    path: "PoolManager.GetObj"
    description:
      zh: >
          从池取或异步实例化；返回值恒为 null，实例通过回调交付。
          
      en: >
          Takes an object from the pool or instantiates one; returns null, delivers via callback.
          
  - protocol: rpc
    path: "PoolManager.PushObj"
    description:
      zh: >
          把对象归还到对应池。
          
      en: >
          Returns an object to its pool.
          
  - protocol: rpc
    path: "PoolManager.Clear"
    description:
      zh: >
          清空全部对象池。
          
      en: >
          Clears all pools.
          
deps:
  - kind: call
    to: project-chaos.framework.resources.manager
    from_api: "rpc:PoolManager.GetObj"
    to_api: "rpc:ResManager.Load"
    label: {zh: "实例化对象", en: "Instantiate object"}
  - kind: reference
    to: project-chaos.framework.pool.data
    from_api: "rpc:PoolManager.GetObj"
    to_api: "rpc:PoolData.GetPool"
    label: {zh: "池存储", en: "Pool storage"}
---
