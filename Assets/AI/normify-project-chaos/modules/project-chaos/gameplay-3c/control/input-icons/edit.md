---
uid: f1c0002b
id: project-chaos.gameplay-3c.control.input-icons.edit
parent: project-chaos.gameplay-3c.control.input-icons
name: {zh: "映射表维护", en: "Icon Map Maintenance"}
description:
  zh: >
      映射表维护：精确查找、缺项补录、条目排序与精灵收集（供编辑器窗口与图集构建）。
      
  en: >
      Icon map maintenance: exact lookup, get-or-add entry for backfilling, deterministic sorting and sprite collection.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.548Z"
fingerprint: 2023bd6e048d39c1e0e4e46f86e528400a6368e0a22057657e7f1dfae14dab60
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/KeyIconMap.cs"
    line: 216
    end_line: 284
apis:
  - protocol: rpc
    path: "KeyIconMap.FindEntryExact"
    description:
      zh: >
          严格匹配查找（不降级）。
          
      en: >
          Exact-match lookup without fallback.
          
  - protocol: rpc
    path: "KeyIconMap.GetOrAddEntry"
    description:
      zh: >
          取条目，不存在则新建（供编辑器工具补录）。
          
      en: >
          Gets or creates an entry (used by editor tooling).
          
  - protocol: rpc
    path: "KeyIconMap.SortEntries"
    description:
      zh: >
          按规则排序条目。
          
      en: >
          Sorts entries deterministically.
          
  - protocol: rpc
    path: "KeyIconMap.CollectSprites"
    description:
      zh: >
          收集全部条目引用的精灵。
          
      en: >
          Collects all sprites referenced by entries.
          
deps:
  - kind: reference
    to: project-chaos.gameplay-3c.control.input-icons.map-asset
    from_api: "rpc:KeyIconMap.SortEntries"
    to_api: "rpc:KeyIconMap.style"
    label: {zh: "图标映射表", en: "Icon map asset"}
---
