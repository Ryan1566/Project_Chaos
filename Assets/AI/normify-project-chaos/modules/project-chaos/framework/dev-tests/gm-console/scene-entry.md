---
uid: 9f3c1a73
id: project-chaos.framework.dev-tests.gm-console.scene-entry
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool]
name: {zh: "GM 入口按钮与常驻画布", en: "GM Entry Button"}
description:
  zh: >
      场景里的 GM 入口：GMBtn 上的按钮组件开关 GM 面板；GM_Canvas 跨场景常驻。2026-10 起常驻组件已换成通用的 framework.dont-destroy（GM/GMDontDestroy.cs 已不存在）。
      
  en: >
      The in-scene GM entry: the button component on GMBtn toggles the GM panel, and the GM_Canvas survives scene loads. Since 2026-10 the persistence component is the generic framework DontDestroy helper (GM/GMDontDestroy.cs no longer exists).
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.707Z"
fingerprint: 6f4d093777e9476a73c2cffca750461c0525969e2f9c21284a73e8b2c456c0db
source:
  - path: "Assets/Scripts/Runtime/GM/EnterGM.cs"
    line: 1
    end_line: 27
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GM/EnterGM.cs"
    description:
      zh: >
          GM 入口按钮：开关 GM 面板。
          
      en: >
          GM entry button: toggles the GM panel.
          
deps:
  - kind: reference
    to: project-chaos.framework.dont-destroy
    from_api: "file:Assets/Scripts/Runtime/GM/EnterGM.cs"
    to_api: "file:Assets/Scripts/Runtime/DontDestroy.cs"
    label: {zh: "常驻画布", en: "Persistent canvas"}
---
