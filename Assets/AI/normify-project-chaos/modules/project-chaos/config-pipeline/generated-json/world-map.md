---
uid: cf000210
id: project-chaos.config-pipeline.generated-json.world-map
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "世界观地图 Json", en: "World Map JSON"}
description:
  zh: >
      世界观地图本体、势力范围与标签的运行时 Json 产物。
      
  en: >
      Runtime JSON for the world map layout, faction areas and labels.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.431Z"
fingerprint: 127d19f1d3e5d2bca2854b9d6147abc06f58b074a3b28fb476d21036c2a1fe93
source:
  - path: "Assets/Resources/Data/Json/Runtime/WorldMapTable_WorldMap.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldMapTable_WorldFaction.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldMapTable_WorldFactionLabel.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/WorldMapTable_WorldMap.json"
    description:
      zh: >
          世界观地图运行时 Json
          
      en: >
          World map runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.world-map
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
