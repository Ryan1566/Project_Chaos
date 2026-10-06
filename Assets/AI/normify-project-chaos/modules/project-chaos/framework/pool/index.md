---
uid: f1c0000c
id: project-chaos.framework.pool
parent: project-chaos.framework
name: {zh: "对象池", en: "GameObject Pool"}
description:
  zh: >
      PoolManager 对象池：按名字分池（PoolData 持有池根节点与空闲列表），GetObj 从池取或经 ResManager 异步实例化、PushObj 回收、Clear 清空；注意 GetObj 返回值恒为 null，实例通过回调交付。
  en: >
      PoolManager: pools keyed by name (PoolData holds the pool root and free list); GetObj reuses from the pool or instantiates via ResManager, PushObj returns objects, Clear empties all. Note GetObj always returns null — the instance is delivered through the callback.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
