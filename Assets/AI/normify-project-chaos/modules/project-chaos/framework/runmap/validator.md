---
uid: 4977af4d
id: project-chaos.framework.runmap.validator
parent: project-chaos.framework.runmap
tags: [runmap, validator]
name: {zh: "地图生成：校验器", en: "RunMap: Validator"}
description:
  zh: >
      连通性与接口匹配校验：全图 BFS、入口→boss 主路长度、每层内部连通、层间连接数与同列对齐、boss 唯一终点、去掉 boss 后仍连通。
      
  en: >
      Connectivity and socket-matching validation: full-graph BFS, entry-to-boss path length, per-layer connectivity, layer-link counts and column alignment, boss as the only end, and connectivity after removing the boss door.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.718Z"
fingerprint: e4f7af986127896c2463360006e1d1976ca5fa592462ea42668b6f7f81f1e9e8
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapValidator.cs"
    line: 1
    end_line: 248
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapValidator.cs#L1-L248"
    description:
      zh: >
          生成后校验 C10.1~C10.10 与块使用约束；返回失败清单而不是抛异常。
          
      en: >
          Post-generation checks C10.1-C10.10 plus chunk usage constraints; returns a list of failures instead of throwing.
          
---
