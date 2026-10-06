---
uid: f1c00046
id: project-chaos.framework.setting-rows.keybind-listening
parent: project-chaos.framework.setting-rows
name: {zh: "改键行监听态", en: "Keybind Listening State"}
description:
  zh: >
      改键行监听态：改键中显示等待提示，含可点击/可交互重写与按钮回调绑定。
      
  en: >
      Keybind row listening state: shows waiting prompts while rebinding, plus clickable/interactable overrides and button wiring.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.754Z"
fingerprint: 2186e49773a62815771d127f9d2318a12e8d0b72a5564611164ee53443310610
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Keybind.cs"
    line: 151
    end_line: 239
apis:
  - protocol: rpc
    path: "SettingRow_Keybind.SetListening"
    description:
      zh: >
          切换主槽位的监听态并显示提示文案。
          
      en: >
          Toggles listening state for the primary slot.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetSecondListening"
    description:
      zh: >
          切换第二槽位的监听态。
          
      en: >
          Toggles listening state for the second slot.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetClickable"
    description:
      zh: >
          重写可点击状态处理。
          
      en: >
          Overrides clickable state handling.
          
  - protocol: rpc
    path: "SettingRow_Keybind.SetInteractable"
    description:
      zh: >
          重写可交互状态处理。
          
      en: >
          Overrides interactable state handling.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Keybind.SetListening"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
