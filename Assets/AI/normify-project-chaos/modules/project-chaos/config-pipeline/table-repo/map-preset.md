---
uid: 9e0d0106
id: project-chaos.config-pipeline.table-repo.map-preset
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "MapPreset 原型图主表", en: "MapPreset Workbook"}
description:
  zh: >
      MapPreset_原型图主表.xlsx → MapPresetConfig：Id / biomeId（外键 → MapBiome.Id）/ nameCN / mainPathMin、mainPathMax / branchMin、branchMax / totalMin、totalMax / specialTreasure（死路奖励房下限）/ specialCamp（扎营点，预留）/ retryMax（单节点选块/回溯上限）/ enable。
      
  en: >
      MapPreset_原型图主表.xlsx -> MapPresetConfig: Id / biomeId (FK to MapBiome.Id) / nameCN / mainPathMin-Max / branchMin-Max / totalMin-Max / specialTreasure (dead-end reward rooms, lower bound) / specialCamp (camp sites, reserved) / retryMax (per-node pick/backtrack limit) / enable.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.407Z"
fingerprint: 272e7a1112ae817941234a8336fed35131333a40f7f708254ff052737f2c41ee
source:
  - path: "Assets/Excel/Chaos_excel/MapPreset_原型图主表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/MapPreset_原型图主表.xlsx"
    description:
      zh: >
          原型图主表工作簿：每个主题的布局预算（主路/支路/房间数）。
          
      en: >
          MapPreset workbook: per-biome layout budget (path/branch/room counts).
          
---
