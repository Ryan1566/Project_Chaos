---
uid: 6b0d41af
id: project-chaos.config-pipeline
parent: project-chaos
tags: [excel, config, codegen]
name: {zh: "配置表管线", en: "Config-Table Pipeline"}
description:
  zh: >
      Excel 配置表全链路：Assets/Excel 下的表仓 xlsx 与本地化 CSV、ConfigManager/ConfigLoader/FileUtil 的导出与运行时读取、7 行表头约定与 12 类型白名单、生成物 Resources/Data/Json/Runtime/*.json 与 MVP/Model/ConfigData/*.cs、SL/Recorder 的本地记录数据。
      
  en: >
      End-to-end Excel config pipeline: the xlsx table repo and localization CSVs under Assets/Excel, export and runtime reading via ConfigManager/ConfigLoader/FileUtil, the 7-row header convention and 12-type whitelist, generated Resources/Data/Json/Runtime/*.json and MVP/Model/ConfigData/*.cs, plus SL/Recorder local record data.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:02:58.251Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
deps:
  - kind: call
    to: project-chaos.framework
    label: {zh: "用路径常量与日志", en: "Uses GlobalPath and logs"}
---
