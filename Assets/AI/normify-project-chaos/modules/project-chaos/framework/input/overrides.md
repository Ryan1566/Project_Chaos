---
uid: f1c00021
id: project-chaos.framework.input.overrides
parent: project-chaos.framework.input
name: {zh: "改键覆盖存取", en: "Binding Overrides"}
description:
  zh: >
      改键覆盖项持久化：序列化/反序列化为 JSON（由设置系统存盘）与按组清空。
      
  en: >
      Binding override persistence: save/load overrides as JSON (stored inside settings) and clear all or one group.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.746Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 201
    end_line: 277
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
