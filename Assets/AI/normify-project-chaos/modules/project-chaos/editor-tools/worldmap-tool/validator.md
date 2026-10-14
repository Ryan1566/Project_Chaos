---
uid: aa91000e
id: project-chaos.editor-tools.worldmap-tool.validator
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "导出校验", en: "Export Validator"}
description:
  zh: >
      导出前校验：数据行 0 条与顶层前缀冲突属硬拒绝；坐标越界、(Kind,Type,RefId) 重复、颜色格式、顶点数与顺序、自交与极小面积分别报错或告警。新增：同一 (Stage,FactionId) 的两块区域颜色不一致 → Error 并阻断顶层导出（否则一个势力会被画成两个）。
      
  en: >
      Pre-export validation: zero data rows and duplicate top-level prefixes are hard refusals; coordinates out of range, duplicate (Kind,Type,RefId), colour format, vertex count/order, self-intersection and tiny areas are reported. Added: two regions of the same (Stage, FactionId) with different colours is an Error that blocks the top-level export — otherwise one faction would be drawn as two.
      
revision: a7c3b8fe6345669cb33b508b5355f6a13e4cf0af
updated_at: "2026-10-14T13:18:15.974Z"
fingerprint: 5474cf2dbb1d41df2743aeaa0635311bf9cd24b2535f83f5c35a79daca17b00a
source:
  - path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolValidator.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/WorldMapTool/WorldMapToolValidator.cs"
    description:
      zh: >
          导出前校验，产出可读错误清单。
          
      en: >
          Pre-export validation producing a readable error list.
          
---
