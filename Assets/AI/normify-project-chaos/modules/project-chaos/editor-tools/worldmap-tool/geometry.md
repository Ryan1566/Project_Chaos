---
uid: aa91000d
id: project-chaos.editor-tools.worldmap-tool.geometry
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "坐标换算", en: "Geometry"}
description:
  zh: >
      几何工具：归一化(0~1，X 自左向右、Y 自下向上) ↔ 图像像素(左上原点) ↔ UI 锚点三种坐标的换算（Y 轴翻转只在本文件），以及点在多边形内、自交检测、顶点顺序规范化。
      
  en: >
      Geometry helpers: normalised(0~1, X left-to-right, Y bottom-to-top) ↔ image pixels (top-left origin) ↔ UI anchors, with the Y flip confined to this file; point-in-polygon, self-intersection detection and winding normalisation.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.565Z"
fingerprint: db07e8f02a13a816dd1562ec3127c849c50a92bebd5673eb3be22b04891761ff
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolGeometry.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolGeometry.cs"
    description:
      zh: >
          坐标换算（Y 轴翻转集中在此）、点在多边形内、自交检测、顶点顺序规范化。
          
      en: >
          Coordinate conversions (Y-flip in one place), point-in-polygon, self-intersection, winding normalisation.
          
---
