---
uid: f1c00011
id: project-chaos.framework.paths.global-path
parent: project-chaos.framework.paths
name: {zh: "全局路径常量", en: "GlobalPath Constants"}
description:
  zh: >
      全局路径常量。2026-10-12 实测：data_JsonPathToRead = "Data/Json/Runtime/" 与产物目录一致，编辑器内 Resources.Load + JsonUtility 往返均成功，早先登记的“路径错位”已不可复现（见待修清单）。
      
  en: >
      Global path constants. Confirmed on 2026-10-12 that data_JsonPathToRead = "Data/Json/Runtime/" matches the generated JSON location: Resources.Load and JsonUtility round-trip were verified in the editor, so the earlier “path mismatch” defect no longer reproduces (see the defect list).
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:57:09.598Z"
fingerprint: 75aea6f4913b39abaf2861b284bc582fdad1e18fa49931aa7ce68d15d18b2d68
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
