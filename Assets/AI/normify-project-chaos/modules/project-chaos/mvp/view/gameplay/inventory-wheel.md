---
uid: b2c00304
id: project-chaos.mvp.view.gameplay.inventory-wheel
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, inventory]
name: {zh: "背包与道具轮盘", en: "Inventory & Item Wheel"}
description:
  zh: >
      背包与道具轮盘：网格背包面板、轮盘视图，以及逐个扇形的网格绘制组件。
      
  en: >
      Bag and item wheel: the grid inventory panel, the radial item wheel view and the wedge mesh graphic that draws each slot.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.361Z"
fingerprint: 6fd543d1e1fe53bc1d321322b8c4e6bcec18c72232b4b684f3f863ea8520b55c
source:
  - path: "Assets/Scripts/Runtime/MVP/View/InventoryPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialMenuView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialWedgeGraphic.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/InventoryPanel.cs"
    description:
      zh: >
          背包面板
          
      en: >
          Inventory panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RadialMenuView.cs"
    description:
      zh: >
          轮盘菜单视图
          
      en: >
          Radial menu view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RadialWedgeGraphic.cs"
    description:
      zh: >
          轮盘扇形绘制
          
      en: >
          Radial wedge graphic
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染背包", en: "Renders inventory"}
  - kind: call
    to: project-chaos.gameplay-3c.control.shortcuts
    label: {zh: "由轮盘驱动", en: "Driven by radial driver"}
---
