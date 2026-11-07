---
uid: aa910013
id: project-chaos.framework.world-map.runtime-model
parent: project-chaos.framework.world-map
name: {zh: "运行时视图模型", en: "Runtime View Model"}
description:
  zh: >
      WorldMapPoint 与 WorldFactionArea 是服务返回的运行时视图。一个势力可拥有多块区域（同 factionId、不同 regionId），这就是飞地的表示。Contains(normalized) 用射线法判定且把边算在内；编辑器具与运行时各有一份实现（运行时程序集引用不到编辑器程序集），两边都注明改语义要一起改。
      
  en: >
      WorldMapPoint and WorldFactionArea are the runtime views returned by the service. A faction may own several areas (same factionId, different regionId) — that is how enclaves are represented. Contains(normalized) does the ray-casting test with on-edge counting as inside; it is deliberately implemented here as well as in the editor tool because the runtime assembly cannot reference the editor one (both sides say to change the semantics together).
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.989Z"
fingerprint: abb615531b78c66886521a02ab5829f7ade96e9af906fc314d351415e25d310a
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapPoint.cs"
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionArea.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapPoint.cs"
    description:
      zh: >
          点位的运行时视图类型（归一化位置、Kind/Type/RefId、Scale、Layer）。
          
      en: >
          Runtime view type for a map point (normalised position, Kind/Type/RefId, Scale, Layer).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionArea.cs"
    description:
      zh: >
          一块势力区域的运行时视图（一个多边形）；同一 factionId 可有多个（飞地）。
          
      en: >
          Runtime view type for one faction region (one polygon); several of them may share a factionId (enclaves).
          
  - protocol: rpc
    path: "WorldFactionArea.Contains"
    description:
      zh: >
          点在多边形内判定（边算在内）；供势力归属判定使用。
          
      en: >
          Point-in-polygon containment with edges counting as inside; used by the faction ownership test.
          
---
