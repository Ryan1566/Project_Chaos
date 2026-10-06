---
uid: f1c00040
id: project-chaos.framework.setting-rows.toggle
parent: project-chaos.framework.setting-rows
name: {zh: "设置行·开关", en: "Setting Row Toggle"}
description:
  zh: >
      SettingRow_Toggle 布尔设置行（如垂直同步）：值变化回调与静默赋值。
      
  en: >
      SettingRow_Toggle: boolean setting row (e.g. vSync) with value-changed callback and silent set.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.755Z"
fingerprint: 4346772d804f5427938ce142b807362184a3b1b82584eea12da0d2e620a41a98
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Toggle.cs"
    line: 1
    end_line: 42
apis:
  - protocol: rpc
    path: "SettingRow_Toggle.OnValueChanged"
    description:
      zh: >
          开关值变化回调。
          
      en: >
          Value-changed callback.
          
  - protocol: rpc
    path: "SettingRow_Toggle.SetValueWithoutNotify"
    description:
      zh: >
          静默设置开关值（不触发回调）。
          
      en: >
          Sets the value without raising the callback.
          
  - protocol: rpc
    path: "SettingRow_Toggle.SetClickable"
    description:
      zh: >
          重写可点击状态处理。
          
      en: >
          Overrides clickable state handling.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Toggle.OnValueChanged"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
