---
uid: aa910003
id: project-chaos.config-pipeline.table-repo.world-faction
parent: project-chaos.config-pipeline.table-repo
name: {zh: "WorldFaction 势力范围表", en: "WorldFaction Workbook"}
description:
  zh: >
      WorldFaction_势力范围配置表.xlsx：由工具导出 5 张时期分表（放子目录 _worldmap_tool/，不参与导出）后人工/工具合并成顶层这一张。9 列 Index/Stage/FactionId/RegionId/PointOrder/X/Y/Color/Enable；同一 (Stage,FactionId,RegionId) 下按 PointOrder 升序连成闭合多边形，顶点 ≥3；Color 为 #RRGGBB 字符串。
      
  en: >
      WorldFaction_势力范围配置表.xlsx: the tool exports five per-era part tables (in the _worldmap_tool subfolder, never scanned by the exporter) which are merged into this single top-level table. Nine columns Index/Stage/FactionId/RegionId/PointOrder/X/Y/Color/Enable; vertices of one (Stage,FactionId,RegionId) connect in ascending PointOrder into a closed polygon of at least 3 vertices; Color is a #RRGGBB string.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.567Z"
fingerprint: ceb780038e01d16123cb07e941fd15813e0242056f0f6d75e73796f41de7886b
source:
  - path: "Assets/Excel/Chaos_excel/WorldFaction_势力范围配置表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/WorldFaction_势力范围配置表.xlsx"
    description:
      zh: >
          势力范围表工作簿（合并后的顶层表）。
          
      en: >
          Faction-area workbook (the merged top-level table).
          
---
