---
uid: f1c00062
id: project-chaos.framework.dev-tests.ui-probes
parent: project-chaos.framework.dev-tests
name: {zh: "UI 自测探针", en: "UI Probes"}
description:
  zh: >
      UI 自测探针：点击按钮经 UIManager Push/Pop 主菜单面板，配一个空壳 TestPanel。
      
  en: >
      UI dev probes: a button that pushes/pops MainMenuPanel through UIManager, plus an empty TestPanel shell.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.742Z"
fingerprint: b308ffe81af107a312af5ea3c835968dea9210eebc80d15bc3a4b2dccd16d0d3
source:
  - path: "Assets/Scripts/Runtime/GameTest/UITest/TestPanel.cs"
    line: 1
    end_line: 26
  - path: "Assets/Scripts/Runtime/GameTest/UITest/UITestEnter.cs"
    line: 1
    end_line: 43
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/UITest/TestPanel.cs"
    description:
      zh: >
          空面板探针（内容已全部注释）。
          
      en: >
          Empty panel probe (all content commented out).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/UITest/UITestEnter.cs"
    description:
      zh: >
          面板 Push/Pop 切换探针。
          
      en: >
          Panel push/pop toggle probe.
          
deps:
  - kind: call
    to: project-chaos.framework.ui.manager
    from_api: "file:Assets/Scripts/Runtime/GameTest/UITest/UITestEnter.cs"
    to_api: "rpc:UIManager.PushPanel"
    label: {zh: "打开面板", en: "Push panel"}
---
