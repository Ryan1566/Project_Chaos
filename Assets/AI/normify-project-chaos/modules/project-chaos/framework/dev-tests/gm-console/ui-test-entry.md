---
uid: 9f3c1a74
id: project-chaos.framework.dev-tests.gm-console.ui-test-entry
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool]
name: {zh: "UI 测试入口", en: "UI Test Entry"}
description:
  zh: >
      GM 面板里的 UI 测试入口：点击后用 UIManager 推入主菜单面板，再触发 CloseGM 收起 GM 面板。历史上它是场景里独立的 UI 测试按钮，现在作为 GM 控制台的一个版块按钮由面板运行时挂载。
      
  en: >
      The UI test entry inside the GM panel: pushes the main-menu panel through UIManager and then fires CloseGM to collapse the GM panel. It used to be a standalone scene button and is now attached to a section button at runtime.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.707Z"
fingerprint: e07a66661d5354220249d197188d6fb0d161d1ae964453434e84a244880a9fac
source:
  - path: "Assets/Scripts/Runtime/GM/GM_UITestEnter.cs"
    line: 1
    end_line: 32
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GM/GM_UITestEnter.cs"
    description:
      zh: >
          UI 测试入口组件。
          
      en: >
          The UI test entry component.
          
deps:
  - kind: call
    to: project-chaos.framework.ui.manager
    label: {zh: "推入主菜单面板", en: "Pushes the main-menu panel"}
  - kind: call
    to: project-chaos.framework.events.extension
    label: {zh: "触发 CloseGM", en: "Fires CloseGM"}
---
