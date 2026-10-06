---
uid: c1a20702
id: project-chaos.config-pipeline.generated-models.loading-table-config
parent: project-chaos.config-pipeline.generated-models
name: {zh: "LoadingTableConfig 类", en: "LoadingTableConfig Class"}
description:
  zh: >
      轮播读条表的 Config 模式生成类：Id(uint)、srcImg(string)、sort(uint)、showSeconds(float)、fadeSeconds(float)、enable(bool)。注意第 5 行端标记留空时，该列会从 Json 中丢弃但 C# 字段保留（永远是默认值）。
      
  en: >
      Generated Config-mode class for the loading carousel: Id (uint), srcImg (string), sort (uint), showSeconds (float), fadeSeconds (float) and enable (bool). Note that a blank row-5 end marker drops the column from JSON but keeps the C# field at its default value.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.729Z"
fingerprint: 5747f73cc90997ad936ab42b455fa53872164e62b116e023e00c9bb10baaaf28
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/LoadingTableConfig.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/LoadingTableConfig.cs"
    description:
      zh: >
          LoadingTable 的常量字段容器（生成）。
          
      en: >
          Generated LoadingTable field container for constants.
          
  - protocol: rpc
    path: "LoadingTableConfig"
    description:
      zh: >
          可序列化的 LoadingTable 配置行类。
          
      en: >
          Serializable LoadingTable config row class.
          
---
