---
uid: cf000306
id: project-chaos.config-pipeline.generated-models.market
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "交易数据类", en: "Market Data Class"}
description:
  zh: >
      市场商品与报价对应的 C# 数据类。
      
  en: >
      C# data classes for market goods and offers.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.438Z"
fingerprint: 59fc380069aa84a5b10573a9f14f5a6feb8efc17dd18ed366e0ab025efc92456
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MarketTable_MarketConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MarketTable_MarketOfferConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MarketTable_MarketConfig.cs"
    description:
      zh: >
          交易数据类
          
      en: >
          Market data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.market
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
