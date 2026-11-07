---
uid: cf000206
id: project-chaos.config-pipeline.generated-json.market
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "交易 Json", en: "Market JSON"}
description:
  zh: >
      市场商品与报价的运行时 Json 产物。
      
  en: >
      Runtime JSON for market goods and offers.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.412Z"
fingerprint: 720fc9c62ccea2c529d040dc8b882f43a959e89ec2885611e1a48faa5ae80dab
source:
  - path: "Assets/Resources/Data/Json/Runtime/MarketTable_Market.json"
  - path: "Assets/Resources/Data/Json/Runtime/MarketTable_MarketOffer.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/MarketTable_Market.json"
    description:
      zh: >
          交易运行时 Json
          
      en: >
          Market runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.market
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
