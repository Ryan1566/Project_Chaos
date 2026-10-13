---
uid: d350af62
id: project-chaos.assets.tilemap.grass-tiles
parent: project-chaos.assets.tilemap
name: {zh: "草地瓦片与调色板", en: "Grass Tiles & Palette"}
description:
  zh: >
      Assets/TileMap 根下的旧资源：Tile/ 下 7 张 Grass_*map001.asset 普通 Tile 资产（各自引用 Assets/Art/PNG 下同名草地贴图），以及 Rectangular Palette.prefab 矩形瓦片调色板。TileMap/RuleTile 目录本身为空，规则瓦片都在 Kenney 套下。
      
  en: >
      Legacy assets directly under Assets/TileMap: seven Grass_*map001.asset plain Tile assets in Tile/ (each referencing the same-named grass texture in Assets/Art/PNG) plus the Rectangular Palette.prefab tile palette. The TileMap/RuleTile folder itself is empty; rule tiles live under the Kenney set.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.374Z"
fingerprint: 450400e624c8056438be16c247c8e9f274489c38e0f9c750feae99d0dba12cbc
source:
  - path: "Assets/TileMap/Rectangular Palette.prefab"
  - path: "Assets/TileMap/Tile/Grass_001map001.asset"
  - path: "Assets/TileMap/Tile/Grass_003map001.asset"
  - path: "Assets/TileMap/Tile/Grass_004map001.asset"
  - path: "Assets/TileMap/Tile/Grass_005map001.asset"
  - path: "Assets/TileMap/Tile/Grass_006map001.asset"
  - path: "Assets/TileMap/Tile/Grass_007map001.asset"
  - path: "Assets/TileMap/Tile/Grass_02map001.asset"
apis:
  - protocol: file
    path: "Assets/TileMap/Rectangular Palette.prefab"
    description:
      zh: >
          矩形瓦片调色板预制体，用于在 Tilemap 上刷瓦片。
          
      en: >
          Rectangular tile palette prefab used to paint tilemaps.
          
  - protocol: file
    path: "Assets/TileMap/Tile/Grass_001map001.asset"
    description:
      zh: >
          草地瓦片资产 001，7 张同族瓦片的代表。
          
      en: >
          Grass tile asset 001, representative of the seven-tile set.
          
  - protocol: file
    path: "Assets/TileMap/Tile/Grass_007map001.asset"
    description:
      zh: >
          草地瓦片资产 007。
          
      en: >
          Grass tile asset 007.
          
deps:
  - kind: reference
    to: project-chaos.assets.art.tile-textures
    from_api: "file:Assets/TileMap/Tile/Grass_001map001.asset"
    to_api: "file:Assets/Art/PNG/Grass_001map001.png"
    label: {zh: "瓦片引用草地贴图", en: "Tile references grass texture"}
---
