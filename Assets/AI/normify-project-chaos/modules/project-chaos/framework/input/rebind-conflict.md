---
uid: f1c00026
id: project-chaos.framework.input.rebind-conflict
parent: project-chaos.framework.input
name: {zh: "改键冲突处理", en: "Rebind Conflict"}
description:
  zh: >
      改键冲突处理：新按键已被其它绑定占用时自动解绑冲突项，并含 Action/绑定索引/按键组查找等辅助实现。
      
  en: >
      Rebind conflict handling: clears the duplicate binding that already owns the newly pressed control, plus action/binding lookup helpers.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.746Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 951
    end_line: 1087
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs#L951-L1087"
    description:
      zh: >
          冲突解绑、Action/绑定索引查找与覆盖项移除等内部实现段。
          
      en: >
          Conflict resolution, action lookup and override removal internals.
          
---
