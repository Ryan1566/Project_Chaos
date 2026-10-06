---
uid: f1c0002b
id: project-chaos.framework.input-icons.edit
parent: project-chaos.framework.input-icons
name: {zh: "映射表维护", en: "Icon Map Maintenance"}
description:
  zh: >
      映射表维护：精确查找、缺项补录、条目排序与精灵收集（供编辑器窗口与图集构建）。
      
  en: >
      Icon map maintenance: exact lookup, get-or-add entry for backfilling, deterministic sorting and sprite collection.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.744Z"
fingerprint: dce2712ad67121c6bd9ec2cabd8da2255fe257ba39608aa239a2482f97fe189e
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/KeyIconMap.cs"
    line: 216
    end_line: 285
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
    to: project-chaos.framework.input-icons.map-asset
    from_api: "rpc:KeyIconMap.SortEntries"
    to_api: "rpc:KeyIconMap.style"
    label: {zh: "图标映射表", en: "Icon map asset"}
---
