---
uid: cf000201
id: project-chaos.config-pipeline.generated-json.building
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "建筑 Json", en: "Building JSON"}
description:
  zh: >
      建筑表的运行时 Json 产物。
      
  en: >
      Runtime JSON for the building table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.406Z"
fingerprint: 31cb092022a4b568af7b5e099e06556fd127b7dddd8a82bd8f22786d59007c29
source:
  - path: "Assets/Resources/Data/Json/Runtime/BuildingTable_Building.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/BuildingTable_Building.json"
    description:
      zh: >
          建筑运行时 Json
          
      en: >
          Building runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.building
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
