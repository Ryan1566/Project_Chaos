---
uid: b2c00210
id: project-chaos.bootstrap.w5-gameplay.battle-prep
parent: project-chaos.bootstrap.w5-gameplay
tags: [bootstrap, gameplay, loadout]
name: {zh: "战备装载", en: "Battle Loadout"}
description:
  zh: >
      入局前的战备草稿：把仓库与局内背包的物资按槽位摆好，维护 3~10 个预设，校验数量上限与重要道具规则，最后交给派遣流程带入本局。
      
  en: >
      Pre-run loadout draft: arranges warehouse and run-bag items into battle slots, keeps 3-10 presets, validates quantity limits and important-item rules, then hands the layout to the dispatch flow.
      
revision: 08f9a356150c4617b28273d13535c6f14ae4d299
updated_at: "2026-11-10T14:57:10.676Z"
fingerprint: 170a7304681fa798bd8d0c326531ddd26bfa9fd347905289d21a2fb8135f0f91
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/BattleLoadout.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/Meta/BattleLoadout.cs"
    description:
      zh: >
          战备装载模型（槽位、数量、预设）
          
      en: >
          Battle loadout model (slots, quantities, presets)
          
  - protocol: rpc
    path: "rpc:BattleLoadout.SetQuantity"
    description:
      zh: >
          设置某个道具的携带数量
          
      en: >
          Sets how many of an item are carried
          
  - protocol: rpc
    path: "rpc:BattleLoadout.Move"
    description:
      zh: >
          在仓库与背包之间移动槽位
          
      en: >
          Moves a slot between warehouse and bag
          
  - protocol: rpc
    path: "rpc:BattleLoadout.LoadPreset"
    description:
      zh: >
          套用备战后预设
          
      en: >
          Applies a saved loadout preset
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.meta-events
    label: {zh: "读取仓库与道具", en: "Reads warehouse and items"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "携带入局", en: "Carries into the run"}
---
