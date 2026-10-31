---
uid: aa910002
id: project-chaos.config-pipeline.table-repo.world-map
parent: project-chaos.config-pipeline.table-repo
name: {zh: "WorldMap 世界观地图表", en: "WorldMap Workbook"}
description:
  zh: >
      WorldMap_世界观地图表.xlsx：由世界观地图绘制工具导出、放在源表目录顶层、可被 ExcelTool/ExportExcelConfigs 直接读取。10 列 Index/MapId/Kind/Type/RefId/X/Y/Scale/Layer/Enable；X、Y 为归一化 0~1（X 自左向右、Y 自下向上），Kind 1=固定地点 2=兴趣点，(Kind,Type,RefId) 指向地点表/兴趣点表的主键。
      
  en: >
      WorldMap_世界观地图表.xlsx: exported by the world map authoring tool, placed at the top level of the source-table repo, directly consumable by ExcelTool/ExportExcelConfigs. Ten columns Index/MapId/Kind/Type/RefId/X/Y/Scale/Layer/Enable; X and Y are normalised 0~1 (X left-to-right, Y bottom-to-top); Kind 1 = fixed location, 2 = POI; (Kind,Type,RefId) keys into the location/POI table.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.610Z"
fingerprint: c01e46862ff33b804694a703be9008251e5c3c03886d076b95e8a509baf2def4
source:
  - path: "Assets/Excel/Chaos_excel/WorldMap_世界观地图表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/WorldMap_世界观地图表.xlsx"
    description:
      zh: >
          地图表工作簿：行3 备注/行4 属性名/行5 端标记 client/行6 类型/行7 起数据。
          
      en: >
          Map workbook: row 3 remarks, row 4 field names, row 5 client flag, row 6 types, data from row 7.
          
---
