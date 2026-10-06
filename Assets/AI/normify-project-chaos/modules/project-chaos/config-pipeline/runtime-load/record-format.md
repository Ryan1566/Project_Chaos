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
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.731Z"
fingerprint: a4f45a2229b4a5a8f6377de9fa5c8685696b8a5d9c964b0b8c110e062bc07823
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
