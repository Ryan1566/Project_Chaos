---
uid: f1c00057
id: project-chaos.framework.save.slot-data
parent: project-chaos.framework.save
name: {zh: "存档槽位数据结构", en: "Save Slot Data Types"}
description:
  zh: >
      存档数据结构：SaveSlotMeta（槽位、最后保存 UTC ticks、版本）、SavePayload、SaveSlotFile 文件封装与运行时 SaveSlotInfo 视图。
      
  en: >
      Save file data types: SaveSlotMeta (index, last save ticks, version), SavePayload, SaveSlotFile and the runtime SaveSlotInfo view.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.753Z"
fingerprint: 5e78725ef29f2d32c69b69f7413c73a35ff7e5c87cae25d9395ffcee25c04d57
source:
  - path: "Assets/Scripts/Runtime/GameBase/SaveManager/SaveSlotData.cs"
    line: 1
    end_line: 94
apis:
  - protocol: rpc
    path: "SaveSlotInfo.HasUsableSave"
    description:
      zh: >
          该槽是否有可用存档（非空且未损坏）。
          
      en: >
          Whether the slot holds a usable (non-empty, non-corrupt) save.
          
  - protocol: rpc
    path: "SaveSlotMeta.slotIndex"
    description:
      zh: >
          槽位序号字段（1-3）。
          
      en: >
          Slot index field (1-3).
          
  - protocol: rpc
    path: "SavePayload.version"
    description:
      zh: >
          存档负载版本字段。
          
      en: >
          Payload schema version field.
          
  - protocol: rpc
    path: "SaveSlotFile.meta"
    description:
      zh: >
          槽文件内的元信息块。
          
      en: >
          Metadata block inside a slot file.
          
---
