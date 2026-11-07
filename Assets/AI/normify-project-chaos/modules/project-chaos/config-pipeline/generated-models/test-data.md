---
uid: cf000312
id: project-chaos.config-pipeline.generated-models.test-data
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated, test]
name: {zh: "测试存档数据类", en: "Test Record Data Class"}
description:
  zh: >
      测试表的 Data 模式类，导出为 .record 存档而非运行时 Json。
      
  en: >
      Data-mode counterpart of the test table class, written as a .record save instead of runtime JSON.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.452Z"
fingerprint: 38f1fb0151d108bc22ead0da9c46b48ece90baf5dbca7af42cf896993f611fde
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ModelData/TestTableData.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ModelData/TestTableData.cs"
    description:
      zh: >
          测试存档数据类
          
      en: >
          Test record data class
          
deps:
  - kind: reference
    to: project-chaos.config-pipeline.record-store
    label: {zh: "同表的 Data 模式", en: "Same table in Data mode"}
---
