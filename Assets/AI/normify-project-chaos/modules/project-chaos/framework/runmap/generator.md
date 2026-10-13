---
uid: 871324de
id: project-chaos.framework.runmap.generator
parent: project-chaos.framework.runmap
tags: [runmap, orchestrator]
name: {zh: "地图生成：编排器", en: "RunMap: Generator"}
description:
  zh: >
      编排器：换种子重试、逐次校验，重试用尽则退到保底链式布局；同时负责一局三场景的生成。
      
  en: >
      Orchestrator: retries with derived seeds, validates each attempt, and falls back to a guaranteed-playable chained layout when retries run out; also drives a full three-scene run.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.429Z"
fingerprint: 04189848655d30f0e2e13c49cc3f1fdc9480bade71357e1030a185c9b330ed86
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapGenerator.cs"
    line: 1
    end_line: 184
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapGenerator.cs#L1-L184"
    description:
      zh: >
          生成单场景地图（重试→降级语义）、生成一局三场景、以及报告缺失的空间层。
          
      en: >
          Generate one scene map with retry-then-fallback semantics, generate a three-scene run, and report which layers are missing.
          
---
