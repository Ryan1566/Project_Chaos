---
uid: cf000212
id: project-chaos.config-pipeline.generated-json.table-index
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "配置表索引", en: "Config Table Index"}
description:
  zh: >
      全部导出表的索引（表名/子表/类型），用于不逐个加载文件就能枚举运行时配置目录。
      
  en: >
      Index of every exported config table (name, sub-sheet, type), used to enumerate the runtime catalog without loading each file.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.432Z"
fingerprint: bbf46639c2f994b8bcef9f7712525e06c5395b8eb07709b1542c5ad86b50f341
source:
  - path: "Assets/Resources/Data/Json/Runtime/ConfigTableIndex.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/ConfigTableIndex.json"
    description:
      zh: >
          配置表索引
          
      en: >
          Config table index
          
deps:
  - kind: reference
    to: project-chaos.config-pipeline.exporter.json-writer
    label: {zh: "由导出器写出", en: "Written by the exporter"}
---
