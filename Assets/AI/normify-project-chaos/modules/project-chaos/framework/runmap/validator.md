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
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.430Z"
fingerprint: 52297c58032b3cb9cf3a62b84bbd62de1cf23565f6aaaad8c0d5478ac68a5de5
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
