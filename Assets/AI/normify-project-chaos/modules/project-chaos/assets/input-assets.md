---
uid: e91c4d76
id: project-chaos.assets.input-assets
parent: project-chaos.assets
name: {zh: "输入资产", en: "Input Assets"}
description:
  zh: >
      输入资产：ChaosInputActions.inputactions（1 map / 3 action / 14 binding）与 KeyIconMap 控制项→图标映射表。
      
  en: >
      Input assets: the ChaosInputActions .inputactions asset (1 map, 3 actions, 14 bindings) and KeyIconMap mapping controls to icon sprites.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.703Z"
fingerprint: 5977e8248f196b262cf65456b6970302d0cab9adf8e12148e0c8e2f1772a97fa
source:
  - path: "Assets/Resources/Input/ChaosInputActions.inputactions"
  - path: "Assets/Resources/Data/Input/KeyIconMap.asset"
apis:
  - protocol: file
    path: "Assets/Resources/Input/ChaosInputActions.inputactions"
    description:
      zh: >
          Input System 动作资产（1 个 Player map，含 Move/Attack/Jump 与 14 条绑定）。
          
      en: >
          Input System action asset (1 map 'Player' with Move/Attack/Jump and 14 bindings).
          
  - protocol: file
    path: "Assets/Resources/Data/Input/KeyIconMap.asset"
    description:
      zh: >
          把控制路径与设备变体映射到输入图标 sprite，供键位提示取图。
          
      en: >
          Maps control paths and device variants to input icon sprites for key prompts.
          
deps:
  - kind: reference
    to: project-chaos.assets.art.icon.atlas
    from_api: "file:Assets/Resources/Data/Input/KeyIconMap.asset"
    to_api: "file:Assets/Art/icon/IconAtlas_Input.spriteatlasv2"
    label: {zh: "映射到输入图标图集", en: "Maps to input icon atlas"}
---
