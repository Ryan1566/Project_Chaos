---
uid: a3c00015
id: project-chaos.gameplay-3c.camera.day-night-sky
parent: project-chaos.gameplay-3c.camera
tags: [camera, day-night, render]
name: {zh: "镜头：昼夜天空", en: "Camera: Day/Night Sky"}
description:
  zh: >
      相机级的昼夜天空：按当前昼夜阶段给相机背景上色与渐变，并在村庄/局内两种模式间切换色调。
      
  en: >
      Per-camera day/night sky: tints the camera background and blends sky colour from the current DayNightClock phase, switching tints between village and run modes.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.833Z"
fingerprint: 2ffb4be91bad3ffa1b3d8d6a2fdf1ff69884f0369ab8aee80ecbf3a0ff1426aa
source:
  - path: "Assets/Scripts/Runtime/3C/Camera/DayNightSkyRenderer.cs"
apis:
  - protocol: rpc
    path: "Camera.DayNightSkyRenderer.Attach"
    description:
      zh: >
          给相机挂载天空渲染
          
      en: >
          Attach a sky renderer to a camera
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/3C/Camera/DayNightSkyRenderer.cs"
    description:
      zh: >
          昼夜天空渲染器
          
      en: >
          Day/night sky renderer
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.day-night
    label: {zh: "读取昼夜阶段", en: "Reads day/night phase"}
  - kind: reference
    to: project-chaos.gameplay-3c.camera.rig
    label: {zh: "跟随相机模式", en: "Follows camera mode"}
---
