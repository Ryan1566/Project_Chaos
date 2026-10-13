---
uid: 9e0d0004
id: project-chaos.project-infra.agent-workspace.runmap-art.generator
parent: project-chaos.project-infra.agent-workspace.runmap-art
tags: [runmap, tooling]
name: {zh: "配表生成脚本与草稿", en: "Config Generator & Drafts"}
description:
  zh: >
      生成地图配置表的 Python 脚本 gen_map_configs.py，以及 xlsx/ 下 7 张表草稿（MapPreset / PresetLayer / MapBiome / MapChunk / ChunkSocket / ChunkSlot / MapLoading）。这里是**草稿**，正式源表在 Assets/Excel/Chaos_excel/（见 config-pipeline.table-repo）。
      
  en: >
      The Python script that generates the map config tables, plus the seven xlsx drafts under xlsx/ (MapPreset / PresetLayer / MapBiome / MapChunk / ChunkSocket / ChunkSlot / MapLoading). These are drafts — the authoritative source tables live in Assets/Excel/Chaos_excel/ (see config-pipeline.table-repo).
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.445Z"
fingerprint: 8d0ccedecf3ccbe371a518edf78c4d8e644a69b573e1ba4ced2a5b04426be88d
source:
  - path: "Assets/AI/runmap-art/gen_map_configs.py"
apis:
  - protocol: file
    path: "Assets/AI/runmap-art/gen_map_configs.py"
    description:
      zh: >
          地图配置表生成脚本（与 xlsx/ 下 7 张表草稿配套）。
          
      en: >
          Python generator for the map config tables (pairs with the seven xlsx drafts in xlsx/).
          
  - protocol: file
    path: "Assets/AI/runmap-art/xlsx/MapPreset_原型图主表.xlsx"
    description:
      zh: >
          原型图主表（MapPreset）草稿。
          
      en: >
          Draft of the map-preset master table (MapPreset).
          
  - protocol: file
    path: "Assets/AI/runmap-art/xlsx/ChunkSocket_房间块接口表.xlsx"
    description:
      zh: >
          房间块接口表（ChunkSocket）草稿。
          
      en: >
          Draft of the chunk-socket table.
          
---
