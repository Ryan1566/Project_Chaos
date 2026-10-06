---
uid: f1c0003f
id: project-chaos.framework.setting-rows.button
parent: project-chaos.framework.setting-rows
name: {zh: "设置行·按钮", en: "Setting Row Button"}
description:
  zh: >
      SettingRow_Button 动作行：如重置按键方案，暴露 OnClick 回调并重写可点击状态。
      
  en: >
      SettingRow_Button: clickable action row (e.g. reset bindings) exposing an OnClick callback and clickable override.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.753Z"
fingerprint: 2fea257bd73ae0a3975880a8925fb3520b6e2a38e4b525122e20175149e7a695
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Button.cs"
    line: 1
    end_line: 34
apis:
  - protocol: rpc
    path: "SettingRow_Button.OnClick"
    description:
      zh: >
          点击回调。
          
      en: >
          Click callback invoked by the row button.
          
  - protocol: rpc
    path: "SettingRow_Button.SetClickable"
    description:
      zh: >
          重写可点击状态处理。
          
      en: >
          Overrides clickable state handling.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Button.OnClick"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
