---
uid: aa910006
id: project-chaos.config-pipeline.generated-json.world-faction
parent: project-chaos.config-pipeline.generated-json
name: {zh: "WorldFaction 导出 Json", en: "WorldFaction JSON"}
description:
  zh: >
      WorldFactionConfig.json：导出器把势力范围表转成的运行时产物，由 WorldMapService 读取并按当前时期筛出多边形顶点。不可手改。
      
  en: >
      WorldFactionConfig.json: the runtime artifact produced from the faction-area workbook, read by WorldMapService which filters polygon vertices by the current era. Do not hand-edit.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.606Z"
fingerprint: c9f988a7e7059f17aa1d21f2234e508c9e5fed8f56c19e7fc6065a862b3744b0
source:
  - path: "Assets/Resources/Data/Json/Runtime/WorldFactionConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/WorldFactionConfig.json"
    description:
      zh: >
          势力范围表运行时 Json（datas 数组）。
          
      en: >
          Runtime JSON of the faction-area workbook (datas array).
          
---
