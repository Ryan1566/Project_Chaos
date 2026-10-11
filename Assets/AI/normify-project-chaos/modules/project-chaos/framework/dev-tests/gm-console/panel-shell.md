---
uid: 9f3c1a71
id: project-chaos.framework.dev-tests.gm-console.panel-shell
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool, ui]
name: {zh: "GM 面板骨架与切换", en: "GM Panel Shell and Switching"}
description:
  zh: >
      GM 面板的骨架与切换状态机：按固定节点路径绑定 MainList / 副列表 Content / ContentArea / 标题 / 关闭按钮，并维护「选分类 → 重建副列表 → 选版块 → 只显示该版块页面」；关闭按钮与 CloseGM 事件收面板。含一段踩坑说明：GMPanel 上不许再挂 Canvas，否则面板里的控件不参与射线检测。
      
  en: >
      GM panel shell and switching state machine: binds MainList / sub-list Content / ContentArea / title / close button by fixed node paths and maintains category select -> section list rebuild -> section select -> show only that page. Also documents the pitfall: GMPanel must not carry its own Canvas, or its controls are never raycast.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.707Z"
fingerprint: 07ceed9bdb9694deba7f828640afcba72d609509c58db0db64b7f17bdc5dc472
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
