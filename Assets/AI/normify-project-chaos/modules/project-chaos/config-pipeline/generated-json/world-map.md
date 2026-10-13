---
uid: aa910005
id: project-chaos.config-pipeline.generated-json.world-map
parent: project-chaos.config-pipeline.generated-json
name: {zh: "WorldMap 导出 Json", en: "WorldMap JSON"}
description:
  zh: >
      WorldMapConfig.json：导出器把 WorldMap_世界观地图表.xlsx 转成的运行时产物（{"datas":[...]} 结构），由 WorldMapService 通过 ConfigLoader 读取。不可手改。
      
  en: >
      WorldMapConfig.json: the runtime artifact produced by the exporter from the WorldMap workbook ({"datas":[...]}), read by WorldMapService through ConfigLoader. Do not hand-edit.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.568Z"
fingerprint: 722b0c8d34f7f82142f36b559ae189ea6ba589a949ddc14af5c3d387d2389ff9
source:
  - path: "Assets/Resources/Data/Json/Runtime/WorldMapConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/WorldMapConfig.json"
    description:
      zh: >
          地图表运行时 Json（datas 数组）。
          
      en: >
          Runtime JSON of the map workbook (datas array).
          
---
