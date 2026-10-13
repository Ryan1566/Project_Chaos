---
uid: 9e0d0107
id: project-chaos.config-pipeline.table-repo.preset-layer
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "PresetLayer 原型图层序表", en: "PresetLayer Workbook"}
description:
  zh: >
      PresetLayer_原型图层序表.xlsx → PresetLayerConfig：Id / presetId（外键 → MapPreset.Id）/ layerOrder（从 1 连续递增）/ layer（1 地下 2 地表 3 地表建筑）/ isMainPath（主路是否必须经过本层）/ linkFromLayerOrder（与第几层相连，0=首层）/ allowDirect / linkMin、linkMax（跨层连接件数量）。
      
  en: >
      PresetLayer_原型图层序表.xlsx -> PresetLayerConfig: Id / presetId (FK to MapPreset.Id) / layerOrder (1-based, contiguous) / layer (1 underground, 2 surface, 3 surface building) / isMainPath (must the main path cross this layer) / linkFromLayerOrder (0 = first layer) / allowDirect / linkMin, linkMax (cross-layer connector count).
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.408Z"
fingerprint: 1c9e8c9ad63fd1c7c1879216ec5fc68863cf12f9c3c254f009473def97344840
source:
  - path: "Assets/Excel/Chaos_excel/PresetLayer_原型图层序表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/PresetLayer_原型图层序表.xlsx"
    description:
      zh: >
          原型图层序表工作簿：一张原型图的层序与跨层连接预算。
          
      en: >
          PresetLayer workbook: layer order and cross-layer link budget of a preset.
          
---
