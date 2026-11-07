---
uid: a3c00021
id: project-chaos.gameplay-3c.character.locomotion
parent: project-chaos.gameplay-3c.character
tags: [character, physics, movement]
name: {zh: "移动与曲面物理", en: "Locomotion & Surface Physics"}
description:
  zh: >
      地面移动与跳跃、地面/侧墙曲面物理与低摩擦处理（空中推墙不再攀住而是自然下落）、以及低于阈值 Y 时将角色重生的坠落保护。
      
  en: >
      Ground movement and jumping, ground/side-wall surface physics with low-friction handling (a wall push in the air now drops instead of sticking), and the fall guard that respawns the player below a threshold Y.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.747Z"
fingerprint: 9caed20e5152a2caa36dd4825a4de04c6b6a7971d62397f8bf47b74a485ae9d1
source:
  - path: "Assets/Scripts/Runtime/3C/Character/PlayerLocomotion.cs"
  - path: "Assets/Scripts/Runtime/3C/Character/PlayerSurfacePhysics.cs"
  - path: "Assets/Scripts/Runtime/3C/Character/PlayerFallGuard.cs"
apis:
  - protocol: rpc
    path: "PlayerLocomotion.InjectInput"
    description:
      zh: >
          InjectInput(horizontal, jump, duration)：注入脚本化移动意图。
          
      en: >
          InjectInput(horizontal, jump, duration): feeds a scripted move intent.
          
  - protocol: rpc
    path: "PlayerLocomotion.SetFacing"
    description:
      zh: >
          SetFacing(facing)：强制设定朝向。
          
      en: >
          SetFacing(facing): forces the facing direction.
          
  - protocol: rpc
    path: "PlayerSurfacePhysics.ProbeGround"
    description:
      zh: >
          ProbeGround(terrainOnly)：侧墙物理使用的地面探测。
          
      en: >
          ProbeGround(terrainOnly): ground probe used by the side-wall physics.
          
  - protocol: rpc
    path: "PlayerSurfacePhysics.SetSlideMaterial"
    description:
      zh: >
          SetSlideMaterial(active)：开关低摩擦接触，推墙时会自然下落。
          
      en: >
          SetSlideMaterial(active): toggles low-friction contact so a pushing fall drops naturally.
          
deps:
  - kind: call
    to: project-chaos.gameplay-3c.control.input.gameplay-scope
    from_api: "rpc:PlayerLocomotion.InjectInput"
    to_api: "rpc:InputManager.ReadMove"
    label: {zh: "注入移动意图", en: "Injects move intent"}
---
