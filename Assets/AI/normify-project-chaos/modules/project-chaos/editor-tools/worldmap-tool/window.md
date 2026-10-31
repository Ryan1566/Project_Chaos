---
uid: aa91000b
id: project-chaos.editor-tools.worldmap-tool.window
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "工具窗口", en: "Tool Window"}
description:
  zh: >
      绘制窗口：5 种模式、可缩放平移的画布、点位与多边形绘制、可拖宽侧栏与三个导出按钮。绘制在 GUI.BeginClip 内用剪辑局部坐标（实测会平移原点），命中判定用窗口坐标，菜单 8 断言两者一致。预览只在生成顺序相邻顶点间画实线，收口边为淡虚线；维护「笔尖」并可插到笔尖之后。两个“新建”按钮分工：工具栏「新建势力区域」取该时期下一可用 FactionId，侧栏「新建区域（同势力·飞地）」建飞地。侧栏是 时期→势力→区域 三级可折叠树，支持拖拽「区域」行：同势力内排序、或跨势力移动（跨势力需二次确认）。
      
  en: >
      Meta world map authoring window. Drawing uses clip-local coordinates (GUI.BeginClip translates the origin) while hit-testing uses window coordinates; menu 8 asserts they match. Preview chains consecutive vertices, the auto-close edge is dashed, new vertices insert after the pen head. Toolbar 'new faction area' takes the era's next free FactionId. Sidebar: resizable era→faction→region tree; dragging a region reorders it within a faction or moves it to another faction (confirmed).
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.613Z"
fingerprint: 8b3bbc8dee4f09e9830983fbae480b7483f21a26863fed129a1f3a4d4bd6c600
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolWindow.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolWindow.cs"
    description:
      zh: >
          编辑器窗口（菜单 Tools/局外地图/0.）：画布、模式、列表与导出按钮。
          
      en: >
          EditorWindow (Tools/局外地图/0.) hosting the canvas, modes, lists and export buttons.
          
  - protocol: rpc
    path: "Menu/Tools/局外地图/0. 打开世界观地图绘制工具"
    description:
      zh: >
          打开绘制窗口的菜单项。
          
      en: >
          Menu entry that opens the authoring window.
          
  - protocol: rpc
    path: "Menu/Tools/局外地图/8. 校验坐标对齐（所见即所点）"
    description:
      zh: >
          自检项：断言「绘制路径」与「命中路径」映射到同一像素（所见=所点），并现场重测 BeginClip 原点是否被平移。
          
      en: >
          Self-check asserting that the drawing path and the hit-test path map to the same pixel (what-you-see = what-you-click), and re-measuring the BeginClip origin.
          
---
