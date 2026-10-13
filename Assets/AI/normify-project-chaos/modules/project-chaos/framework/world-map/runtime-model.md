---
uid: aa910013
id: project-chaos.framework.world-map.runtime-model
parent: project-chaos.framework.world-map
name: {zh: "运行时视图模型", en: "Runtime View Model"}
description:
  zh: >
      WorldMapService 产出的运行时视图类型：WorldMapPoint（归一化位置、Kind/Type/RefId、Scale、Layer）与 WorldFactionArea（FactionId、RegionId、颜色、顶点数组），均已按 MapId/时期与 Enable 过滤。
      
  en: >
      Runtime view types produced by WorldMapService: WorldMapPoint (normalised position, Kind/Type/RefId, Scale, Layer) and WorldFactionArea (FactionId, RegionId, colour, vertex array), both already filtered by MapId/era and Enable.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.569Z"
fingerprint: c51436e81a83e921702bb94672a48d6f54bf5d0be25b0c1ad09d731fee95a949
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapPoint.cs"
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionArea.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapPoint.cs"
    description:
      zh: >
          地图点位与势力区域的运行时视图类型（归一化位置、颜色、顶点数组）。
          
      en: >
          Runtime view types for a map point and a faction area (normalised position, colour, vertex array).
          
---
