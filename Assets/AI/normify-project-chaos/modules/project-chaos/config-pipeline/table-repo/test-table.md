---
uid: c1a20101
id: project-chaos.config-pipeline.table-repo.test-table
parent: project-chaos.config-pipeline.table-repo
name: {zh: "测试表工作簿", en: "TestTable Workbook"}
description:
  zh: >
      示例角色表：4 条 Index / CharName / Occupation / head_icon（类型 uint/string/uint/uint）。第 7 行就是第一条数据，不存在示例行；表尾留空行会被当成一条空数据。
      
  en: >
      The sample character table: four rows of Index, CharName, Occupation and head_icon (types uint/string/uint/uint). Row 7 is the first data row - there is no example row, and a trailing blank row would be imported as an empty record.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.732Z"
fingerprint: f4cc4e9d51f96c845e3b400834261f38c18aab92e803860875cf347432899c87
source:
  - path: "Assets/Excel/Chaos_excel/TestTable_测试表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/TestTable_测试表.xlsx"
    description:
      zh: >
          含 4 条角色数据的源工作簿。
          
      en: >
          Source workbook with 4 character rows.
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.exporter.export-menu
    from_api: "file:Assets/Excel/Chaos_excel/TestTable_测试表.xlsx"
    to_api: "rpc:Menu/ExcelTool/ExportExcelConfigs"
    label: {zh: "被导出器读取", en: "Read by the config export"}
---
