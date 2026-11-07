---
uid: b2c00302
id: project-chaos.mvp.view.gameplay.run-map
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, map]
name: {zh: "局内地图界面", en: "Run Map UI"}
description:
  zh: >
      局内地图表现：全屏地图视图、兴趣点与队伍标记，以及进入新地形时弹出的地形名提示。
      
  en: >
      In-run map presentation: the full-screen map view, POI/team markers and the terrain name toast shown when entering a new biome.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.359Z"
fingerprint: a4001235af04a35f979e4d3a793b9a8dd832bbb2594f3fcacb5c7800ecbf57f4
source:
  - path: "Assets/Scripts/Runtime/MVP/View/RunMapView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunMapMarkerView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunTerrainToast.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunMapView.cs"
    description:
      zh: >
          局内地图视图
          
      en: >
          Run map view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunMapMarkerView.cs"
    description:
      zh: >
          地图标记视图
          
      en: >
          Map marker view
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunTerrainToast.cs"
    description:
      zh: >
          地形提示
          
      en: >
          Terrain toast
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染地图状态", en: "Renders map state"}
  - kind: reference
    to: project-chaos.framework.runmap.exploration
    label: {zh: "对应探索地图", en: "Mirrors exploration map"}
---
