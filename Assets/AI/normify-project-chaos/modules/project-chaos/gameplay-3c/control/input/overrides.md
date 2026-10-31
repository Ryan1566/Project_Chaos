---
uid: f1c00021
id: project-chaos.gameplay-3c.control.input.overrides
parent: project-chaos.gameplay-3c.control.input
name: {zh: "改键覆盖存取", en: "Binding Overrides"}
description:
  zh: >
      改键覆盖项持久化：序列化/反序列化为 JSON（由设置系统存盘）与按组清空。
      
  en: >
      Binding override persistence: save/load overrides as JSON (stored inside settings) and clear all or one group.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.543Z"
fingerprint: a3e7fdcf7243984b01ff23825e8d60d83ccd36aee697222dab345f0bff3f3cb3
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 165
    end_line: 245
apis:
  - protocol: rpc
    path: "InputManager.SaveOverridesJson"
    description:
      zh: >
          把全部改键覆盖项序列化为 JSON 字符串。
          
      en: >
          Serializes all binding overrides to a JSON string.
          
  - protocol: rpc
    path: "InputManager.LoadOverridesJson"
    description:
      zh: >
          从 JSON 字符串恢复改键覆盖项。
          
      en: >
          Restores binding overrides from a JSON string.
          
  - protocol: rpc
    path: "InputManager.ClearAllOverrides"
    description:
      zh: >
          清空全部改键覆盖项。
          
      en: >
          Clears all binding overrides.
          
  - protocol: rpc
    path: "InputManager.ClearOverridesFor"
    description:
      zh: >
          清空指定按键组（键鼠/手柄）的覆盖项。
          
      en: >
          Clears overrides for one binding group.
          
---
