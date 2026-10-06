---
uid: f1c00059
id: project-chaos.framework.save.service-write
parent: project-chaos.framework.save
name: {zh: "存档写入", en: "Save Slot Writing"}
description:
  zh: >
      SaveSlotService 写入侧：Save 写槽并刷新时间、CreateNew 新建、Delete 删除、找第一个非空槽、磁盘存在性判定，并对外来文件名告警忽略。
      
  en: >
      SaveSlotService write side: Save/CreateNew/Delete, first non-empty slot lookup and disk existence check, plus foreign-file warnings on disk.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.752Z"
fingerprint: 8e71d66d7c2df229548a801e2b34a665614a6e0faa67777f36a02d0b91c1fece
source:
  - path: "Assets/Scripts/Runtime/GameBase/SaveManager/SaveSlotService.cs"
    line: 219
    end_line: 384
apis:
  - protocol: rpc
    path: "SaveSlotService.Save"
    description:
      zh: >
          把负载写入槽位并刷新最后保存时间。
          
      en: >
          Writes a payload into a slot (updates meta ticks).
          
  - protocol: rpc
    path: "SaveSlotService.CreateNew"
    description:
      zh: >
          在槽位创建一份新存档。
          
      en: >
          Creates a new empty save in a slot.
          
  - protocol: rpc
    path: "SaveSlotService.Delete"
    description:
      zh: >
          删除槽位文件。
          
      en: >
          Deletes a slot file.
          
  - protocol: rpc
    path: "SaveSlotService.FindFirstNonEmptySlot"
    description:
      zh: >
          查找第一个非空槽（全空返回 1）。
          
      en: >
          Finds the first non-empty slot (else 1).
          
  - protocol: rpc
    path: "SaveSlotService.HasSaveFile"
    description:
      zh: >
          槽文件是否存在于磁盘。
          
      en: >
          Whether a slot file exists on disk.
          
deps:
  - kind: reference
    to: project-chaos.framework.save.slot-data
    from_api: "rpc:SaveSlotService.Save"
    to_api: "rpc:SavePayload.version"
    label: {zh: "负载模型", en: "Payload model"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:SaveSlotService.Save"
    to_api: "rpc:GlobalPath.save_SlotDir"
    label: {zh: "存档目录", en: "Save dir"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SaveSlotService.Save"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "存档日志", en: "Save logging"}
---
