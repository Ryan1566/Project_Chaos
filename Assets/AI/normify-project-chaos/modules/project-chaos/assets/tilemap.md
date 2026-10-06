---
uid: 2c90f47b
id: project-chaos.assets.tilemap
parent: project-chaos.assets
name: {zh: "Tilemap 资源", en: "Tilemap Assets"}
description:
  zh: >
      2D 瓦片地图资产：7 张 Grass_*map001 瓦片资产 + 矩形调色板预制体。TileMap/RuleTile 目前为空。
      
  en: >
      2D tilemap assets: 7 Grass_*map001 Tile assets plus the rectangular palette prefab. TileMap/RuleTile is currently empty.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.707Z"
fingerprint: bb1189cded2c65d7bb64cf895cd161c822ebe9ad0d790ecc27749dc3ad4c5c79
source:
  - path: "Assets/TileMap/Rectangular Palette.prefab"
  - path: "Assets/TileMap/Tile/Grass_001map001.asset"
  - path: "Assets/TileMap/Tile/Grass_007map001.asset"
apis:
  - protocol: file
    path: "Assets/TileMap/Rectangular Palette.prefab"
    description:
      zh: >
          矩形瓦片调色板预制体，用于绘制 Tilemap。
          
      en: >
          Rectangular tile palette prefab used to paint tilemaps.
          
  - protocol: file
    path: "Assets/TileMap/Tile/Grass_001map001.asset"
    description:
      zh: >
          草地瓦片资产 001，7 张同族瓦片的代表。
          
      en: >
          Grass tile asset 001, representative of the 7-tile set.
          
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
