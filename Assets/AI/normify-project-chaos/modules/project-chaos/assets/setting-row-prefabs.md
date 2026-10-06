---
uid: 5b7d20ae
id: project-chaos.assets.setting-row-prefabs
parent: project-chaos.assets
name: {zh: "设置行预制体", en: "Settings Row Prefabs"}
description:
  zh: >
      五个设置行预制体（按钮/按键绑定/选择器/滑条/开关），由设置面板实例化拼出选项列表。
      
  en: >
      Five settings-row prefabs (Button/Keybind/Selector/Slider/Toggle) instanced by the settings panel to build its option list.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.706Z"
fingerprint: 1ff81c78dae96aa2839d812f1c012d1b7f5988b4546beb0e4bbbb671edaf0687
source:
  - path: "Assets/Prefabs/UI/SettingRows/SettingRow_Button.prefab"
  - path: "Assets/Prefabs/UI/SettingRows/SettingRow_Keybind.prefab"
  - path: "Assets/Prefabs/UI/SettingRows/SettingRow_Selector.prefab"
  - path: "Assets/Prefabs/UI/SettingRows/SettingRow_Slider.prefab"
  - path: "Assets/Prefabs/UI/SettingRows/SettingRow_Toggle.prefab"
apis:
  - protocol: file
    path: "Assets/Prefabs/UI/SettingRows/SettingRow_Button.prefab"
    description:
      zh: >
          按钮设置行预制体（18KB）。
          
      en: >
          Button row prefab (18 KB).
          
  - protocol: file
    path: "Assets/Prefabs/UI/SettingRows/SettingRow_Keybind.prefab"
    description:
      zh: >
          按键绑定设置行预制体（39KB）。
          
      en: >
          Key-rebinding row prefab (39 KB).
          
  - protocol: file
    path: "Assets/Prefabs/UI/SettingRows/SettingRow_Selector.prefab"
    description:
      zh: >
          选择器设置行预制体（31KB）。
          
      en: >
          Selector (dropdown/stepper) row prefab (31 KB).
          
  - protocol: file
    path: "Assets/Prefabs/UI/SettingRows/SettingRow_Slider.prefab"
    description:
      zh: >
          滑条设置行预制体（20KB）。
          
      en: >
          Slider row prefab (20 KB).
          
  - protocol: file
    path: "Assets/Prefabs/UI/SettingRows/SettingRow_Toggle.prefab"
    description:
      zh: >
          开关设置行预制体（16KB）。
          
      en: >
          Toggle row prefab (16 KB).
          
deps:
  - kind: reference
    to: project-chaos.assets.input-assets
    from_api: "file:Assets/Prefabs/UI/SettingRows/SettingRow_Keybind.prefab"
    to_api: "file:Assets/Resources/Input/ChaosInputActions.inputactions"
    label: {zh: "按键行绑定输入动作", en: "Keybind row binds input"}
---
