---
uid: f1c00041
id: project-chaos.framework.setting-rows.slider
parent: project-chaos.framework.setting-rows
name: {zh: "设置行·滑条", en: "Setting Row Slider"}
description:
  zh: >
      SettingRow_Slider 整数滑条行（如鼠标灵敏度）：范围配置、静默赋值与值变化回调。
      
  en: >
      SettingRow_Slider: integer slider row (e.g. mouse sensitivity) with range configuration and silent set.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.755Z"
fingerprint: 97b5468f9564d9e4abba7f47bebcb6d856109282ed6dee6368e686c65a124dec
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Slider.cs"
    line: 1
    end_line: 68
apis:
  - protocol: rpc
    path: "SettingRow_Slider.OnValueChanged"
    description:
      zh: >
          滑条值变化回调。
          
      en: >
          Integer value-changed callback.
          
  - protocol: rpc
    path: "SettingRow_Slider.Configure"
    description:
      zh: >
          配置滑条范围。
          
      en: >
          Configures the slider range.
          
  - protocol: rpc
    path: "SettingRow_Slider.SetValueWithoutNotify"
    description:
      zh: >
          静默设置滑条值。
          
      en: >
          Sets the value without raising the callback.
          
  - protocol: rpc
    path: "SettingRow_Slider.SetClickable"
    description:
      zh: >
          重写可点击状态处理。
          
      en: >
          Overrides clickable state handling.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Slider.Configure"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
