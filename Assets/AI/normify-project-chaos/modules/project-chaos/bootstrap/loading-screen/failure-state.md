---
uid: 5c1a7e0f
id: project-chaos.bootstrap.loading-screen.failure-state
parent: project-chaos.bootstrap.loading-screen
tags: [bootstrap, loading, error-handling]
name: {zh: "失败态兜底", en: "Failure Fallback"}
description:
  zh: >
      TryFail 把界面切成暗红底+红条并记录 LastRunFailed，让玩家停在原地看到失败而不是黑屏卡死；IsSceneInBuildSettings 只做软预检（不用 Application.CanStreamedLevelBeLoaded——本机实测它对已登记场景在编辑器下也返回 false）。
      
  en: >
      TryFail switches the screen to a dark-red background with a red bar and records LastRunFailed, so the player sees a failure instead of a frozen black screen. IsSceneInBuildSettings is only a soft pre-check — Application.CanStreamedLevelBeLoaded is deliberately avoided because on this Unity version it returns false even for registered scenes in the editor.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.721Z"
fingerprint: 061eb4233455261dec94ded8289ecf3ff31bc96f8fe3d2671bc973fb81d5169a
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 598
    end_line: 629
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 74
    end_line: 77
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L598-L619"
    description:
      zh: >
          TryFail：失败态上色与标记，不黑屏卡死。
          
      en: >
          TryFail: colours and flags failure without freezing.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L620-L629"
    description:
      zh: >
          IsSceneInBuildSettings：按约定路径软预检构建清单。
          
      en: >
          IsSceneInBuildSettings: soft check against build list.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L74-L77"
    description:
      zh: >
          兜底色/失败色常量。
          
      en: >
          Fallback and failure colour constants.
          
---
