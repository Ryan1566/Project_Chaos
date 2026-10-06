---
uid: c1a20306
id: project-chaos.config-pipeline.exporter.row-schema
parent: project-chaos.config-pipeline.exporter
name: {zh: "表头行约定", en: "Header Row Schema"}
description:
  zh: >
      硬编码的 7 行表头约定：第 1 行类名（仅供人看）、第 2 行是否读取（未实现）、第 3 行中文备注、第 4 行英文属性名（唯一强校验，为空直接抛异常）、第 5 行端标记（server 从类与 Json 整列丢弃；留空只从 Json 丢弃）、第 6 行类型、第 7 行起为数据。第 7 行就是第一条数据；表尾留空行会变成一条空记录。
      
  en: >
      The hard-coded 7-row header contract: row 1 class name (human only), row 2 read flag (not implemented), row 3 Chinese remark, row 4 English property name (the only hard validation - blank throws), row 5 end marker (server drops the column from class and JSON, blank drops it from JSON only), row 6 type, row 7 onward data. Row 7 is the first data row; a trailing blank row becomes an empty record.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.728Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 36
    end_line: 52
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 251
    end_line: 323
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L36-L52"
    description:
      zh: >
          硬编码表头行号常量所在行段。
          
      en: >
          Line range of the hard-coded header row index constants.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L251-L323"
    description:
      zh: >
          表头与单元格读取函数所在行段。
          
      en: >
          Line range of the header and cell range readers.
          
  - protocol: rpc
    path: "ConfigManager.GetProperties"
    description:
      zh: >
          读第 4 行字段名，为空则报错。
          
      en: >
          Reads row 4 field names, throwing on a blank one.
          
  - protocol: rpc
    path: "ConfigManager.GetEnd"
    description:
      zh: >
          读第 5 行端标记（client/both/server）。
          
      en: >
          Reads row 5 end markers (client/both/server).
          
  - protocol: rpc
    path: "ConfigManager.GetValues"
    description:
      zh: >
          从第 7 行往下读一列单元格文本。
          
      en: >
          Reads a column's cell text from row 7 down.
          
  - protocol: rpc
    path: "ConfigManager.GetRemark"
    description:
      zh: >
          读第 3 行中文备注做字段注释。
          
      en: >
          Reads row 3 Chinese remarks for field comments.
          
---
