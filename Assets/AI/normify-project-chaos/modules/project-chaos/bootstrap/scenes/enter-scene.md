---
uid: 5c1a7e12
id: project-chaos.bootstrap.scenes.enter-scene
parent: project-chaos.bootstrap.scenes
tags: [bootstrap, scene]
name: {zh: "进入场景", en: "Enter Scene"}
description:
  zh: >
      EnterScene 是启动场景（Build Settings index 0），也是整个局外层的宿主：UIRoot（Canvas + 各面板 + GM_Canvas）、CameraRoot、Managers（GameManager/Entry、LocalizationManager、AudioManager）都挂幂等 DontDestroy 守卫，从局内返回时不会重复生成。
      
  en: >
      EnterScene is the boot scene (Build Settings index 0) and the host of the whole meta layer: UIRoot (Canvas with the panel prefab instances plus GM_Canvas), CameraRoot and Managers (GameManager with Entry, LocalizationManager, AudioManager) all carry the idempotent DontDestroy guard so that returning from a run does not duplicate them.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:20:37.049Z"
fingerprint: 51fa724dae4a11316bf65bbce38899bed2e9f49de840af68b95bfef233636cf4
source:
  - path: "Assets/Scenes/EnterScene.unity"
    line: 1
    end_line: 1003
apis:
  - protocol: file
    path: "Assets/Scenes/EnterScene.unity"
    description:
      zh: >
          进入场景资产（未登记进构建清单）。
          
      en: >
          Enter scene asset (not in the build list).
          
---
