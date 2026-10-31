---
uid: 9e0d0105
id: project-chaos.config-pipeline.table-repo.map-loading
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap, loading]
name: {zh: "MapLoading 地图读条表", en: "MapLoading Workbook"}
description:
  zh: >
      MapLoading_地图生成读条表.xlsx → MapLoadingConfig：Id / stage（1 参数装配 2 生成房间图 3 排布网格 4 挑选房间块 5 实例化 6 封堵填充 7 校验 8 完成）/ sort / descCN、descTW、descEN、descJP、descKR（读条文案，禁止英文逗号/双引号/换行）。该表在策划案草案的 6 张表之外新增，用于让读条界面播报地图生成阶段。
      
  en: >
      MapLoading_地图生成读条表.xlsx -> MapLoadingConfig: Id / stage (1 assemble params, 2 build room graph, 3 lay out grid, 4 pick room blocks, 5 instantiate, 6 seal and fill, 7 validate, 8 done) / sort / descCN, descTW, descEN, descJP, descKR (captions; English commas, double quotes and newlines are forbidden). This table was added on top of the six tables drafted in the design doc, so the loading screen can narrate runmap generation.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.608Z"
fingerprint: d6478e92423457b1e3a29cc810877a3f57160cd802988e9075c6c2aa12c7bd6d
source:
  - path: "Assets/Excel/Chaos_excel/MapLoading_地图生成读条表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/MapLoading_地图生成读条表.xlsx"
    description:
      zh: >
          地图生成读条表工作簿：五个语种的读条阶段文案。
          
      en: >
          MapLoading workbook: loading-screen stage captions in five languages.
          
---
