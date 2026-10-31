---
uid: 9f3c1a71
id: project-chaos.framework.dev-tests.gm-console.panel-shell
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool, ui]
name: {zh: "GM 面板骨架与切换", en: "GM Panel Shell and Switching"}
description:
  zh: >
      GM 面板外壳（场景对象，不走 UIManager 面板栈）：板块列表 + 内容区，由 EnterGM 开关。它的 Canvas 层级高于 UI 画布，因此会压住世界观地图面板左上角（已知观感问题）。
      
  en: >
      GM panel shell (scene object, not pushed through the UIManager stack): section list plus a content area, toggled by EnterGM. Its canvas sits above the UI canvas, so it overlaps the top-left of the world map panel (known cosmetic issue).
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.614Z"
fingerprint: 255736699a608b0d4c4a4d5756dd5e2ffa04756b7559541748789d167612db08
source:
  - path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    line: 1
    end_line: 160
  - path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    line: 230
    end_line: 338
  - path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    line: 455
    end_line: 468
apis:
  - protocol: rpc
    path: "GMPanel.ClosePanel"
    description:
      zh: >
          收起 GM 面板（关闭按钮与 CloseGM 事件都走它）。
          
      en: >
          Hides the GM panel; used by the close button and the CloseGM event.
          
deps:
  - kind: call
    to: project-chaos.framework.events.manager
    label: {zh: "订阅 CloseGM 事件", en: "Subscribes to CloseGM event"}
---
