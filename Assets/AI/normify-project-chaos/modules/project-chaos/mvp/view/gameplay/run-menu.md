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
updated_at: "2026-11-10T14:57:10.692Z"
fingerprint: 1bcff612916eb81a1169aa398992d25bf806308cba65c0d726e732d6d74a06af
source:
  - path: "Assets/Scripts/Runtime/MVP/View/RunCampPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunMenuPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunSettlementPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/PausePanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunResumeRecoveryPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunResumeRecoveryPanel.cs"
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
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/RunResumeRecoveryPanel.cs"
    description:
      zh: >
          局内恢复提示面板（回标题后继续/放弃）
          
      en: >
          Run resume recovery panel (continue/abandon from title)
          
  - protocol: rpc
    path: "rpc:RunResumeRecoveryPanel.Open"
    description:
      zh: >
          按原因弹出恢复提示
          
      en: >
          Opens the recovery panel with a reason
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染菜单状态", en: "Renders menu state"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.day-night
    label: {zh: "扎营推进时间", en: "Camp advances clock"}
---
