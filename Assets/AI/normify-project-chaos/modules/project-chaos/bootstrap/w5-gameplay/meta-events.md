---
uid: 3f95a0e6
id: project-chaos.bootstrap.w5-gameplay.meta-events
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, event, market, save]
name: {zh: "事件市场掉落与结算", en: "Events, Market, Drops & Settlement"}
description:
  zh: >
      EventService 处理剧情事件与选项奖励，MarketService 执行交易（失败回滚），MetaProgressService 保存局外进度；奖励与一次性 ID 原子保存，结算保存可幂等重试。GameplayText 汇总局内文案键，RunDropItem 是带出前先拾取的结算掉落。门客阵亡、扎营修整与背包带出仍散在 RunEncounter.Team / RunSceneFlow 里。
      
  en: >
      EventService handles story events and option rewards, MarketService executes trades with rollback on failure, and MetaProgressService saves meta progress; rewards and one-shot IDs are saved atomically and settlement saves retry idempotently. GameplayText collects in-run text keys; RunDropItem is the world drop settled before withdraw. Retainer death, camping and bag withdraw still live inside RunEncounter.Team/RunSceneFlow.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.697Z"
fingerprint: 52e54bc27f1e1af0be679e96566d0319a33ece66d7209ce85fc15609a4f16413
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/EventService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/MarketService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/MetaProgressService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/GameplayText.cs"
  - path: "Assets/Scripts/Runtime/Player/RunDropItem.cs"
apis:
  - protocol: rpc
    path: "MetaProgressService.SaveProgress"
    description:
      zh: >
          保存当前会话槽位
          
      en: >
          Save the active session slot
          
  - protocol: rpc
    path: "EventService.ChooseOption"
    description:
      zh: >
          选择事件选项并原子发放奖励与一次性 ID
          
      en: >
          Choose an event option, granting rewards and one-shot IDs atomically
          
  - protocol: rpc
    path: "MarketService.Trade"
    description:
      zh: >
          执行市场交易，失败回滚资源与物品
          
      en: >
          Execute a market trade, rolling back resources and items on failure
          
deps:
  - kind: call
    to: project-chaos.framework.save
    label: {zh: "奖励与结算走存档事务", en: "Rewards via save transactions"}
  - kind: reference
    to: project-chaos.config-pipeline
    label: {zh: "读取事件与市场配置", en: "Reads event/market config"}
---
