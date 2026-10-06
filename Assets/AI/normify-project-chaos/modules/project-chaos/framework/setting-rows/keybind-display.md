---
uid: f1c00045
id: project-chaos.framework.setting-rows.keybind-display
parent: project-chaos.framework.setting-rows
name: {zh: "改键行显示", en: "Keybind Row Display"}
description:
  zh: >
      改键行显示：把文本/图标/颜色推给两个槽位的 KeyIconText 子组件。
      
  en: >
      Keybind row display: pushes text/icon/tint into the KeyIconText children of both slots.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.754Z"
fingerprint: 2186e49773a62815771d127f9d2318a12e8d0b72a5564611164ee53443310610
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Keybind.cs"
    line: 104
    end_line: 150
apis:
  - protocol: rpc
    path: "SettingRow_Keybind.SetKeyText"
    description:
      zh: >
          设置主槽位按键文本。
          
      en: >
          Sets the primary slot's key text.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetKeyDisplay"
    description:
      zh: >
          设置主槽位的文本/图标/颜色。
          
      en: >
          Sets the primary slot's text/icon/tint.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetSecondText"
    description:
      zh: >
          设置第二槽位按键文本。
          
      en: >
          Sets the second slot's key text.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetSecondDisplay"
    description:
      zh: >
          设置第二槽位的文本/图标/颜色。
          
      en: >
          Sets the second slot's text/icon/tint.
          
deps:
  - kind: call
    to: project-chaos.framework.setting-rows.icon-text
    from_api: "rpc:SettingRow_Keybind.SetKeyDisplay"
    to_api: "rpc:KeyIconText.SetDisplay"
    label: {zh: "按键图标文本", en: "Key icon text"}
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Keybind.SetKeyText"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
