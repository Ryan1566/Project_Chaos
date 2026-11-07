---
uid: cf000204
id: project-chaos.config-pipeline.generated-json.item
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "物品 Json", en: "Item JSON"}
description:
  zh: >
      物品与掉落的运行时 Json 产物。
      
  en: >
      Runtime JSON for items and drops.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.409Z"
fingerprint: d8a0987f4a29a9850621d61d95832a18e79b9476fcf9619df0be85973cb9f749
source:
  - path: "Assets/Resources/Data/Json/Runtime/ItemTable_Item.json"
  - path: "Assets/Resources/Data/Json/Runtime/ItemTable_Drop.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/ItemTable_Item.json"
    description:
      zh: >
          物品运行时 Json
          
      en: >
          Item runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.item
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
