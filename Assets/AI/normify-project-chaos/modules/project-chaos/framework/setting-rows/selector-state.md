---
uid: f1c00043
id: project-chaos.framework.setting-rows.selector-state
parent: project-chaos.framework.setting-rows
name: {zh: "选择器状态维护", en: "Selector State"}
description:
  zh: >
      选择器状态段：静默设置下标/文本、带夹取的步进、选项计数与显示刷新。
      
  en: >
      Selector state block: silent index/text setters, stepping with clamping, option counting and display refresh.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.754Z"
fingerprint: b8e91d28c0d242c15945e58c8198e62e34aba89f4a1fcd321a04300a52c4c5b6
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Selector.cs"
    line: 136
    end_line: 221
apis:
  - protocol: rpc
    path: "SettingRow_Selector.SetIndexWithoutNotify"
    description:
      zh: >
          静默设置选中下标。
          
      en: >
          Sets the selected index without raising the callback.
          
  - protocol: rpc
    path: "SettingRow_Selector.SetTextWithoutNotify"
    description:
      zh: >
          静默设置显示文本。
          
      en: >
          Sets the value text without raising the callback.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Selector.cs#L136-L221"
    description:
      zh: >
          选择器状态维护与刷新显示实现段。
          
      en: >
          Selector state/refresh implementation block.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Selector.SetIndexWithoutNotify"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
