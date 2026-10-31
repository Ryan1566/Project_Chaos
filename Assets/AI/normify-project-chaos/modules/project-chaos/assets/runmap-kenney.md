---
uid: 1d4f77b3
id: project-chaos.assets.runmap-kenney
parent: project-chaos.assets
tags: [assets, runmap, kenney]
name: {zh: "局内地图 Kenney 皮肤", en: "RunMap Kenney Skin"}
description:
  zh: >
      Assets/TileMap/Kenney/** 与 Assets/Resources/Prefabs/RunMap/Kenney/**：局内地图的 Kenney（Platformer Art，CC0）皮肤——每套空间层主题 7 张瓦片 + 1 张 RuleTile（5 条规则）+ 6 类房间块 prefab。房间块命名 Chunk_{层}_{类型}，层 ∈ Surface/Building/Underground，类型 ∈ Entry/Corridor/Combat/DeadEnd/BossGate/LayerLink_Stair，共 3×6=18 个，是 framework.runmap 生成器的取件依据。许可见 Assets/AI/runmap-art/KENNEY_LICENSES.md。
      
  en: >
      Assets/TileMap/Kenney/** and Assets/Resources/Prefabs/RunMap/Kenney/** - the CC0 Kenney (Platformer Art) skin for in-run maps: 7 tiles plus one RuleTile (5 rules) per layer theme and 6 room-chunk kinds with sockets, spawn slots, camera bounds and colliders. Chunks follow Chunk_{layer}_{type} (layer Surface/Building/Underground; type Entry/Corridor/Combat/DeadEnd/BossGate/LayerLink_Stair; 18 total), used by framework.runmap. License: Assets/AI/runmap-art/KENNEY_LICENSES.md.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:40.536Z"
fingerprint: e273747de21e0fbf25cf7de190cc3c66653c86314ef9ab8edfb7db3752f91672
source:
  - path: "Assets/TileMap/Kenney/Surface/Tile_Top.asset"
    line: 1
    end_line: 1
  - path: "Assets/TileMap/Kenney/Surface/RuleTile_Surface.asset"
    line: 1
    end_line: 1
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_Entry.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_Corridor.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_Combat.prefab"
    line: 1
    end_line: 1
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_DeadEnd.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_BossGate.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_LayerLink_Stair.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_Entry.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_Corridor.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_Combat.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_DeadEnd.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_BossGate.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Building/Chunk_Building_LayerLink_Stair.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_Entry.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_Corridor.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_Combat.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_DeadEnd.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_BossGate.prefab"
  - path: "Assets/Resources/Prefabs/RunMap/Kenney/Underground/Chunk_Underground_LayerLink_Stair.prefab"
apis:
  - protocol: file
    path: "Assets/TileMap/Kenney/Surface/Tile_Top.asset"
    description:
      zh: >
          Kenney 皮肤瓦片的代表性 Tile 资产（每主题 7 种，70×70 原图，PPU=70）。
          
      en: >
          Representative Kenney skin tile asset (7 kinds per layer theme, 70x70 source, PPU 70).
          
  - protocol: file
    path: "Assets/TileMap/Kenney/Surface/RuleTile_Surface.asset"
    description:
      zh: >
          地表主题的 RuleTile（5 条规则）—— 供手作房间与不规则地形直接刷。
          
      en: >
          RuleTile for the surface theme (5 rules) — for hand-authored rooms and irregular terrain painting.
          
  - protocol: file
    path: "Assets/Resources/Prefabs/RunMap/Kenney/Surface/Chunk_Surface_Combat.prefab"
    description:
      zh: >
          代表性皮肤房间块 prefab（在 Resources 下，自带 RunMapChunkTiles 瓦片引用）。
          
      en: >
          Representative Kenney-skinned room-chunk prefab (under Resources; carries the RunMapChunkTiles reference).
          
---
