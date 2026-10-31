---
uid: a3c00012
id: project-chaos.gameplay-3c.camera.shake
parent: project-chaos.gameplay-3c.camera
tags: [camera, shake, impulse]
name: {zh: "战斗镜头抖动", en: "Combat Camera Shake"}
description:
  zh: >
      基于 Cinemachine Impulse 的战斗镜头抖动：玩家侧 CinemachineImpulseSource + 相机侧 ImpulseListener，走独立 channel 与自定义抖动曲线，不再手写位置偏移。CombatCameraShakeSettings 是保存幅度、上限与触发间隔的 ScriptableObject。
      
  en: >
      Combat camera shake built on Cinemachine Impulse: a CinemachineImpulseSource on the player plus an ImpulseListener on the camera, driven on an independent channel with a custom shake curve, so no hand-written positional offset remains. CombatCameraShakeSettings is the ScriptableObject holding amplitudes, limits and trigger interval.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.159Z"
fingerprint: 830a427836c2f81709b80aa6152163137524ba1278a4dde17735e0241addb0a2
source:
  - path: "Assets/Scripts/Runtime/3C/Camera/CombatCameraShake.cs"
  - path: "Assets/Scripts/Runtime/3C/Camera/CombatCameraShakeSettings.cs"
apis:
  - protocol: rpc
    path: "CombatCameraShake.Attach"
    description:
      zh: >
          Attach(transform)：给指定目标挂上一个抖动源。
          
      en: >
          Attach(transform): adds a shake source to the given target.
          
  - protocol: rpc
    path: "CombatCameraShake.Detach"
    description:
      zh: >
          Detach(expectedTarget)：移除抖动源，目标已变则拒绝。
          
      en: >
          Detach(expectedTarget): removes the source, refusing if the target changed.
          
  - protocol: rpc
    path: "CombatCameraShake.NotifyHit"
    description:
      zh: >
          NotifyHit(from, dir, heavy)：触发命中抖动。
          
      en: >
          NotifyHit(from, dir, heavy): fires the hit impulse.
          
  - protocol: rpc
    path: "CombatCameraShake.NotifyHurt"
    description:
      zh: >
          NotifyHurt(from, dir)：触发受击抖动。
          
      en: >
          NotifyHurt(from, dir): fires the hurt impulse.
          
---
