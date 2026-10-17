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
      
revision: b6dae190152cdcce64be3587bb098f7af5b42563
updated_at: "2026-10-17T09:35:47.638Z"
fingerprint: 79fe0a4104293edee319629b78ce1aa1dedab6d99339ae5e7a5cbf33a46373de
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
