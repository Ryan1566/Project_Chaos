---
uid: aa91000e
id: project-chaos.editor-tools.worldmap-tool.validator
parent: project-chaos.editor-tools.worldmap-tool
name: {zh: "导出校验", en: "Export Validator"}
description:
  zh: >
      导出前校验：数据行 0 条与顶层文件名前缀冲突属硬拒绝（会让整批导出崩或静默覆盖）；坐标越界、(Kind,Type,RefId) 重复、颜色格式、多边形顶点数与顺序、自交与极小面积分别报错或告警。
      
  en: >
      Pre-export validation: zero data rows and duplicate top-level filename prefixes are hard refusals (they would break or silently overwrite the batch export); coordinates out of range, duplicate (Kind,Type,RefId), colour format, polygon vertex count/order, self-intersection and tiny area are reported as errors or warnings.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.566Z"
fingerprint: ff8812625e4b51eaae5ca4716d40c17896ba29129e859c28913c2d514f775674
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
