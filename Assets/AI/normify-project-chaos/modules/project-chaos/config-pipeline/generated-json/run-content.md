---
uid: cf00020b
id: project-chaos.config-pipeline.generated-json.run-content
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "局内内容 Json", en: "Run Content JSON"}
description:
  zh: >
      局内行为与交互的运行时 Json 产物。
      
  en: >
      Runtime JSON for in-run behaviors and interactions.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.419Z"
fingerprint: 2674974a66bde541f9776a9df0f5c42ede90e8936608416ddea817ef930d262b
source:
  - path: "Assets/Resources/Data/Json/Runtime/RunContentTable_RunBehavior.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunContentTable_RunInteraction.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/RunContentTable_RunBehavior.json"
    description:
      zh: >
          局内内容运行时 Json
          
      en: >
          Run content runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.run-content
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
