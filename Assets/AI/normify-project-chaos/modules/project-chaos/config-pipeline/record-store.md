---
uid: c1a20008
id: project-chaos.config-pipeline.record-store
parent: project-chaos.config-pipeline
name: {zh: "本地记录存档", en: "Local Record Store"}
description:
  zh: >
      Data 模式导出到 Assets/Data/Records：内容与 config Json 同为 {"datas":[...]}，只是扩展名为 .record，运行时由 Recorder 读取（编辑器与 Standalone 走 Assets 下路径，包体走 Records 路径）。
      
  en: >
      Data-mode export written to Assets/Data/Records: the same {"datas":[...]} payload as the config JSON but with the .record extension, read at runtime by Recorder (editor and standalone use the Assets path, packages use the Records path).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.730Z"
fingerprint: 7c510137c54e89e456e4541faf9a746edf8c31964a8a3d5fe3b4c6755dc1ae9a
source:
  - path: "Assets/Data/Records/TestTableData.record"
apis:
  - protocol: file
    path: "Assets/Data/Records/TestTableData.record"
    description:
      zh: >
          导出的 TestTable .record 记录文件。
          
      en: >
          Exported TestTable .record save file.
          
---
