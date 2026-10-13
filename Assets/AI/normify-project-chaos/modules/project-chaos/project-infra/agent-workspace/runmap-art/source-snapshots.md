---
uid: 9e0d0006
id: project-chaos.project-infra.agent-workspace.runmap-art.source-snapshots
parent: project-chaos.project-infra.agent-workspace.runmap-art
tags: [runmap, snapshot, risk]
name: {zh: "源码快照副本", en: "Source Snapshots"}
description:
  zh: >
      RunMap 实现的 `.cs.txt` 快照副本（src/ 下 13 个 + RunMapPlaceholderPipeline.cs.txt），仅供对照。漂移风险：副本会与真源（Scripts/Runtime/GameBase/RunMap/、Scripts/Editor/RunMapTool/）静默不一致。
      
  en: >
      The .cs.txt snapshot copies of the RunMap implementation (thirteen under src/ plus RunMapPlaceholderPipeline.cs.txt), kept for reference only. Drift risk: these copies silently diverge from the real sources under Scripts/Runtime/GameBase/RunMap/ and Scripts/Editor/RunMapTool/.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.445Z"
fingerprint: d208af1697d72393a798b2ba4bb9980f6cf0c1b246dcb3f01f27575028feff65
source:
  - path: "Assets/AI/runmap-art/src/RunMapGenerator.cs.txt"
apis:
  - protocol: file
    path: "Assets/AI/runmap-art/src/RunMapGenerator.cs.txt"
    description:
      zh: >
          RunMap 生成器实现的快照副本（非编译源）。
          
      en: >
          Snapshot copy of the RunMap generator implementation (not a compile source).
          
  - protocol: file
    path: "Assets/AI/runmap-art/RunMapPlaceholderPipeline.cs.txt"
    description:
      zh: >
          占位管线工具的快照副本（非编译源）。
          
      en: >
          Snapshot copy of the placeholder pipeline tool (not a compile source).
          
---
