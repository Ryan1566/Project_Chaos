---
uid: b2c00301
id: project-chaos.mvp.view.gameplay.run-hud
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, hud]
name: {zh: "局内 HUD", en: "Run HUD"}
description:
  zh: >
      常驻的局内 HUD：深度指示、队伍头像与血量条、快捷背包格，以及绑定全部玩法控件的根节点。
      
  en: >
      Always-on run HUD: depth indicator, team portraits/health row, quick bag slots and the root that binds all gameplay widgets.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.358Z"
fingerprint: 2cfbd1950f1f80e7a2749205a733b1ac81224b556dd9abd16f0be2b7ddf7f6a4
source:
  - path: "Assets/Scripts/Runtime/MVP/View/RunDepthHud.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunTeamHud.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunBagSlotView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/GameplayUi.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunDepthHud.cs"
    description:
      zh: >
          深度 HUD
          
      en: >
          Depth HUD
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunTeamHud.cs"
    description:
      zh: >
          队伍 HUD
          
      en: >
          Team HUD
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunBagSlotView.cs"
    description:
      zh: >
          背包格视图
          
      en: >
          Bag slot view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/GameplayUi.cs"
    description:
      zh: >
          玩法 UI 根
          
      en: >
          Gameplay UI root
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染 HUD 状态", en: "Renders HUD state"}
---
