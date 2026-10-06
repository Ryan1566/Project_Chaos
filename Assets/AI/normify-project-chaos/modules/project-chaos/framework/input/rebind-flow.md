---
uid: f1c00025
id: project-chaos.framework.input.rebind-flow
parent: project-chaos.framework.input
name: {zh: "改键流程", en: "Rebind Flow"}
description:
  zh: >
      改键流程：BeginRebind 启动监听（超时自动取消、取消键边沿判定）、结果回调分发与 CancelRebind 手动取消。
      
  en: >
      Rebind operation flow: BeginRebind with per-binding timeout and cancel-key handling, result callback and CancelRebind, plus override application.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.746Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 691
    end_line: 950
apis:
  - protocol: rpc
    path: "InputManager.BeginRebind"
    description:
      zh: >
          对指定绑定索引开始改键监听。
          
      en: >
          Starts a rebinding operation for one binding index.
          
  - protocol: rpc
    path: "InputManager.CancelRebind"
    description:
      zh: >
          取消正在进行的改键监听。
          
      en: >
          Cancels the running rebinding operation.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs#L691-L950"
    description:
      zh: >
          改键开始、结束与取消的流程实现段。
          
      en: >
          Rebind start/finish/cancel flow internals.
          
deps:
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:InputManager.BeginRebind"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "改键日志", en: "Rebind logging"}
---
