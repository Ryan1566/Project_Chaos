---
uid: a3c00011
id: project-chaos.gameplay-3c.camera.rig
parent: project-chaos.gameplay-3c.camera
tags: [camera, cinemachine]
name: {zh: "镜头控制器", en: "Gameplay Camera Rig"}
description:
  zh: >
      输出相机的唯一控制器。在 CinemachineBrain 下包装一个 CinemachineVirtualCamera（FramingTransposer + Confiner2D），在村庄与局内两种模式间切换；手写逻辑只剩锚点位置计算与 LateUpdate 里的 manual brain 更新。
      
  en: >
      The only controller of the output camera. Wraps a CinemachineVirtualCamera (FramingTransposer + Confiner2D) under a CinemachineBrain, switching between village and run modes; the only hand-written logic left is the anchor position and the manual brain update in LateUpdate.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.158Z"
fingerprint: 1540b8dd18954287d815272e5d91d443176f084237baf3fc2a22f401089b74c4
source:
  - path: "Assets/Scripts/Runtime/3C/Camera/GameplayCameraRig.cs"
apis:
  - protocol: rpc
    path: "GameplayCameraRig.For"
    description:
      zh: >
          For(camera)：返回该输出相机对应的控制器，需要时创建。
          
      en: >
          For(camera): returns the rig that owns this output camera, creating it if needed.
          
  - protocol: rpc
    path: "GameplayCameraRig.BindRun"
    description:
      zh: >
          BindRun(target, bounds)：在矩形范围内跟随局内目标。
          
      en: >
          BindRun(target, bounds): follow a run target inside a rectangle.
          
  - protocol: rpc
    path: "GameplayCameraRig.BindVillage"
    description:
      zh: >
          BindVillage(target, width, fixedY)：以固定纵向锚点跟随村庄目标。
          
      en: >
          BindVillage(target, width, fixedY): follow a village target with fixed vertical anchor.
          
  - protocol: rpc
    path: "GameplayCameraRig.SnapToTarget"
    description:
      zh: >
          SnapToTarget()：把虚拟相机直接跳到当前锚点，无缓动。
          
      en: >
          SnapToTarget(): teleports the virtual camera to the current anchor with no easing.
          
  - protocol: rpc
    path: "GameplayCameraRig.NotifyTargetWarped"
    description:
      zh: >
          NotifyTargetWarped(target, delta)：重生/传送后重置锚点、阻尼与同帧缓存。
          
      en: >
          NotifyTargetWarped(target, delta): resets anchor, damping and frame caches after a respawn or teleport.
          
  - protocol: rpc
    path: "GameplayCameraRig.Release"
    description:
      zh: >
          Release(expectedTarget)：仅当相机仍由该目标持有时才释放。
          
      en: >
          Release(expectedTarget): releases the camera only if it is still held by that target.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.packages
    label: {zh: "依赖 Cinemachine 2.10.3 包", en: "Cinemachine 2.10.3 package"}
---
