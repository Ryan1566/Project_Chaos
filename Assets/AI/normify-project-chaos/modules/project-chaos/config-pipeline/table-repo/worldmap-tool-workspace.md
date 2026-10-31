---
uid: aa910004
id: project-chaos.config-pipeline.table-repo.worldmap-tool-workspace
parent: project-chaos.config-pipeline.table-repo
name: {zh: "地图工具工作区", en: "World Map Tool Workspace"}
description:
  zh: >
      Assets/Excel/Chaos_excel/_worldmap_tool/：世界观地图绘制工具的草稿与中间产物目录——WorldMapDraft.json（源图路径、真实像素尺寸、MapId、点位、势力区域）与 5 张时期分表 xlsx。该目录是子目录，导出器只扫顶层不递归，因此这里的文件永不参与导出，也是时期分表可以同名共存的原因。
      
  en: >
      Assets/Excel/Chaos_excel/_worldmap_tool/: draft and intermediate files of the world map authoring tool — WorldMapDraft.json (source image path, real pixel size, map id, points, faction areas) plus the five per-era part workbooks. Because the exporter only scans top-level files, nothing here is ever exported, which is also why the part tables may share a filename prefix.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.611Z"
fingerprint: 1d2454c53b118ef8c413e676e4214d7933c227e86d07ab76b2226ccc832f0e09
source:
  - path: "Assets/Excel/Chaos_excel/_worldmap_tool/WorldMapDraft.json"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/_worldmap_tool/WorldMapDraft.json"
    description:
      zh: >
          工具草稿：重新打开窗口时恢复上次的摆放与绘制。
          
      en: >
          Tool draft: restores the previous placement and drawing when the window reopens.
          
---
