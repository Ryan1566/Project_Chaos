---
uid: 9e0d0101
id: project-chaos.config-pipeline.table-repo.chunk-slot
parent: project-chaos.config-pipeline.table-repo
tags: [excel, runmap]
name: {zh: "ChunkSlot 房间块标记点表", en: "ChunkSlot Workbook"}
description:
  zh: >
      ChunkSlot_房间块标记点表.xlsx → ChunkSlotConfig：Id / chunkId（外键 → MapChunk.Id）/ slotType（1 敌人 2 精英 3 宝箱 5 场景事件 6 扎营点）/ posX、posY（块内局部坐标，单位米）。
      
  en: >
      ChunkSlot_房间块标记点表.xlsx -> ChunkSlotConfig: Id / chunkId (FK to MapChunk.Id) / slotType (1 enemy, 2 elite, 3 chest, 5 scene event, 6 camp site) / posX, posY (chunk-local metres).
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.406Z"
fingerprint: 55b1df98c40017c3dde9bad8528b872f104de63329448c904189b4a3195fabeb
source:
  - path: "Assets/Excel/Chaos_excel/ChunkSlot_房间块标记点表.xlsx"
apis:
  - protocol: file
    path: "Assets/Excel/Chaos_excel/ChunkSlot_房间块标记点表.xlsx"
    description:
      zh: >
          房间块标记点表工作簿：块内标记点（刷怪/宝箱/事件/扎营）。
          
      en: >
          ChunkSlot workbook: marker points (spawn/chest/event/camp) inside room blocks.
          
---
