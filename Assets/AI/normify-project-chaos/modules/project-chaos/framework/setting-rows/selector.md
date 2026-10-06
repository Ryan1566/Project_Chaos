---
uid: f1c00042
id: project-chaos.framework.setting-rows.selector
parent: project-chaos.framework.setting-rows
name: {zh: "设置行·选择器", en: "Setting Row Selector"}
description:
  zh: >
      SettingRow_Selector 左右选择行（如窗口模式）：可用显示文本或本地化 Key 配置选项，暴露选项变化回调。
      
  en: >
      SettingRow_Selector: left/right selector row (e.g. window mode) configured from display texts or localization keys.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.754Z"
fingerprint: b8e91d28c0d242c15945e58c8198e62e34aba89f4a1fcd321a04300a52c4c5b6
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingRows/SettingRow_Selector.cs"
    line: 1
    end_line: 135
apis:
  - protocol: rpc
    path: "SettingRow_Selector.OnValueChanged"
    description:
      zh: >
          选项变化回调。
          
      en: >
          Index-changed callback.
          
  - protocol: rpc
    path: "SettingRow_Selector.Configure"
    description:
      zh: >
          用显示文本配置选项。
          
      en: >
          Configures options from display texts.
          
  - protocol: rpc
    path: "SettingRow_Selector.ConfigureOptions"
    description:
      zh: >
          用本地化 Key 配置选项。
          
      en: >
          Configures options from localization keys.
          
  - protocol: rpc
    path: "SettingRow_Selector.SetClickable"
    description:
      zh: >
          重写可点击状态处理。
          
      en: >
          Overrides clickable state handling.
          
deps:
  - kind: reference
    to: project-chaos.framework.setting-rows.base
    from_api: "rpc:SettingRow_Selector.Configure"
    to_api: "rpc:SettingRowBase.SetClickable"
    label: {zh: "行基类", en: "Row base class"}
  - kind: reference
    to: project-chaos.framework.localization.localized-text
    from_api: "rpc:SettingRow_Selector.ConfigureOptions"
    to_api: "rpc:LocalizedText.SetKey"
    label: {zh: "选项文本", en: "Option text"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SettingRow_Selector.Configure"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "设置行日志", en: "Row logging"}
---
