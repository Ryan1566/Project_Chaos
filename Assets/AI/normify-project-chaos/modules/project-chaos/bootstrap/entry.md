---
uid: 5c1a7e01
id: project-chaos.bootstrap.entry
parent: project-chaos.bootstrap
tags: [bootstrap, startup]
name: {zh: "启动入口", en: "Entry Point"}
description:
  zh: >
      Entry 入口脚本：TestScene 启动时按固定次序初始化 UIManager → InputManager → SettingsManager。顺序不可随意调换——输入动作表必须先于 SettingsManager 加载，否则存档里的按键绑定没有可覆盖的对象。
      
  en: >
      The Entry script: on TestScene start it initialises UIManager → InputManager → SettingsManager in a fixed order. The order is not arbitrary — the input action asset must load before SettingsManager, otherwise stored keybindings have nothing to apply to.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.721Z"
fingerprint: 30885a88f7753cadbd0bd32f8bd335897311348ed2e6c70b3991939de3a50548
source:
  - path: "Assets/Scripts/Runtime/Entry.cs"
    line: 5
    end_line: 18
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/Entry.cs#L5-L18"
    description:
      zh: >
          启动入口脚本全文（MonoBehaviour Entry）。
          
      en: >
          Whole entry script (MonoBehaviour Entry).
          
  - protocol: rpc
    path: "Entry.Start"
    description:
      zh: >
          Unity 启动回调：依次初始化 UI、输入、设置三个管理器。
          
      en: >
          Unity start callback: initialises UI, input and settings managers in order.
          
---
