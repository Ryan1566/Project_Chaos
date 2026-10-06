---
uid: f1c0003e
id: project-chaos.framework.setting-rows.icon-text
parent: project-chaos.framework.setting-rows
name: {zh: "按键图标文本", en: "Key Icon Text"}
description:
  zh: >
      KeyIconText 按键图标文本：同一槽位可显示文本、图标或两者，支持仅图标与变暗态，供改键行使用。
      
  en: >
      KeyIconText: shows either text, an icon, or both for one keybinding slot, with dimmed/icon-only modes used by setting rows.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.753Z"
fingerprint: 18f4ea2cf5a0bd03dfb7514dd9cd8536889a361a9030013a8e06b9cba041e794
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/KeyIconText.cs"
    line: 1
    end_line: 157
apis:
  - protocol: rpc
    path: "KeyIconText.SetDisplay"
    description:
      zh: >
          设置显示文本、图标与颜色。
          
      en: >
          Sets display text plus icon and tint.
          
  - protocol: rpc
    path: "KeyIconText.SetIconOnly"
    description:
      zh: >
          仅显示图标。
          
      en: >
          Shows only the icon.
          
  - protocol: rpc
    path: "KeyIconText.SetDimmed"
    description:
      zh: >
          变暗显示。
          
      en: >
          Dims the icon/text.
          
  - protocol: rpc
    path: "KeyIconText.IsIconShown"
    description:
      zh: >
          当前是否显示图标。
          
      en: >
          Whether the icon is currently shown.
          
  - protocol: rpc
    path: "KeyIconText.CurrentText"
    description:
      zh: >
          当前文本内容。
          
      en: >
          Current text of the TMP label.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:KeyIconText.SetDisplay"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
---
