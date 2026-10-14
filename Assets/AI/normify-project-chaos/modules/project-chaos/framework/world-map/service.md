---
uid: aa910010
id: project-chaos.framework.world-map.service
parent: project-chaos.framework.world-map
name: {zh: "地图数据服务", en: "World Map Service"}
description:
  zh: >
      加载并缓存导出的 WorldMap/WorldFaction Json，对外提供 Points(mapId)、Areas(stage)、FactionIds(stage) 与 TryGetFactionAt(stage, normalized, out factionId, out regionId)。飞地=一个势力多个 RegionId：任一区域命中即决定归属；同势力所有区域的颜色归一化为第一块（表里有冲突时记一条 Warn）。
      
  en: >
      Loads and caches the generated WorldMap/WorldFaction JSON, exposing Points(mapId), Areas(stage), FactionIds(stage) and TryGetFactionAt(stage, normalized, out factionId, out regionId). Enclaves are one faction with several RegionIds: any-region containment decides ownership, and all regions of a faction are normalised to the first region's colour (a conflicting table logs one warning).
      
revision: a7c3b8fe6345669cb33b508b5355f6a13e4cf0af
updated_at: "2026-10-14T13:18:15.975Z"
fingerprint: 4f83d72c7e03da5dae78972dd16b4eb8a261006a517914b189c6442a2126805e
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapService.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapService.cs"
    description:
      zh: >
          Points(mapId) / Areas(stage) / Reload()；表缺失时只告警并返回空集。
          
      en: >
          Points(mapId) / Areas(stage) / Reload(); returns empty sets with a warning when tables are missing.
          
  - protocol: rpc
    path: "WorldMapService.FactionIds"
    description:
      zh: >
          某时期内去重后的势力 ID（保持首次出现顺序）。
          
      en: >
          Distinct faction ids of an era, in first-appearance order.
          
  - protocol: rpc
    path: "WorldMapService.TryGetFactionAt"
    description:
      zh: >
          归属判定：点落在某势力任一区域内即属于该势力（含飞地）；两片之间的缝隙不属于任何势力。
          
      en: >
          Ownership test: a point inside ANY region of a faction counts as that faction (enclaves included); the gap between two regions owns nothing.
          
---
