---
uid: f1c0005f
id: project-chaos.framework.dev-tests.config-probes
parent: project-chaos.framework.dev-tests
name: {zh: "配置自测探针", en: "Config Probes"}
description:
  zh: >
      配置/存档自测探针：EPPlus 裸读 xlsx、ConfigLoader 异步读表、Recorder 读/改存档。多为示例代码，部分已注释。
      
  en: >
      Config/save dev probes: raw xlsx reading via EPPlus, async config loading via ConfigLoader and record read/update via Recorder. Assert no failure; most are commented-out examples.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.741Z"
fingerprint: b4564f2251178a9c42f112fa398630cb184176226b3dcc3d5f9f73e67116d235
source:
  - path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/ImpTableTest.cs"
    line: 1
    end_line: 74
  - path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/LoadConfigTest.cs"
    line: 1
    end_line: 22
  - path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/RecordTest.cs"
    line: 1
    end_line: 32
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/ImpTableTest.cs"
    description:
      zh: >
          基于 EPPlus 的 xlsx 原始读取探针（从第 7 行开始）。
          
      en: >
          EPPlus-based raw xlsx read probe (from row 7).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/LoadConfigTest.cs"
    description:
      zh: >
          ConfigLoader 异步读配置探针。
          
      en: >
          ConfigLoader async load probe.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/DataConfigTest/RecordTest.cs"
    description:
      zh: >
          Recorder 存档读取/更新探针。
          
      en: >
          Recorder read/update probe.
          
deps:
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "file:Assets/Scripts/Runtime/GameTest/DataConfigTest/ImpTableTest.cs"
    to_api: "rpc:GlobalPath.data_ExcelPath"
    label: {zh: "Excel 目录", en: "Excel dir"}
---
