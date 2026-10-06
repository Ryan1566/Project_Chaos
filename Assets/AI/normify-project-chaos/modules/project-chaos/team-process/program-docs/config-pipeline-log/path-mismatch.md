---
uid: 7c0d0073
id: project-chaos.team-process.program-docs.config-pipeline-log.path-mismatch
parent: project-chaos.team-process.program-docs.config-pipeline-log
tags: [excel, paths]
name: {zh: "路径错位实证", en: "Path-Mismatch Proof"}
description:
  zh: >
      配置表管线记录下篇：广为人知的配置读取路径错位——方法、原始输出、结论、失败链条与临时绕行——以及 ConfigLoader/Recorder 职责重叠。
      
  en: >
      Config-pipeline record part 3: the celebrated config-read path mismatch — method, raw output, conclusion, failure chain and temporary workaround — plus the ConfigLoader/Recorder overlap.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.802Z"
fingerprint: a0e526ba94b8397d1c104025e5bf8a5693676d6b8749b6b7e58b4037cf6dfc1a
source:
  - path: "Assets/Chaos_Story/Story/05_程序/02_配置表管线记录.md"
    line: 438
    end_line: 637
apis:
  - protocol: file
    path: "Assets/Chaos_Story/Story/05_程序/02_配置表管线记录.md#L438-L637"
    description:
      zh: >
          GlobalPath 与磁盘实际目录、路径错位的实测证据与失败链条、与 Recorder 的重复实现比较。
          
      en: >
          GlobalPath vs the actual directories, the measured path-mismatch proof and its failure chain, and the comparison with Recorder.
          
deps:
  - kind: reference
    to: project-chaos.team-process.arch-docs.path-naming.baselines
    label: {zh: "路径规范的实证", en: "Path rules applied"}
---
