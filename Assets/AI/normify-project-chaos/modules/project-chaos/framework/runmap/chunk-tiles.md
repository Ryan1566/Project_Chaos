---
uid: 2aa44b5a
id: project-chaos.framework.runmap.chunk-tiles
parent: project-chaos.framework.runmap
tags: [runmap, chunk]
name: {zh: "地图生成：块瓦片引用", en: "RunMap: Chunk Tile Refs"}
description:
  zh: >
      每个房间块 prefab 自带的小组件：实心瓦片与顶面瓦片的引用。让运行时装配能在不依赖 AssetDatabase、也不把瓦片资产拷进 Resources 的前提下补瓦片。
      
  en: >
      Tiny component carried by every room-chunk prefab: the fill and top Tile references. Lets the runtime assembler paint tiles without AssetDatabase and without duplicating tile assets into Resources.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.717Z"
fingerprint: 440340506a8982977ee0a97a82d860b3c6f14ca566d657376612a0bea476a436
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapChunkTiles.cs"
    line: 1
    end_line: 23
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapChunkTiles.cs#L1-L23"
    description:
      zh: >
          挂在每个房间块 prefab 根上的组件，暴露运行时装配封堆/加地形所需的实心瓦片与顶面瓦片。
          
      en: >
          Component on each room-chunk prefab root exposing the fill/top Tile assets the runtime assembler needs for sealing and decoration.
          
---
