---
uid: 5c1a7e10
id: project-chaos.bootstrap.loading-screen.metrics
parent: project-chaos.bootstrap.loading-screen
tags: [bootstrap, loading, diagnostics]
name: {zh: "运行指标与回调", en: "Metrics & Callback"}
description:
  zh: >
      供自动化验收读取的 static 运行统计：进度满时刻、目标场景激活时刻、激活前最大单帧、加载帧数、目标场景名、是否失败。设计成 static 是因为 LoadingScene 激活瞬间本对象会被销毁。sceneLoaded 静态回调只注册一次并把激活时刻写回。
      
  en: >
      Static run statistics for automated acceptance: time progress hit full, time the target scene activated, largest pre-activation frame delta, loading frame count, target scene name and failure flag. They are static because this object is destroyed the moment LoadingScene activates. A static sceneLoaded callback is hooked exactly once and writes the activation timestamp back.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.722Z"
fingerprint: 061eb4233455261dec94ded8289ecf3ff31bc96f8fe3d2671bc973fb81d5169a
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 79
    end_line: 125
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 213
    end_line: 247
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 72
    end_line: 72
apis:
  - protocol: rpc
    path: "LoadingController.ProgressFullRealtime"
    description:
      zh: >
          进度首次到满的时刻（realtime）。
          
      en: >
          Realtime moment progress first hit full.
          
  - protocol: rpc
    path: "LoadingController.ActivatedRealtime"
    description:
      zh: >
          目标场景完成激活的时刻（realtime）。
          
      en: >
          Realtime moment the target scene activated.
          
  - protocol: rpc
    path: "LoadingController.MaxFrameDeltaBeforeActivationSeconds"
    description:
      zh: >
          激活前（跳过第 1 帧）的最大单帧时长。
          
      en: >
          Largest pre-activation frame delta seconds.
          
  - protocol: rpc
    path: "LoadingController.LastTargetScene"
    description:
      zh: >
          最近一次加载的目标场景名。
          
      en: >
          Target scene name of the latest load.
          
  - protocol: rpc
    path: "LoadingController.LastRunFailed"
    description:
      zh: >
          最近一次加载是否失败（停在读条界面）。
          
      en: >
          Whether the latest load failed and stalled.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L213-L247"
    description:
      zh: >
          HookSceneLoadedOnce/OnAnySceneLoaded：一次性注册并记录激活时刻。
          
      en: >
          HookSceneLoadedOnce/OnAnySceneLoaded: one-shot hook.
          
---
