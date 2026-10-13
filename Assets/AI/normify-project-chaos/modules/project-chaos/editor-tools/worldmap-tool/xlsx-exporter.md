---
uid: aa91000f
id: project-chaos.editor-tools.worldmap-tool.xlsx-exporter
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "xlsx 导出", en: "XLSX Exporter"}
description:
  zh: >
      EPPlus 4.5.3 写出符合项目 7 行表头的 xlsx（行1 表名、行2 IsConfig、行3 备注、行4 属性名、行5 client、行6 类型、行7 起数据）：地图表写到源表目录顶层，5 张时期势力分表写到 _worldmap_tool 子目录，另可选导出合并势力表。
      
  en: >
      EPPlus 4.5.3 writer producing xlsx in the project's 7-row header format (row 1 name, row 2 IsConfig, row 3 remarks, row 4 field names, row 5 client, row 6 types, data from row 7): the map table at the top level of the source-table repo, the five per-era faction parts into the _worldmap_tool subfolder, and an optional merged faction table.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.566Z"
fingerprint: 55c0bb1a33ae76cfbb8f7acefd5c48b78aa6e7e0d3ed8a09d78e496edb1dff16
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
