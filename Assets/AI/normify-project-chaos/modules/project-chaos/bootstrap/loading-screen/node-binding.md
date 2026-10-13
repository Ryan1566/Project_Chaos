---
uid: 5c1a7e0c
id: project-chaos.bootstrap.loading-screen.node-binding
parent: project-chaos.bootstrap.loading-screen
tags: [bootstrap, loading, ui]
name: {zh: "节点绑定与进度条", en: "Nodes & Progress"}
description:
  zh: >
      按硬约定节点名（BgImage / FadeImage / ProgressBar/Fill）在 Canvas 下找控件，并把进度条强制设成 Filled/Horizontal/Left；每帧把 0~1 的进度写进 fillAmount。找不到节点只报错不崩，读条界面退化成纯色底。
      
  en: >
      Locates widgets by hard-coded node names (BgImage / FadeImage / ProgressBar/Fill) under the Canvas and forces the bar to Filled/Horizontal/Left; writes 0..1 progress into fillAmount each frame. Missing nodes log an error rather than crash, degrading to a flat coloured screen.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.396Z"
fingerprint: 69a4a1bd4f73b2dedecbe57dd419556d295a3c3480ce4cb7287c141697c1b224
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 155
    end_line: 211
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 584
    end_line: 597
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L155-L211"
    description:
      zh: >
          Awake/Start/FindNodes：定位背景、遮罩与进度条节点。
          
      en: >
          Awake/Start/FindNodes: locates background, fade and bar.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L584-L597"
    description:
      zh: >
          SetProgressVisual：把归一化进度写进进度条。
          
      en: >
          SetProgressVisual: writes progress into the bar.
          
---
