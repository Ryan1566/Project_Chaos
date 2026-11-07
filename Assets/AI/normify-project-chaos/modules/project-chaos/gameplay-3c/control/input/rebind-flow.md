---
uid: f1c00025
id: project-chaos.gameplay-3c.control.input.rebind-flow
parent: project-chaos.gameplay-3c.control.input
name: {zh: "改键流程", en: "Rebind Flow"}
description:
  zh: >
      改键流程：BeginRebind 启动监听（超时自动取消、取消键边沿判定）、结果回调分发与 CancelRebind 手动取消。
      
  en: >
      Rebind operation flow: BeginRebind with per-binding timeout and cancel-key handling, result callback and CancelRebind, plus override application.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.748Z"
fingerprint: c11d341f13f442ad4c81a2e57d2833d76598d5112b05d252706445ca41dec428
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 660
    end_line: 948
apis:
  - protocol: rpc
    path: "InputManager.BeginRebind"
    description:
      zh: >
          BeginRebind(actionName, bindingIndex, kind, onFinished)：开始监听新绑定。
          
      en: >
          BeginRebind(actionName, bindingIndex, kind, onFinished): starts listening for a new binding.
          
  - protocol: rpc
    path: "InputManager.CancelRebind"
    description:
      zh: >
          CancelRebind()：中断进行中的改键并恢复原绑定。
          
      en: >
          CancelRebind(): aborts the running rebind and restores the previous path.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs#L660-L948"
    description:
      zh: >
          本模块在 InputManager.cs 中的源码区段。
          
      en: >
          Source region of this module inside InputManager.cs.
          
deps:
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:InputManager.BeginRebind"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "改键日志", en: "Rebind logging"}
---
