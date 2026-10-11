---
uid: 9e0d0205
id: project-chaos.config-pipeline.generated-json.map-loading
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap, loading]
name: {zh: "MapLoading 导出 Json", en: "MapLoading JSON"}
description:
  zh: >
      MapLoading 的运行时导出 Json（Resources 路径 Data/Json/Runtime/MapLoadingConfig，不带扩展名）。驱动读条界面的地图生成阶段文案 —— **禁止手改**。
      
  en: >
      Generated runtime JSON for MapLoading (Resources path Data/Json/Runtime/MapLoadingConfig, no extension). Drives the loading-screen stage captions during runmap generation — never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.693Z"
fingerprint: e40515a887a2e680a1ea1d6cd34423772e18770d114cce6d9d4b306e7a60e859
source:
  - path: "Assets/Resources/Data/Json/Runtime/MapLoadingConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/MapLoadingConfig.json"
    description:
      zh: >
          MapLoading 运行时 Json（Resources 路径 Data/Json/Runtime/MapLoadingConfig）。
          
      en: >
          Runtime JSON of MapLoading (Resources path Data/Json/Runtime/MapLoadingConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-loading
    from_api: "file:Assets/Resources/Data/Json/Runtime/MapLoadingConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/MapLoading_地图生成读条表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
