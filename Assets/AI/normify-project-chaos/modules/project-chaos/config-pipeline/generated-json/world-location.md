---
uid: cf00020f
id: project-chaos.config-pipeline.generated-json.world-location
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "世界地点 Json", en: "World Location JSON"}
description:
  zh: >
      世界地点及其交互、时代分布、关系、临时状态与文案的运行时 Json 产物。
      
  en: >
      Runtime JSON for world locations, actions, era placement, relations, temporary states and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.427Z"
fingerprint: 1480c48db29e7b5ed12d0ef973581dad601b2b8c73e0385b2a94f068defc3b72
source:
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_Location.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_LocationAction.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_LocationEra.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_LocationRelation.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_LocationTemporary.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldLocationTable_LocationText.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/WorldLocationTable_Location.json"
    description:
      zh: >
          世界地点运行时 Json
          
      en: >
          World location runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.world-location
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
