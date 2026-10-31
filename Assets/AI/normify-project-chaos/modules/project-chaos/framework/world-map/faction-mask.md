---
uid: aa910011
id: project-chaos.framework.world-map.faction-mask
parent: project-chaos.framework.world-map
name: {zh: "势力蒙版", en: "Faction Mask"}
description:
  zh: >
      MaskableGraphic：把每个势力多边形光栅化成一张半透明纹理（宽约 512、按底图宽高比、2×2 超采样、source-over 混合）并以单个 quad 渲染；因用光栅化而非三角化，天然支持凹多边形。
      
  en: >
      MaskableGraphic that rasterises every faction polygon into a single translucent texture (about 512 px wide, image aspect, 2x2 supersampling, source-over blending) and renders it as one quad; concave polygons are supported because rasterisation replaces triangulation.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:20:37.069Z"
fingerprint: 884ccc5089191643045fe464d4d111e47df6def3d8324a3c880f9ae843706df6
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionMask.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionMask.cs"
    description:
      zh: >
          把势力多边形光栅化成一张半透明覆盖纹理的 MaskableGraphic。
          
      en: >
          MaskableGraphic that rasterises faction polygons into one translucent overlay texture.
          
---
