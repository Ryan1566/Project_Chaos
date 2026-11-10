---
uid: b2c00502
id: project-chaos.mvp.model.gameplay.meta
parent: project-chaos.mvp.model.gameplay
tags: [mvp, model, meta]
name: {zh: "局外视图模型", en: "Meta View Models"}
description:
  zh: >
      局外视图状态：派遣编队与路线、技能树棋盘、市场报价、事件选项、办事任务、门客招募与轮盘状态。
      
  en: >
      Meta-side view state: dispatch roster/route planning, skill tree board, market offers, event options, office tasks, retainer recruitment and item wheel state.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.926Z"
fingerprint: 6130a69e775fc1b6744efd683edbb844cead033bd9535f15596d3078e184dc5d
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/DispatchModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/FamilyTreeModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/MarketModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/OfficeTasksDisplayState.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/OfficeTasksModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RadialMenuViewState.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RetainerRecruitmentModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunContentPanelModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunContentViewState.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/DispatchModel.cs"
    description:
      zh: >
          派遣模型
          
      en: >
          Dispatch model
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/FamilyTreeModel.cs"
    description:
      zh: >
          技能树模型
          
      en: >
          Skill tree model
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/OfficeTasksModel.cs"
    description:
      zh: >
          办事任务模型
          
      en: >
          Office tasks model
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.meta-events
    label: {zh: "持有局外状态", en: "Holds meta state"}
---
