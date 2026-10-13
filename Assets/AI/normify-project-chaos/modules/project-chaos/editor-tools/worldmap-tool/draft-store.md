---
uid: aa91000c
id: project-chaos.editor-tools.worldmap-tool.draft-store
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "草稿存取", en: "Draft Store"}
description:
  zh: >
      可序列化草稿模型（源图路径、真实像素尺寸、MapId、点位、势力区域）与 _worldmap_tool/WorldMapDraft.json 的读写，保证重新打开窗口能继续上次的工作。
      
  en: >
      Serializable draft model (source image path, real pixel size, map id, points, faction areas) plus load/save of _worldmap_tool/WorldMapDraft.json so a session can be resumed.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.565Z"
fingerprint: c4dd9439955fdae3199a8115eca3432cf0646a2d92545d63820f35753f85ae73
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolData.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolData.cs"
    description:
      zh: >
          草稿模型与工作区 JSON 持久化。
          
      en: >
          Draft model and JSON persistence of the tool workspace.
          
---
