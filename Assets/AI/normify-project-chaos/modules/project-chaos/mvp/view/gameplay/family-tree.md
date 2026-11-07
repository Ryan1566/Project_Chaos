---
uid: b2c00307
id: project-chaos.mvp.view.gameplay.family-tree
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, skill-tree]
name: {zh: "家族树技能界面", en: "Family Tree (Skill Tree) UI"}
description:
  zh: >
      家族树技能棋盘：节点布局、可解锁分支、消耗角标，以及节点状态的公共视觉。
      
  en: >
      The family/skill tree board: node layout, unlockable branches, cost badges and the shared visuals for node states.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.364Z"
fingerprint: cf4938728a1bbc0059678e0be9fe9cc207bb5656f0f291d7a19db78e885ef3ec
source:
  - path: "Assets/Scripts/Runtime/MVP/View/FamilyTreePanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/FamilyTreeVisuals.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/FamilyTreePanel.cs"
    description:
      zh: >
          家族树技能面板
          
      en: >
          Family tree / skill tree panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/FamilyTreeVisuals.cs"
    description:
      zh: >
          技能树节点视觉
          
      en: >
          Tree node visuals
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染技能树", en: "Renders skill tree"}
  - kind: reference
    to: project-chaos.config-pipeline.generated-models.meta-skill-tree
    label: {zh: "读技能树配置", en: "Reads skill tree rows"}
---
