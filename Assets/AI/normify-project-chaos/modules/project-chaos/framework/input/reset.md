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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.937Z"
fingerprint: 91856414ec49bb874cc0c264ab798d341f2263a768ae022c2cef522278480ccd
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
