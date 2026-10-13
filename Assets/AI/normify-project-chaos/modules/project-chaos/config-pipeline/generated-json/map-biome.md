---
uid: 9e0d0203
id: project-chaos.config-pipeline.generated-json.map-biome
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "MapBiome 导出 Json", en: "MapBiome JSON"}
description:
  zh: >
      MapBiome 的运行时导出 Json（Resources 路径 Data/Json/Runtime/MapBiomeConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for MapBiome (Resources path Data/Json/Runtime/MapBiomeConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.401Z"
fingerprint: 3f9cab5b06fc387efce7bcb45d0d270896f498ee185274fe37d82d851f3ec52e
source:
  - path: "Assets/Resources/Data/Json/Runtime/MapBiomeConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/MapBiomeConfig.json"
    description:
      zh: >
          MapBiome 运行时 Json（Resources 路径 Data/Json/Runtime/MapBiomeConfig）。
          
      en: >
          Runtime JSON of MapBiome (loaded via Resources path Data/Json/Runtime/MapBiomeConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-biome
    from_api: "file:Assets/Resources/Data/Json/Runtime/MapBiomeConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/MapBiome_地图主题表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
