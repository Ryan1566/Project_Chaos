---
uid: 7c0d0081
id: project-chaos.team-process.program-docs.coding-conventions.encodings
parent: project-chaos.team-process.program-docs.coding-conventions
tags: [code, encoding]
name: {zh: "混合编码规则", en: "Mixed Encodings"}
description:
  zh: >
      最高优先级规则：.cs 混合编码真相与避免打坏中文注释的读写流程。
      
  en: >
      Highest-priority rule: the mixed-encoding reality of the .cs set and the read/write procedure that avoids corrupting Chinese comments.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.801Z"
fingerprint: e739ed90582b30e56e975cc7b0dbe1c74d95e72904b67d3a5f5c658593d6d698
source:
  - path: "Assets/Chaos_Story/Story/05_程序/03_编码与协作约定.md"
    line: 1
    end_line: 188
apis:
  - protocol: file
    path: "Assets/Chaos_Story/Story/05_程序/03_编码与协作约定.md#L1-L188"
    description:
      zh: >
          字节级编码事实（GBK / UTF-8 BOM / 无 BOM）与强制读写规则及可直接复制的命令。
          
      en: >
          The byte-level encoding facts (GBK vs UTF-8 BOM vs no BOM) and the mandatory read/write rules with copy-paste commands.
          
deps:
  - kind: reference
    to: project-chaos.team-process.contract.pitfalls
    label: {zh: "契约第 1 条", en: "Contract item 1"}
---
