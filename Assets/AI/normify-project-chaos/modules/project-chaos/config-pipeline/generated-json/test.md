---
uid: cf00020d
id: project-chaos.config-pipeline.generated-json.test
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated, test]
name: {zh: "测试 Json", en: "Test JSON"}
description:
  zh: >
      管线自检测试表的运行时 Json 产物。
      
  en: >
      Runtime JSON for the pipeline self-check table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.423Z"
fingerprint: 23949173e653007937bff120f42fc8af5f1fc761bb2c4b88ffd821b1d0d0fc17
source:
  - path: "Assets/Resources/Data/Json/Runtime/TestTable_Test.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/TestTable_Test.json"
    description:
      zh: >
          测试运行时 Json
          
      en: >
          Test runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.test
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
