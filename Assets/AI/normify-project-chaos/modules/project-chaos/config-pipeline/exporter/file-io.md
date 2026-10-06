---
uid: c1a20307
id: project-chaos.config-pipeline.exporter.file-io
parent: project-chaos.config-pipeline.exporter
name: {zh: "配置文件读写", en: "Config File IO"}
description:
  zh: >
      导出器与 Recorder 共用的文件系统助手：把路径拼到 Application.dataPath 下，枚举目录内容并跳过 .meta 与 ~ 临时文件，保存时先删后写。它不会创建目录——输出目录不存在会直接报错，而不是静默什么都不做。
      
  en: >
      Filesystem helper shared by the exporter and Recorder: resolves paths against Application.dataPath, lists folder contents while skipping .meta and ~temp files, and saves by deleting then writing. It never creates directories, so a missing output folder is a hard error rather than a silent no-op.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.727Z"
fingerprint: 11440ab6863d19afddab82acd4dc17d253a2448eb1316a8f32a922bedd4a234e
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/FileUtil.cs"
    line: 9
    end_line: 64
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/FileUtil.cs#L9-L64"
    description:
      zh: >
          共用的目录枚举与写文件助手。
          
      en: >
          The shared file listing and saving helper.
          
  - protocol: rpc
    path: "FileUtil.LoadFiles"
    description:
      zh: >
          列出 Assets 下某目录的非 meta 文件，目录不存在就报错。
          
      en: >
          Lists non-meta files in a folder under Assets, throwing if absent.
          
  - protocol: rpc
    path: "FileUtil.SaveFile"
    description:
      zh: >
          覆盖式写文件（无备份），目录不存在就报错。
          
      en: >
          Overwrites a file without backup, throwing if the folder is absent.
          
---
