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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.988Z"
fingerprint: a7bc57c14bc59844b0efbc89476c39564e1b9ce157f82cd6516829de6dc75dca
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
