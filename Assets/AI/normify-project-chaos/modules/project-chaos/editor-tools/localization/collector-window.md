---
uid: e0a10101
id: project-chaos.editor-tools.localization.collector-window
parent: project-chaos.editor-tools.localization
name: {zh: "面板文字收集器窗口", en: "Prefab Text Collector Window"}
description:
  zh: >
      收集器窗口外壳与数据模型：搜索路径区、目标配置区、结果列表与条目行、勾选/清除动作栏，TextItem 记录文字来源（预制体/场景）、层级路径、原文本与已存在标记。静默模式下只写日志不弹模态框。
      
  en: >
      Collector window shell and data model: search-path section, target-config section, result list and item rows, selection/clear action bar; TextItem records the text source (prefab/scene), hierarchy path, raw text and already-exists flags. Silent mode logs instead of showing modal dialogs.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.736Z"
fingerprint: 2ec00da630a7367162e0191a090516c894c4f0b514fd97c19677514061967710
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs"
    line: 32
    end_line: 491
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs#L32-L491"
    description:
      zh: >
          收集器窗口 UI 与 TextItem 数据模型所在行段。
          
      en: >
          Line range holding the collector window UI and TextItem data model.
          
  - protocol: rpc
    path: "Menu/Tools/本地化/面板文字收集器"
    description:
      zh: >
          打开面板文字收集器窗口的编辑器菜单项。
          
      en: >
          Editor menu item that opens the prefab text collector window.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ShowWindow"
    description:
      zh: >
          打开收集器窗口并设定最小尺寸。
          
      en: >
          Opens the collector window with its minimum size.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.SilentMode"
    description:
      zh: >
          静默开关：为 true 时跳过模态确认框。
          
      en: >
          Silent switch: skips modal confirmations when true.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.TextItem"
    description:
      zh: >
          一条「预制体/场景里的文字」记录。
          
      en: >
          One collected text record from a prefab or scene.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.localization.collector-scan
    from_api: "rpc:PrefabTextCollectorWindow.ShowWindow"
    to_api: "rpc:PrefabTextCollectorWindow.Scan"
    label: {zh: "点击扫描触发收集", en: "Scan button triggers scan"}
---
