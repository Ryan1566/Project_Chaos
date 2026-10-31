---
uid: f1c00022
id: project-chaos.gameplay-3c.control.input.reset
parent: project-chaos.gameplay-3c.control.input
name: {zh: "绑定重置", en: "Binding Reset"}
description:
  zh: >
      绑定重置与显示文本：ResetBinding 支持整条绑定或组合键分部重置，GetBindingDisplay 取显示名。
      
  en: >
      Binding reset and display text: ResetBinding for a whole binding or one composite part, plus GetBindingDisplay.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.544Z"
fingerprint: a3e7fdcf7243984b01ff23825e8d60d83ccd36aee697222dab345f0bff3f3cb3
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 246
    end_line: 294
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
