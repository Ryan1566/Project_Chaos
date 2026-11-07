---
uid: cf000205
id: project-chaos.config-pipeline.generated-json.loading
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "读条 Json", en: "Loading JSON"}
description:
  zh: >
      加载文案与地图生成读条的运行时 Json 产物。
      
  en: >
      Runtime JSON for loading copy and map-loading segments.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.411Z"
fingerprint: c5491394a71c0f7b790c03c3461302169e8b3e92e584058a61dd124794602621
source:
  - path: "Assets/Resources/Data/Json/Runtime/LoadingTable_Loading.json"
  - path: "Assets/Resources/Data/Json/Runtime/LoadingTable_MapLoading.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/LoadingTable_Loading.json"
    description:
      zh: >
          读条运行时 Json
          
      en: >
          Loading runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.loading
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
