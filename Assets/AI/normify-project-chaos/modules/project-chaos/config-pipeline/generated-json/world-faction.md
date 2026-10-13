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
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.568Z"
fingerprint: 2ace35fd5ba13cb4b68642ef6204cc642433f60d1d95c28f61701e69c573154f
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
