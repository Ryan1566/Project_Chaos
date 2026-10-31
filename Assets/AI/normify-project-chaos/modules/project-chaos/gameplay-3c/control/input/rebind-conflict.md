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
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.546Z"
fingerprint: a3e7fdcf7243984b01ff23825e8d60d83ccd36aee697222dab345f0bff3f3cb3
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
