---
uid: b2c00503
id: project-chaos.mvp.model.gameplay.village
parent: project-chaos.mvp.model.gameplay
tags: [mvp, model, village]
name: {zh: "村庄视图模型", en: "Village View Models"}
description:
  zh: >
      村庄视图状态：村庄世界模型（建筑、生产、时辰）与村内办事任务状态。
      
  en: >
      Village view state: the village world model (buildings, production, time of day) and the office-task state inside the village.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.841Z"
fingerprint: 9fa5fde34a70e2fa559b10b0473aa3632f4c3065431459140c3102dd14383e81
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageWorldModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageOfficeModel.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageWorldModel.cs"
    description:
      zh: >
          村庄世界模型
          
      en: >
          Village world model
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageOfficeModel.cs"
    description:
      zh: >
          村内办事模型
          
      en: >
          Village office model
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.village
    label: {zh: "持有村庄状态", en: "Holds village state"}
---
