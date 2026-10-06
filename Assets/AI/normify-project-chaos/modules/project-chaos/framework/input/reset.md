---
uid: f1c00022
id: project-chaos.framework.input.reset
parent: project-chaos.framework.input
name: {zh: "绑定重置", en: "Binding Reset"}
description:
  zh: >
      绑定重置与显示文本：ResetBinding 支持整条绑定或组合键分部重置，GetBindingDisplay 取显示名。
      
  en: >
      Binding reset and display text: ResetBinding for a whole binding or one composite part, plus GetBindingDisplay.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.747Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 278
    end_line: 356
apis:
  - protocol: rpc
    path: "InputManager.ResetBinding"
    description:
      zh: >
          把某绑定（或组合键的一个分部）重置为默认路径。
          
      en: >
          Resets a binding (or one composite part) to its default path.
          
  - protocol: rpc
    path: "InputManager.GetBindingDisplay"
    description:
      zh: >
          取某绑定的显示文本。
          
      en: >
          Gets the display text for a binding.
          
---
