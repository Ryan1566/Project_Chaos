---
uid: e0a10102
id: project-chaos.editor-tools.localization.collector-scan
parent: project-chaos.editor-tools.localization
name: {zh: "文字扫描与 Key 生成", en: "Text Scan and Key Generation"}
description:
  zh: >
      扫描管线：按目录/单个 prefab/场景三类输入收集 TMP 文字，重建对象层级路径，按面板名与路径生成唯一 Key（含设置面板的 Setting 前缀特例），并记录首尾空白与 Key 冲突。
      
  en: >
      Scan pipeline: collects TMP text from directories, single prefabs and scenes, rebuilds object hierarchy paths, derives unique keys from prefab name and path (with the setting-panel prefix special case), and flags leading/trailing whitespace and key collisions.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.702Z"
fingerprint: f5eec2e75e4438251b6820a84dcc122eb620e15be08a7dc2759b088c2ce3045e
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs"
    line: 492
    end_line: 929
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs#L492-L929"
    description:
      zh: >
          扫描与 Key 生成实现所在行段。
          
      en: >
          Line range holding the scan and key-generation implementation.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.Scan"
    description:
      zh: >
          按搜索路径批量扫描并分组收集文字。
          
      en: >
          Scans all search paths and groups collected text.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ScanPrefab"
    description:
      zh: >
          扫描单个预制体，提取其 TMP 文字。
          
      en: >
          Scans one prefab and extracts its TMP texts.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ScanScene"
    description:
      zh: >
          扫描场景（Additive 打开后关闭）中的硬编码文字。
          
      en: >
          Scans a scene for hard-coded texts, opening it additively.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.SuggestKey"
    description:
      zh: >
          由面板名与对象路径推导本地化 Key。
          
      en: >
          Derives a localization key from prefab name and object path.
          
---
