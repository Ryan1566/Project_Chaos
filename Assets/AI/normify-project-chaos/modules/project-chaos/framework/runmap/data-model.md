---
uid: fe1e6752
id: project-chaos.framework.runmap.data-model
parent: project-chaos.framework.runmap
tags: [runmap, data]
name: {zh: "地图生成：数据与种子", en: "RunMap: Data Model"}
description:
  zh: >
      枚举与房间块/预设定义、MapLayout 房间图、自持状态的 xorshift 随机数与 FNV-1a 种子派生（SceneSeed = FNV1a(RunSeed, sceneIndex)）；全程不碰 UnityEngine.Random。
      
  en: >
      Enums, chunk/preset definitions, MapLayout room graph and the xorshift RNG plus FNV-1a seed derivation (SceneSeed = FNV1a(RunSeed, sceneIndex)).
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.942Z"
fingerprint: e0d4216e018317832154b447c23eb6c5793ba9fc735d9590332c448e9e65aadd
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapData.cs"
    line: 1
    end_line: 320
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapData.cs#L1-L320"
    description:
      zh: >
          核心类型：空间层/块类型/接口方向、房间块与预设定义、房间图与确定比对的稳定签名。
          
      en: >
          Core types: layers, chunk kinds, sockets, chunk/preset definitions, room graph and the stable signature used for determinism checks.
          
---
