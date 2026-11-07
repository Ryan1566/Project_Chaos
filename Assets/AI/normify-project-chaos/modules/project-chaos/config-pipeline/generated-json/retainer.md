---
uid: cf00020a
id: project-chaos.config-pipeline.generated-json.retainer
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "门客 Json", en: "Retainer JSON"}
description:
  zh: >
      门客属性与成长的运行时 Json 产物。
      
  en: >
      Runtime JSON for retainer stats and growth.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.418Z"
fingerprint: f633bbc883078430698f921323c13944778a83075237cb566b5f509c26c4df0b
source:
  - path: "Assets/Resources/Data/Json/Runtime/RetainerTable_Retainer.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/RetainerTable_Retainer.json"
    description:
      zh: >
          门客运行时 Json
          
      en: >
          Retainer runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.retainer
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
