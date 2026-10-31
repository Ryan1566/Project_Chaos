---
uid: a3c00013
id: project-chaos.gameplay-3c.camera.viewport
parent: project-chaos.gameplay-3c.camera
tags: [camera, cinemachine]
name: {zh: "相机视口边界", en: "Camera Viewport Bounds"}
description:
  zh: >
      CinemachineExtension：在 Cinemachine 的最终阶段 Stage.Finalize 才夹紧相机位置（抖动之后），并在地图小于视口时保持居中。
      
  en: >
      A CinemachineExtension that clamps the camera position at Cinemachine's final Stage.Finalize, after shake is applied, and keeps a map smaller than the viewport centred.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.159Z"
fingerprint: f992f279e78ff9e430234118aa909a5325f3cb68145e914fe460f3aa2b33c624
source:
  - path: "Assets/Scripts/Runtime/3C/Camera/CameraViewportBounds.cs"
apis:
  - protocol: rpc
    path: "CameraViewportBounds.Bounds"
    description:
      zh: >
          Bounds：视口被夹紧到的矩形。
          
      en: >
          Bounds: the rectangle the viewport is clamped to.
          
  - protocol: rpc
    path: "CameraViewportBounds.Output"
    description:
      zh: >
          Output：该扩展读取的 Cinemachine 输出相机。
          
      en: >
          Output: the Cinemachine output camera this extension reads.
          
---
