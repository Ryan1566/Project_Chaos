---
uid: 9e0d0102
id: project-chaos.config-pipeline.table-repo.chunk-socket
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "ChunkSocket 房间块接口表", en: "ChunkSocket Workbook"}
description:
  zh: >
      ChunkSocket_房间块接口表.xlsx → ChunkSocketConfig：Id / chunkId（外键 → MapChunk.Id）/ dir（1 左 2 右 3 上 4 下）/ heightProfile（横向高度档 0/1/2；竖直开口恒为 0，按同列匹配）/ socketType（1 普通通道 2 跨层通道 3 boss 门）/ posX、posY（块内局部坐标，单位米）。
      
  en: >
      ChunkSocket_房间块接口表.xlsx -> ChunkSocketConfig: Id / chunkId (FK to MapChunk.Id) / dir (1 left, 2 right, 3 up, 4 down) / heightProfile (horizontal height tier 0/1/2; vertical openings are always 0 and match by column) / socketType (1 normal passage, 2 cross-layer passage, 3 boss gate) / posX, posY (chunk-local metres).
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.697Z"
fingerprint: 1dcc9908ea65381e6fe4a1a82dc1d467e8eb27553c567fa5dac349a0c3c955c8
source:
  - path: "Assets/Excel/Chaos_excel/ChunkSocket_房间块接口表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/ChunkSocket_房间块接口表.xlsx"
    description:
      zh: >
          房间块接口表工作簿：每个房间块的可连接开口。
          
      en: >
          ChunkSocket workbook: connectable openings of each room block.
          
---
