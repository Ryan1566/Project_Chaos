---
uid: a3c00024
id: project-chaos.gameplay-3c.character.ladder
parent: project-chaos.gameplay-3c.character
tags: [character, movement, ladder]
name: {zh: "角色：梯子攀爬", en: "Character: Ladder Climbing"}
description:
  zh: >
      角色爬梯：纵向输入时自动附梯、按梯子边界驱动纵向位移、跳跃或离开梯子时脱离，并把控制权交还 PlayerLocomotion。
      
  en: >
      Ladder traversal for the player: auto-attach on vertical input, drive vertical motion with ladders clamping, detach on jump or leaving the ladder, and hand control back to PlayerLocomotion.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.921Z"
fingerprint: ef44c8ec5c983d5c65c59df6d4fe642fb9381652f74cce71c95d1a0217ba8965
source:
  - path: "Assets/Scripts/Runtime/3C/Character/LadderClimber.cs"
apis:
  - protocol: rpc
    path: "LadderClimber.TryAttach"
    description:
      zh: >
          附上梯子
          
      en: >
          Attach the character to a ladder
          
  - protocol: rpc
    path: "LadderClimber.Navigate"
    description:
      zh: >
          沿梯上下移动
          
      en: >
          Climb up or down along a ladder
          
  - protocol: rpc
    path: "LadderClimber.Detach"
    description:
      zh: >
          脱离梯子并恢复重力
          
      en: >
          Detach and restore gravity
          
  - protocol: rpc
    path: "LadderClimber.IsClimbing"
    description:
      zh: >
          角色是否在爬梯
          
      en: >
          Whether the character is climbing
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/3C/Character/LadderClimber.cs"
    description:
      zh: >
          梯子攀爬控制器
          
      en: >
          Ladder climbing controller
          
deps:
  - kind: reference
    to: project-chaos.framework.runmap.exploration.data
    label: {zh: "使用梯子几何", en: "Uses MapLadder geometry"}
  - kind: call
    to: project-chaos.gameplay-3c.character.locomotion
    label: {zh: "接管移动", en: "Overrides locomotion"}
---
