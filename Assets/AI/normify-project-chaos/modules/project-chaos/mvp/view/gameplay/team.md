---
uid: b2c00308
id: project-chaos.mvp.view.gameplay.team
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, team]
name: {zh: "队伍界面", en: "Team UI"}
description:
  zh: >
      队伍管理视图：门客槽位、成长属性，以及选择或换人时的 Spine 立绘预览。
      
  en: >
      Team management view: retainer slots, growth stats and the Spine portrait preview used when picking or swapping members.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.365Z"
fingerprint: 7585a8c6e25cbfa763475ef4dd38d03052e5bda736ae6130e62c712bc769dfcf
source:
  - path: "Assets/Scripts/Runtime/MVP/View/RunTeamView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunTeamSpinePreview.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunTeamView.cs"
    description:
      zh: >
          队伍面板
          
      en: >
          Team panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunTeamSpinePreview.cs"
    description:
      zh: >
          队伍 Spine 预览
          
      en: >
          Team Spine preview
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染队伍状态", en: "Renders team state"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.team-supply
    label: {zh: "读门客成长", en: "Reads retainer growth"}
---
