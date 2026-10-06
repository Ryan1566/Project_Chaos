---
uid: f1c0004d
id: project-chaos.framework.logging.colors
parent: project-chaos.framework.logging
name: {zh: "日志颜色", en: "Log Colors"}
description:
  zh: >
      LogColor 颜色枚举与 LogColorInfo 十六进制色表，供链式构建的具名颜色快捷方法使用。
      
  en: >
      LogColor enum plus LogColorInfo hex table, used by the fluent builder's named color shortcuts.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.750Z"
fingerprint: 503d7507b447926bb5b8e08138f49b97c6aab3be3010c72b18e2f95725dea3fd
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/LogColor.cs"
    line: 1
    end_line: 57
apis:
  - protocol: rpc
    path: "LogColorInfo.GetHex"
    description:
      zh: >
          取 LogColor 对应的十六进制颜色。
          
      en: >
          Returns the hex color of a LogColor value.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/LogManager/LogColor.cs"
    description:
      zh: >
          LogColor 枚举与十六进制色表脚本。
          
      en: >
          LogColor enum and hex table script asset.
          
---
