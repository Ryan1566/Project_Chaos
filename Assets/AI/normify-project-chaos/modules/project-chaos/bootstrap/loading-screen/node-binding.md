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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:10.104Z"
fingerprint: 3c1cad158685b77698b5133115ab31efdbfd3f85713842163821e086c42559fd
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
