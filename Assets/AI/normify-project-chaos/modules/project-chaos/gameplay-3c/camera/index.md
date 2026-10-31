---
uid: a3c00002
id: project-chaos.gameplay-3c.camera
parent: project-chaos.gameplay-3c
tags: [camera, cinemachine]
name: {zh: "镜头：Cinemachine 跟随、构图与抖动", en: "Camera: Cinemachine Follow, Framing & Shake"}
description:
  zh: >
      镜头归属：Cinemachine 跟随、构图、边界约束与战斗抖动。GameplayCameraRig 是输出相机的唯一控制器（For/BindRun/BindVillage/SnapToTarget/NotifyTargetWarped/Release）；VillageCamera 与 PlayerCameraFollow 只是兼容与生命周期门面，不再实现独立的相机位姿算法；抖动走 Cinemachine Impulse（独立 channel），CameraViewportBounds 在 Cinemachine 最终阶段夹紧视口。
      
  en: >
      Sole owner of the camera: Cinemachine follow, framing, boundary constraints and combat shake. GameplayCameraRig is the only controller of the output camera; VillageCamera and PlayerCameraFollow are compatibility/lifecycle facades only, and shake runs through Cinemachine Impulse on its own channel while CameraViewportBounds clamps the viewport at Cinemachine's final stage.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:06.518Z"
fingerprint: pending
source: []
---
