---
uid: a3c00001
id: project-chaos.gameplay-3c
parent: project-chaos
tags: [gameplay, 3c, runtime]
name: {zh: "3C 层（相机 / 角色 / 控制）", en: "3C Layer: Camera, Character & Control"}
description:
  zh: >
      承载 Assets/Scripts/Runtime/3C/ 下的相机、角色与控制三个子系统（19 个 .cs）。职责边界：镜头只由 Camera/ 统一控制，界面层不直接改相机跟随；Character/ 只负责角色自身的移动、曲面物理、坠落保护、攻击与生命；Control/ 只读取输入意图，不承载库存与结算规则。整局地图生成、对象作用域、地图边界与结算不归 3C。
      
  en: >
      Owns the camera, character and control subsystems under Assets/Scripts/Runtime/3C (19 C# files). Boundaries: only Camera/ drives the gameplay camera (the UI layer never moves it); Character/ handles locomotion, surface physics, fall guard, attacks and health; Control/ only turns raw input into intent and carries no inventory or settlement rules. Whole-run map generation, object scopes, map bounds and settlement stay outside 3C.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.157Z"
fingerprint: 1d705376bf0134ac9c1376b3dbda0daa4e0616015019a162640302f56267bbcc
source:
  - path: "Assets/Scripts/Runtime/3C/README.md"
---
