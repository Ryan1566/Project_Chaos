---
uid: c1a20703
id: project-chaos.config-pipeline.generated-models.test-table-data
parent: project-chaos.config-pipeline.generated-models
name: {zh: "TestTableData 类", en: "TestTableData Class"}
description:
  zh: >
      Data 模式生成的类（TestTableData）写入 MVP/Model/ModelData：字段与 config 类一致，但注释明确说明它是 TestTableModel 的持久化数据类，供 Recorder 缓存使用。
      
  en: >
      Generated Data-mode class (TestTableData) written into MVP/Model/ModelData: same fields as the config class but explicitly marked as the persistence shape for TestTableModel and the Recorder cache.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.729Z"
fingerprint: 38f1fb0151d108bc22ead0da9c46b48ece90baf5dbca7af42cf896993f611fde
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ModelData/TestTableData.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ModelData/TestTableData.cs"
    description:
      zh: >
          TestTable 的可写存档数据类（生成）。
          
      en: >
          Generated TestTable writable data class for saves.
          
  - protocol: rpc
    path: "TestTableData"
    description:
      zh: >
          由 Recorder 持久化的 TestTable 模型行类。
          
      en: >
          Serializable TestTable model row class persisted by Recorder.
          
---
