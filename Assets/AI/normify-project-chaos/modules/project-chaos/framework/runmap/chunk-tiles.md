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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:57:20.393Z"
fingerprint: 25d1f9572bba7e8279f02235a35535c2dbab358a1d0cdab130aac4b3251c536c
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
