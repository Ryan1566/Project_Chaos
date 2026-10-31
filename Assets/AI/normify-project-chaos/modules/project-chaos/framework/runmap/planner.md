---
uid: 7e798e62
id: project-chaos.framework.runmap.planner
parent: project-chaos.framework.runmap
tags: [runmap, planner]
name: {zh: "地图生成：规划器", en: "RunMap: Planner"}
description:
  zh: >
      地图规划器：跨层主路、挂在竖直开口上的支路、带冲突检查的网格布局、按所需接口的加权选块，以及标记点世界坐标的产出。
      
  en: >
      Planner for the map: main path across layers, branches hanging off vertical openings, grid placement with conflict checks, weighted chunk picking by required sockets, and slot world-coordinate emission.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:57:20.394Z"
fingerprint: 0b1061f725ad3985cc873c4609578911a3fdd5fe943a4f80555d7b66b9aeeb49
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapPlanner.cs"
    line: 1
    end_line: 408
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapPlanner.cs#L1-L408"
    description:
      zh: >
          Pass A~D：生成抽象房间图、嵌入网格（跨层连接件与前后节点同列）、按接口选块；失败原因记在 LastFailure 上。
          
      en: >
          Pass A-D: build the abstract room graph, place it on the grid (layer links share their column) and pick chunks by socket; exposes LastFailure.
          
---
