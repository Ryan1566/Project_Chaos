---
uid: f1c00058
id: project-chaos.framework.save.service-read
parent: project-chaos.framework.save
name: {zh: "存档读取", en: "Save Slot Reading"}
description:
  zh: >
      SaveSlotService 读取侧：目录解析与创建、槽位校验、路径拼装、ReadAllSlots 恒返 3 项、TryLoad 读槽并对损坏文件降级。
      
  en: >
      SaveSlotService read side: directory resolution, slot validation, path building, ReadAllSlots (always 3) and TryLoad with corrupt-file fallback.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.752Z"
fingerprint: 8e71d66d7c2df229548a801e2b34a665614a6e0faa67777f36a02d0b91c1fece
source:
  - path: "Assets/Scripts/Runtime/GameBase/SaveManager/SaveSlotService.cs"
    line: 1
    end_line: 218
apis:
  - protocol: rpc
    path: "SaveSlotService.SavesDirectory"
    description:
      zh: >
          persistentDataPath 下的 Saves 目录。
          
      en: >
          Saves directory under persistentDataPath.
          
  - protocol: rpc
    path: "SaveSlotService.IsValidSlot"
    description:
      zh: >
          校验槽位下标是否合法（1-3）。
          
      en: >
          Validates a slot index (1-3).
          
  - protocol: rpc
    path: "SaveSlotService.GetSlotPath"
    description:
      zh: >
          拼出某槽位的文件路径。
          
      en: >
          Builds the file path of a slot.
          
  - protocol: rpc
    path: "SaveSlotService.ReadAllSlots"
    description:
      zh: >
          读取全部三个槽（恒返回 3 项）。
          
      en: >
          Reads all three slots (always returns 3 items).
          
  - protocol: rpc
    path: "SaveSlotService.TryLoad"
    description:
      zh: >
          尝试读取槽文件（损坏时安全降级）。
          
      en: >
          Tries to load a slot file (corrupt falls back safely).
          
deps:
  - kind: reference
    to: project-chaos.framework.save.slot-data
    from_api: "rpc:SaveSlotService.ReadAllSlots"
    to_api: "rpc:SaveSlotInfo.HasUsableSave"
    label: {zh: "槽位视图", en: "Slot view"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:SaveSlotService.GetSlotPath"
    to_api: "rpc:GlobalPath.save_SlotDir"
    label: {zh: "存档目录", en: "Save dir"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SaveSlotService.TryLoad"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "存档日志", en: "Save logging"}
---
