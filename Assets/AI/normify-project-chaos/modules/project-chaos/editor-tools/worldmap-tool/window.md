---
uid: aa91000b
id: project-chaos.editor-tools.worldmap-tool.window
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "工具窗口", en: "Tool Window"}
description:
  zh: >
      绘制窗口：5 种模式、可缩放平移的画布、点位与多边形绘制、侧栏列表、三个导出按钮。绘制在 GUI.BeginClip 内且用剪辑局部坐标（探针实测 BeginClip 会平移原点），命中判定用窗口坐标，两者的等价性由菜单 8 断言。「新建区域(同势力)」分配下一个可用 RegionId 并继承颜色。多边形预览只在生成顺序相邻的顶点之间画实线链边；收口边在顶点≥3 时以淡虚线表示（可开关，并标明“导出时自动闭合、不是生成顺序”）。维护「笔尖」= 最后一个生成点（可在画势力模式点已有顶点、选择模式点中顶点、或工具栏两个按钮设置），从笔尖到鼠标画橡皮筋，新顶点**插入到笔尖之后** —— 因此可以从任意顶点继续连线。
      
  en: >
      Authoring window: five modes, zoom/pan canvas, point/polygon drawing, lists and export buttons. Drawing uses clip-local coordinates inside GUI.BeginClip (probe: it translates the origin); hit-testing uses window coordinates, and menu 8 asserts they match. Preview chains consecutive vertices only; the auto-close edge is a dim dashed line. A pen head (set by clicking or selecting a vertex, or toolbar buttons) is joined to the cursor by a rubber band, and new vertices insert right after it.
      
revision: a7c3b8fe6345669cb33b508b5355f6a13e4cf0af
updated_at: "2026-10-14T13:38:17.129Z"
fingerprint: fb96e16d36ad28ea95d744ad1d5541481ddd3f4e3567a27b90c871ae6856eac4
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
