---
uid: f1c00054
id: project-chaos.framework.pool.data
parent: project-chaos.framework.pool
name: {zh: "池数据", en: "Pool Data"}
description:
  zh: >
      PoolData 单个对象池：持有池根节点与空闲列表，提供取用与归还。
      
  en: >
      PoolData: one named pool holding its root GameObject and free list, with take/return helpers.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.751Z"
fingerprint: f03d0c3c98a00b3fed7488a60428ce24496e6a69cde5958034233ba7098aa650
source:
  - path: "Assets/Scripts/Runtime/GameBase/GameObjectPool/PoolManager.cs"
    line: 1
    end_line: 42
apis:
  - protocol: rpc
    path: "PoolData.GetPool"
    description:
      zh: >
          从本池取一个对象。
          
      en: >
          Pops one object from this pool.
          
  - protocol: rpc
    path: "PoolData.PushObj"
    description:
      zh: >
          把对象放回本池。
          
      en: >
          Pushes one object back into this pool.
          
  - protocol: rpc
    path: "PoolData.fatherObj"
    description:
      zh: >
          池根节点。
          
      en: >
          Pool root GameObject in the hierarchy.
          
---
