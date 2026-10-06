---
uid: e0a10104
id: project-chaos.editor-tools.localization.collector-writer
parent: project-chaos.editor-tools.localization
name: {zh: "LocalizedText 回写", en: "LocalizedText Write-back"}
description:
  zh: >
      把 Key 回写到预制体/场景对象：给目标补 LocalizedText 组件并设 Key，清理选中条目的本地化组件；预制体走 LoadPrefabContents 隔离编辑，场景走已加载场景并按 dirty 收集保存。
      
  en: >
      Writes keys back to prefab/scene objects: adds LocalizedText components and assigns keys, or clears localization on selected items; prefabs are edited through LoadPrefabContents and scenes through loaded-scene lookup with dirty tracking.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.736Z"
fingerprint: 2ec00da630a7367162e0191a090516c894c4f0b514fd97c19677514061967710
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs"
    line: 1313
    end_line: 1791
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs#L1313-L1791"
    description:
      zh: >
          预制体/场景组件回写与清理所在行段。
          
      en: >
          Line range holding prefab/scene component write-back and cleanup.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.WriteComponents"
    description:
      zh: >
          按来源分派预制体与场景的组件回写。
          
      en: >
          Dispatches component write-back per source kind.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.WritePrefabItems"
    description:
      zh: >
          在隔离场景中给预制体对象写入 Key。
          
      en: >
          Writes keys into prefab objects inside an isolated scene.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.WriteSceneItems"
    description:
      zh: >
          在当前已加载场景中给对象写入 Key 并记脏。
          
      en: >
          Writes keys into objects of loaded scenes and marks them dirty.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ApplyLocalizedText"
    description:
      zh: >
          给单个对象补 LocalizedText 并赋 Key。
          
      en: >
          Adds LocalizedText to one object and assigns its key.
          
deps:
  - kind: reference
    to: project-chaos.editor-tools.localization.collector-import
    from_api: "rpc:PrefabTextCollectorWindow.WriteComponents"
    to_api: "rpc:PrefabTextCollectorWindow.ImportToConfig"
    label: {zh: "只回写已导入的条目", en: "Writes only imported entries"}
---
