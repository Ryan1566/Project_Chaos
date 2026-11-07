---
uid: cf000304
id: project-chaos.config-pipeline.generated-models.item
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "物品数据类", en: "Item Data Class"}
description:
  zh: >
      物品与掉落对应的 C# 数据类。
      
  en: >
      C# data classes for items and drops.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.435Z"
fingerprint: fa1881569c04a415c251cb38ad72f0476d31ebc81bcf0ee3864964ff3299e442
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ItemTable_ItemConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ItemTable_DropConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/ItemTable_ItemConfig.cs"
    description:
      zh: >
          物品数据类
          
      en: >
          Item data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.item
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
