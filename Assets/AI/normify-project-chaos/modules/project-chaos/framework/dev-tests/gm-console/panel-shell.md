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
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:46.651Z"
fingerprint: 46e9c0ef06a72b1823d6d594fe13c9a5037cee8e8cff5714cae42caeab8a3bce
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
