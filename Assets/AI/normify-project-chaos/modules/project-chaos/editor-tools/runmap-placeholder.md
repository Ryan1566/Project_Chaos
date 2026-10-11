---
uid: e0a10009
id: project-chaos.editor-tools.runmap-placeholder
parent: project-chaos.editor-tools
tags: [editor, art, runmap]
name: {zh: "局内地图灰盒资产管线", en: "RunMap Greybox Pipeline"}
description:
  zh: >
      Assets/Scripts/Editor/RunMapTool/RunMapPlaceholderPipeline.cs：局内地图的占位（灰盒）资产管线——程序化生成三套空间层主题的 64×64 瓦片与 Tile 资产，并按《局内地图生成系统》§4.3 规格批量拼装 40×24 房间块 prefab（Socket / SpawnSlot / CameraBounds / Composite 碰撞）；菜单 Tools/局内地图/*。
      
  en: >
      Assets/Scripts/Editor/RunMapTool/RunMapPlaceholderPipeline.cs — greybox asset pipeline for in-run maps: procedurally draws 64x64 tiles for three layer themes and assembles 40x24 room-chunk prefabs (Socket_*, SpawnSlot_*, CameraBounds, composite colliders) per the map-generation spec; menus under Tools/局内地图/*.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.704Z"
fingerprint: 8e34e13f6db546a774bf2d1f8f7d1f6ae1dd8a15cefc7c4166ac5ad4b569602b
source:
  - path: "Assets/Scripts/Editor/RunMapTool/RunMapPlaceholderPipeline.cs"
    line: 1
    end_line: 733
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/RunMapTool/RunMapPlaceholderPipeline.cs#L1-L733"
    description:
      zh: >
          局内地图灰盒资产管线源码：程序化出瓦片与房间块 prefab。
          
      en: >
          The greybox asset pipeline source (tiles + room-chunk prefabs) for in-run maps.
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/0. 一键生成（瓦片 + 房间块 + 预览图）"
    description:
      zh: >
          一键生成：瓦片 + 房间块 prefab + 预览图。
          
      en: >
          One-click: tiles + room-chunk prefabs + preview images.
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/1. 只生成占位瓦片"
    description:
      zh: >
          只生成三套主题的占位瓦片（PNG + Tile 资产）。
          
      en: >
          Generate only the placeholder tiles (PNG + Tile assets) for the three layer themes.
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/2. 只生成占位房间块 Prefab"
    description:
      zh: >
          只生成占位房间块 prefab（含 Socket / SpawnSlot / CameraBounds）。
          
      en: >
          Generate only the placeholder room-chunk prefabs (sockets, spawn slots, camera bounds).
          
---
