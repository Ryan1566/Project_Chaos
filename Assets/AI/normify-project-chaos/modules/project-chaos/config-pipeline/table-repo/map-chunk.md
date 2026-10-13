---
uid: 9e0d0104
id: project-chaos.config-pipeline.table-repo.map-chunk
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "MapChunk 房间块表", en: "MapChunk Workbook"}
description:
  zh: >
      MapChunk_房间块表.xlsx → MapChunkConfig：Id / biomeId / themeKey / kind（1 入口 2 通路 3 战斗 9 层间连接 10 boss 门 11 死路奖励）/ layer（0=跨层）/ prefabPath（Resources 相对路径，不含扩展名）/ weight / maxCount / isUnique / enable。这是生成器抽块的依据，prefabPath 必须与 Chunk_* 预制体命名对得上。
      
  en: >
      MapChunk_房间块表.xlsx -> MapChunkConfig: Id / biomeId / themeKey / kind (1 entry, 2 corridor, 3 combat, 9 layer link, 10 boss gate, 11 dead-end reward) / layer (0 = cross-layer) / prefabPath (Resources-relative, no extension) / weight / maxCount / isUnique / enable. This is the table the generator draws from, and prefabPath must match the Chunk_* prefab naming.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.407Z"
fingerprint: dff792707961feba6f67fbe5938a6fc03b0ebb95ea241b563a2ca437d0682048
source:
  - path: "Assets/Excel/Chaos_excel/MapChunk_房间块表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/MapChunk_房间块表.xlsx"
    description:
      zh: >
          房间块表工作簿：房间块池，含预制体路径与抽取权重。
          
      en: >
          MapChunk workbook: the room-block pool with prefab paths and draw weights.
          
---
