---
uid: f1c00012
id: project-chaos.framework.paths.panel-type
parent: project-chaos.framework.paths
name: {zh: "面板类型枚举", en: "PanelType Enum"}
description:
  zh: >
      PanelType 面板类型枚举（主菜单/保存/设置/暂停/状态/对话/任务/读条/存档/结局/GM 等 14 项），UIManager 据此动态拼路径并实例化面板；注意 GMPanel 实际是场景对象，不走 UIManager 面板栈。
      
  en: >
      PanelType enum (MainMenuPanel, SettingPanel, SLPanel, ProgressPanel, GMPanel...). Drives dynamic panel path building; note GMPanel is a scene object and is not pushed through the UIManager stack.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.715Z"
fingerprint: 88248870190a2a1d1d9a45a479a4705e60c2c0616cda96a9d6b6a1454533dbc1
source:
  - path: "Assets/Scripts/Runtime/Path&Type/PanelType.cs"
    line: 1
    end_line: 22
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/Path&Type/PanelType.cs"
    description:
      zh: >
          14 个面板类型枚举值（含 GMPanel），用于辅助拼面板预制体路径。
          
      en: >
          14 panel-type enum values (incl. GMPanel) used to build panel prefab paths.
          
---
