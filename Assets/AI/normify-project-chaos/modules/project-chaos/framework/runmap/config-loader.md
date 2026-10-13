---
uid: ffca1275
id: project-chaos.framework.runmap.config-loader
parent: project-chaos.framework.runmap
tags: [runmap, config]
name: {zh: "地图生成：表驱动装载", en: "RunMap: Config Loader"}
description:
  zh: >
      地图名录的配置表装载层：把 MapBiome/MapChunk/ChunkSocket/ChunkSlot/MapPreset/PresetLayer 六张表拼成规划器直接吃的 RunMapCatalog 模型——从写死的灰盒名录换成表驱动，算法一行未改。
      
  en: >
      Config-table loader for the run-map catalog: joins MapBiome + MapChunk + ChunkSocket + ChunkSlot + MapPreset + PresetLayer into the same RunMapCatalog model the planner consumes, so switching from the hard-coded greybox catalog to tables changes no algorithm.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.429Z"
fingerprint: 2dd4f42730ab6f644753c4332a62a31ff31926f60539867d2f9b835906e16907
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapConfigLoader.cs"
    line: 1
    end_line: 173
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapConfigLoader.cs#L1-L173"
    description:
      zh: >
          从 MapBiome/MapChunk/ChunkSocket/ChunkSlot/MapPreset/PresetLayer 六张配置表装载房间块与预设名录（走 ConfigLoader）。
          
      en: >
          Load the chunk/preset catalog from the MapBiome/MapChunk/ChunkSocket/ChunkSlot/MapPreset/PresetLayer config tables via ConfigLoader.
          
  - protocol: rpc
    path: "RunMapConfigLoader.LoadFromConfigs"
    description:
      zh: >
          LoadFromConfigs()：返回表驱动名录；任一张表读不到就返回 null（由调用方降级到灰盒名录）。
          
      en: >
          LoadFromConfigs(): returns a RunMapCatalog built from the tables, or null when a table is missing.
          
---
