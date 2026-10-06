---
uid: c1a20303
id: project-chaos.config-pipeline.exporter.json-writer
parent: project-chaos.config-pipeline.exporter
name: {zh: "逐表 Json 写出", en: "Table JSON Writer"}
description:
  zh: >
      逐表 Json 写出：遍历列，跳过端标记为 server 与留空的列，逐格经类型白名单转换，拼成键值对，再把扁平文本重组为 datas 数组，包一层 JsonUtility 可读的外壳，美化后按模式存为 .json（Config）或 .record（Data）。转换失败会带上表/页签/行列/字段/类型上下文重新抛出。
      
  en: >
      Per-table JSON writer: walks the columns, skips server-marked and blank-marked ones, converts each cell through the type whitelist, joins key-value pairs, reshapes the flat text into a datas array, wraps it for JsonUtility, pretty-prints it, and saves as .json (Config) or .record (Data). Conversion failures are rethrown with sheet, row, column, field and type context.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.728Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 182
    end_line: 250
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L182-L250"
    description:
      zh: >
          ExportJson（逐表记录写出）所在行段。
          
      en: >
          Line range of ExportJson, the per-table record writer.
          
  - protocol: rpc
    path: "ConfigManager.ExportJson"
    description:
      zh: >
          为单表构建并写出 datas 数组。
          
      en: >
          Builds and writes the datas array for one table.
          
deps:
  - kind: call
    to: project-chaos.config-pipeline.exporter.row-schema
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "rpc:ConfigManager.GetValues"
    label: {zh: "取每列单元格值", en: "Collects cell values"}
  - kind: call
    to: project-chaos.config-pipeline.type-whitelist
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "rpc:ConfigManager.Convert"
    label: {zh: "逐格做类型门禁", en: "Type-gates each cell"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.json-builder
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "rpc:ConfigManager.GetJsonFromJsonK_V"
    label: {zh: "拼装键值文本", en: "Assembles key-value text"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.json-formatter
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "rpc:ConfigManager.FormatJson"
    label: {zh: "美化 Json 排版", en: "Pretty-prints the JSON"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.file-io
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "rpc:FileUtil.SaveFile"
    label: {zh: "写出产物文件", en: "Writes the artifact"}
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.test-table
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "file:Assets/Resources/Data/Json/Runtime/TestTableConfig.json"
    label: {zh: "产出运行期 Json", en: "Emits runtime JSON"}
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.loading-table
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "file:Assets/Resources/Data/Json/Runtime/LoadingTableConfig.json"
    label: {zh: "产出轮播表 Json", en: "Emits carousel JSON"}
  - kind: dataflow
    to: project-chaos.config-pipeline.record-store
    from_api: "rpc:ConfigManager.ExportJson"
    to_api: "file:Assets/Data/Records/TestTableData.record"
    label: {zh: "产出 .record 存档", en: "Emits the .record save"}
---
