---
uid: f1c00026
id: project-chaos.gameplay-3c.control.input.rebind-conflict
parent: project-chaos.gameplay-3c.control.input
name: {zh: "改键冲突处理", en: "Rebind Conflict"}
description:
  zh: >
      改键冲突处理：新按键已被其它绑定占用时自动解绑冲突项，并含 Action/绑定索引/按键组查找等辅助实现。
      
  en: >
      Rebind conflict handling: clears the duplicate binding that already owns the newly pressed control, plus action/binding lookup helpers.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.748Z"
fingerprint: c11d341f13f442ad4c81a2e57d2833d76598d5112b05d252706445ca41dec428
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 949
    end_line: 1061
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs#L949-L1061"
    description:
      zh: >
          本模块在 InputManager.cs 中的源码区段（ResolveConflict 及其辅助）。
          
      en: >
          Source region of this module inside InputManager.cs (ResolveConflict and helpers).
          
---
