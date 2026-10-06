---
uid: f1c0003d
id: project-chaos.framework.setting-rows.base
parent: project-chaos.framework.setting-rows
name: {zh: "设置行基类", en: "Setting Row Base"}
description:
  zh: >
      SettingRowBase 抽象基类：按名字找子节点、可点击/可交互状态与禁用配色、settingId 字段与 Inspector 自检。
      
  en: >
      SettingRowBase abstract class: child lookup helpers, clickable/interactable state with disabled tinting, and the settingId field.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.753Z"
fingerprint: 5d66eb0bff3b0706c3d9c260008a56f70c43e4688c611d5d2e8bb1372e7fc163
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRowBase.cs"
    line: 1
    end_line: 141
apis:
  - protocol: rpc
    path: "SettingRowBase.SetClickable"
    description:
      zh: >
          设置行是否可点击。
          
      en: >
          Makes the row clickable or not.
          
  - protocol: rpc
    path: "SettingRowBase.SetInteractable"
    description:
      zh: >
          设置行是否可交互。
          
      en: >
          Makes the row interactable or not.
          
  - protocol: rpc
    path: "SettingRowBase.DisabledColor"
    description:
      zh: >
          由基础色计算禁用色。
          
      en: >
          Disabled tint derived from a base color.
          
  - protocol: rpc
    path: "SettingRowBase.Clickable"
    description:
      zh: >
          行当前是否可点击。
          
      en: >
          Whether the row is clickable.
          
  - protocol: rpc
    path: "SettingRowBase.settingId"
    description:
      zh: >
          本行绑定的设置项 id。
          
      en: >
          Setting id bound to this row.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.ids
    from_api: "rpc:SettingRowBase.settingId"
    to_api: "rpc:SettingIds.Language"
    label: {zh: "设置项 id", en: "Setting ids"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SettingRowBase.SetClickable"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "设置行日志", en: "Row logging"}
---
