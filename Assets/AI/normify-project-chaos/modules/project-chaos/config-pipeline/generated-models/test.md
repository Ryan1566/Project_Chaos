---
uid: cf00030d
id: project-chaos.config-pipeline.generated-models.test
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated, test]
name: {zh: "测试数据类", en: "Test Data Class"}
description:
  zh: >
      管线自检测试表对应的 C# 数据类。
      
  en: >
      C# data class for the pipeline self-check table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.446Z"
fingerprint: 7f3da761006fd5f3aff1f7313081eae1e76a14740318c95ab4bb69f62f795582
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/TestTable_TestConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/TestTable_TestConfig.cs"
    description:
      zh: >
          测试数据类
          
      en: >
          Test data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.test
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
