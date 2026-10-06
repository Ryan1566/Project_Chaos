---
uid: f1c00044
id: project-chaos.framework.setting-rows.keybind
parent: project-chaos.framework.setting-rows
name: {zh: "改键行核心", en: "Keybind Row Core"}
description:
  zh: >
      SettingRow_Keybind 核心：解析单/双槽位的按键与图标子节点，暴露改键与重置请求回调。
      
  en: >
      SettingRow_Keybind core: resolves key/icon child nodes for one or two slots and exposes rebind/reset request callbacks.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.754Z"
fingerprint: 2186e49773a62815771d127f9d2318a12e8d0b72a5564611164ee53443310610
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Keybind.cs"
    line: 1
    end_line: 103
apis:
  - protocol: rpc
    path: "SettingRow_Keybind.OnRebindRequested"
    description:
      zh: >
          主槽位请求改键。
          
      en: >
          Raised when the primary slot requests rebinding.
          
  - protocol: rpc
    path: "SettingRow_Keybind.OnSecondRebindRequested"
    description:
      zh: >
          第二槽位请求改键。
          
      en: >
          Raised when the second slot requests rebinding.
          
  - protocol: rpc
    path: "SettingRow_Keybind.OnResetRequested"
    description:
      zh: >
          请求重置按键。
          
      en: >
          Raised when reset is requested.
          
  - protocol: rpc
    path: "SettingRow_Keybind.HasSecondSlot"
    description:
      zh: >
          本行是否具有第二槽位。
          
      en: >
          Whether the row has a second binding slot.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Keybind.HasSecondSlot"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
  - kind: reference
    to: project-chaos.framework.setting-rows.icon-text
    from_api: "rpc:SettingRow_Keybind.HasSecondSlot"
    to_api: "rpc:KeyIconText.SetDisplay"
    label: {zh: "按键图标文本", en: "Key icon text"}
---
