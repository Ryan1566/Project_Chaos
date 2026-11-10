---
uid: b7c00002
id: project-chaos.editor-tools.camera-tool.verification
parent: project-chaos.editor-tools.camera-tool
tags: [editor, camera, verification]
name: {zh: "镜头验证器", en: "Camera Verifications"}
description:
  zh: >
      对已发布镜头栈的行为验证：CombatCameraShakeVerification 校验命中/受击/重击抖动、强度上限与最小触发间隔；CinemachineFlowVerification 走一遍跟随、构图与村庄模式切换。两者都在 Tools/Project Chaos/验证 下加临时菜单项。
      
  en: >
      Verifications that assert the shipped camera stack behaves: CombatCameraShakeVerification checks hit/hurt/heavy impulses, the intensity cap and the min trigger interval; CinemachineFlowVerification walks follow, framing and village-mode switching. Both add a temporary menu entry under Tools/Project Chaos/验证.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-11-10T14:52:00.914Z"
fingerprint: b923d8b90504fc142d9cf0b346db5824dd6da6cdef4977d539ba46090ff8d9fb
source:
  - path: "Assets/Scripts/Editor/CameraTool/CombatCameraShakeVerification.cs"
  - path: "Assets/Scripts/Editor/CameraTool/CinemachineFlowVerification.cs"
apis:
  - protocol: rpc
    path: "CombatCameraShakeVerification.Verify"
    description:
      zh: >
          Verify()：在临时场景跑一遍验证并返回报告串。
          
      en: >
          Verify(): builds a scratch scene and returns the report string.
          
  - protocol: rpc
    path: "CombatCameraShakeVerification.VerifyBatch"
    description:
      zh: >
          VerifyBatch()：给 CI / batchmode 的无头入口。
          
      en: >
          VerifyBatch(): headless entry for CI and batchmode.
          
  - protocol: rpc
    path: "CombatCameraShakeVerification.LastPassed"
    description:
      zh: >
          LastPassed：最近一次验证的结果。
          
      en: >
          LastPassed: result of the most recent verification run.
          
  - protocol: rpc
    path: "CinemachineFlowVerification.Start"
    description:
      zh: >
          Start()：跑跟随/构图/村庄模式的流程验证。
          
      en: >
          Start(): runs the follow/framing/village-mode flow verification.
          
  - protocol: rpc
    path: "CinemachineFlowVerification.Status"
    description:
      zh: >
          Status：运行中验证的可读状态串。
          
      en: >
          Status: human-readable status of the running verification.
          
deps:
  - kind: reference
    to: project-chaos.gameplay-3c.camera.rig
    from_api: "rpc:CombatCameraShakeVerification.Verify"
    label: {zh: "验证已发布的控制器", en: "Verifies the shipped rig"}
  - kind: reference
    to: project-chaos.gameplay-3c.camera.shake
    from_api: "rpc:CombatCameraShakeVerification.Verify"
    label: {zh: "验证抖动栈", en: "Verifies the shake stack"}
---
