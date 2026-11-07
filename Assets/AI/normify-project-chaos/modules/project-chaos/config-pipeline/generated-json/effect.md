---
uid: cf000202
id: project-chaos.config-pipeline.generated-json.effect
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "效果 Json", en: "Effect JSON"}
description:
  zh: >
      效果表的运行时 Json 产物。
      
  en: >
      Runtime JSON for the effect table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.407Z"
fingerprint: 2096cad4a1817208c49d7ca0486f2ce3caeab2b893021b604eeea972f9151270
source:
  - path: "Assets/Resources/Data/Json/Runtime/EffectTable_Effect.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/EffectTable_Effect.json"
    description:
      zh: >
          效果运行时 Json
          
      en: >
          Effect runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.effect
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
