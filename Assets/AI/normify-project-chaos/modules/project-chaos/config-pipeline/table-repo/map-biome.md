---
uid: 9e0d0103
id: project-chaos.config-pipeline.table-repo.map-biome
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "MapBiome 地图主题表", en: "MapBiome Workbook"}
description:
  zh: >
      MapBiome_地图主题表.xlsx → MapBiomeConfig：Id / themeKey（美术瓦片主题 Key，与 TileMap 目录名一致）/ nameCN / layer（1 地下 2 地表 3 地表建筑）/ retryMax（整图重生成上限）/ enable。
      
  en: >
      MapBiome_地图主题表.xlsx -> MapBiomeConfig: Id / themeKey (art-tile theme key, must match the TileMap directory name) / nameCN / layer (1 underground, 2 surface, 3 surface building) / retryMax (whole-map regeneration limit) / enable.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.607Z"
fingerprint: 210ee6cc2f146bdb72b63c46d28a0d38258987ec2495f231428044d7195f1c6e
source:
  - path: "Assets/Excel/Chaos_excel/MapBiome_地图主题表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/MapBiome_地图主题表.xlsx"
    description:
      zh: >
          地图主题表工作簿：地图主题（生物群系）定义。
          
      en: >
          MapBiome workbook: map theme (biome) definitions.
          
---
