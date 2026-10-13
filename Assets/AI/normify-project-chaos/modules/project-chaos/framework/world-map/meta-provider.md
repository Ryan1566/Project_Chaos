---
uid: aa910012
id: project-chaos.framework.world-map.meta-provider
parent: project-chaos.framework.world-map
name: {zh: "元数据查询", en: "Meta Provider"}
description:
  zh: >
      为将来的地点表与兴趣点表预留的查询缝：TryGetMeta(kind,type,refId,out name,out icon)。两张表落地前实现恒返回 false，面板改用 Kind/Type/RefId 占位显示。
      
  en: >
      Lookup seam for the future location and POI tables: TryGetMeta(kind,type,refId, out name, out icon). Until those tables exist the implementation returns false and the panel shows a placeholder built from Kind/Type/RefId.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.569Z"
fingerprint: 512f6171c1c192465286e0016412eda9ccf64f7c87836cc7886fcd2e4b0c53b1
source:
  - path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapMetaProvider.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapMetaProvider.cs"
    description:
      zh: >
          TryGetMeta(kind,type,refId,out name,out icon)；MVP 实现恒返回 false。
          
      en: >
          TryGetMeta(kind,type,refId,out name,out icon); the MVP implementation always returns false.
          
---
