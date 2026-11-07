---
uid: cf000209
id: project-chaos.config-pipeline.generated-json.progress
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "时代进度 Json", en: "Progress JSON"}
description:
  zh: >
      时代与深度推进的运行时 Json 产物。
      
  en: >
      Runtime JSON for era and depth progression.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.417Z"
fingerprint: 951cabc944a38f8ad0e07eb41da568195c7f6a775564065fc3762582f34805e9
source:
  - path: "Assets/Resources/Data/Json/Runtime/ProgressTable_Era.json"
  - path: "Assets/Resources/Data/Json/Runtime/ProgressTable_Depth.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/ProgressTable_Era.json"
    description:
      zh: >
          时代进度运行时 Json
          
      en: >
          Progress runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.progress
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
