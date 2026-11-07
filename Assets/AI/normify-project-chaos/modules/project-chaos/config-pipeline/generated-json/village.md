---
uid: cf00020e
id: project-chaos.config-pipeline.generated-json.village
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "村庄 Json", en: "Village JSON"}
description:
  zh: >
      村庄场景布局与文本的运行时 Json 产物。
      
  en: >
      Runtime JSON for village scene layout and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.424Z"
fingerprint: 3c1f0f7aaed32538a13ae14422bcab757cab76cebf62cf7108bf13a7d35a0805
source:
  - path: "Assets/Resources/Data/Json/Runtime/VillageTable_VillageScene.json"
  - path: "Assets/Resources/Data/Json/Runtime/VillageTable_VillageText.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/VillageTable_VillageScene.json"
    description:
      zh: >
          村庄运行时 Json
          
      en: >
          Village runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.village
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
