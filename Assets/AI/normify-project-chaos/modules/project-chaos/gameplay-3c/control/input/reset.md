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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.749Z"
fingerprint: c11d341f13f442ad4c81a2e57d2833d76598d5112b05d252706445ca41dec428
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
