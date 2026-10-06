---
uid: c1a20102
id: project-chaos.config-pipeline.table-repo.loading-table
parent: project-chaos.config-pipeline.table-repo
name: {zh: "读条轮播表工作簿", en: "LoadingTable Workbook"}
description:
  zh: >
      读条界面轮播表：图片路径、播放顺序、停留秒数、淡入淡出秒数与启用开关。enable 列是 bool 规则的活样本（只认 true/false、1/0、是/否，其它取值导出期直接报错）。
      
  en: >
      Loading-screen carousel table: image path, sort order, dwell seconds, fade seconds and an enable flag. The enable column exercises the bool rule (true/false, 1/0, yes/no only; anything else fails at export time).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.731Z"
fingerprint: 0c0b82b66804a450d5d8079b8c466f24a096bc2aa178c0e6741414b216500757
source:
  - path: "Assets/Excel/Chaos_excel/LoadingTable_读条轮播表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/LoadingTable_读条轮播表.xlsx"
    description:
      zh: >
          含 3 条轮播数据的源工作簿。
          
      en: >
          Source workbook with 3 carousel rows.
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.exporter.export-menu
    from_api: "file:Assets/Excel/Chaos_excel/LoadingTable_读条轮播表.xlsx"
    to_api: "rpc:Menu/ExcelTool/ExportExcelConfigs"
    label: {zh: "被导出器读取", en: "Read by the config export"}
---
