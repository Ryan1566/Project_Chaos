---
uid: b21cc7e0
id: project-chaos.framework.runmap.assembler
parent: project-chaos.framework.runmap
tags: [runmap, assembly]
name: {zh: "地图生成：装配与封堆", en: "RunMap: Assembler (Pass E)"}
description:
  zh: >
      Pass E 的运行时实现：从 Resources 实例化房间块、封堆未使用的 L/R/U/D 开口（幂等的边界带填充）、并施加确定性地形修饰（高台/坑/柱子/台阶）。已取代原先只能在编辑器里跑的装配器。
      
  en: >
      Pass E runtime implementation: instantiates room chunks from Resources, seals every unused L/R/U/D opening (idempotent band-fill) and applies deterministic terrain decoration (terrace / pit / pillars / steps). Replaced the editor-only assembler.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.716Z"
fingerprint: 1429a49c64aedfcc4b0cb41651132ff8c341bb19ede7cd97aaf638d4a6a8f0bf
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapAssembler.cs"
    line: 1
    end_line: 256
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapAssembler.cs#L1-L256"
    description:
      zh: >
          运行时把 MapLayout 拼成场景实体：走 ResManager 从 Resources 取块、按格子摆放、封堆未用开口、加地形起伏。
          
      en: >
          Assemble a MapLayout into the active scene at runtime: load prefabs from Resources via ResManager, position by grid cell, seal unused openings, add terrain decoration.
          
  - protocol: rpc
    path: "RunMapAssembler.Assemble"
    description:
      zh: >
          Assemble(layout, catalog, parent)：返回拼好的根物体（一个房间都没装上则返回 null）。
          
      en: >
          Assemble(layout, catalog, parent): returns the assembled root object (or null when nothing could be placed).
          
---
