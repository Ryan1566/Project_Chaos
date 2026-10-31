---
uid: b7c00003
id: project-chaos.editor-tools.camera-tool.runtime-probes
parent: project-chaos.editor-tools.camera-tool
tags: [editor, camera, probe]
name: {zh: "镜头运行时探针", en: "Camera Runtime Probes"}
description:
  zh: >
      Play 模式探针：挂到正在跑的游戏上，报告 Cinemachine 跟随/构图与 Impulse 抖动是否真的在移动相机；含一个假受击目标，用于在没有玩法输入的情况下触发命中。
      
  en: >
      Play-mode probes that attach to the running game and report whether Cinemachine follow/framing and impulse shake actually move the camera, including a dummy damage target used to trigger hits without gameplay input.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.167Z"
fingerprint: ed745838cd648486fcde9926513eb44841a03d71ede572f482f40c7c0a40cd3a
source:
  - path: "Assets/Scripts/Editor/CameraTool/CinemachineFramingRuntimeProbe.cs"
  - path: "Assets/Scripts/Editor/CameraTool/CinemachineShakeRuntimeProbe.cs"
apis:
  - protocol: rpc
    path: "CinemachineFramingRuntimeProbe.Start"
    description:
      zh: >
          Start()：开始构图运行时探针。
          
      en: >
          Start(): begins the framing runtime probe.
          
  - protocol: rpc
    path: "CinemachineFramingRuntimeProbe.Status"
    description:
      zh: >
          Status：最新的探针状态串。
          
      en: >
          Status: latest probe status string.
          
  - protocol: rpc
    path: "CinemachineShakeRuntimeProbe.Start"
    description:
      zh: >
          Start()：开始 Impulse 抖动运行时探针。
          
      en: >
          Start(): begins the impulse-shake runtime probe.
          
  - protocol: rpc
    path: "CinemachineProbeDamageTarget.TakeDamage"
    description:
      zh: >
          TakeDamage(amount)：给探针计命中的假目标。
          
      en: >
          TakeDamage(amount): dummy target that counts hits for the probe.
          
deps:
  - kind: reference
    to: project-chaos.gameplay-3c.camera.shake
    from_api: "rpc:CinemachineShakeRuntimeProbe.Start"
    label: {zh: "探测线上抖动", en: "Probes the live shake"}
---
