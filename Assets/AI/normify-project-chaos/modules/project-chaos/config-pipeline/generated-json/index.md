---
uid: c1a20006
id: project-chaos.config-pipeline.generated-json
parent: project-chaos.config-pipeline
name: {zh: "导出 Json 产物", en: "Generated JSON Artifacts"}
description:
  zh: >
      ExcelTool/ExportExcelConfigs 生成的运行期常量 Json，位于 Resources/Data/Json/Runtime，固定结构为 {"datas":[...]}，由 ConfigLoader 用 JsonUtility 反序列化为 DataList<T>。属派生产物，表改了要重跑导出。
  en: >
      Runtime constant JSON produced by ExcelTool/ExportExcelConfigs under Resources/Data/Json/Runtime. Every file has the fixed shape {"datas":[...]}, which ConfigLoader deserialises into DataList<T> with JsonUtility. Derived artifacts: re-run the export after editing a table.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
