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
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.407Z"
fingerprint: 5c3fa56c8f4a920778a4ecb8584c7aaff212693f546ef037ce13f1bbd2037c82
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
