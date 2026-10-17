---
uid: aa91000b
id: project-chaos.editor-tools.worldmap-tool.window
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "工具窗口", en: "Tool Window"}
description:
  zh: >
      绘制窗口：5 种模式、可缩放平移的画布、点位与多边形绘制、侧栏列表、三个导出按钮。绘制在 GUI.BeginClip 内且用剪辑局部坐标（探针实测 BeginClip 会平移原点），命中判定用窗口坐标，两者等价性由菜单 8 断言。预览只在生成顺序相邻顶点间画实线，收口边为淡虚线；维护「笔尖」（可点选已有顶点/选择模式/工具栏按钮指定），橡皮筋从笔尖跟鼠标，新顶点插入到笔尖之后。两个“新建”按钮分工：工具栏「新建势力区域」取该时期下一个可用 FactionId、RegionId=1，并对颜色逐级避让（建议配色→后备调色板→黄金角派生）；侧栏「新建区域（同势力·飞地）」建同一势力的飞地。
      
  en: >
      Authoring window for the meta world map. Drawing uses clip-local coordinates inside GUI.BeginClip (probe: it translates the origin); hit-testing uses window coordinates, asserted by menu 8. Preview chains consecutive vertices only, the auto-close edge is dashed, and new vertices insert after the pen head. Toolbar 'new faction area' allocates the era's next free FactionId with RegionId=1 and an avoided colour; sidebar 'new region (same faction)' adds an enclave.
      
revision: b6dae190152cdcce64be3587bb098f7af5b42563
updated_at: "2026-10-17T09:35:52.613Z"
fingerprint: 6ee21495aa5620eda45d929f696586b7e12d2507d2e9f1d1bd59c4948380fcf0
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
