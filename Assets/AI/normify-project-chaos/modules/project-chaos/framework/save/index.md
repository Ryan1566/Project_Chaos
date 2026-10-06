---
uid: f1c0000e
id: project-chaos.framework.save
parent: project-chaos.framework
name: {zh: "存档槽位", en: "Save Slots"}
description:
  zh: >
      存档槽位体系：SaveSlotService 固定 1/2/3 三槽（读元信息与负载、写槽、新建、删除、找第一个非空槽、损坏槽降级）、SaveSlotMeta/SavePayload/SaveSlotFile/SaveSlotInfo 数据结构，以及跨场景会话单例 GameSession（当前槽、负载、目标场景名）。
  en: >
      Save-slot stack: SaveSlotService with fixed slots 1/2/3 (read meta+payload, write, create, delete, find first non-empty, corrupt-slot fallback), the SaveSlotMeta/SavePayload/SaveSlotFile/SaveSlotInfo data types, and the cross-scene GameSession singleton (current slot, payload, target scene).
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
