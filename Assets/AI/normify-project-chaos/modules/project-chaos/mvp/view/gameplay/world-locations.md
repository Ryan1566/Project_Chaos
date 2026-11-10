---
uid: b2c0030a
id: project-chaos.mvp.view.gameplay.world-locations
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, world-map]
name: {zh: "世界地点界面", en: "World Locations UI"}
description:
  zh: >
      世界观地图兴趣点层：地点列表与主城/小城/兴趣点面板、平移缩放导航，以及 6 倍放大后的势力标签。
      
  en: >
      World-map POI layer: the location list/board (capital, town, interest points), pan/zoom navigation and faction labels on the 6x-zoomed map.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.944Z"
fingerprint: 29512a8f8c04d708de5c9ef951e0aa266037b3c7f8e2c8f012f7fbe39981b9a9
source:
  - path: "Assets/Scripts/Runtime/MVP/View/WorldLocationsView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/WorldMapNavigation.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/WorldMapFactionLabelView.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/WorldLocationsView.cs"
    description:
      zh: >
          世界地点视图
          
      en: >
          World locations view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/WorldMapNavigation.cs"
    description:
      zh: >
          世界观地图导航
          
      en: >
          World map navigation
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/WorldMapFactionLabelView.cs"
    description:
      zh: >
          势力标签视图
          
      en: >
          Faction label view
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.world-map
    label: {zh: "渲染兴趣点", en: "Renders POI state"}
  - kind: reference
    to: project-chaos.config-pipeline.generated-models.world-location
    label: {zh: "读地点配置", en: "Reads location sheets"}
---
