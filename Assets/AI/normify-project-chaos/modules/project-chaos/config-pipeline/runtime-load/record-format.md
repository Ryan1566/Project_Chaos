---
uid: c1a20504
id: project-chaos.config-pipeline.runtime-load.record-format
parent: project-chaos.config-pipeline.runtime-load
name: {zh: "存档 Json 美化", en: "Record JSON Formatting"}
description:
  zh: >
      与导出器重复的第二份 Json 美化实现，把 .record 内容排成 JsonUtility.ToJson(obj, true) 风格。实现会跟踪字符串状态，避免把文本内的逗号与花括号当结构符；空白不影响 JsonUtility 解析。
      
  en: >
      A second copy of the JSON pretty-printer (the exporter has its own) that formats .record payloads to JsonUtility.ToJson(obj, true) style. It tracks string state so commas and braces inside text are not treated as structure; whitespace does not affect JsonUtility parsing.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.984Z"
fingerprint: fbe24babc899733b90af3580c3252690c4f37edaf54fda5eba2be135af69a7c0
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs"
    line: 232
    end_line: 311
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs#L232-L311"
    description:
      zh: >
          写存档前使用的 .record 美化器。
          
      en: >
          The .record pretty-printer used before writing saves.
          
  - protocol: rpc
    path: "Recorder.FormatJson"
    description:
      zh: >
          把存档 Json 美化为 4 空格缩进。
          
      en: >
          Pretty-prints save JSON with 4-space indentation.
          
---
