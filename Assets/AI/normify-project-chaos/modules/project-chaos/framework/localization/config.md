---
uid: f1c0002d
id: project-chaos.framework.localization.config
parent: project-chaos.framework.localization
name: {zh: "本地化配置资产", en: "Localization Config"}
description:
  zh: >
      LocalizationData 配置资产：按键查条目、存在性判定、增删条目与键列表枚举，对应 Data/Localization 下的本地化配置。
      
  en: >
      LocalizationData ScriptableObject: key lookup, contains, add/remove entry and key enumeration for the localization table asset.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.747Z"
fingerprint: a43836ec9d7eccc09086bde991db83bfcaa9da7c3997ca5e93cad03b191ad019
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationData.cs"
    line: 156
    end_line: 217
apis:
  - protocol: rpc
    path: "LocalizationData.GetEntry"
    description:
      zh: >
          按键取条目。
          
      en: >
          Gets an entry by key.
          
  - protocol: rpc
    path: "LocalizationData.ContainsKey"
    description:
      zh: >
          判断键是否存在。
          
      en: >
          Checks whether a key exists.
          
  - protocol: rpc
    path: "LocalizationData.AddEntry"
    description:
      zh: >
          新增/替换条目。
          
      en: >
          Adds or replaces an entry.
          
  - protocol: rpc
    path: "LocalizationData.RemoveEntry"
    description:
      zh: >
          按键删除条目。
          
      en: >
          Removes an entry by key.
          
  - protocol: rpc
    path: "LocalizationData.GetAllKeys"
    description:
      zh: >
          返回全部键。
          
      en: >
          Returns all keys.
          
deps:
  - kind: reference
    to: project-chaos.framework.localization.entry
    from_api: "rpc:LocalizationData.GetEntry"
    to_api: "rpc:LocalizationEntry.GetText"
    label: {zh: "条目文本", en: "Entry text"}
---
