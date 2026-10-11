---
uid: 7c0d0093
id: project-chaos.team-process.system-designs.runmap-data-and-config
parent: project-chaos.team-process.system-designs
tags: [design, runmap, config]
name: {zh: "地图生成：数据与配置表", en: "RunMap: Data & Config"}
description:
  zh: >
      §7–§9：运行时数据结构与存档字段（种子/场景序号/图版本，不存布局）、6 张配置表字段草案（MapBiome/MapPreset/PresetLayer/MapChunk/ChunkSocket/ChunkSlot，12 类型白名单、无 enum、拆表+外键）、UI 与小地图要求。
      
  en: >
      Doc §7–§9: runtime structures and save fields (seed/scene/mapVersion, layout not saved), field drafts for six config tables (whitelist-only types, no enums, split tables with foreign keys), and UI/minimap requirements.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.756Z"
fingerprint: 4e068de8cc72d20c5a6106054d10511656be94f23b535031e6678639a6b6c287
source:
  - path: "Assets/Chaos_Story/Story/02_系统策划/01_局内地图生成系统.md"
    line: 358
    end_line: 539
apis:
  - protocol: file
    path: "Assets/Chaos_Story/Story/02_系统策划/01_局内地图生成系统.md#L358-L539"
    description:
      zh: >
          运行时结构（RunMapContext/RoomNode/RoomEdge/MapLayout/SpawnSlot）、存档字段与图版本兼容、6 张表的字段草案与枚举数字对照、UI 与小地图要求。
          
      en: >
          Runtime structures, save fields and mapVersion compatibility, field drafts for the six tables, and UI/minimap requirements.
          
---
