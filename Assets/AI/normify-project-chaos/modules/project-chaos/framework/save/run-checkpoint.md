---
uid: b2c00211
id: project-chaos.framework.save.run-checkpoint
parent: project-chaos.framework.save
tags: [framework, save, run]
name: {zh: "局内检查点存档", en: "Run Checkpoint Save"}
description:
  zh: >
      局内存档检查点：把当前局的门客、计时道具、委托、内容领取与地面掉落快照成 ActiveRunSave，供回到标题后继续或放弃。
      
  en: >
      In-run checkpoint save: snapshots retainers, timed items, quests, content claims and ground drops into ActiveRunSave so the run can be resumed or abandoned from the title screen.
      
revision: 08f9a356150c4617b28273d13535c6f14ae4d299
updated_at: "2026-11-10T14:57:10.677Z"
fingerprint: cb5c38f9034def97bac40b6af6044e3e1a298799d5bc0b614bb4f8229004b471
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/RunCheckpoint.cs"
  - path: "Assets/Scripts/Runtime/GameBase/SaveManager/ActiveRunSave.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/Meta/RunCheckpoint.cs"
    description:
      zh: >
          检查点保存与放弃入口
          
      en: >
          Checkpoint save and abandon entry points
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/SaveManager/ActiveRunSave.cs"
    description:
      zh: >
          局内存档数据模型
          
      en: >
          Run save data model
          
  - protocol: rpc
    path: "rpc:RunCheckpoint.SaveAndReturnToTitle"
    description:
      zh: >
          存档并回到标题
          
      en: >
          Saves and returns to the title
          
  - protocol: rpc
    path: "rpc:RunCheckpoint.Abandon"
    description:
      zh: >
          放弃本局存档
          
      en: >
          Abandons the checkpoint
          
deps:
  - kind: reference
    to: project-chaos.framework.save.session
    label: {zh: "写入会话存档", en: "Writes session save"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.encounter
    label: {zh: "快照局内遭遇状态", en: "Snapshots encounter state"}
---
