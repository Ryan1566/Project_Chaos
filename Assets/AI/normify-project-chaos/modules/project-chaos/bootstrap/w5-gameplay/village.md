---
uid: 6a1c47d2
id: project-chaos.bootstrap.w5-gameplay.village
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, village, production]
name: {zh: "实体村庄生产", en: "Physical Village & Production"}
description:
  zh: >
      经读条进入实体村庄场景（VillageSceneFlow/VillageSceneBootstrap/VillageSceneSettings）；VillageWorkforce 管岗位分配，VillagePlot 管地块；VillageService 以原子方式处理建造/升级/取消/收获，TickProduction 只为独占岗位的入屋 NPC 累积生产；行为树与生产 NPC 驱动工人演出，Model/Presenter/View 三件套驱动村庄 HUD。
      
  en: >
      Enter the physical village through the loading screen (VillageSceneFlow/VillageSceneBootstrap/VillageSceneSettings); VillageWorkforce assigns jobs and VillagePlot owns plots; VillageService handles build/upgrade/cancel/harvest atomically and TickProduction only accumulates for the exclusive building-job owner; a behaviour tree and production NPCs drive workers, with the model/presenter/view trio driving the village HUD.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.696Z"
fingerprint: c3d84053d4d5dc2cc0c6383691b380ddd25713e50f105871f4561958ca0b0417
source:
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneFlow.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneBootstrap.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneSettings.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageWorkforce.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillagePlot.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/VillageService.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/Production/VillageBehaviorTree.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/Production/VillageProductionNpc.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageWorldModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillageWorldPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/VillageHud.cs"
apis:
  - protocol: rpc
    path: "VillageSceneFlow.EnterFromSave"
    description:
      zh: >
          经读条进入实体村庄，失败返回可重试的存档界面
          
      en: >
          Load the physical village, with retryable save-slot recovery
          
  - protocol: rpc
    path: "VillageService.Execute"
    description:
      zh: >
          建造、升级、取消、收获立即保存，失败回滚资源与建筑
          
      en: >
          Persist building commands atomically or restore resources and buildings
          
  - protocol: rpc
    path: "VillageService.TickProduction"
    description:
      zh: >
          仅独占岗位的入屋工作 NPC 累积生产时间
          
      en: >
          Accumulate production only for the working owner of a building job
          
  - protocol: rpc
    path: "VillageWorkforce.Assign"
    description:
      zh: >
          按门客分配写入/释放建筑背后的岗位
          
      en: >
          Assign or release the job slots behind a building based on retainer assignment
          
  - protocol: rpc
    path: "VillagePlot.Snapshot"
    description:
      zh: >
          读取地块的作物/生长状态供村庄 HUD 使用
          
      en: >
          Read a plot's crop/growth state for the village HUD
          
deps:
  - kind: call
    to: project-chaos.framework.save
    label: {zh: "建造命令回滚并存档", en: "Roll back and persist builds"}
  - kind: call
    to: project-chaos.bootstrap.scene-loading
    label: {zh: "经读条进入村庄", en: "Load village via loading"}
---
