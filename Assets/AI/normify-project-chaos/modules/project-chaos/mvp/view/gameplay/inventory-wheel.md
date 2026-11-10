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
updated_at: "2026-11-10T14:57:10.693Z"
fingerprint: 75186c4882521f0ea9d4fc92bfb65e7751bc739fe0770d3b8d9bd990a6698a6f
source:
  - path: "Assets/Scripts/Runtime/MVP/View/InventoryPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialMenuView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialWedgeGraphic.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialMenuSkin.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RadialMenuSkin.cs"
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
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/RadialMenuSkin.cs"
    description:
      zh: >
          轮盘皮肤资产（按层级/数量取切片与着色）
          
      en: >
          Radial menu skin asset (slice and tint by tier/count)
          
  - protocol: rpc
    path: "rpc:RadialMenuSkin.GetSlice"
    description:
      zh: >
          取当前轮盘扇区的精灵与颜色
          
      en: >
          Resolves wedge sprite and tint
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染背包", en: "Renders inventory"}
  - kind: call
    to: project-chaos.gameplay-3c.control.shortcuts
    label: {zh: "由轮盘驱动", en: "Driven by radial driver"}
---
