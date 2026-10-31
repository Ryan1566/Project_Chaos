---
uid: f1c00024
id: project-chaos.gameplay-3c.control.input.rebind-steps
parent: project-chaos.gameplay-3c.control.input
name: {zh: "改键步骤拆分", en: "Rebind Steps"}
description:
  zh: >
      改键步骤拆分：把一条绑定（含 1DAxis/2DVector 组合键）拆为带标签的 RebindStep 列表，使每个方向可独立改键。
      
  en: >
      Rebind step splitting: turns a binding (including 1D/2D composites) into per-part RebindStep records with labels.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.545Z"
fingerprint: a3e7fdcf7243984b01ff23825e8d60d83ccd36aee697222dab345f0bff3f3cb3
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 573
    end_line: 659
apis:
  - protocol: rpc
    path: "InputManager.GetRebindSteps"
    description:
      zh: >
          GetRebindSteps(actionName, kind)：把复合绑定拆成每个部一条记录。
          
      en: >
          GetRebindSteps(actionName, kind): splits a composite binding into one step per part.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs#L573-L659"
    description:
      zh: >
          本模块在 InputManager.cs 中的源码区段。
          
      en: >
          Source region of this module inside InputManager.cs.
          
---
