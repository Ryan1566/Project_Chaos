---
uid: f1c00057
id: project-chaos.framework.save.slot-data
parent: project-chaos.framework.save
name: {zh: "存档槽位数据结构", en: "Save Slot Data Types"}
description:
  zh: >
      存档落盘结构。SavePayload 现含 version 与 uint era = 1（当前时期，决定世界观地图显示哪一时期的势力层）。追加字段=零迁移：JsonUtility 对旧存档里缺失的键保留字段初始值。
      
  en: >
      Save payload structure. SavePayload currently carries version plus uint era = 1 (the current era/stage, driving which faction layer the world map shows). Appending a field is a zero-migration change: JsonUtility keeps the field initialiser for keys missing from older saves.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.944Z"
fingerprint: 96b43579715b745fa400a7e1f225cf6a186a15b152da078e6501d7a42619d982
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
