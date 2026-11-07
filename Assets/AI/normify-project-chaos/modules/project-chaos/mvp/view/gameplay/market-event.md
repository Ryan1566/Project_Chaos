---
uid: b2c00306
id: project-chaos.mvp.view.gameplay.market-event
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, market]
name: {zh: "市场、事件与门客", en: "Market, Event & Retainer"}
description:
  zh: >
      村庄界面：市场交易面板、剧情事件与选项面板、门客招募面板。
      
  en: >
      Village meta screens: the market trade panel, the event/choice panel and the retainer recruitment panel.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.364Z"
fingerprint: 70d55e892bd60c04ae6f587da09c59675b816cb958aa09438be8057f0731cff6
source:
  - path: "Assets/Scripts/Runtime/MVP/View/MarketPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/EventPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RetainerPanel.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/MarketPanel.cs"
    description:
      zh: >
          交易面板
          
      en: >
          Market panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/EventPanel.cs"
    description:
      zh: >
          剧情事件面板
          
      en: >
          Event panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RetainerPanel.cs"
    description:
      zh: >
          门客招募面板
          
      en: >
          Retainer recruitment panel
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染局外面板", en: "Renders meta screens"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.meta-events
    label: {zh: "交易与招募", en: "Trades and recruits"}
---
