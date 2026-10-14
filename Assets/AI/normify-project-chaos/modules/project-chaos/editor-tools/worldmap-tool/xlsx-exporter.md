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
      
revision: a7c3b8fe6345669cb33b508b5355f6a13e4cf0af
updated_at: "2026-10-14T13:18:15.974Z"
fingerprint: 9043b05f4d2c5674d8a6390058d766b7d2db59f915be09b49b8872de189d24c9
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
