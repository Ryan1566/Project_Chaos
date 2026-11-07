---
uid: cf000203
id: project-chaos.config-pipeline.generated-json.event
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "剧情事件 Json", en: "Event JSON"}
description:
  zh: >
      剧情事件与选项的运行时 Json 产物。
      
  en: >
      Runtime JSON for events and event options.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.408Z"
fingerprint: 970c4566020a6881344002e327ec7d78e90943d8c6a0e6195af49d7240ee22d3
source:
  - path: "Assets/Resources/Data/Json/Runtime/EventTable_Event.json"
  - path: "Assets/Resources/Data/Json/Runtime/EventTable_EventOption.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/EventTable_Event.json"
    description:
      zh: >
          剧情事件运行时 Json
          
      en: >
          Event runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.event
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
