---
uid: b3acccfe
id: project-chaos.framework.runmap.loading-text
parent: project-chaos.framework.runmap
tags: [runmap, localization, loading]
name: {zh: "地图生成：读条文案", en: "RunMap: Loading Text"}
description:
  zh: >
      地图生成阶段的读条文案。**刻意不走游戏本地化系统**：MapLoading 表一列一语言（简中/繁中/英/日/韩），运行时按设置里当前的语言（SettingsManager.Applied.language）取列，某语言为空则回退简中。
      
  en: >
      Loading-screen text for the map-generation stages. Deliberately NOT routed through the game's localization system: one column per language (CN/TW/EN/JP/KR) in the MapLoading table, selected by the language currently set in settings (SettingsManager.Applied.language), with fallback to Simplified Chinese.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.717Z"
fingerprint: 43e2e1c8bd5ee8cf8a22ea1ea5404c4f239b99518f57815f4e0e88e05dd486d0
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapLoadingText.cs"
    line: 1
    end_line: 112
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapLoadingText.cs#L1-L112"
    description:
      zh: >
          按当前设置语言取读条阶段文案；也可指定语言取列、或按 sort 列出全部阶段。
          
      en: >
          Read the loading-stage description for the current settings language; pick a language column; list all stages by sort.
          
  - protocol: rpc
    path: "RunMapLoadingText.ForStage"
    description:
      zh: >
          ForStage(stage)：取某生成阶段的读条文案，语言 = 设置里当前选中的语言。
          
      en: >
          ForStage(stage): the loading-screen text for a generation stage, in the language currently set in settings.
          
---
