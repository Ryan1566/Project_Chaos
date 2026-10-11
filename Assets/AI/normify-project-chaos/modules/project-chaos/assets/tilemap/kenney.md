---
uid: 91be4c07
id: project-chaos.assets.tilemap.kenney
parent: project-chaos.assets.tilemap
name: {zh: "Kenney 瓦片与规则瓦片", en: "Kenney Tiles & RuleTiles"}
description:
  zh: >
      Assets/TileMap/Kenney 下 3 套空间层主题（Surface / Building / Underground），每套 7 张 Tile_*.asset（Fill/Top/TopL/TopR/Rounded/Beam/Deco，各配同名 70×70 PNG，PPU=70）+ 1 张 RuleTile_*.asset，共 45 个文件。RuleTile 是按八邻域规则自动拼接的瓦片（5 条 Tiling Rule，各带输出精灵与碰撞体），Tile_*.asset 则是单张固定精灵。运行期皮肤视角另见 assets.runmap-kenney。
      
  en: >
      Three layer themes under Assets/TileMap/Kenney (Surface/Building/Underground), each with 7 Tile_*.asset (Fill, Top, TopL, TopR, Rounded, Beam, Deco plus matching 70x70 PNGs, PPU 70) and one RuleTile_*.asset - 45 files. A RuleTile auto-tiles by 8-neighbour rules (5 tiling rules with output sprite and collider) while Tile_*.asset is a single fixed sprite. Runtime skin view: assets.runmap-kenney.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.671Z"
fingerprint: d5c191922479582d2fac859c674cf8fcbd4396bde79162a082b5434919c6e58f
source:
  - path: "Assets/TileMap/Kenney/Building/RuleTile_Building.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Beam.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Beam.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_Deco.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Deco.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_Fill.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Fill.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_Rounded.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Rounded.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_Top.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_Top.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_TopL.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_TopL.png"
  - path: "Assets/TileMap/Kenney/Building/Tile_TopR.asset"
  - path: "Assets/TileMap/Kenney/Building/Tile_TopR.png"
  - path: "Assets/TileMap/Kenney/Surface/RuleTile_Surface.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Beam.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Beam.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Deco.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Deco.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Fill.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Fill.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Rounded.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Rounded.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Top.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_Top.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_TopL.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_TopL.png"
  - path: "Assets/TileMap/Kenney/Surface/Tile_TopR.asset"
  - path: "Assets/TileMap/Kenney/Surface/Tile_TopR.png"
  - path: "Assets/TileMap/Kenney/Underground/RuleTile_Underground.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Beam.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Beam.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Deco.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Deco.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Fill.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Fill.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Rounded.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Rounded.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Top.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_Top.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_TopL.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_TopL.png"
  - path: "Assets/TileMap/Kenney/Underground/Tile_TopR.asset"
  - path: "Assets/TileMap/Kenney/Underground/Tile_TopR.png"
apis:
  - protocol: file
    path: "Assets/TileMap/Kenney/Building/RuleTile_Building.asset"
    description:
      zh: >
          建筑主题规则瓦片（5 条 Tiling Rule），按八邻域自动选精灵与碰撞体。
          
      en: >
          Building-theme RuleTile (5 tiling rules) picking sprites and colliders from 8-neighbour context.
          
  - protocol: file
    path: "Assets/TileMap/Kenney/Underground/Tile_TopL.asset"
    description:
      zh: >
          地下主题左上角普通 Tile（固定精灵），同套还有 Top/TopR/Rounded 等。
          
      en: >
          Underground top-left plain Tile (fixed sprite); Top/TopR/Rounded siblings exist.
          
  - protocol: file
    path: "Assets/TileMap/Kenney/Surface/Tile_Rounded.asset"
    description:
      zh: >
          地表主题圆底普通 Tile（固定精灵）。
          
      en: >
          Surface rounded plain Tile (fixed sprite).
          
---
