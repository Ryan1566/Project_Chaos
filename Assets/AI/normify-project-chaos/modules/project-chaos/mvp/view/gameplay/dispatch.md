---
uid: b2c00305
id: project-chaos.mvp.view.gameplay.dispatch
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, dispatch]
name: {zh: "派遣与办事任务", en: "Dispatch & Office Tasks"}
description:
  zh: >
      局外办事界面：派遣面板（编队、路线、奖励）、办事任务板，以及卡控它们的时代进度面板。
      
  en: >
      Meta-side errand UI: the dispatch panel (roster, routes, rewards), the office-task board and the era-progress panel that gates them.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:57:10.689Z"
fingerprint: 8e995c942ca204b4b3bdc193ddb28e25499359c55e538e820171a7a5f2a014cd
source:
  - path: "Assets/Scripts/Runtime/MVP/View/DispatchPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/DispatchVisuals.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/OfficeTasksView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/OfficeTasksPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/EraProgressPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/BattlePreparationPanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/BattlePreparationPanel.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/DispatchPanel.cs"
    description:
      zh: >
          派遣面板
          
      en: >
          Dispatch panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/OfficeTasksView.cs"
    description:
      zh: >
          办事任务视图
          
      en: >
          Office tasks view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/EraProgressPanel.cs"
    description:
      zh: >
          时代进度面板
          
      en: >
          Era progress panel
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/BattlePreparationPanel.cs"
    description:
      zh: >
          战备装载面板（槽位拖拽、预设页、数量确认）
          
      en: >
          Battle preparation panel (slot drag, presets, quantity confirm)
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染派遣状态", en: "Renders dispatch state"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "发起派遣", en: "Starts dispatch runs"}
---
