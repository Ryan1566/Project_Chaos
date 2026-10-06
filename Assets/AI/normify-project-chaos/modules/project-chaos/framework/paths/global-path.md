---
uid: f1c00011
id: project-chaos.framework.paths.global-path
parent: project-chaos.framework.paths
name: {zh: "全局路径常量", en: "GlobalPath Constants"}
description:
  zh: >
      GlobalPath 全局常量：Resources 子路径、Excel/Json/.record 落盘路径、UI 预制体搜索路径、本地化配置路径与存档槽位常量。工程内路径定位的单一事实源。
      
  en: >
      GlobalPath constants: Resources sub-paths, Excel/Json/record output paths, UI prefab search paths, save-slot constants. Single source of truth for paths.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.751Z"
fingerprint: dcb958855036cde1a5d93841c2c7392f506f34d7eec8fcc589f0fe6a529aeba8
source:
  - path: "Assets/Scripts/Runtime/Path&Type/GlobalPath.cs"
    line: 1
    end_line: 179
apis:
  - protocol: rpc
    path: "GlobalPath.ui_PanelPrefabSearchPaths"
    description:
      zh: >
          面板预制体搜索路径数组。
          
      en: >
          Panel prefab search path list.
          
  - protocol: rpc
    path: "GlobalPath.res_MusicPath"
    description:
      zh: >
          Resources 下的音乐目录。
          
      en: >
          Music clips directory under Resources.
          
  - protocol: rpc
    path: "GlobalPath.res_SoundPath"
    description:
      zh: >
          Resources 下的音效目录。
          
      en: >
          SFX clips directory under Resources.
          
  - protocol: rpc
    path: "GlobalPath.res_TestPath"
    description:
      zh: >
          测试预制体目录 Test/。
          
      en: >
          Test prefab directory under Resources.
          
  - protocol: rpc
    path: "GlobalPath.res_InputActionsPath"
    description:
      zh: >
          Resources 下的 InputActionAsset 路径。
          
      en: >
          InputActionAsset path under Resources.
          
  - protocol: rpc
    path: "GlobalPath.res_KeyIconMapPath"
    description:
      zh: >
          Resources 下的 KeyIconMap 资源路径。
          
      en: >
          KeyIconMap asset path under Resources.
          
  - protocol: rpc
    path: "GlobalPath.data_JsonPath"
    description:
      zh: >
          配置 Json 落盘目录（Resources/Data/Json/Runtime/）。
          
      en: >
          Config JSON output directory.
          
  - protocol: rpc
    path: "GlobalPath.data_JsonPathToRead"
    description:
      zh: >
          运行时读取前缀 Json/Runtime/（与落盘路径错位，已知 P0）。
          
      en: >
          Runtime read prefix; mismatched with actual disk path (known defect).
          
  - protocol: rpc
    path: "GlobalPath.data_RecordPath"
    description:
      zh: >
          存档 .record 落盘目录。
          
      en: >
          Record (.record) output directory.
          
  - protocol: rpc
    path: "GlobalPath.data_ExcelPath"
    description:
      zh: >
          Excel 源表目录。
          
      en: >
          Excel source-table directory.
          
  - protocol: rpc
    path: "GlobalPath.save_SlotDir"
    description:
      zh: >
          存档槽位目录名。
          
      en: >
          Save slots directory name.
          
  - protocol: rpc
    path: "GlobalPath.save_SlotCount"
    description:
      zh: >
          存档槽位数量，固定 3。
          
      en: >
          Fixed save slot count (3).
          
---
