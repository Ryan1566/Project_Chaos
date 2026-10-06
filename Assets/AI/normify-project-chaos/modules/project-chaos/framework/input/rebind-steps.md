---
uid: f1c00024
id: project-chaos.framework.input.rebind-steps
parent: project-chaos.framework.input
name: {zh: "改键步骤拆分", en: "Rebind Steps"}
description:
  zh: >
      改键步骤拆分：把一条绑定（含 1DAxis/2DVector 组合键）拆为带标签的 RebindStep 列表，使每个方向可独立改键。
      
  en: >
      Rebind step splitting: turns a binding (including 1D/2D composites) into per-part RebindStep records with labels.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.746Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 605
    end_line: 690
apis:
  - protocol: rpc
    path: "InputManager.GetRebindSteps"
    description:
      zh: >
          把一条绑定拆成可分别改键的步骤（组合键分部）。
          
      en: >
          Splits a binding into rebindable steps (composite parts).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs#L605-L690"
    description:
      zh: >
          改键步骤拆分与分部标签实现段。
          
      en: >
          Rebind step splitting internals.
          
---
