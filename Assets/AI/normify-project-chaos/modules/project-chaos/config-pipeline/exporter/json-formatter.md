---
uid: c1a20305
id: project-chaos.config-pipeline.exporter.json-formatter
parent: project-chaos.config-pipeline.exporter
name: {zh: "Json 排版美化", en: "JSON Pretty Printer"}
description:
  zh: >
      排版阶段，把导出的 .json 与 .record 变成人读格式，风格与 JsonUtility.ToJson(obj, true) 一致（4 空格缩进）。实现会跟踪字符串状态与转义，避免把中文文本里的逗号、冒号、花括号误当结构符；空白对 JsonUtility 无影响。
      
  en: >
      Formatting pass that makes exported .json and .record files human-readable at the same 4-space style as JsonUtility.ToJson(obj, true). It tracks string state and escapes so commas, colons and braces inside Chinese text are not mistaken for structure; whitespace is not significant to JsonUtility.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.728Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 474
    end_line: 553
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L474-L553"
    description:
      zh: >
          Json 美化器所在行段。
          
      en: >
          Line range of the JSON pretty-printer.
          
  - protocol: rpc
    path: "ConfigManager.FormatJson"
    description:
      zh: >
          把紧凑 Json 美化为 4 空格缩进。
          
      en: >
          Pretty-prints compact JSON with 4-space indentation.
          
---
