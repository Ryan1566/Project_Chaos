---
uid: c1a20502
id: project-chaos.config-pipeline.runtime-load.record-cache
parent: project-chaos.config-pipeline.runtime-load
name: {zh: "存档缓存与写盘", en: "Record Cache and Flush"}
description:
  zh: >
      存档缓存：构造函数枚举存档目录下所有 .record（编辑器/Standalone 走 Assets 下路径，包体走内置 Records 路径）并读入「文件名→文本」字典；ForceSave 把缓存回写这些文件。同时声明 MVP 模型实现的 IModel 生命周期契约。
      
  en: >
      Save-file cache: the constructor lists every .record in the record folder (editor and standalone use the Assets path, packages use the built-in Records path) and reads each into a name-to-text dictionary; ForceSave rewrites those files from the cache. Also declares the IModel lifecycle contract that MVP models implement.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.731Z"
fingerprint: a4f45a2229b4a5a8f6377de9fa5c8685696b8a5d9c964b0b8c110e062bc07823
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs"
    line: 12
    end_line: 92
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs#L12-L92"
    description:
      zh: >
          IModel 契约以及 Recorder 缓存与写盘。
          
      en: >
          The IModel contract plus the Recorder cache and flush.
          
  - protocol: rpc
    path: "Recorder"
    description:
      zh: >
          持有内存 .record 缓存的单例。
          
      en: >
          Singleton owning the in-memory .record cache.
          
  - protocol: rpc
    path: "Recorder.ForceSave"
    description:
      zh: >
          把缓存里的所有存档写回磁盘。
          
      en: >
          Flushes every cached record back to disk.
          
  - protocol: rpc
    path: "IModel"
    description:
      zh: >
          MVP 模型生命周期接口（Init/Dispose）。
          
      en: >
          MVP model lifecycle interface (Init/Dispose).
          
deps:
  - kind: call
    to: project-chaos.config-pipeline.exporter.file-io
    from_api: "rpc:Recorder"
    to_api: "rpc:FileUtil.LoadFiles"
    label: {zh: "枚举存档文件", en: "Lists record files"}
---
