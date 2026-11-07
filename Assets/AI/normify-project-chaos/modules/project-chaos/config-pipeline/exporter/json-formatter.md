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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.982Z"
fingerprint: c36e05801bd99f1c4242cc6e4fa756962c262e19fd2e05ec4214c2674d2c6b03
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
