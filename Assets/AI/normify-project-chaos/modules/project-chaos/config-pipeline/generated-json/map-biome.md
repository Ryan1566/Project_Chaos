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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.604Z"
fingerprint: 14c49d69be5b3ee3a4401365999fee19d5ccd7aafcc4cb22b4b1a242ad5c5740
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
