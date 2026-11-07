---
uid: c1a20302
id: project-chaos.config-pipeline.exporter.class-generator
parent: project-chaos.config-pipeline.exporter
name: {zh: "C# 类生成器", en: "C# Class Generator"}
description:
  zh: >
      生成 C# 容器类：前缀 using System 与 [Serializable]，类名 = 表名 + 模式名，然后逐列输出 public 字段——字段类型原样照抄第 6 行文本，行尾注释取第 3 行。第 5 行标 server 的列整列丢弃。
      
  en: >
      Generates the C# container class: prepends using System and [Serializable], names the class file-name plus mode, then emits one public field per column, copying row 6 text verbatim as the field type and row 3 as the trailing comment. Columns marked server in row 5 are dropped entirely.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.982Z"
fingerprint: c36e05801bd99f1c4242cc6e4fa756962c262e19fd2e05ec4214c2674d2c6b03
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 141
    end_line: 181
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L141-L181"
    description:
      zh: >
          ExportClass（C# 数据类生成器）所在行段。
          
      en: >
          Line range of ExportClass, the C# data-class code generator.
          
  - protocol: rpc
    path: "ConfigManager.ExportClass"
    description:
      zh: >
          按表生成 Config/Data 模式的可序列化类。
          
      en: >
          Emits a serializable class per table for Config or Data mode.
          
deps:
  - kind: call
    to: project-chaos.config-pipeline.exporter.row-schema
    from_api: "rpc:ConfigManager.ExportClass"
    to_api: "rpc:ConfigManager.GetProperties"
    label: {zh: "读取表头行", en: "Reads header rows"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.file-io
    from_api: "rpc:ConfigManager.ExportClass"
    to_api: "rpc:FileUtil.SaveFile"
    label: {zh: "写出类文件", en: "Writes the class file"}
---
