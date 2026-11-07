---
uid: b2c00303
id: project-chaos.mvp.view.gameplay.run-menu
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, menu]
name: {zh: "局内菜单与结算", en: "Run Menus & Settlement"}
description:
  zh: >
      局内菜单：扎营（休息、推昼夜、存档）、带撤退/结算的暂停菜单，以及离开地图后的结算界面。
      
  en: >
      Run menus: camp (rest, day/night shift, save), the in-run pause menu with retreat/settle, and the settlement screen after leaving the map.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.360Z"
fingerprint: d2927a38f51d3bba0759fefbfd4817720845ae045a0d045d9979bfbeea6f6a35
source:
  - path: "Assets/Scripts/Runtime/MVP/View/RunCampPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunMenuPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunSettlementPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/PausePanel.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunCampPanel.cs"
    description:
      zh: >
          扎营面板
          
      en: >
          Camp panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunMenuPanel.cs"
    description:
      zh: >
          局内菜单
          
      en: >
          Run menu panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunSettlementPanel.cs"
    description:
      zh: >
          结算面板
          
      en: >
          Settlement panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/PausePanel.cs"
    description:
      zh: >
          暂停面板
          
      en: >
          Pause panel
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染菜单状态", en: "Renders menu state"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.day-night
    label: {zh: "扎营推进时间", en: "Camp advances clock"}
---
