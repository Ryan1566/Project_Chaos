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
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.697Z"
fingerprint: 25cf6584d10aae8a8b80fe8222ef9984888351a69e268fbd8372a583f008586d
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
