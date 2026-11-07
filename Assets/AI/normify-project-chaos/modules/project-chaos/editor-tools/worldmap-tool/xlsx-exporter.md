---
uid: aa91000f
id: project-chaos.editor-tools.worldmap-tool.xlsx-exporter
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "xlsx 导出", en: "XLSX Exporter"}
description:
  zh: >
      EPPlus 4.5.3 写出符合项目 7 行表头的工作簿：顶层地图表、_worldmap_tool 子目录下的 5 张时期势力分表（导出器不扫描子目录）、以及合并势力表。同一 (Stage,FactionId) 的所有区域一律写该势力第一块区域的颜色，把同势力异色挡在源头。
      
  en: >
      EPPlus 4.5.3 writer producing the project's 7-row header workbooks: the map table at the top level, the five per-era faction parts into the _worldmap_tool subfolder (never scanned by the exporter), and the merged faction table. All regions of one (Stage, FactionId) are written with the faction's first-region colour, so the colour clash is stopped at the source.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.985Z"
fingerprint: c1b55e72aa62d0fea6c31daed21f22d5742acdcc233af5bbc74467975fd7e80b
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolXlsxExporter.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolXlsxExporter.cs"
    description:
      zh: >
          用 EPPlus 写出地图表、5 张时期分表与合并势力表。
          
      en: >
          Writes the map workbook, the five per-era part tables and the merged faction workbook with EPPlus.
          
---
