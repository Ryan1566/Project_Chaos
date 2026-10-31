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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.944Z"
fingerprint: e14b099292185ca6a1edce85d01cf94359b84e25e9737771e3c0736139f32154
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
