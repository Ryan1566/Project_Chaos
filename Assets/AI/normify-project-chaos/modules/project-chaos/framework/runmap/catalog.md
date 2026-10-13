---
uid: ddb2aa02
id: project-chaos.framework.runmap.catalog
parent: project-chaos.framework.runmap
tags: [runmap, catalog]
name: {zh: "地图生成：块名录", en: "RunMap: Chunk Catalog"}
description:
  zh: >
      房间块与预设名录：按主题/层/类型取块池、层→主题解析、以及保证一局三层齐全的场景分配；现在是灰盒名录，将来换成表装载。
      
  en: >
      Chunk and preset registry: pools by theme/layer/kind, per-layer theme resolution, and run-level preset allocation that guarantees the three layers appear across a run.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.429Z"
fingerprint: fd13e1d3304f665e43f658f2e563a142739006741ada7cd4d222e0b927d8fa81
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapCatalog.cs"
    line: 1
    end_line: 250
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapCatalog.cs#L1-L250"
    description:
      zh: >
          与 18 个灰盒 prefab 严格对应的块/预设名录；后续可直接换成配置表装载。
          
      en: >
          Greybox chunk/preset catalog mirroring the 18 placeholder prefabs; swappable for a config-table loader later.
          
---
