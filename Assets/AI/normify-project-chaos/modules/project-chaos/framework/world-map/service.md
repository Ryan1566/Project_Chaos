---
uid: aa910010
id: project-chaos.framework.world-map.service
parent: project-chaos.framework.world-map
name: {zh: "地图数据服务", en: "World Map Service"}
description:
  zh: >
      单例服务：通过 ConfigLoader 懒加载 WorldMapConfig/WorldFactionConfig Json，对外提供 Points(mapId) 与 Areas(stage)（按 Stage/FactionId/RegionId 分组、按 PointOrder 排序成多边形）以及换档用的 Reload()；表缺失或为空只记警告不抛异常。
      
  en: >
      Singleton service that lazily loads WorldMapConfig/WorldFactionConfig JSON through ConfigLoader, exposes Points(mapId) and Areas(stage) (grouped by Stage/FactionId/RegionId and sorted by PointOrder into polygons) and Reload() for save switches; missing or empty tables only log a warning.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.569Z"
fingerprint: 11abc1801ec77485756761e011e0a30e63a93fb45b2bb864480b98f33984cde4
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapService.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapService.cs"
    description:
      zh: >
          Points(mapId)、Areas(stage)、Reload()；表缺失时只告警并返回空集。
          
      en: >
          Points(mapId), Areas(stage), Reload(); returns empty sets with a warning when tables are missing.
          
---
