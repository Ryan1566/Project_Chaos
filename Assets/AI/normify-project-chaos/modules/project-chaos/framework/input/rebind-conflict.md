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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.617Z"
fingerprint: 91856414ec49bb874cc0c264ab798d341f2263a768ae022c2cef522278480ccd
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
