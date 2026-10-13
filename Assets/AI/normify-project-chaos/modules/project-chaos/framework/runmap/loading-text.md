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
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.430Z"
fingerprint: 789e7f5eb1922b9d83b114e83a015703f8ea087a10a7f0ebf26699c35365932c
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
