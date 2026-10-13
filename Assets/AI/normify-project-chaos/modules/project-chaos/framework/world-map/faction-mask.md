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
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.569Z"
fingerprint: 5a53a1c03de37e2775fbd6ac742270bab49879db66c19ce57411bf12fd1daf22
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
