---
uid: aa910016
id: project-chaos.editor-tools.worldmap-tool.region-tree
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "区域树模型", en: "Region Tree Model"}
description:
  zh: >
      势力区域主从树的纯数据层（不含绘制）：把草稿的势力顶点按 时期 → 势力 → 区域 三级聚成节点（时期 1~5 全列、势力去重并排序、区域带 sourceIndex），并提供拖拽节点/放置目标/移动意图/重排序意图等结构，供窗口渲染与拖放使用。
      
  en: >
      Pure data layer of the faction-region master/detail tree (no drawing): groups the draft's faction vertices into era → faction → region nodes (all five eras listed, factions de-duplicated and ordered, regions carrying a source index), and provides the drag node, drop target, move-intent and reorder-intent structures used by the window's rendering and drag-and-drop.
      
revision: 3e4115979ea7e37fe093c0b4cd179612739ab7c5
updated_at: "2026-10-17T09:59:30.827Z"
fingerprint: d8044cb5a64bb51be8c25fc3551a4e3798c04b07c3231a7d6724d35e7ce2b199
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolRegionTree.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolRegionTree.cs"
    description:
      zh: >
          构建 时期 → 势力 → 区域 三级树行（时期 1~5 全列、势力有序、区域带 sourceIndex）。
          
      en: >
          Builds the era → faction → region tree rows (all five eras, ordered factions, regions with source index).
          
  - protocol: rpc
    path: "WorldMapRegionTree.Build"
    description:
      zh: >
          侧栏使用的树构建器；按展开状态返回可见行。
          
      en: >
          Tree builder used by the sidebar; returns visible rows for the expanded state.
          
---
