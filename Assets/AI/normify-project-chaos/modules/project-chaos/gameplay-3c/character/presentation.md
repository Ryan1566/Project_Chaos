---
uid: a3c00023
id: project-chaos.gameplay-3c.character.presentation
parent: project-chaos.gameplay-3c.character
tags: [character, presentation]
name: {zh: "角色表现", en: "Character Presentation"}
description:
  zh: >
      角色表现数据与道具使用动画：局内角色的职业、立绘与预览皮肤，以及道具被使用时播放的动画。
      
  en: >
      Character presentation data and item-use animation: profession, portrait and preview skin for the run character, plus the animation played when an item is used.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.164Z"
fingerprint: 3014c64c687b29466e490f090bcbb40f90049c276f678752305a0f3d57b1cc4a
source:
  - path: "Assets/Scripts/Runtime/3C/Character/ItemUseAnimation.cs"
  - path: "Assets/Scripts/Runtime/3C/Character/RunCharacterPresentation.cs"
apis:
  - protocol: rpc
    path: "ItemUseAnimation.Play"
    description:
      zh: >
          Play(item)：播放与道具匹配的使用动画。
          
      en: >
          Play(item): plays the item-use animation matching the given item.
          
---
