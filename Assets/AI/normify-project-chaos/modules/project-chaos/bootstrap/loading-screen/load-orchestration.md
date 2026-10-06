---
uid: 5c1a7e0d
id: project-chaos.bootstrap.loading-screen.load-orchestration
parent: project-chaos.bootstrap.loading-screen
tags: [bootstrap, loading]
name: {zh: "加载主流程编排", en: "Load Orchestration"}
description:
  zh: >
      Run 协程：把后台加载线程优先级提到 High，用 unscaled 时间推进轮播与进度，做 0.9 封顶归一化（激活后 progress 会跳到 1.0，故用 Clamp01 双保险），等进度满+资源就绪+至少一帧后置 allowSceneActivation=true；退出时恢复原优先级。
      
  en: >
      The Run coroutine: raises the background loading thread priority to High, advances carousel and progress on unscaled time, normalises the 0.9 ceiling (progress jumps to 1.0 after activation, hence the Clamp01 belt-and-braces), and sets allowSceneActivation=true only once progress is full, assets are ready and a frame has rendered; restores the original priority on exit.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.722Z"
fingerprint: 061eb4233455261dec94ded8289ecf3ff31bc96f8fe3d2671bc973fb81d5169a
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 249
    end_line: 401
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L251-L387"
    description:
      zh: >
          Run 协程：加载、归一化、激活的完整时序。
          
      en: >
          Run coroutine: full load/normalise/activate sequence.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L388-L401"
    description:
      zh: >
          RestorePriority：归还后台加载线程优先级。
          
      en: >
          RestorePriority: restores loading thread priority.
          
deps:
  - kind: call
    to: project-chaos.bootstrap.scene-loading.manager
    from_api: "file:Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L251-L387"
    to_api: "rpc:ScenesLoadManager.BeginLoadSceneAsync"
    label: {zh: "持控式异步加载目标场景", en: "Hands-off async scene load"}
---
