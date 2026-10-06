---
uid: f1c00036
id: project-chaos.framework.settings.enums
parent: project-chaos.framework.settings
name: {zh: "设置选项枚举", en: "Settings Option Enums"}
description:
  zh: >
      设置选项枚举：窗口模式/帧率/输入设备/手柄型号/画质/设置分类，配 InputScheme 映射与 SettingsLabels 中文标签表。
      
  en: >
      Settings option enums (window mode, frame rate, input device, gamepad model, quality, category) with InputScheme mapping helpers and SettingsLabels tables.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.756Z"
fingerprint: d8afff3ee09f2f9b674ec3efc181e6fa496c3dfea536fa5e7612876ea2361476
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsEnums.cs"
    line: 1
    end_line: 183
apis:
  - protocol: rpc
    path: "InputScheme.IsGamepad"
    description:
      zh: >
          判断某设备取值是否属于手柄。
          
      en: >
          Whether a device value means gamepad input.
          
  - protocol: rpc
    path: "InputScheme.ModelToDevice"
    description:
      zh: >
          手柄型号→设备类型。
          
      en: >
          Maps a gamepad model to a device type.
          
  - protocol: rpc
    path: "InputScheme.DeviceToModel"
    description:
      zh: >
          设备类型→手柄型号。
          
      en: >
          Maps a device type to a gamepad model.
          
  - protocol: rpc
    path: "SettingsLabels.Get"
    description:
      zh: >
          按下标安全取标签表项。
          
      en: >
          Safely reads a label table by index.
          
  - protocol: rpc
    path: "SettingsLabels.GamepadButtonName"
    description:
      zh: >
          取手柄按键的中文名。
          
      en: >
          Localized gamepad button name.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsEnums.cs"
    description:
      zh: >
          设置选项枚举与中文标签表。
          
      en: >
          Option enums and label tables for settings rows.
          
---
