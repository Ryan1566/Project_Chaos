---
uid: c1a20301
id: project-chaos.config-pipeline.exporter.export-menu
parent: project-chaos.config-pipeline.exporter
name: {zh: "导出菜单与编排", en: "Export Menus"}
description:
  zh: >
      ExcelTool 菜单下的编辑器入口：ExportExcel 同时跑两种模式，ExportExcelConfigs/ExportExcelModels 各跑一种。每次遍历表目录下所有文件、过滤非 .xlsx、读第 1 个页签，遇到空维度表就 return（会中断整批导出），然后逐表导出 Json 与类，最后 AssetDatabase.Refresh。
      
  en: >
      Editor entry points under the ExcelTool menu: ExportExcel runs both modes, ExportExcelConfigs and ExportExcelModels run one each. Each pass loads every file in the table folder, skips non-.xlsx, reads worksheet 1, returns early on an empty-dimension sheet (which aborts the whole batch), then exports JSON and a class per table before refreshing the asset database.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.727Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 53
    end_line: 140
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L53-L140"
    description:
      zh: >
          导出菜单与两模式编排所在行段。
          
      en: >
          Line range holding the export menus and their two-mode orchestration.
          
  - protocol: rpc
    path: "Menu/ExcelTool/ExportExcel"
    description:
      zh: >
          同时跑 Config 与 Data 导出的菜单项。
          
      en: >
          Menu item running both Config and Data export.
          
  - protocol: rpc
    path: "Menu/ExcelTool/ExportExcelConfigs"
    description:
      zh: >
          只导出常量 Config 表的菜单项。
          
      en: >
          Menu item exporting only the constant Config tables.
          
  - protocol: rpc
    path: "Menu/ExcelTool/ExportExcelModels"
    description:
      zh: >
          只导出可写 Data 表的菜单项。
          
      en: >
          Menu item exporting only the writable Data tables.
          
  - protocol: rpc
    path: "ConfigManager.ExportConfigs"
    description:
      zh: >
          遍历 xlsx 并以 Config 模式导出 Json 与类。
          
      en: >
          Iterates xlsx files and exports JSON plus classes in Config mode.
          
  - protocol: rpc
    path: "ConfigManager.ExportModels"
    description:
      zh: >
          遍历 xlsx 并以 Data 模式导出 Json 与类。
          
      en: >
          Iterates xlsx files and exports JSON plus classes in Data mode.
          
deps:
  - kind: call
    to: project-chaos.config-pipeline.exporter.class-generator
    from_api: "rpc:Menu/ExcelTool/ExportExcelConfigs"
    to_api: "rpc:ConfigManager.ExportClass"
    label: {zh: "生成数据类", en: "Generates classes"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.json-writer
    from_api: "rpc:Menu/ExcelTool/ExportExcelConfigs"
    to_api: "rpc:ConfigManager.ExportJson"
    label: {zh: "写 Json 产物", en: "Writes JSON output"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.file-io
    from_api: "rpc:Menu/ExcelTool/ExportExcelConfigs"
    to_api: "rpc:FileUtil.LoadFiles"
    label: {zh: "列出表文件", en: "Lists table files"}
---
