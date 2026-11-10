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
updated_at: "2026-11-10T14:52:00.909Z"
fingerprint: a5e4899f7d30d91ce44f6665994553600693669d847576f0e59d321de7f89dfc
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
